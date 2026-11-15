using Inventor;
using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using InventorDxfExporter.Settings;
using InventorDxfExporter.UI.Forms;
using System.Diagnostics;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Commands;

public sealed class ConvertStepCommand
{
    private readonly InventorApplication _application;
    private readonly ExportSettingsStore _settingsStore = new();
    private readonly ServiceContainer _services;
    public ConvertStepCommand(InventorApplication application) { _application = application; _services = new ServiceContainer(application); }

    public void Execute()
    {
        var owner = new InventorWindowOwner(_application.MainFrameHWND);
        if (_application.ActiveDocument is not AssemblyDocument assembly)
        {
            MessageBox.Show(owner, "Convert STEP chỉ chạy trên Assembly.", "Convert STEP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var settings = _settingsStore.Load();
        using var form = new ConvertStepForm(settings);
        if (form.ShowDialog(owner) != DialogResult.OK) return;
        form.ApplyTo(settings); _settingsStore.Save(settings);
        OperationResult result;
        using (var progress = new OperationProgressForm("Convert STEP", owner))
        {
            progress.Show(owner); progress.Refresh();
            try { result = _services.Convert.Convert(assembly, settings, progress); }
            catch (OperationCanceledException)
            {
                progress.MarkCanceled();
                progress.Close();
                MessageBox.Show(owner, "Đã hủy Convert STEP.", "Convert STEP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Convert bị lỗi: " + ex.Message, "Convert STEP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            progress.Close();
        }
        var summary = $"Thành công: {result.Success}\nBỏ qua: {result.Skipped}\nLỗi: {result.Errors}";
        if (!string.IsNullOrWhiteSpace(result.LogFile)) summary += "\nLog: " + result.LogFile;
        if (result.Messages.Count > 0) summary += "\n\n" + string.Join("\n", result.Messages);
        MessageBox.Show(owner, summary, "Kết quả Convert STEP", MessageBoxButtons.OK,
            result.Errors > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        if (!string.IsNullOrWhiteSpace(result.LogFile))
        {
            var answer = MessageBox.Show(
                owner,
                $"Đã ghi log:\n{result.LogFile}\n\nBạn có muốn mở thư mục chứa log không?",
                "Mở thư mục log",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = System.IO.Path.GetDirectoryName(result.LogFile)!,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(owner, "Không mở được thư mục log: " + ex.Message,
                        "Convert STEP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
