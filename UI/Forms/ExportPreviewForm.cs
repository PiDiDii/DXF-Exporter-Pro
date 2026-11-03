using InventorDxfExporter.Core;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class ExportPreviewForm : Form
{
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    public ExportPreviewForm(IReadOnlyList<ExportItem> items, ExportSettings settings)
    {
        Text = "Xem trước danh sách xuất"; StartPosition = FormStartPosition.CenterParent; MinimumSize = new System.Drawing.Size(760, 420);
        Size = new System.Drawing.Size(920, 520); FormBorderStyle = FormBorderStyle.Sizable;
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Include", HeaderText = "Xuất", FillWeight = 40 });
        _grid.Columns.Add("File", "Tên file DXF"); _grid.Columns.Add("Material", "Vật liệu"); _grid.Columns.Add("Thickness", "Độ dày"); _grid.Columns.Add("Quantity", "Số lượng");
        _grid.Columns[1].FillWeight = 240; _grid.Columns[2].FillWeight = 110; _grid.Columns[3].FillWeight = 80; _grid.Columns[4].FillWeight = 70;
        foreach (var item in items)
        {
            var name = FileNameBuilder.BuildFileName(settings.FilePattern, item.Thickness, item.Quantity, item.Material, item.DetailName);
            _grid.Rows.Add(true, name, item.Material, item.Thickness, item.Quantity);
            _grid.Rows[^1].Tag = item.DocumentPath;
        }
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(8, 4, 4, 4) };
        var all = new Button { Text = "Chọn tất cả", AutoSize = true }; all.Click += (_, _) => SetAll(true);
        var none = new Button { Text = "Bỏ chọn tất cả", AutoSize = true }; none.Click += (_, _) => SetAll(false);
        top.Controls.Add(all); top.Controls.Add(none);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var go = new Button { Text = "Tiếp tục xuất", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, AutoSize = true };
        bottom.Controls.Add(go); bottom.Controls.Add(cancel);
        Controls.Add(_grid); Controls.Add(top); Controls.Add(bottom); AcceptButton = go; CancelButton = cancel;
    }

    public IReadOnlyList<string> SelectedDocumentPaths
    {
        get
        {
            _grid.EndEdit();
            return _grid.Rows.Cast<DataGridViewRow>().Where(r => Convert.ToBoolean(r.Cells[0].Value ?? false))
                .Select(r => r.Tag as string).Where(s => !string.IsNullOrEmpty(s)).Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    private void SetAll(bool value) { foreach (DataGridViewRow row in _grid.Rows) row.Cells[0].Value = value; }
}
