using InventorDxfExporter.Core;
using System.Globalization;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class ConvertStepForm : Form
{
    private readonly TextBox _filter = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _mode = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _skip = new() { Text = "Bỏ qua Part đã có Flat Pattern", AutoSize = true };
    private readonly NumericUpDown _tolerance = new() { DecimalPlaces = 3, Minimum = 0.001M, Maximum = 50, Increment = 0.001M, Width = 110 };
    private readonly TextBox _logFolder = new() { Dock = DockStyle.Fill };

    public ConvertStepForm(ExportSettings settings)
    {
        Text = "Convert STEP"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false; MaximizeBox = false; ClientSize = new System.Drawing.Size(760, 320);
        _filter.Text = settings.ConvFilter; _skip.Checked = settings.ConvSkipExisting;
        _tolerance.Value = Math.Clamp((decimal)settings.ConvTolerance, _tolerance.Minimum, _tolerance.Maximum);
        _logFolder.Text = settings.ConvLogFolder;
        _mode.Items.AddRange(new object[] { "1. Tự phát hiện độ dày, điền Thickness", "2. Khớp Sheet Metal Rule có sẵn" });
        _mode.SelectedIndex = settings.ConvMode == 1 ? 0 : 1;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 3, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        foreach (var row in new[] { 0, 1, 2, 3, 4 }) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        layout.Controls.Add(new Label { Text = "Lọc tên Part", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0); layout.Controls.Add(_filter, 1, 0); layout.SetColumnSpan(_filter, 2);
        layout.Controls.Add(new Label { Text = "Chế độ", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1); layout.Controls.Add(_mode, 1, 1); layout.SetColumnSpan(_mode, 2);
        layout.Controls.Add(new Label { Text = "Dung sai (mm)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2); layout.Controls.Add(_tolerance, 1, 2);
        layout.Controls.Add(_skip, 1, 3);
        layout.Controls.Add(new Label { Text = "Thư mục log", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4); layout.Controls.Add(_logFolder, 1, 4);
        var browse = new Button { Text = "Chọn thư mục", Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(92, 32), Margin = new Padding(4) };
        browse.Click += (_, _) => { using var dialog = new FolderBrowserDialog { SelectedPath = _logFolder.Text, Description = "Chọn thư mục lưu log Convert" }; if (dialog.ShowDialog(this) == DialogResult.OK) _logFolder.Text = dialog.SelectedPath; };
        layout.Controls.Add(browse, 2, 4);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 4, 10, 4), WrapContents = false };
        var run = new Button { Text = "Chạy Convert", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(136, 32), Margin = new Padding(4) };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(84, 32), Margin = new Padding(4) };
        buttons.Controls.Add(run); buttons.Controls.Add(cancel); Controls.Add(layout); Controls.Add(buttons); AcceptButton = run; CancelButton = cancel;
    }

    public void ApplyTo(ExportSettings settings)
    {
        settings.ConvFilter = _filter.Text.Trim(); settings.ConvMode = _mode.SelectedIndex + 1; settings.ConvSkipExisting = _skip.Checked;
        settings.ConvTolerance = (double)_tolerance.Value; settings.ConvLogFolder = _logFolder.Text.Trim();
        settings.DoConvert = true; settings.DoExport = false; settings.ActionIndex = 0;
    }
}
