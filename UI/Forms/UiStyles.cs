using System.Drawing;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

internal static class UiStyles
{
    public static readonly Font RegularFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font SemiboldFont = new("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font ProgressFont = new("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font TitleFont = new("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Color Canvas = Color.FromArgb(245, 247, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Text = Color.FromArgb(35, 44, 56);
    public static readonly Color MutedText = Color.FromArgb(103, 113, 126);
    public static readonly Color Border = Color.FromArgb(222, 227, 233);
    public static readonly Color Accent = Color.FromArgb(35, 105, 175);
    public static readonly Color AccentLight = Color.FromArgb(226, 239, 251);

    public static void Initialize(Form form)
    {
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.Font = RegularFont;
        form.BackColor = Canvas;
        form.ForeColor = Text;
    }

    public static Control CreateHeader(string title, string description)
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(18, 12, 18, 10),
            Margin = Padding.Empty
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = TitleFont,
            ForeColor = Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = description,
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 1);
        header.Controls.Add(layout);
        return header;
    }

    public static GroupBox CreateSection(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ForeColor = Text,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    public static FlowLayoutPanel CreateFooter(params Control[] controls)
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(12, 8, 16, 8),
            BackColor = Surface,
            Margin = Padding.Empty
        };
        foreach (var control in controls)
        {
            control.Margin = new Padding(6, 2, 0, 2);
            footer.Controls.Add(control);
        }
        return footer;
    }

    public static Button CreateButton(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(94, 34),
            Padding = new Padding(10, 4, 10, 4),
            UseVisualStyleBackColor = !primary,
            FlatStyle = primary ? FlatStyle.Flat : FlatStyle.System
        };
        if (primary)
        {
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
        }
        return button;
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = Border;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(239, 243, 247);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = SemiboldFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 34;
        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = AccentLight;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
        grid.RowHeadersVisible = false;
        grid.AllowUserToResizeRows = false;
    }

    public static Label CreateMutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = MutedText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 2, 0, 2)
        };
    }

    public static TableLayoutPanel CreateFieldGrid(int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
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

    public static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 2, 8, 2)
    };

    public static Button CreateFolderPicker(
        Form owner,
        TextBox path,
        string buttonText,
        string description)
    {
        var button = CreateButton(buttonText);
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = path.Text,
                Description = description,
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog(owner) == DialogResult.OK)
                path.Text = dialog.SelectedPath;
        };
        return button;
    }
}
