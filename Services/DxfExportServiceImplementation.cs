using System.Diagnostics;
using System.Globalization;
using System.Text;
using Inventor;
using InventorDxfExporter.Core;
using IOEnvironment = System.Environment;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;

namespace InventorDxfExporter.Services;

/// <summary>G1 V14-compatible DXF export pipeline, isolated from UI; COM operations stay on Inventor's STA.</summary>
public sealed class DxfExportServiceImplementation : IDxfExportService
{
    private const string DxfTranslatorId = "{C24E3AC4-122E-11D5-8E91-0010B541CD80}";
    private readonly Inventor.Application? _application;
    private readonly IPartInfoService? _partInfo;
    private readonly IBomService? _bom;
    private readonly DxfProcessor _processor = new();

    public DxfExportServiceImplementation() { }
    public DxfExportServiceImplementation(Inventor.Application application, IPartInfoService partInfo, IBomService bom)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _partInfo = partInfo ?? throw new ArgumentNullException(nameof(partInfo));
        _bom = bom ?? throw new ArgumentNullException(nameof(bom));
    }

    public OperationResult Export(Document document, ExportSettings settings) => Export(document, settings, NullOperationMonitor.Instance, null);

    public IReadOnlyList<ExportItem> CollectItems(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var items = new Dictionary<string, WorkItem>(StringComparer.OrdinalIgnoreCase);
        var skipped = new List<string>(); var errors = new List<string>(); var suppressed = new List<ExportItem>();
        CollectDocumentItems(document, items, skipped, errors, suppressed);
        return items.Values.Select(x => x.Item).ToArray();
    }

    public OperationResult Export(Document document, ExportSettings settings, IOperationMonitor? monitor,
        IReadOnlyCollection<string>? selectedDocumentPaths)
    {
        ArgumentNullException.ThrowIfNull(document); ArgumentNullException.ThrowIfNull(settings);
        monitor ??= NullOperationMonitor.Instance;
        var items = new Dictionary<string, WorkItem>(StringComparer.OrdinalIgnoreCase);
        var skipped = new List<string>(); var errors = new List<string>(); var suppressed = new List<ExportItem>();
        var log = new StringBuilder(); var outputLines = new StringBuilder();
        var totalTimer = Stopwatch.StartNew();
        var dxfTimer = new Stopwatch(); var postTimer = new Stopwatch(); var infoTimer = new Stopwatch(); var bomTimer = new Stopwatch();
        bool oldScreen = true, oldSilent = false, fastApplied = false;
        int exported = 0, skippedExisting = 0, bendCount = 0, deletedSuppressed = 0;
        string reportPath = string.Empty;

        try
        {
            if (settings.FastMode && _application is not null)
            {
                oldScreen = _application.ScreenUpdating; oldSilent = _application.SilentOperation;
                _application.ScreenUpdating = false; _application.SilentOperation = true; fastApplied = true;
            }

            CollectDocumentItems(document, items, skipped, errors, suppressed);
            if (selectedDocumentPaths is not null)
            {
                var selected = new HashSet<string>(selectedDocumentPaths, StringComparer.OrdinalIgnoreCase);
                foreach (var key in items.Keys.Where(k => !selected.Contains(k)).ToArray()) items.Remove(key);
            }
            if (items.Count == 0)
                return new OperationResult(0, skipped.Count, errors.Count, string.Empty,
                    skipped.Concat(errors).Append("Không tìm thấy part Sheet Metal hợp lệ trong phạm vi đã chọn.").ToArray());
            if (string.IsNullOrWhiteSpace(settings.OutputRoot)) throw new InvalidOperationException("Chưa chọn thư mục xuất.");

            int index = 0;
            foreach (var work in items.Values)
            {
                monitor.CancellationToken.ThrowIfCancellationRequested();
                var item = work.Item; index++;
                monitor.Report(new OperationProgress(index - 1, items.Count, item.DetailName, "Đang xuất DXF"));
                try
                {
                    string folder = IOPath.Combine(settings.OutputRoot, FileNameBuilder.CleanName(item.Material), FileNameBuilder.CleanName(item.Thickness));
                    Directory.CreateDirectory(folder);
                    string path = IOPath.Combine(folder, FileNameBuilder.BuildFileName(settings.FilePattern, item.Thickness, item.Quantity, item.Material, item.DetailName));
                    if (IOFile.Exists(path))
                    {
                        if (settings.OverwriteMode == "Bỏ qua file đã có") { skippedExisting++; outputLines.AppendLine("BỎ QUA FILE ĐÃ CÓ: " + path); continue; }
                        if (settings.OverwriteMode == "Hỏi từng file")
                        {
                            var answer = System.Windows.Forms.MessageBox.Show("File đã tồn tại:" + IOEnvironment.NewLine + path + IOEnvironment.NewLine + "Ghi đè?", "Xác nhận", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
                            if (answer != System.Windows.Forms.DialogResult.Yes) { skippedExisting++; continue; }
                        }
                    }
                    var temporaryDxfPath = path + ".tmp_" + Guid.NewGuid().ToString("N") + ".dxf";
                    try
                    {
                        dxfTimer.Start();
                        bool usedFallbackOptions;
                        try { usedFallbackOptions = ExportOnePart(work.Document, temporaryDxfPath, settings, BuildDxfOptions(settings)); }
                        finally { dxfTimer.Stop(); }
                        postTimer.Start();
                        try
                        {
                            var options = new DxfProcessOptions(BuildDxfOptions(settings), settings.BendMode, settings.BendUpLen, settings.BendUpInset,
                                settings.BendDownLen, settings.BendDownInset, settings.AddInfoText, settings.TextAlignment,
                                settings.TextHeight, settings.TextLineGap, settings.TextYOffset);
                            bendCount += _processor.Process(temporaryDxfPath, path, options, item).BendLinesProcessed;
                        }
                        finally { postTimer.Stop(); }
                        if (settings.UpdateInfoAndBom && _partInfo is not null)
                        {
                            infoTimer.Start();
                            try { _partInfo.UpdateFromFlatPattern(work.Document, true); }
                            catch (Exception ex) { errors.Add(item.DetailName + " (cập nhật Description/Thickness): " + ex.Message); }
                            finally { infoTimer.Stop(); }
                        }
                        exported++;
                        outputLines.AppendLine((usedFallbackOptions ? "OK (tùy chọn DXF tối giản): " : "OK: ") + path);
                    }
                    finally
                    {
                        if (IOFile.Exists(temporaryDxfPath)) IOFile.Delete(temporaryDxfPath);
                    }
                }
                catch (Exception ex) { errors.Add(item.DetailName + ": " + ex.Message); }
                monitor.Report(new OperationProgress(index, items.Count, item.DetailName, "Đang xuất DXF"));
            }

            monitor.CancellationToken.ThrowIfCancellationRequested();
            string bomPath = string.Empty;
            if (settings.UpdateInfoAndBom && _bom is not null)
            {
                bomTimer.Start();
                try
                {
                    var bomSettings = new InventorDxfExporter.Settings.BomExportSettingsStore().Load();
                    bomPath = _bom.Export(items.Values.Select(x => x.Item).ToArray(), bomSettings);
                    if (_bom is BomServiceImplementation bomImpl) errors.AddRange(bomImpl.Errors);
                }
                catch (Exception ex) { errors.Add("Xuất BOM: " + ex.Message); }
                finally { bomTimer.Stop(); }
            }

            foreach (var old in suppressed)
            {
                try
                {
                    if (old.DocumentPath.Length > 0 && items.ContainsKey(old.DocumentPath)) continue;
                    string stalePath = IOPath.Combine(settings.OutputRoot, FileNameBuilder.CleanName(old.Material), FileNameBuilder.CleanName(old.Thickness),
                        FileNameBuilder.BuildFileName(settings.FilePattern, old.Thickness, old.Quantity, old.Material, old.DetailName));
                    if (!IOFile.Exists(stalePath)) continue;
                    if (settings.CleanupSuppressed) { IOFile.Delete(stalePath); deletedSuppressed++; }
                    else outputLines.AppendLine("CẢNH BÁO: File cũ của part Suppress vẫn còn: " + stalePath);
                }
                catch (Exception ex) { errors.Add(old.DetailName + " (suppress): " + ex.Message); }
            }

            totalTimer.Stop();
            var report = new StringBuilder();
            report.AppendLine("HOÀN TẤT XUẤT DXF");
            report.AppendLine("Thời gian (giây): Tổng " + Seconds(totalTimer) + " | Xuất DXF (Unfold+DataIO) " + Seconds(dxfTimer) +
                " | Xử lý file DXF " + Seconds(postTimer) + " | Cập nhật thuộc tính + Save " + Seconds(infoTimer) + " | BOM " + Seconds(bomTimer));
            report.AppendLine("Part duy nhất: " + items.Count); report.AppendLine("Đã xuất: " + exported); report.AppendLine("Bỏ qua file đã có: " + skippedExisting);
            report.AppendLine("Bỏ qua khi duyệt: " + skipped.Count); if (settings.BendMode == "Chỉ đánh dấu 2 đầu") report.AppendLine("Đường chấn đã đánh dấu: " + bendCount);
            report.AppendLine("Lỗi: " + errors.Count); report.AppendLine("File suppressed đã xóa: " + deletedSuppressed);
            if (!string.IsNullOrEmpty(bomPath)) report.AppendLine("BOM: " + bomPath);
            if (skipped.Count > 0) { report.AppendLine("\nBỎ QUA:"); foreach (var line in skipped) report.AppendLine(line); }
            if (errors.Count > 0) { report.AppendLine("\nLỖI:"); foreach (var line in errors) report.AppendLine(line); }
            report.AppendLine(); report.Append(outputLines);
            reportPath = IOPath.Combine(settings.OutputRoot,
                "BaoCao_Export_DXF_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + ".txt");
            IOFile.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(true));
            monitor.Report(new OperationProgress(items.Count, items.Count, reportPath, "Hoàn tất"));
            return new OperationResult(exported, skipped.Count + skippedExisting, errors.Count, reportPath,
                new[] { $"Part duy nhất: {items.Count}; đã xuất: {exported}; bỏ qua: {skipped.Count + skippedExisting}; lỗi: {errors.Count}.", "Báo cáo: " + reportPath }.Concat(errors).ToArray());
        }
        catch (OperationCanceledException)
        {
            totalTimer.Stop();
            return new OperationResult(exported, skipped.Count + skippedExisting, errors.Count, reportPath,
                new[] { "Đã hủy theo yêu cầu. Các file đã xuất trước thời điểm hủy được giữ nguyên.", "Đã hoàn tất: " + exported }.Concat(errors).ToArray());
        }
        catch (Exception ex)
        {
            return new OperationResult(exported, skipped.Count + skippedExisting, errors.Count + 1, reportPath,
                new[] { "Lỗi tổng quát: " + ex.Message }.Concat(errors).ToArray());
        }
        finally
        {
            if (fastApplied && _application is not null)
            {
                try { _application.SilentOperation = oldSilent; } catch { }
                try { _application.ScreenUpdating = oldScreen; } catch { }
            }
        }
    }

    private static void CollectDocumentItems(Document document, Dictionary<string, WorkItem> items, List<string> skipped, List<string> errors, List<ExportItem> suppressed)
    {
        if (document.DocumentType == DocumentTypeEnum.kPartDocumentObject) AddPartItem(document as PartDocument, 1, items, skipped, errors);
        else if (document.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
            ScanOccurrences(((AssemblyDocument)document).ComponentDefinition.Occurrences, items, skipped, errors, suppressed);
        else skipped.Add("Chỉ hỗ trợ Part Sheet Metal hoặc Assembly.");
    }

    private static void ScanOccurrences(ComponentOccurrences occurrences, Dictionary<string, WorkItem> items, List<string> skipped, List<string> errors, List<ExportItem> suppressed)
    {
        ScanOccurrences(occurrences.Count, index => (ComponentOccurrence)occurrences[index], items, skipped, errors, suppressed);
    }

    private static void ScanOccurrences(ComponentOccurrencesEnumerator occurrences, Dictionary<string, WorkItem> items, List<string> skipped, List<string> errors, List<ExportItem> suppressed)
    {
        ScanOccurrences(occurrences.Count, index => (ComponentOccurrence)occurrences[index], items, skipped, errors, suppressed);
    }

    private static void ScanOccurrences(int count, Func<int, ComponentOccurrence> getOccurrence, Dictionary<string, WorkItem> items, List<string> skipped, List<string> errors, List<ExportItem> suppressed)
    {
        for (int index = 1; index <= count; index++)
        {
            var occurrence = getOccurrence(index);
            try
            {
                if (occurrence.Suppressed) { skipped.Add("Suppressed: " + occurrence.Name); TryCollectSuppressed(occurrence, suppressed); continue; }
                if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kAssemblyDocumentObject) ScanOccurrences(occurrence.SubOccurrences, items, skipped, errors, suppressed);
                else if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kPartDocumentObject) AddPartItem(occurrence.Definition.Document as PartDocument, 1, items, skipped, errors);
            }
            catch (Exception ex) { errors.Add(occurrence.Name + ": " + ex.Message); }
        }
    }

    private static void TryCollectSuppressed(ComponentOccurrence occurrence, List<ExportItem> suppressed)
    {
        try
        {
            if (occurrence.DefinitionDocumentType != DocumentTypeEnum.kPartDocumentObject) return;
            var part = occurrence.Definition.Document as PartDocument;
            if (part?.ComponentDefinition is not SheetMetalComponentDefinition sm || string.IsNullOrEmpty(part.FullFileName)) return;
            suppressed.Add(new ExportItem(part.FullFileName, IOPath.GetFileNameWithoutExtension(part.DisplayName), GetMaterial(part), GetThickness(sm), 1));
        }
        catch { }
    }

    private static void AddPartItem(PartDocument? part, int quantity, Dictionary<string, WorkItem> items, List<string> skipped, List<string> errors)
    {
        if (part is null) { skipped.Add("Part không thể mở"); return; }
        try
        {
            if (part.ComponentDefinition is not SheetMetalComponentDefinition sm) { skipped.Add(part.DisplayName + " - không phải Sheet Metal"); return; }
            if (string.IsNullOrEmpty(part.FullFileName)) { skipped.Add(part.DisplayName + " - chưa lưu"); return; }
            if (items.TryGetValue(part.FullFileName, out var existing))
                items[part.FullFileName] = existing with { Item = existing.Item with { Quantity = existing.Item.Quantity + quantity } };
            else
            {
                var (xMm, yMm, sizeIssue) = GetFlatPatternSize(part, sm);
                items.Add(part.FullFileName, new WorkItem(
                    new ExportItem(part.FullFileName, IOPath.GetFileNameWithoutExtension(part.DisplayName),
                        GetMaterial(part), GetThickness(sm), quantity, xMm, yMm, sizeIssue),
                    part));
            }
        }
        catch (Exception ex) { errors.Add(part.DisplayName + ": " + ex.Message); }
    }

    private static (double? Xmm, double? Ymm, string? Issue) GetFlatPatternSize(
        PartDocument part,
        SheetMetalComponentDefinition sheetMetal)
    {
        try
        {
            if (!sheetMetal.HasFlatPattern)
                return (null, null, "Chưa có Flat Pattern");

            var bounds = sheetMetal.FlatPattern.RangeBox;
            var units = part.UnitsOfMeasure;
            var x = units.ConvertUnits(Math.Abs(bounds.MaxPoint.X - bounds.MinPoint.X),
                UnitsTypeEnum.kDatabaseLengthUnits, UnitsTypeEnum.kMillimeterLengthUnits);
            var y = units.ConvertUnits(Math.Abs(bounds.MaxPoint.Y - bounds.MinPoint.Y),
                UnitsTypeEnum.kDatabaseLengthUnits, UnitsTypeEnum.kMillimeterLengthUnits);
            var z = units.ConvertUnits(Math.Abs(bounds.MaxPoint.Z - bounds.MinPoint.Z),
                UnitsTypeEnum.kDatabaseLengthUnits, UnitsTypeEnum.kMillimeterLengthUnits);
            var planarExtents = new[] { x, y, z }.OrderByDescending(extent => extent).Take(2).ToArray();
            if (planarExtents.Any(extent => !double.IsFinite(extent) || extent <= 0))
                return (null, null, "Kích thước Flat Pattern không hợp lệ");

            return (planarExtents[0], planarExtents[1], null);
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            return (null, null, "Không đọc được kích thước Flat Pattern: " + ex.Message);
        }
    }

    private static string GetMaterial(PartDocument part)
    {
        try { return part.ActiveMaterial?.DisplayName ?? "Khong_Xac_Dinh"; } catch { return "Khong_Xac_Dinh"; }
    }
    private static string GetThickness(SheetMetalComponentDefinition sheetMetal)
    {
        try { return "T" + Math.Round(System.Convert.ToDouble(sheetMetal.Thickness.Value, CultureInfo.InvariantCulture) * 10.0, 3).ToString("0.###", CultureInfo.InvariantCulture); }
        catch { return "T_Khong_Xac_Dinh"; }
    }

    private bool ExportOnePart(PartDocument part, string path, ExportSettings settings, string options)
    {
        if (part.ComponentDefinition is not SheetMetalComponentDefinition sm) throw new InvalidOperationException("Không phải Sheet Metal.");
        if (!sm.HasFlatPattern) sm.Unfold();
        if (!sm.HasFlatPattern) throw new InvalidOperationException("Không tạo được Flat Pattern.");
        try
        {
            var app = part.Parent as Inventor.Application;
            if (app?.ApplicationAddIns.ItemById[DxfTranslatorId] is TranslatorAddIn translator && !translator.Activated) translator.Activate();
        }
        catch { }
        var exportErrors = new List<Exception>();
        if (TryWriteDxf(sm.DataIO, options, path, "SheetMetal.DataIO với tùy chọn hiện tại", exportErrors)) return false;
        if (TryWriteDxf(sm.DataIO, BuildFallbackDxfOptions(settings), path, "SheetMetal.DataIO với tùy chọn DXF tối giản", exportErrors)) return true;
        if (TryWriteDxf(sm.FlatPattern.DataIO, options, path, "FlatPattern.DataIO với tùy chọn hiện tại", exportErrors)) return false;
        if (TryWriteDxf(sm.FlatPattern.DataIO, BuildFallbackDxfOptions(settings), path, "FlatPattern.DataIO với tùy chọn DXF tối giản", exportErrors)) return true;

        throw new InvalidOperationException("Không thể xuất DXF hợp lệ bằng SheetMetal.DataIO hoặc FlatPattern.DataIO. " +
            string.Join(" | ", exportErrors.Select(error => error.Message)), new AggregateException(exportErrors));
    }

    private bool TryWriteDxf(DataIO dataIO, string format, string path, string context, List<Exception> errors)
    {
        try
        {
            dataIO.WriteDataToFile(format, path);
            if (!_processor.Validate(path))
                throw new InvalidDataException("DXF được tạo nhưng sai cấu trúc hoặc không có entity hợp lệ.");
            return true;
        }
        catch (Exception ex)
        {
            errors.Add(new InvalidOperationException(context + ": " + ex.Message, ex));
            if (IOFile.Exists(path)) IOFile.Delete(path);
            return false;
        }
    }

    private static string BuildDxfOptions(ExportSettings settings) => DxfLayerOptionsBuilder.Build(settings, includeLineTypes: true);
    private static string BuildFallbackDxfOptions(ExportSettings settings) => DxfLayerOptionsBuilder.Build(settings, includeLineTypes: false);
    private static string Seconds(Stopwatch timer) => (timer.ElapsedMilliseconds / 1000.0).ToString("0.0", CultureInfo.InvariantCulture);
    private sealed record WorkItem(ExportItem Item, PartDocument Document);
}
