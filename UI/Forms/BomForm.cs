using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class BomForm : Form
{
    private readonly TextBox _output = new() { Dock = DockStyle.Fill };
    public BomForm(string outputDirectory)
    {
        Text = "Xuất BOM"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false; MaximizeBox = false; ClientSize = new System.Drawing.Size(500, 120);
        _output.Text = outputDirectory;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Thư mục BOM", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0); layout.Controls.Add(_output, 1, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(new Button { Text = "Xuất BOM", DialogResult = DialogResult.OK, AutoSize = true });
        buttons.Controls.Add(new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, AutoSize = true }); layout.Controls.Add(buttons, 1, 1);
        Controls.Add(layout); AcceptButton = buttons.Controls[0] as Button; CancelButton = buttons.Controls[1] as Button;
    }
    public string OutputDirectory => _output.Text.Trim();
}
