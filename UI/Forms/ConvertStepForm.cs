using InventorDxfExporter.Core;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

public sealed class ConvertStepForm : Form
{
    private readonly TextBox _filter = new() { Dock = DockStyle.Fill };
    private readonly RadioButton _detectThickness = new()
    {
        Text = "Tự phát hiện độ dày và điền Thickness",
        AutoSize = true
    };
    private readonly RadioButton _matchRule = new()
    {
        Text = "Khớp với Sheet Metal Rule có sẵn",
        AutoSize = true
    };
    private readonly CheckBox _skip = new() { Text = "Bỏ qua Part đã có Flat Pattern", AutoSize = true };
    private readonly CheckBox _assignInnerFaceAsASide = new() { Text = "Gán A-Side vào mặt trong", AutoSize = true };
    private readonly NumericUpDown _tolerance = new()
    {
        DecimalPlaces = 3,
        Minimum = 0.001M,
        Maximum = 50,
        Increment = 0.001M,
        Width = 150,
        TextAlign = HorizontalAlignment.Right
    };
    private readonly TextBox _logFolder = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _writeLog = new() { Text = "Ghi file log sau mỗi lần Convert", AutoSize = true };

    public ConvertStepForm(ExportSettings settings)
    {
        UiStyles.Initialize(this);
        Text = "Convert STEP";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new System.Drawing.Size(720, 650);
        MinimumSize = new System.Drawing.Size(620, 470);

        _filter.Text = settings.ConvFilter;
        _filter.PlaceholderText = "Để trống để xử lý tất cả Part STEP";
        _skip.Checked = settings.ConvSkipExisting;
        _assignInnerFaceAsASide.Checked = settings.ConvAssignInnerFaceAsASide;
        _tolerance.Value = Math.Clamp((decimal)settings.ConvTolerance, _tolerance.Minimum, _tolerance.Maximum);
        _logFolder.Text = settings.ConvLogFolder;
        _logFolder.PlaceholderText = "Chọn nơi lưu nhật ký chuyển đổi";
        _writeLog.Checked = settings.ConvWriteLog;
        _detectThickness.Checked = settings.ConvMode == 1;
        _matchRule.Checked = !_detectThickness.Checked;

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
        root.Controls.Add(UiStyles.CreateHeader("Chuyển đổi STEP", "Thiết lập cách tạo Sheet Metal Part từ các tệp STEP trong Assembly."), 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 4),
            ColumnCount = 1,
            RowCount = 3
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filterSection = UiStyles.CreateSection("Phạm vi");
        var filterGrid = UiStyles.CreateFieldGrid(1);
        filterGrid.Controls.Add(UiStyles.CreateFieldLabel("Lọc tên Part"), 0, 0);
        filterGrid.Controls.Add(_filter, 1, 0);
        filterSection.Controls.Add(filterGrid);

        var modeSection = UiStyles.CreateSection("Cách thiết lập Sheet Metal");
        var modeLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 4, 8, 8),
            ColumnCount = 1,
            RowCount = 4
        };
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        modeLayout.Controls.Add(_detectThickness, 0, 0);
        modeLayout.Controls.Add(_matchRule, 0, 1);
        var toleranceRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 0)
        };
        toleranceRow.Controls.Add(UiStyles.CreateMutedLabel("Dung sai độ dày (mm)"));
        _tolerance.Margin = new Padding(12, 0, 0, 0);
        toleranceRow.Controls.Add(_tolerance);
        modeLayout.Controls.Add(toleranceRow, 0, 2);
        modeLayout.Controls.Add(_assignInnerFaceAsASide, 0, 3);
        modeSection.Controls.Add(modeLayout);

        var outputSection = UiStyles.CreateSection("Kết quả và nhật ký");
        var outputGrid = UiStyles.CreateFieldGrid(3);
        outputGrid.Controls.Add(_skip, 1, 0);
        outputGrid.SetColumnSpan(_skip, 2);
        outputGrid.Controls.Add(_writeLog, 1, 1);
        outputGrid.SetColumnSpan(_writeLog, 2);
        outputGrid.Controls.Add(UiStyles.CreateFieldLabel("Thư mục log"), 0, 2);
        outputGrid.Controls.Add(_logFolder, 1, 2);
        var browse = UiStyles.CreateFolderPicker(this, _logFolder, "Duyệt", "Chọn thư mục lưu log Convert");
        outputGrid.Controls.Add(browse, 2, 2);
        outputSection.Controls.Add(outputGrid);

        content.Controls.Add(filterSection, 0, 0);
        content.Controls.Add(modeSection, 0, 1);
        content.Controls.Add(outputSection, 0, 2);
        root.Controls.Add(content, 0, 1);

        var run = UiStyles.CreateButton("Chạy Convert", primary: true);
        run.DialogResult = DialogResult.OK;
        var cancel = UiStyles.CreateButton("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        root.Controls.Add(UiStyles.CreateFooter(run, cancel), 0, 2);
        Controls.Add(root);
        AcceptButton = run;
        CancelButton = cancel;
    }

    public void ApplyTo(ExportSettings settings)
    {
        settings.ConvFilter = _filter.Text.Trim();
        settings.ConvMode = _detectThickness.Checked ? 1 : 2;
        settings.ConvSkipExisting = _skip.Checked;
        settings.ConvAssignInnerFaceAsASide = _assignInnerFaceAsASide.Checked;
        settings.ConvTolerance = (double)_tolerance.Value;
        settings.ConvLogFolder = _logFolder.Text.Trim();
        settings.ConvWriteLog = _writeLog.Checked;
    }

}
