using Inventor;
using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using InventorDxfExporter.Settings;
using InventorDxfExporter.UI.Forms;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Commands;

public sealed class ConvertStepCommand
{
    private readonly InventorApplication _application;
    private readonly LegacyIniSettingsStore _settingsStore = new();
    private readonly ServiceContainer _services;
    public ConvertStepCommand(InventorApplication application) { _application = application; _services = new ServiceContainer(application); }

    public void Execute()
    {
        if (_application.ActiveDocument is not AssemblyDocument assembly)
        {
            MessageBox.Show("Convert STEP chỉ chạy trên Assembly.", "Convert STEP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var settings = _settingsStore.Load();
        using var form = new ConvertStepForm(settings);
        if (form.ShowDialog() != DialogResult.OK) return;
        form.ApplyTo(settings); _settingsStore.Save(settings);
        OperationResult result;
        using (var progress = new OperationProgressForm("Convert STEP"))
        {
            progress.Show(); progress.Refresh();
            try { result = _services.Convert.Convert(assembly, settings, progress); }
            catch (Exception ex)
            {
                MessageBox.Show("Convert bị lỗi: " + ex.Message, "Convert STEP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            progress.Close();
        }
        var summary = $"Thành công: {result.Success}\nBỏ qua: {result.Skipped}\nLỗi: {result.Errors}";
        if (!string.IsNullOrWhiteSpace(result.LogFile)) summary += "\nLog: " + result.LogFile;
        if (result.Messages.Count > 0) summary += "\n\n" + string.Join("\n", result.Messages);
        MessageBox.Show(summary, "Kết quả Convert STEP", MessageBoxButtons.OK,
            result.Errors > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }
}
