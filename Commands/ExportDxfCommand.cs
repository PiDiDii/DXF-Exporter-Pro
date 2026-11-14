using Inventor;
using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using InventorDxfExporter.Settings;
using InventorDxfExporter.UI.Forms;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;
using IOEnvironment = System.Environment;
using IOPath = System.IO.Path;

namespace InventorDxfExporter.Commands;

public sealed class ExportDxfCommand
{
    private readonly InventorApplication _application;
    private readonly LegacyIniSettingsStore _settingsStore = new();
    private readonly ServiceContainer _services;
    public ExportDxfCommand(InventorApplication application) { _application = application; _services = new ServiceContainer(application); }

    public void Execute()
    {
        var document = _application.ActiveDocument;
        if (document is not PartDocument && document is not AssemblyDocument)
        {
            MessageBox.Show("Xuất DXF chỉ hỗ trợ Part hoặc Assembly.", "Xuất DXF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var settings = _settingsStore.Load();
        using (var form = new DxfExportForm(settings))
        {
            if (form.ShowDialog() != DialogResult.OK) return;
            form.ApplyTo(settings); _settingsStore.Save(settings);
        }

        var items = _services.DxfExport.CollectItems(document);
        if (items.Count == 0)
        {
            MessageBox.Show("Không tìm thấy part Sheet Metal hợp lệ.", "Xuất DXF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var preflight = document is AssemblyDocument assembly
            ? new ExportPreflightService().Inspect(assembly)
            : new ExportPreflightService().Inspect((PartDocument)document);
        var warnings = preflight.Warnings.ToList();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Material) || item.Material == "Khong_Xac_Dinh") warnings.Add($"'{item.DetailName}' thiếu/không xác định vật liệu.");
            if (string.IsNullOrWhiteSpace(item.Thickness) || item.Thickness == "T_Khong_Xac_Dinh") warnings.Add($"'{item.DetailName}' thiếu/không xác định độ dày.");
        }
        var duplicateFiles = items.GroupBy(item => IOPath.Combine(settings.OutputRoot, FileNameBuilder.CleanName(item.Material),
                FileNameBuilder.CleanName(item.Thickness), FileNameBuilder.BuildFileName(settings.FilePattern, item.Thickness, item.Quantity, item.Material, item.DetailName)),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        warnings.AddRange(duplicateFiles.Select(path => "Trùng tên file DXF dự kiến: " + path));
        preflight = preflight with { Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() };
        if (preflight.Warnings.Count > 0 || preflight.UncheckedChecks.Count > 0)
        {
            using var review = new PreflightReviewForm(preflight);
            if (review.ShowDialog() != DialogResult.OK) return;
        }
        IReadOnlyList<string> selectedPaths;
        using (var preview = new ExportPreviewForm(items, settings))
        {
            preview.SettingsRequested += (_, _) =>
            {
                using var settingsForm = new ExportSettingsForm(settings);
                if (settingsForm.ShowDialog(preview) != DialogResult.OK)
                    return;

                settingsForm.ApplyTo(settings);
                _settingsStore.Save(settings);
            };
            if (preview.ShowDialog() != DialogResult.OK) return;
            selectedPaths = preview.SelectedDocumentPaths;
        }
        if (selectedPaths.Count == 0)
        {
            MessageBox.Show("Chưa chọn part nào.", "Xuất DXF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        OperationResult result;
        using (var progress = new OperationProgressForm("Xuất DXF + BOM"))
        {
            progress.Show(); progress.Refresh();
            try { result = _services.DxfExport.Export(document, settings, progress, selectedPaths); }
            catch (Exception ex)
            {
                MessageBox.Show("Xuất DXF bị lỗi: " + ex.Message, "Xuất DXF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            progress.Close();
        }
        var summary = string.Join(IOEnvironment.NewLine, result.Messages);
        if (string.IsNullOrWhiteSpace(summary)) summary = $"Đã xuất: {result.Success}; bỏ qua: {result.Skipped}; lỗi: {result.Errors}.";
        MessageBox.Show(summary, "Kết quả Xuất DXF", MessageBoxButtons.OK,
            result.Errors > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }
}
