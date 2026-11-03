using Inventor;
using InventorDxfExporter.Settings;
using InventorDxfExporter.UI.Forms;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Commands;

public sealed class SettingsCommand
{
    private readonly LegacyIniSettingsStore _settingsStore = new();
    public SettingsCommand(InventorApplication application) { ArgumentNullException.ThrowIfNull(application); }
    public void Execute()
    {
        var settings = _settingsStore.Load();
        using var form = new ExportSettingsForm(settings);
        if (form.ShowDialog() != DialogResult.OK) return;
        form.ApplyTo(settings); _settingsStore.Save(settings);
        MessageBox.Show("Đã lưu cấu hình vào file INI tương thích V14.", "Cài đặt", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
