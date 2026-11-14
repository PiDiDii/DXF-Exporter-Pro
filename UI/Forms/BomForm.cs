using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using System.Globalization;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class BomForm : Form
{
    private readonly BomExportSettings _settings;
    private readonly TextBox _template = CreatePathTextBox();
    private readonly TextBox _outputDirectory = CreatePathTextBox();
    private readonly TextBox _startRow = new() { Width = 72 };
    private readonly Dictionary<string, (CheckBox Enabled, TextBox Column)> _mappings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action<BomExportSettings> _saveSettings;

    public BomForm(BomExportSettings settings, Action<BomExportSettings> saveSettings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
        UiStyles.Initialize(this);
        Text = "Xuất Bảng kê";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        MinimumSize = new System.Drawing.Size(620, 550);
        ClientSize = new System.Drawing.Size(404, 600);

        LoadValues();
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(UiStyles.CreateHeader(
            "Xuất Bảng kê",
            "Chọn file Excel mẫu, ánh xạ các trường và thư mục lưu file kết quả."), 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 12, 18, 4),
            ColumnCount = 1,
            RowCount = 4
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 252));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(BuildFileSection(), 0, 0);
        content.Controls.Add(BuildColumnsSection(), 0, 1);
        content.Controls.Add(BuildStartRow(), 0, 2);
        root.Controls.Add(content, 0, 1);

        var save = UiStyles.CreateButton("Lưu cài đặt");
        save.Click += (_, _) => SaveSettings();
        var export = UiStyles.CreateButton("Xuất BOM", primary: true);
        export.Click += (_, _) => Export();
        var close = UiStyles.CreateButton("Đóng");
        close.DialogResult = DialogResult.Cancel;
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(12, 8, 16, 8),
            BackColor = UiStyles.Surface,
            Margin = Padding.Empty
        };
        foreach (var button in new[] { export, save, close })
        {
            button.Margin = new Padding(6, 2, 0, 2);
            footer.Controls.Add(button);
        }
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
        AcceptButton = export;
        CancelButton = close;
    }

    private Control BuildFileSection()
    {
        var section = UiStyles.CreateSection("File mẫu và nơi lưu");
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 2, 8, 2),
            ColumnCount = 3,
            RowCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.Controls.Add(CreateLabel("File Excel mẫu"), 0, 0);
        grid.Controls.Add(_template, 1, 0);
        var chooseTemplate = UiStyles.CreateButton("Chọn file...");
        chooseTemplate.AutoSize = false;
        chooseTemplate.Dock = DockStyle.Fill;
        chooseTemplate.Margin = new Padding(3);
        chooseTemplate.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                CheckFileExists = true,
                Title = "Chọn file Excel mẫu Bảng kê"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                _template.Text = dialog.FileName;
        };
        grid.Controls.Add(chooseTemplate, 2, 0);

        grid.Controls.Add(CreateLabel("Thư mục lưu"), 0, 1);
        grid.Controls.Add(_outputDirectory, 1, 1);
        var browse = UiStyles.CreateButton("Duyệt...");
        browse.AutoSize = false;
        browse.Dock = DockStyle.Fill;
        browse.Margin = new Padding(3);
        browse.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = _outputDirectory.Text,
                Description = "Chọn thư mục lưu file BOM",
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                _outputDirectory.Text = dialog.SelectedPath;
        };
        grid.Controls.Add(browse, 2, 1);
        section.Controls.Add(grid);
        return section;
    }

    private Control BuildColumnsSection()
    {
        var section = UiStyles.CreateSection("Giá trị xuất bảng kê");
        section.Dock = DockStyle.Fill;
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 0, 4, 0),
            ColumnCount = 3,
            RowCount = BomExportSettings.CreateDefaultColumns().Count + 1
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        grid.Controls.Add(CreateHeaderLabel("Ghi"), 0, 0);
        var valueHeader = CreateHeaderLabel("Giá trị");
        valueHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        grid.Controls.Add(valueHeader, 1, 0);
        grid.Controls.Add(CreateHeaderLabel("Cột ghi"), 2, 0);

        var rowIndex = 1;
        foreach (var defaultMapping in BomExportSettings.CreateDefaultColumns())
        {
            var mapping = GetMapping(defaultMapping.Field);
            var enabled = new CheckBox
            {
                Checked = mapping.Enabled,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(3)
            };
            var column = new TextBox
            {
                Text = mapping.Column,
                Width = 78,
                Anchor = AnchorStyles.None,
                Margin = new Padding(3)
            };
            var fieldLabel = CreateLabel(defaultMapping.Field);
            fieldLabel.Dock = DockStyle.Fill;
            fieldLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            fieldLabel.Margin = Padding.Empty;
            _mappings[defaultMapping.Field] = (enabled, column);
            grid.Controls.Add(enabled, 0, rowIndex);
            grid.Controls.Add(fieldLabel, 1, rowIndex);
            grid.Controls.Add(column, 2, rowIndex);
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            rowIndex++;
        }
        section.Controls.Add(grid);
        return section;
    }

    private Control BuildStartRow()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(8, 8, 0, 0)
        };
        row.Controls.Add(CreateLabel("Dòng bắt đầu ghi các giá trị"));
        _startRow.Margin = new Padding(12, 0, 0, 0);
        row.Controls.Add(_startRow);
        return row;
    }

    private void LoadValues()
    {
        _template.Text = _settings.TemplatePath;
        _outputDirectory.Text = _settings.OutputDirectory;
        _startRow.Text = _settings.StartRow.ToString(CultureInfo.InvariantCulture);
    }

    private BomColumnMapping GetMapping(string field) =>
        _settings.Columns.FirstOrDefault(mapping => mapping.Field.Equals(field, StringComparison.OrdinalIgnoreCase))
        ?? BomExportSettings.CreateDefaultColumns().First(mapping => mapping.Field.Equals(field, StringComparison.OrdinalIgnoreCase));

    private void SaveSettings()
    {
        if (!TryApplySettings())
            return;
        try
        {
            _saveSettings(_settings);
            MessageBox.Show(this, "Đã lưu cài đặt xuất BOM.", "Cài đặt BOM", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Không lưu được cài đặt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Export()
    {
        if (!TryApplySettings())
            return;
        DialogResult = DialogResult.OK;
        Close();
    }

    private bool TryApplySettings()
    {
        var startRow = ParseRow(_startRow, "Dòng bắt đầu");
        if (startRow is null)
            return false;
        if (string.IsNullOrWhiteSpace(_template.Text) || !File.Exists(_template.Text))
        {
            ShowInputError("Chọn file Excel mẫu (.xlsx) tồn tại.", _template);
            return false;
        }
        if (string.IsNullOrWhiteSpace(_outputDirectory.Text))
        {
            ShowInputError("Chọn thư mục lưu file BOM.", _outputDirectory);
            return false;
        }
        _settings.TemplatePath = _template.Text.Trim();
        _settings.OutputDirectory = _outputDirectory.Text.Trim();
        _settings.StartRow = startRow.Value;
        _settings.Columns = _mappings.Select(pair => new BomColumnMapping(
            pair.Key, pair.Value.Enabled.Checked, pair.Value.Column.Text.Trim())).ToList();

        try
        {
            foreach (var mapping in _settings.Columns.Where(mapping => mapping.Enabled))
                _ = BomTemplateWriter.ColumnNumber(mapping.Column);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Cột Excel không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_settings.Columns.Where(mapping => mapping.Enabled)
            .GroupBy(mapping => mapping.Column, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            MessageBox.Show(this, "Mỗi trường BOM phải được gán vào một cột Excel khác nhau.", "Cột bị trùng",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    private int? ParseRow(TextBox input, string label)
    {
        if (int.TryParse(input.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            && value is >= 1 and <= 1_048_576)
            return value;
        ShowInputError($"{label} phải là số từ 1 đến 1048576.", input);
        return null;
    }

    private void ShowInputError(string message, Control control)
    {
        MessageBox.Show(this, message, "Cài đặt BOM chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        control.Focus();
    }

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(2, 2, 8, 2)
    };

    private static Label CreateHeaderLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
        Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold)
    };

    private static TextBox CreatePathTextBox() => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Margin = new Padding(3, 4, 3, 4),
        BorderStyle = BorderStyle.FixedSingle,
        MinimumSize = new System.Drawing.Size(0, 0)
    };
}
