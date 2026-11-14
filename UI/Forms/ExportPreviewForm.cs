using InventorDxfExporter.Core;
using System.Globalization;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class ExportPreviewForm : Form
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        EditMode = DataGridViewEditMode.EditOnEnter
    };
    private readonly Label _selectionSummary = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        ForeColor = UiStyles.Text,
        Margin = new Padding(2, 0, 8, 0)
    };
    private readonly Button _selectAll = UiStyles.CreateButton("Chọn/Bỏ chọn tất cả");
    private readonly Button _settings = UiStyles.CreateButton("Cài đặt");
    private readonly Button _export = UiStyles.CreateButton("Xuất DXF", primary: true);
    private readonly Button _close = UiStyles.CreateButton("Đóng");

    public ExportPreviewForm(IReadOnlyList<ExportItem> items, ExportSettings settings)
    {
        UiStyles.Initialize(this);
        Text = "Danh sách chi tiết xuất DXF";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new System.Drawing.Size(820, 460);
        ClientSize = new System.Drawing.Size(920, 550);
        FormBorderStyle = FormBorderStyle.Sizable;

        ConfigureGrid();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var rowIndex = _grid.Rows.Add(
                index + 1,
                true,
                item.DetailName,
                item.Material,
                item.Quantity,
                FormatThickness(item.Thickness),
                FormatFlatPatternSize(item));
            var row = _grid.Rows[rowIndex];
            row.Tag = item.DocumentPath;
            if (!string.IsNullOrWhiteSpace(item.FlatPatternSizeIssue))
                row.Cells["Size"].ToolTipText = item.FlatPatternSizeIssue;
        }

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1)
                UpdateSelectionSummary();
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 12, 12, 4),
            BackColor = UiStyles.Canvas
        };
        gridHost.Controls.Add(_grid);
        root.Controls.Add(gridHost, 0, 0);

        var footer = CreateFooter(items.Count);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
        AcceptButton = _export;
        CancelButton = _close;
        UpdateSelectionSummary();
    }

    public event EventHandler? SettingsRequested;

    public IReadOnlyList<string> SelectedDocumentPaths
    {
        get
        {
            _grid.EndEdit();
            return _grid.Rows.Cast<DataGridViewRow>()
                .Where(row => Convert.ToBoolean(row.Cells["Include"].Value ?? false))
                .Select(row => row.Tag as string)
                .Where(path => !string.IsNullOrEmpty(path))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    private void ConfigureGrid()
    {
        UiStyles.StyleGrid(_grid);
        _grid.BackgroundColor = System.Drawing.Color.FromArgb(160, 160, 160);
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        _grid.GridColor = System.Drawing.Color.FromArgb(110, 110, 110);
        _grid.RowTemplate.Height = 27;
        _grid.ColumnHeadersHeight = 38;
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _grid.DefaultCellStyle.SelectionBackColor = UiStyles.Accent;
        _grid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Index",
            HeaderText = "STT",
            FillWeight = 42,
            MinimumWidth = 45,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Include",
            HeaderText = "Xuất",
            FillWeight = 48,
            MinimumWidth = 50,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Detail",
            HeaderText = "Tên chi tiết",
            FillWeight = 190,
            MinimumWidth = 150,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Material",
            HeaderText = "Vật liệu",
            FillWeight = 120,
            MinimumWidth = 100,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Quantity",
            HeaderText = "Số lượng",
            FillWeight = 65,
            MinimumWidth = 62,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Thickness",
            HeaderText = "Độ dày (mm)",
            FillWeight = 78,
            MinimumWidth = 82,
            ReadOnly = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Size",
            HeaderText = "Kích thước bao (mm)",
            FillWeight = 155,
            MinimumWidth = 145,
            ReadOnly = true
        });

        foreach (var columnName in new[] { "Index", "Include", "Quantity", "Thickness", "Size" })
            _grid.Columns[columnName]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.Columns["Index"]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.Columns["Include"]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
    }

    private Control CreateFooter(int itemCount)
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 4, 12, 4),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiStyles.Canvas
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _selectionSummary.Text = $"Đã chọn: 0 / {itemCount}";
        _selectionSummary.Dock = DockStyle.Fill;
        _selectionSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _selectAll.Click += (_, _) =>
        {
            var selectedCount = CountSelected();
            SetAll(selectedCount != _grid.Rows.Count);
        };
        _settings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        _export.DialogResult = DialogResult.OK;
        _close.DialogResult = DialogResult.Cancel;
        ConfigureFooterButton(_selectAll, 184);
        ConfigureFooterButton(_settings, 108);
        ConfigureFooterButton(_export, 112);
        ConfigureFooterButton(_close, 98);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        actions.Controls.Add(_close);
        actions.Controls.Add(_export);
        actions.Controls.Add(_settings);
        actions.Controls.Add(_selectAll);
        footer.Controls.Add(_selectionSummary, 0, 0);
        footer.Controls.Add(actions, 1, 0);
        return footer;
    }

    private static void ConfigureFooterButton(Button button, int width)
    {
        button.AutoSize = false;
        button.Size = new System.Drawing.Size(width, 38);
        button.Margin = new Padding(6, 2, 0, 2);
    }

    private static string FormatThickness(string value)
    {
        var displayValue = value.StartsWith('T') ? value[1..] : value;
        if (double.TryParse(displayValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var thickness))
            return thickness.ToString("0.###", CultureInfo.CurrentCulture);
        return displayValue.Equals("_Khong_Xac_Dinh", StringComparison.OrdinalIgnoreCase)
            ? "Chưa xác định"
            : displayValue;
    }

    private static string FormatFlatPatternSize(ExportItem item)
    {
        if (item.FlatPatternXmm is { } xMm && item.FlatPatternYmm is { } yMm)
        {
            var x = xMm.ToString("0.#", CultureInfo.CurrentCulture);
            var y = yMm.ToString("0.#", CultureInfo.CurrentCulture);
            return $"{x} x {y}";
        }

        return item.FlatPatternSizeIssue ?? "—";
    }

    private void SetAll(bool value)
    {
        foreach (DataGridViewRow row in _grid.Rows)
            row.Cells["Include"].Value = value;
        UpdateSelectionSummary();
    }

    private int CountSelected() => _grid.Rows
        .Cast<DataGridViewRow>()
        .Count(row => Convert.ToBoolean(row.Cells["Include"].Value ?? false));

    private void UpdateSelectionSummary()
    {
        var selected = CountSelected();
        _selectionSummary.Text = $"Đã chọn: {selected} / {_grid.Rows.Count}";
        _export.Enabled = selected > 0;
    }
}
