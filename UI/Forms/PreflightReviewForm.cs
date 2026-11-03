using InventorDxfExporter.Services;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class PreflightReviewForm : Form
{
    public PreflightReviewForm(ExportPreflightResult result)
    {
        Text = "Kiểm tra trước khi xuất"; StartPosition = FormStartPosition.CenterParent; MinimumSize = new System.Drawing.Size(720, 420);
        Size = new System.Drawing.Size(840, 520); FormBorderStyle = FormBorderStyle.Sizable;
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        grid.Columns.Add("Part", "Part / Occurrence"); grid.Columns.Add("Sheet", "Sheet Metal"); grid.Columns.Add("Flat", "Flat Pattern"); grid.Columns.Add("Suppressed", "Suppress"); grid.Columns.Add("Duplicate", "Tên trùng"); grid.Columns.Add("Error", "Lỗi kiểm tra");
        foreach (var item in result.Items)
            grid.Rows.Add(item.Label, item.IsSheetMetal ? "Có" : "Không/không rõ", item.HasFlatPattern is null ? "—" : item.HasFlatPattern.Value ? "Có" : "Thiếu", item.IsSuppressed ? "Có" : "Không", item.HasDuplicateName ? "Có" : "Không", item.InspectionError ?? string.Empty);
        var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        text.Lines = result.Warnings.Concat(result.UncheckedChecks.Select(x => "CHƯA KIỂM TRA: " + x)).ToArray();
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 280 };
        split.Panel1.Controls.Add(grid); split.Panel2.Controls.Add(text);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var proceed = new Button { Text = "Tiếp tục xem trước", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, AutoSize = true };
        bottom.Controls.Add(proceed); bottom.Controls.Add(cancel); Controls.Add(split); Controls.Add(bottom); AcceptButton = proceed; CancelButton = cancel;
    }
}
