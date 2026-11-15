using InventorDxfExporter.Services;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class PreflightReviewForm : Form
{
    public PreflightReviewForm(ExportPreflightResult result)
    {
        UiStyles.Initialize(this);
        Text = "Kiểm tra trước khi xuất";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new System.Drawing.Size(960, 620);
        ClientSize = new System.Drawing.Size(1100, 800);
        FormBorderStyle = FormBorderStyle.Sizable;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UiStyles.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Part", HeaderText = "Part / Occurrence", FillWeight = 155 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sheet", HeaderText = "Sheet Metal", FillWeight = 75 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Flat", HeaderText = "Flat Pattern", FillWeight = 78 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Suppressed", HeaderText = "Suppress", FillWeight = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duplicate", HeaderText = "Tên trùng", FillWeight = 72 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Error", HeaderText = "Chi tiết kiểm tra", FillWeight = 160 });
        foreach (var item in result.Items)
        {
            var rowIndex = grid.Rows.Add(
                item.Label,
                item.IsSheetMetal ? "Có" : "Không / chưa rõ",
                item.HasFlatPattern is null ? "—" : item.HasFlatPattern.Value ? "Có" : "Thiếu",
                item.IsSuppressed ? "Có" : "Không",
                item.HasDuplicateName ? "Có" : "Không",
                item.InspectionError ?? string.Empty);
            var row = grid.Rows[rowIndex];
            if (item.InspectionError is not null || item.HasFlatPattern == false || item.HasDuplicateName)
            {
                row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 246, 232);
                row.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 232, 199);
            }
        }

        var summary = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            Padding = new Padding(18, 8, 18, 4),
            BackColor = UiStyles.Canvas
        };
        summary.Controls.Add(CreateSummary($"{result.Items.Count} mục đã kiểm tra", UiStyles.Accent));
        summary.Controls.Add(CreateSummary($"{result.Warnings.Count} cảnh báo", result.Warnings.Count > 0
            ? System.Drawing.Color.FromArgb(181, 104, 0)
            : UiStyles.MutedText));
        summary.Controls.Add(CreateSummary($"{result.UncheckedChecks.Count} mục chưa kiểm tra", result.UncheckedChecks.Count > 0
            ? System.Drawing.Color.FromArgb(181, 104, 0)
            : UiStyles.MutedText));

        var review = new SplitContainer
        {
            Size = new System.Drawing.Size(1060, 540),
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 500,
            Panel1MinSize = 260,
            Panel2MinSize = 120,
            BackColor = UiStyles.Canvas,
            Padding = new Padding(18, 0, 18, 8)
        };
        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = UiStyles.Surface };
        gridHost.Controls.Add(grid);
        review.Panel1.Controls.Add(gridHost);

        var warningSection = UiStyles.CreateSection("Cảnh báo và các mục chưa kiểm tra");
        warningSection.Margin = Padding.Empty;
        var messages = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false,
            BackColor = UiStyles.Surface,
            ForeColor = UiStyles.Text,
            Font = UiStyles.RegularFont
        };
        foreach (var warning in result.Warnings)
            messages.Items.Add("CẢNH BÁO  ·  " + warning);
        foreach (var uncheckedCheck in result.UncheckedChecks)
            messages.Items.Add("CHƯA KIỂM TRA  ·  " + uncheckedCheck);
        if (messages.Items.Count == 0)
            messages.Items.Add("Không có cảnh báo.");
        warningSection.Controls.Add(messages);
        review.Panel2.Controls.Add(warningSection);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(UiStyles.CreateHeader("Kiểm tra trước khi xuất", "Xem các mục cần chú ý trước khi tiếp tục. Chưa có file nào được xuất ở bước này."), 0, 0);
        root.Controls.Add(summary, 0, 1);
        root.Controls.Add(review, 0, 2);

        var proceed = UiStyles.CreateButton("Tiếp tục xem trước", primary: true);
        proceed.DialogResult = DialogResult.OK;
        var cancel = UiStyles.CreateButton("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        root.Controls.Add(UiStyles.CreateFooter(proceed, cancel), 0, 3);
        Controls.Add(root);
        AcceptButton = proceed;
        CancelButton = cancel;
    }

    private static Label CreateSummary(string text, System.Drawing.Color color) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = color,
        Font = UiStyles.SemiboldFont,
        Margin = new Padding(0, 5, 24, 0)
    };
}
