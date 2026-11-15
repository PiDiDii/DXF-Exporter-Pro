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
        var owner = new InventorWindowOwner(_application.MainFrameHWND);
        var document = _application.ActiveDocument;
        if (document is not PartDocument && document is not AssemblyDocument)
        {
            MessageBox.Show(owner, "Xuất BOM chỉ hỗ trợ Part hoặc Assembly.", "Xuất BOM", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var bomSettingsStore = new BomExportSettingsStore();
        var bomSettings = bomSettingsStore.Load();
        using var form = new BomForm(bomSettings, bomSettingsStore.Save);
        if (form.ShowDialog(owner) != DialogResult.OK) return;
        bomSettingsStore.Save(bomSettings);
        string path;
        using (var progress = new OperationProgressForm("Xuất BOM", owner))
        {
            progress.Show(owner);
            progress.Refresh();
            try
            {
                var items = _services.DxfExport.CollectItems(document, progress);
                if (items.Count == 0)
                {
                    progress.MarkCompleted();
                    progress.Close();
                    MessageBox.Show(owner, "Không tìm thấy part Sheet Metal hợp lệ để lập BOM.", "Xuất BOM",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                path = _services.Bom.Export(items, bomSettings,
                    BomServiceImplementation.GetProjectName(document), progress);
                progress.MarkCompleted();
            }
            catch (OperationCanceledException)
            {
                progress.MarkCanceled();
                progress.Close();
                MessageBox.Show(owner, "Đã hủy xuất BOM.", "Xuất BOM", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show(owner, "Xuất BOM bị lỗi: " + ex.Message, "Xuất BOM",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        var summary = string.IsNullOrWhiteSpace(path) ? "Không xuất được BOM." : "Đã xuất BOM:\n" + path;
        if (_services.Bom.Errors.Count > 0) summary += "\n\nLỗi:\n" + string.Join("\n", _services.Bom.Errors);
        MessageBox.Show(owner, summary, "Kết quả Xuất BOM", MessageBoxButtons.OK,
            _services.Bom.Errors.Count > 0 || string.IsNullOrWhiteSpace(path) ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }
}
