using InventorDxfExporter.Core;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class BomPreviewForm : Form
{
    public BomPreviewForm(IReadOnlyList<ExportItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        UiStyles.Initialize(this);
        Text = "Xem trước danh sách BOM";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new System.Drawing.Size(720, 420);
        ClientSize = new System.Drawing.Size(900, 520);
        FormBorderStyle = FormBorderStyle.Sizable;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UiStyles.StyleGrid(grid);
        grid.Columns.Add(CreateColumn("Index", "STT", 45, 45));
        grid.Columns.Add(CreateColumn("PartNumber", "Part Number", 180, 130));
        grid.Columns.Add(CreateColumn("Material", "Vật liệu", 120, 90));
        grid.Columns.Add(CreateColumn("Quantity", "Số lượng", 70, 60));
        grid.Columns.Add(CreateColumn("Thickness", "Độ dày (mm)", 85, 75));
        grid.Columns.Add(CreateColumn("Size", "Kích thước bao (mm)", 160, 140));
        grid.Columns["Index"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.Columns["Quantity"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.Columns["Thickness"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        long totalQuantity = 0;
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            grid.Rows.Add(
                index + 1,
                item.PartNumber ?? item.DetailName,
                item.Material,
                item.Quantity,
                BomDimensionFormatter.FormatThicknessDisplay(item.Thickness),
                BomDimensionFormatter.Format(item.FlatPatternXmm, item.FlatPatternYmm, item.Thickness));
            totalQuantity += item.Quantity;
            if (!string.IsNullOrWhiteSpace(item.FlatPatternSizeIssue))
                grid.Rows[index].Cells["Size"].ToolTipText = item.FlatPatternSizeIssue;
        }

        var summary = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Text = $"Số loại chi tiết: {items.Count}    Tổng số lượng: {totalQuantity}",
            ForeColor = UiStyles.Text,
            Margin = new Padding(2, 0, 8, 0)
        };
        var close = UiStyles.CreateButton("Đóng");
        close.DialogResult = DialogResult.Cancel;
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(12, 8, 16, 8),
            BackColor = UiStyles.Surface,
            Margin = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        footer.Controls.Add(summary, 0, 0);
        footer.Controls.Add(close, 1, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 12, 12, 4),
            BackColor = UiStyles.Canvas
        };
        gridHost.Controls.Add(grid);
        root.Controls.Add(gridHost, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
        CancelButton = close;
    }

    private static DataGridViewTextBoxColumn CreateColumn(
        string name,
        string header,
        float fillWeight,
        int minimumWidth) => new()
    {
        Name = name,
        HeaderText = header,
        FillWeight = fillWeight,
        MinimumWidth = minimumWidth,
        SortMode = DataGridViewColumnSortMode.NotSortable
    };
}
