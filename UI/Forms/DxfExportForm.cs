using InventorDxfExporter.Core;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class DxfExportForm : Form
{
    private readonly TextBox _output = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _overwrite = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _fast = new() { Text = "Tắt cập nhật giao diện (tăng tốc)", AutoSize = true };
    private readonly CheckBox _cleanup = new() { Text = "Tự động xóa DXF cũ của part đang Suppress", AutoSize = true };
    private readonly CheckBox _update = new() { Text = "Cập nhật Description/Thickness và xuất BOM", AutoSize = true };

    public DxfExportForm(ExportSettings settings)
    {
        Text = "Xuất DXF"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false; MaximizeBox = false; ClientSize = new System.Drawing.Size(760, 300);
        _output.Text = settings.OutputRoot; _fast.Checked = settings.FastMode;
        _cleanup.Checked = settings.CleanupSuppressed; _update.Checked = settings.UpdateInfoAndBom;
        _overwrite.Items.AddRange(new object[] { "Hỏi từng file", "Ghi đè tự động", "Bỏ qua file đã có" });
        _overwrite.SelectedItem = settings.OverwriteMode; if (_overwrite.SelectedIndex < 0) _overwrite.SelectedIndex = 0;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 3, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        foreach (var row in new[] { 0, 1, 2, 3 }) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.Controls.Add(new Label { Text = "Thư mục xuất", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0); layout.Controls.Add(_output, 1, 0);
        var browse = new Button { Text = "Chọn thư mục", Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(92, 32), Margin = new Padding(4) }; browse.Click += (_, _) => { using var d = new FolderBrowserDialog { SelectedPath = _output.Text, Description = "Chọn thư mục cha để xuất DXF" }; if (d.ShowDialog(this) == DialogResult.OK) _output.Text = d.SelectedPath; };
        layout.Controls.Add(browse, 2, 0);
        layout.Controls.Add(new Label { Text = "File trùng tên", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1); layout.Controls.Add(_overwrite, 1, 1); layout.SetColumnSpan(_overwrite, 2);
        layout.Controls.Add(_fast, 1, 2); layout.SetColumnSpan(_fast, 2);
        layout.Controls.Add(_cleanup, 1, 3); layout.SetColumnSpan(_cleanup, 2);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 4, 10, 4), WrapContents = false };
        var run = new Button { Text = "Xem trước / Xuất DXF", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(166, 32), Margin = new Padding(4) };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(84, 32), Margin = new Padding(4) };
        _update.Margin = new Padding(12, 8, 8, 4);
        bottom.Controls.Add(run); bottom.Controls.Add(cancel); bottom.Controls.Add(_update);
        Controls.Add(layout); Controls.Add(bottom); AcceptButton = run; CancelButton = cancel;
    }

    public void ApplyTo(ExportSettings settings)
    {
        settings.OutputRoot = _output.Text.Trim(); settings.FilePattern = ExportSettings.DefaultFilePattern; settings.OverwriteMode = _overwrite.Text;
        settings.FastMode = _fast.Checked; settings.CleanupSuppressed = _cleanup.Checked; settings.UpdateInfoAndBom = _update.Checked;
        settings.DoExport = true; settings.DoConvert = false; settings.ActionIndex = 1;
    }
}
