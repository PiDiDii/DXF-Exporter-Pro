using InventorDxfExporter.Core;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class DxfExportForm : Form
{
    private readonly TextBox _output = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _overwrite = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _fast = new() { Text = "Tắt cập nhật giao diện khi xuất", AutoSize = true };
    private readonly CheckBox _cleanup = new() { Text = "Xóa DXF cũ của Part đang Suppress", AutoSize = true };
    private readonly CheckBox _exportBom = new() { Text = "Xuất thêm BOM sau khi xuất DXF", AutoSize = true };
    private readonly CheckBox _showReport = new() { Text = "Hiện báo cáo sau khi xuất DXF", AutoSize = true };
    private readonly Button _settings = UiStyles.CreateButton("Cài đặt");
    private readonly ToolTip _toolTips = new();

    public DxfExportForm(ExportSettings settings)
    {
        UiStyles.Initialize(this);
        Text = "Xuất DXF";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new System.Drawing.Size(700, 500);
        MinimumSize = new System.Drawing.Size(700, 500);

        _output.Text = settings.OutputRoot;
        _output.PlaceholderText = "Chọn thư mục lưu DXF";
        _fast.Checked = settings.FastMode;
        _cleanup.Checked = settings.CleanupSuppressed;
        _exportBom.Checked = settings.ExportBomAfterDxf;
        _showReport.Checked = settings.ShowDxfReport;
        _toolTips.SetToolTip(_cleanup,
            "Xóa các file DXF cũ đã xuất cho các Part đang bị Suppress trong assembly khỏi thư mục xuất. " +
            "Không xóa Part hoặc Assembly trong Inventor. Khi tắt, các file DXF cũ được giữ lại và ghi cảnh báo trong báo cáo.");
        _toolTips.SetToolTip(_showReport,
            "Khi tắt, ẩn báo cáo tổng kết nếu không có lỗi. Các lỗi xuất vẫn luôn được thông báo.");
        _overwrite.Items.AddRange(new object[] { "Hỏi trước khi ghi đè", "Ghi đè tự động", "Bỏ qua file đã có" });
        _overwrite.SelectedIndex = settings.OverwriteMode switch
        {
            "Ghi đè tự động" => 1,
            "Bỏ qua file đã có" => 2,
            _ => 0
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(UiStyles.CreateHeader("Xuất DXF", "Chọn vị trí lưu và cách xử lý file trùng tên."), 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 4),
            ColumnCount = 1,
            RowCount = 2
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var outputSection = UiStyles.CreateSection("Nơi lưu và file trùng");
        var outputGrid = UiStyles.CreateFieldGrid(2);
        outputGrid.Controls.Add(UiStyles.CreateFieldLabel("Thư mục xuất"), 0, 0);
        outputGrid.Controls.Add(_output, 1, 0);
        var browse = UiStyles.CreateFolderPicker(this, _output, "Duyệt", "Chọn thư mục cha để xuất DXF");
        outputGrid.Controls.Add(browse, 2, 0);
        outputGrid.Controls.Add(UiStyles.CreateFieldLabel("Nếu file đã tồn tại"), 0, 1);
        outputGrid.Controls.Add(_overwrite, 1, 1);
        outputGrid.SetColumnSpan(_overwrite, 2);
        outputSection.Controls.Add(outputGrid);

        var optionsSection = UiStyles.CreateSection("Tùy chọn");
        var options = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 6, 4, 2),
            ColumnCount = 1,
            RowCount = 4
        };
        for (var index = 0; index < 4; index++)
            options.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        options.Controls.Add(_exportBom, 0, 0);
        options.Controls.Add(_showReport, 0, 1);
        options.Controls.Add(_fast, 0, 2);
        options.Controls.Add(_cleanup, 0, 3);
        optionsSection.Controls.Add(options);

        content.Controls.Add(outputSection, 0, 0);
        content.Controls.Add(optionsSection, 0, 1);
        root.Controls.Add(content, 0, 1);

        _settings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        var run = UiStyles.CreateButton("Tiếp tục", primary: true);
        run.DialogResult = DialogResult.None;
        run.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_output.Text))
            {
                MessageBox.Show(this, "Hãy chọn thư mục xuất DXF.", "Thiếu thư mục", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _output.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        var cancel = UiStyles.CreateButton("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        root.Controls.Add(UiStyles.CreateFooter(run, cancel, _settings), 0, 2);
        Controls.Add(root);
        AcceptButton = run;
        CancelButton = cancel;
    }

    public event EventHandler? SettingsRequested;

    public void ApplyTo(ExportSettings settings)
    {
        settings.OutputRoot = _output.Text.Trim();
        settings.OverwriteMode = _overwrite.SelectedIndex switch
        {
            1 => "Ghi đè tự động",
            2 => "Bỏ qua file đã có",
            _ => "Hỏi từng file"
        };
        settings.FastMode = _fast.Checked;
        settings.CleanupSuppressed = _cleanup.Checked;
        settings.ExportBomAfterDxf = _exportBom.Checked;
        settings.ShowDxfReport = _showReport.Checked;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTips.Dispose();
        base.Dispose(disposing);
    }
}
