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
    private readonly CheckBox _projectNameEnabled = new() { Text = "Tên dự án", AutoSize = true };
    private readonly TextBox _projectNameColumn = new() { Width = 72 };
    private readonly TextBox _projectNameRow = new() { Width = 72 };
    private readonly CheckBox _exportDateEnabled = new() { Text = "Ngày tháng năm", AutoSize = true };
    private readonly TextBox _exportDateColumn = new() { Width = 72 };
    private readonly TextBox _exportDateRow = new() { Width = 72 };
    private readonly DataGridView _mappingsGrid = new();
    private readonly Action<BomExportSettings> _saveSettings;

    public BomForm(BomExportSettings settings, Action<BomExportSettings> saveSettings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
        UiStyles.Initialize(this);
        Text = "Xuất Bảng kê";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        LoadValues();
        ClientSize = new System.Drawing.Size(760, 650);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(UiStyles.CreateHeader(
            "Xuất Bảng kê",
            "Chọn file mẫu, thiết lập các trường và xuất bảng kê."), 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 10, 18, 8),
            ColumnCount = 1,
            RowCount = 2
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(BuildFileSection(), 0, 0);
        content.Controls.Add(BuildMainContent(), 0, 1);
        root.Controls.Add(content, 0, 1);

        var save = UiStyles.CreateButton("Lưu cài đặt");
        save.Click += (_, _) => SaveSettings();
        var export = UiStyles.CreateButton("Xuất BOM", primary: true);
        export.Click += (_, _) => Export();
        var close = UiStyles.CreateButton("Đóng");
        close.DialogResult = DialogResult.Cancel;
        root.Controls.Add(UiStyles.CreateFooter(export, save, close), 0, 2);
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
        var browse = UiStyles.CreateFolderPicker(this, _outputDirectory, "Duyệt...", "Chọn thư mục lưu file BOM");
        browse.AutoSize = false;
        browse.Dock = DockStyle.Fill;
        browse.Margin = new Padding(3);
        grid.Controls.Add(browse, 2, 1);
        section.Controls.Add(grid);
        return section;
    }

    private Control BuildMetadataSection()
    {
        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty
        };
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
        details.Controls.Add(BuildMetadataCellSection(
            "Tên dự án", _projectNameEnabled, _projectNameColumn, _projectNameRow), 0, 0);
        details.Controls.Add(BuildMetadataCellSection(
            "Ngày tháng năm", _exportDateEnabled, _exportDateColumn, _exportDateRow), 0, 1);
        details.Controls.Add(BuildStartRow(), 0, 2);
        return details;
    }

    private Control BuildMainContent()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        grid.Controls.Add(BuildColumnsSection(), 0, 0);
        grid.Controls.Add(BuildMetadataSection(), 1, 0);
        return grid;
    }

    private static Control BuildMetadataCellSection(
        string title,
        CheckBox enabled,
        TextBox column,
        TextBox row)
    {
        var section = UiStyles.CreateSection(title);
        section.Padding = new Padding(10, 4, 10, 4);
        section.Margin = new Padding(0, 0, 0, 4);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.Controls.Add(CreateHeaderLabel(""), 0, 0);
        grid.Controls.Add(CreateHeaderLabel("Cột ghi"), 1, 0);
        grid.Controls.Add(CreateHeaderLabel("Dòng ghi"), 2, 0);
        enabled.Anchor = AnchorStyles.Left;
        enabled.Margin = new Padding(2, 0, 0, 0);
        column.Dock = DockStyle.Fill;
        column.Margin = new Padding(6, 2, 6, 2);
        row.Dock = DockStyle.Fill;
        row.Margin = new Padding(6, 2, 6, 2);
        column.PlaceholderText = "Ví dụ: C";
        row.PlaceholderText = "Ví dụ: 2";
        grid.Controls.Add(enabled, 0, 1);
        grid.Controls.Add(column, 1, 1);
        grid.Controls.Add(row, 2, 1);
        enabled.CheckedChanged += (_, _) =>
        {
            column.Enabled = enabled.Checked;
            row.Enabled = enabled.Checked;
        };
        section.Controls.Add(grid);
        return section;
    }

    private Control BuildColumnsSection()
    {
        var section = UiStyles.CreateSection("Giá trị xuất bảng kê");
        var savedMappings = _settings.Columns
            .Where(mapping => BomExportSettings.SupportedFields.Contains(
                mapping.Field, StringComparer.OrdinalIgnoreCase))
            .GroupBy(mapping => mapping.Field, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var mappings = BomExportSettings.CreateDefaultColumns()
            .Select(defaultMapping => savedMappings.TryGetValue(defaultMapping.Field, out var savedMapping)
                ? new BomColumnMapping(defaultMapping.Field, savedMapping.Enabled, savedMapping.Column)
                : defaultMapping)
            .ToList();

        _mappingsGrid.Dock = DockStyle.Fill;
        _mappingsGrid.AutoGenerateColumns = false;
        _mappingsGrid.AllowUserToAddRows = false;
        _mappingsGrid.AllowUserToDeleteRows = false;
        _mappingsGrid.AllowUserToResizeColumns = false;
        _mappingsGrid.AllowUserToResizeRows = false;
        _mappingsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _mappingsGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _mappingsGrid.ColumnHeadersHeight = 28;
        _mappingsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _mappingsGrid.RowTemplate.Height = 32;
        _mappingsGrid.RowTemplate.MinimumHeight = 32;
        _mappingsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _mappingsGrid.MultiSelect = false;
        _mappingsGrid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _mappingsGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Ghi",
            Width = 48,
            ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _mappingsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Field",
            HeaderText = "Giá trị",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
        });
        _mappingsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Column",
            HeaderText = "Cột ghi",
            Width = 88,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        UiStyles.StyleGrid(_mappingsGrid);
        _mappingsGrid.ColumnHeadersHeight = 28;
        _mappingsGrid.RowTemplate.Height = 32;
        _mappingsGrid.RowTemplate.MinimumHeight = 32;

        foreach (var mapping in mappings)
            _mappingsGrid.Rows.Add(mapping.Enabled, mapping.Field, mapping.Column);

        section.Controls.Add(_mappingsGrid);
        return section;
    }

    private Control BuildStartRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(12, 0, 12, 0),
            Margin = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var label = new Label
        {
            Text = "Dòng bắt đầu ghi",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0)
        };
        _startRow.Dock = DockStyle.None;
        _startRow.Width = 100;
        _startRow.Height = 24;
        _startRow.Anchor = AnchorStyles.Right;
        _startRow.Margin = Padding.Empty;
        row.Controls.Add(label, 0, 0);
        row.Controls.Add(_startRow, 1, 0);
        return row;
    }

    private void LoadValues()
    {
        _template.Text = _settings.TemplatePath;
        _outputDirectory.Text = _settings.OutputDirectory;
        _startRow.Text = _settings.StartRow.ToString(CultureInfo.InvariantCulture);
        SetMetadataCell(_settings.ProjectNameCell, _projectNameEnabled, _projectNameColumn, _projectNameRow);
        SetMetadataCell(_settings.ExportDateCell, _exportDateEnabled, _exportDateColumn, _exportDateRow);
    }

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
        if (!TryBuildMetadataCell(_projectNameEnabled, _projectNameColumn, _projectNameRow,
                "Tên dự án", out var projectNameCell)
            || !TryBuildMetadataCell(_exportDateEnabled, _exportDateColumn, _exportDateRow,
                "Ngày tháng năm", out var exportDateCell))
            return false;
        if (!string.IsNullOrEmpty(projectNameCell)
            && projectNameCell.Equals(exportDateCell, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "Ngày và Tên dự án phải được ghi vào hai ô Excel khác nhau.",
                "Ô tiêu đề bị trùng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        _settings.TemplatePath = _template.Text.Trim();
        _settings.OutputDirectory = _outputDirectory.Text.Trim();
        _settings.StartRow = startRow.Value;
        _settings.ProjectNameCell = projectNameCell;
        _settings.ExportDateCell = exportDateCell;
        _mappingsGrid.EndEdit();
        _settings.Columns = _mappingsGrid.Rows
            .Cast<DataGridViewRow>()
            .Select(row => new BomColumnMapping(
                Convert.ToString(row.Cells["Field"].Value, CultureInfo.InvariantCulture) ?? string.Empty,
                row.Cells["Enabled"].Value is true,
                Convert.ToString(row.Cells["Column"].Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty))
            .ToList();

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

    private static void SetMetadataCell(
        string address,
        CheckBox enabled,
        TextBox column,
        TextBox row)
    {
        enabled.Checked = !string.IsNullOrWhiteSpace(address);
        var separator = address.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
        column.Text = separator < 0 ? address : address[..separator];
        row.Text = separator < 0 ? string.Empty : address[separator..];
        column.Enabled = enabled.Checked;
        row.Enabled = enabled.Checked;
    }

    private bool TryBuildMetadataCell(
        CheckBox enabled,
        TextBox column,
        TextBox row,
        string label,
        out string address)
    {
        address = string.Empty;
        if (!enabled.Checked)
            return true;

        var columnName = column.Text.Trim().ToUpperInvariant();
        try
        {
            _ = BomTemplateWriter.ColumnNumber(columnName);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, $"{label}: cột Excel không hợp lệ",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            column.Focus();
            return false;
        }

        if (!int.TryParse(row.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var rowNumber)
            || rowNumber is < 1 or > 1_048_576)
        {
            ShowInputError($"{label}: dòng phải là số từ 1 đến 1048576.", row);
            return false;
        }

        address = columnName + rowNumber.ToString(CultureInfo.InvariantCulture);
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
        Font = UiStyles.SemiboldFont
    };

    private static TextBox CreatePathTextBox() => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Margin = new Padding(3, 4, 3, 4),
        BorderStyle = BorderStyle.FixedSingle,
        ScrollBars = ScrollBars.Horizontal,
        WordWrap = false,
        MinimumSize = new System.Drawing.Size(0, 0)
    };
}
