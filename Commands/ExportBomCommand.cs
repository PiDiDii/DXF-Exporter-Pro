using Inventor;
using InventorDxfExporter.Services;
using InventorDxfExporter.Settings;
using InventorDxfExporter.UI.Forms;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Commands;

public sealed class ExportBomCommand
{
    private readonly InventorApplication _application;
    private readonly ServiceContainer _services;
    public ExportBomCommand(InventorApplication application) { _application = application; _services = new ServiceContainer(application); }

    public void Execute()
    {
        var document = _application.ActiveDocument;
        if (document is not PartDocument && document is not AssemblyDocument)
        {
            MessageBox.Show("Xuất BOM chỉ hỗ trợ Part hoặc Assembly.", "Xuất BOM", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var bomSettingsStore = new BomExportSettingsStore();
        var bomSettings = bomSettingsStore.Load();
        using var form = new BomForm(bomSettings, bomSettingsStore.Save);
        if (form.ShowDialog() != DialogResult.OK) return;
        bomSettingsStore.Save(bomSettings);
        var items = _services.DxfExport.CollectItems(document);
        if (items.Count == 0)
        {
            MessageBox.Show("Không tìm thấy part Sheet Metal hợp lệ để lập BOM.", "Xuất BOM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string path = _services.Bom.Export(items, bomSettings);
        var summary = string.IsNullOrWhiteSpace(path) ? "Không xuất được BOM." : "Đã xuất BOM:\n" + path;
        if (_services.Bom.Errors.Count > 0) summary += "\n\nLỗi:\n" + string.Join("\n", _services.Bom.Errors);
        MessageBox.Show(summary, "Kết quả Xuất BOM", MessageBoxButtons.OK,
            _services.Bom.Errors.Count > 0 || string.IsNullOrWhiteSpace(path) ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }
}
