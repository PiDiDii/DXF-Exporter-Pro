using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Inventor;
using InventorDxfExporter.Core;
using IOEnvironment = System.Environment;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;
using InventorPoint = Inventor.Point;

namespace InventorDxfExporter.Services;

/// <summary>Converts imported parts to sheet metal and creates a flat pattern.</summary>
public sealed class ConvertServiceImplementation : IConvertService
{
    private const string SheetMetalSubtype = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}";
    private readonly Inventor.Application _application;

    public ConvertServiceImplementation(Inventor.Application application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public OperationResult Convert(AssemblyDocument assembly, ExportSettings settings) =>
        Convert(assembly, settings, NullOperationMonitor.Instance);

    public OperationResult Convert(AssemblyDocument assembly, ExportSettings settings, IOperationMonitor? monitor)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(settings);
        monitor ??= NullOperationMonitor.Instance;

        var filterText = (settings.ConvFilter ?? string.Empty).Trim().ToUpperInvariant();
        var skipExisting = settings.ConvSkipExisting;
        var assignInnerFaceAsASide = settings.ConvAssignInnerFaceAsASide;
        var conversionMode = settings.ConvMode;
        var toleranceMm = settings.ConvTolerance;
        if (toleranceMm <= 0) toleranceMm = 0.01;

        var logFolder = (settings.ConvLogFolder ?? string.Empty).Trim();
        if (logFolder.Length == 0)
        {
            logFolder = !string.IsNullOrEmpty(assembly.FullFileName)
                ? IOPath.GetDirectoryName(assembly.FullFileName) ?? IOEnvironment.GetFolderPath(IOEnvironment.SpecialFolder.Desktop)
                : IOEnvironment.GetFolderPath(IOEnvironment.SpecialFolder.Desktop);
        }
        if (settings.ConvWriteLog)
        {
            try
            {
                if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Không tạo được thư mục log '{logFolder}': {ex.Message}");
                logFolder = IOEnvironment.GetFolderPath(IOEnvironment.SpecialFolder.Desktop);
            }
        }

        var log = new StringBuilder();
        log.AppendLine("SM BATCH CONVERTER - Inventor 2027");
        log.AppendLine("Chế độ    : " + (conversionMode == 1 ? "1 - Tự phát hiện độ dày, điền thủ công" : "2 - Tự phát hiện độ dày, khớp Sheet Metal Rule"));
        log.AppendLine("Dung sai  : ±" + toleranceMm.ToString("0.000", CultureInfo.InvariantCulture) + " mm");
        log.AppendLine("Gán A-Side mặt trong: " + (assignInnerFaceAsASide ? "Có" : "Không"));
        log.AppendLine("Thời gian : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
        log.AppendLine("Assembly  : " + assembly.DisplayName);
        log.AppendLine("Lọc       : " + (filterText.Length == 0 ? "(Tất cả)" : filterText));
        log.AppendLine(new string('=', 80));
        log.AppendLine();

        var successCount = 0;
        var skipCount = 0;
        var errorCount = 0;
        var oldSilent = _application.SilentOperation;
        var oldScreenUpdating = _application.ScreenUpdating;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _application.SilentOperation = true;
            _application.ScreenUpdating = false;

            var referenced = assembly.AllReferencedDocuments.Cast<Document>().ToArray();
            int itemIndex = 0;
            foreach (Document doc in referenced)
            {
                monitor.CancellationToken.ThrowIfCancellationRequested();
                itemIndex++;
                if (doc.DocumentType != DocumentTypeEnum.kPartDocumentObject) continue;
                var part = (PartDocument)doc;
                var fullName = IOPath.GetFileName(part.FullFileName);
                var partName = IOPath.GetFileNameWithoutExtension(part.FullFileName);
                if (filterText.Length != 0 && !partName.ToUpperInvariant().Contains(filterText, StringComparison.Ordinal)) continue;
                monitor.Report(new OperationProgress(itemIndex - 1, referenced.Length, fullName, "Đang Convert STEP"));

                log.AppendLine(">>> " + fullName);
                try
                {
                    if (skipExisting && part.SubType == SheetMetalSubtype)
                    {
                        var existingSm = (SheetMetalComponentDefinition)part.ComponentDefinition;
                        if (existingSm.HasFlatPattern)
                        {
                            log.AppendLine("    → BỎ QUA (đã có Flat Pattern)");
                            skipCount++;
                            log.AppendLine();
                            continue;
                        }
                    }

                    if (part.SubType != SheetMetalSubtype) part.SubType = SheetMetalSubtype;
                    var smDef = (SheetMetalComponentDefinition)part.ComponentDefinition;

                    Face? tempFace = null;
                    var thicknessMm = GetInnerFaceAndThickness(smDef, ref tempFace);
                    log.AppendLine("    Thickness = " + thicknessMm.ToString("0.000", CultureInfo.InvariantCulture) + " mm");
                    if (thicknessMm <= 0.1 || thicknessMm > 30 || tempFace is null)
                        throw new InvalidOperationException("Không xác định được mặt trong / Thickness");

                    if (conversionMode == 1)
                    {
                        smDef.UseSheetMetalStyleThickness = false;
                        smDef.Thickness.Value = thicknessMm / 10;
                        log.AppendLine("    Điền tay Thickness = " + thicknessMm.ToString("0.000", CultureInfo.InvariantCulture) + " mm");
                    }
                    else
                    {
                        var matchedRule = FindMatchingSheetMetalRule(smDef, thicknessMm, toleranceMm);
                        if (matchedRule is null)
                            throw new InvalidOperationException("Không tìm thấy Sheet Metal Rule khớp Thickness " + thicknessMm.ToString("0.000", CultureInfo.InvariantCulture) + " mm (tolerance ±" + toleranceMm.ToString("0.000", CultureInfo.InvariantCulture) + "mm)");
                        matchedRule.Activate();
                        smDef.UseSheetMetalStyleThickness = true;
                        log.AppendLine("    Áp Sheet Metal Rule: " + matchedRule.Name + " (Thickness rule = " + (SafeThicknessToCm(matchedRule.Thickness) * 10).ToString("0.000", CultureInfo.InvariantCulture) + " mm)");
                    }

                    part.Update();
                    if (assignInnerFaceAsASide)
                    {
                        Face? innerFace = null;
                        GetInnerFaceAndThickness(smDef, ref innerFace);
                        if (innerFace is null) throw new InvalidOperationException("Không xác định lại được mặt trong sau khi đổi Thickness");
                        try
                        {
                            smDef.ASideFace = innerFace;
                            log.AppendLine("    Đã gán A-Side = mặt trong");
                        }
                        catch
                        {
                            log.AppendLine("    Cảnh báo: Không gán được ASideFace");
                        }
                    }
                    else log.AppendLine("    Bỏ qua gán A-Side theo tùy chọn");

                    try
                    {
                        if (!smDef.HasFlatPattern) smDef.Unfold();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Lỗi khi Unfold(): " + ex.Message, ex);
                    }
                    try { part.Update(); }
                    catch (Exception ex) { throw new InvalidOperationException("Lỗi khi Update(): " + ex.Message, ex); }
                    try { if (part.Dirty) part.Save(); }
                    catch (Exception ex) { throw new InvalidOperationException("Lỗi khi Save(): " + ex.Message, ex); }

                    log.AppendLine("    ★ THÀNH CÔNG");
                    successCount++;
                }
                catch (Exception ex)
                {
                    log.AppendLine("    ✗ LỖI: " + ex.Message);
                    errorCount++;
                }
                log.AppendLine();
                monitor.Report(new OperationProgress(itemIndex, referenced.Length, fullName, "Đang Convert STEP"));
            }
        }
        catch (OperationCanceledException)
        {
            log.AppendLine("Đã hủy theo yêu cầu người dùng.");
        }
        catch (Exception ex)
        {
            log.AppendLine("LỖI TỔNG QUÁT: " + ex.Message);
            errorCount++;
        }
        finally
        {
            try { _application.SilentOperation = oldSilent; }
            catch (Exception ex) { Trace.TraceError($"Không khôi phục được SilentOperation sau Convert: {ex}"); }
            try { _application.ScreenUpdating = oldScreenUpdating; }
            catch (Exception ex) { Trace.TraceError($"Không khôi phục được ScreenUpdating sau Convert: {ex}"); }
            stopwatch.Stop();
        }

        log.AppendLine(new string('=', 80));
        log.AppendLine("TỔNG KẾT");
        log.AppendLine("Thành công : " + successCount);
        log.AppendLine("Bỏ qua     : " + skipCount);
        log.AppendLine("Lỗi        : " + errorCount);
        log.AppendLine("Thời gian  : " + (stopwatch.ElapsedMilliseconds / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " giây");
        log.AppendLine("Kết thúc   : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));

        var logFile = string.Empty;
        var resultMessages = new List<string>();
        if (settings.ConvWriteLog)
        {
            logFile = IOPath.Combine(logFolder, "SM_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".txt");
            try { IOFile.WriteAllText(logFile, log.ToString(), Encoding.UTF8); }
            catch (Exception ex)
            {
                Trace.TraceError($"Không ghi được log Convert '{logFile}': {ex}");
                resultMessages.Add("Không ghi được file log: " + ex.Message);
                logFile = string.Empty;
            }
        }

        return new OperationResult(successCount, skipCount, errorCount, logFile, resultMessages);
    }

    private static double SafeThicknessToCm(object? raw)
    {
        if (raw is double number) return number;
        var text = raw?.ToString()?.Trim() ?? string.Empty;
        var match = Regex.Match(text, @"[-+]?[0-9]*[\.,]?[0-9]+");
        var numberText = match.Value.Replace(',', '.');
        if (!double.TryParse(numberText, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            throw new InvalidOperationException("Không đọc được giá trị Thickness: '" + text + "'");
        var lower = text.ToLowerInvariant();
        if (lower.Contains("in", StringComparison.Ordinal)) return value * 2.54;
        if (lower.Contains("mm", StringComparison.Ordinal)) return value / 10;
        return value;
    }

    private static SheetMetalStyle? FindMatchingSheetMetalRule(SheetMetalComponentDefinition smDef, double thicknessMm, double toleranceMm)
    {
        SheetMetalStyle? bestRule = null;
        var bestDifference = double.MaxValue;
        foreach (SheetMetalStyle rule in smDef.SheetMetalStyles)
        {
            try
            {
                var ruleThicknessMm = SafeThicknessToCm(rule.Thickness) * 10;
                var difference = Math.Abs(ruleThicknessMm - thicknessMm);
                if (difference <= toleranceMm && difference < bestDifference)
                {
                    bestDifference = difference;
                    bestRule = rule;
                }
            }
            catch { /* Ignore styles whose Thickness cannot be read. */ }
        }
        return bestRule;
    }

    private double GetInnerFaceAndThickness(SheetMetalComponentDefinition smDef, ref Face? innerFace)
    {
        try
        {
            if (smDef.SurfaceBodies.Count == 0) return -1;
            SurfaceBody body = smDef.SurfaceBodies[1];
            InventorPoint min = body.RangeBox.MinPoint;
            InventorPoint max = body.RangeBox.MaxPoint;
            InventorPoint center = _application.TransientGeometry.CreatePoint((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);
            var faces = new List<Face>();
            foreach (Face face in body.Faces)
                if (face.Geometry is Plane) faces.Add(face);
            if (faces.Count < 2) return FallbackThickness(smDef, ref innerFace);

            faces.Sort((a, b) => b.Evaluator.Area.CompareTo(a.Evaluator.Area));
            Face face1 = faces[0];
            Face? face2 = null;
            var maxArea2 = 0.0;
            Plane plane1 = (Plane)face1.Geometry;
            for (var i = 1; i < faces.Count; i++)
            {
                Face face = faces[i];
                Plane plane = (Plane)face.Geometry;
                if (Math.Abs(plane1.Normal.DotProduct(plane.Normal)) > 0.999 && face.Evaluator.Area > maxArea2)
                {
                    maxArea2 = face.Evaluator.Area;
                    face2 = face;
                }
            }
            if (face2 is null) return FallbackThickness(smDef, ref innerFace);
            var thicknessMm = Math.Round(_application.MeasureTools.GetMinimumDistance(face1, face2) * 10, 3);

            UnitVector normal1 = face1.IsParamReversed ? plane1.Normal : _application.TransientGeometry.CreateUnitVector(-plane1.Normal.X, -plane1.Normal.Y, -plane1.Normal.Z);
            Vector vector1 = center.VectorTo(face1.PointOnFace);
            var dot1 = normal1.DotProduct(vector1.AsUnitVector());
            Plane plane2 = (Plane)face2.Geometry;
            UnitVector normal2 = face2.IsParamReversed ? plane2.Normal : _application.TransientGeometry.CreateUnitVector(-plane2.Normal.X, -plane2.Normal.Y, -plane2.Normal.Z);
            Vector vector2 = center.VectorTo(face2.PointOnFace);
            var dot2 = normal2.DotProduct(vector2.AsUnitVector());
            innerFace = dot1 > dot2 ? face1 : face2;
            return thicknessMm;
        }
        catch { return -1; }
    }

    private double FallbackThickness(SheetMetalComponentDefinition smDef, ref Face? baseFace)
    {
        try
        {
            SurfaceBody body = smDef.SurfaceBodies[1];
            var maxArea = 0.0;
            baseFace = null;
            foreach (Face face in body.Faces)
            {
                if (face.Geometry is Plane && face.Evaluator.Area > maxArea)
                {
                    maxArea = face.Evaluator.Area;
                    baseFace = face;
                }
            }
            if (baseFace is null) return -1;
            InventorPoint point1 = baseFace.PointOnFace;
            Plane plane = (Plane)baseFace.Geometry;
            UnitVector normal = baseFace.IsParamReversed ? plane.Normal : _application.TransientGeometry.CreateUnitVector(-plane.Normal.X, -plane.Normal.Y, -plane.Normal.Z);
            ObjectsEnumerator? objects = null;
            ObjectsEnumerator? points = null;
            body.FindUsingRay(point1, normal, 0.001, out objects, out points);
            if (points is null || points.Count < 2) return -1;
            InventorPoint point2 = (InventorPoint)points[2];
            return Math.Round(point1.DistanceTo(point2) * 10, 3);
        }
        catch { return -1; }
    }
}
