using InventorDxfExporter.Core;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class DxfExportForm : Form
{
    private readonly TextBox _output = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _overwrite = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _fast = new() { Text = "Tắt cập nhật giao diện khi xuất", AutoSize = true };
    private readonly CheckBox _cleanup = new() { Text = "Xóa DXF cũ của Part đang Suppress", AutoSize = true };
    private readonly CheckBox _update = new() { Text = "Cập nhật Description/Thickness và xuất BOM", AutoSize = true };

    public DxfExportForm(ExportSettings settings)
    {
        UiStyles.Initialize(this);
        Text = "Xuất DXF";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new System.Drawing.Size(720, 440);

        _output.Text = settings.OutputRoot;
        _output.PlaceholderText = "Chọn thư mục lưu DXF";
        _fast.Checked = settings.FastMode;
        _cleanup.Checked = settings.CleanupSuppressed;
        _update.Checked = settings.UpdateInfoAndBom;
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
        var outputGrid = CreateFieldGrid(2);
        outputGrid.Controls.Add(CreateFieldLabel("Thư mục xuất"), 0, 0);
        outputGrid.Controls.Add(_output, 1, 0);
        var browse = UiStyles.CreateButton("Duyệt...");
        browse.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = _output.Text,
                Description = "Chọn thư mục cha để xuất DXF",
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                _output.Text = dialog.SelectedPath;
        };
        outputGrid.Controls.Add(browse, 2, 0);
        outputGrid.Controls.Add(CreateFieldLabel("Nếu file đã tồn tại"), 0, 1);
        outputGrid.Controls.Add(_overwrite, 1, 1);
        outputGrid.SetColumnSpan(_overwrite, 2);
        outputSection.Controls.Add(outputGrid);

        var optionsSection = UiStyles.CreateSection("Tùy chọn");
        var options = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 6, 4, 2),
            ColumnCount = 1,
            RowCount = 3
        };
        options.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
        options.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
        options.RowStyles.Add(new RowStyle(SizeType.Percent, 33.4F));
        options.Controls.Add(_update, 0, 0);
        options.Controls.Add(_fast, 0, 1);
        options.Controls.Add(_cleanup, 0, 2);
        optionsSection.Controls.Add(options);

        content.Controls.Add(outputSection, 0, 0);
        content.Controls.Add(optionsSection, 0, 1);
        root.Controls.Add(content, 0, 1);

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
        root.Controls.Add(UiStyles.CreateFooter(run, cancel), 0, 2);
        Controls.Add(root);
        AcceptButton = run;
        CancelButton = cancel;
    }

    public void ApplyTo(ExportSettings settings)
    {
        settings.OutputRoot = _output.Text.Trim();
        settings.FilePattern = ExportSettings.DefaultFilePattern;
        settings.OverwriteMode = _overwrite.SelectedIndex switch
        {
            1 => "Ghi đè tự động",
            2 => "Bỏ qua file đã có",
            _ => "Hỏi từng file"
        };
        settings.FastMode = _fast.Checked;
        settings.CleanupSuppressed = _cleanup.Checked;
        settings.UpdateInfoAndBom = _update.Checked;
        settings.DoExport = true;
        settings.DoConvert = false;
        settings.ActionIndex = 1;
    }

    private static TableLayoutPanel CreateFieldGrid(int rows)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 4, 10, 6),
            ColumnCount = 3,
            RowCount = rows
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        for (var index = 0; index < rows; index++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
        return grid;
    }

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 2, 8, 2)
    };
}
