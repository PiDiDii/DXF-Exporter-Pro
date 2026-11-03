using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using InventorDxfExporter.UI.Forms;

namespace InventorDxfExporter.Preview;

internal sealed class PreviewDashboardForm : Form
{
    private readonly ListBox _forms = new() { Dock = DockStyle.Fill };
    private readonly (string Name, Action Open)[] _previews;

    public PreviewDashboardForm()
    {
        Text = "Xem trước giao diện DXF Exporter";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new System.Drawing.Size(560, 390);

        var settings = CreateSettings();
        var items = CreateItems();
        var preflight = CreatePreflightResult();
        _previews = new (string Name, Action Open)[]
        {
            ("Cài đặt xuất DXF", () => ShowDialog(new ExportSettingsForm(CreateSettings(), previewMode: true))),
            ("Xuất DXF", () => ShowDialog(new DxfExportForm(CreateSettings()))),
            ("Convert STEP", () => ShowDialog(new ConvertStepForm(CreateSettings()))),
            ("Xuất BOM", () => ShowDialog(new BomForm(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))),
            ("Danh sách xem trước DXF", () => ShowDialog(new ExportPreviewForm(items, settings))),
            ("Kiểm tra trước khi xuất", () => ShowDialog(new PreflightReviewForm(preflight))),
            ("Tiến trình thao tác", ShowProgressPreview)
        };
        foreach (var preview in _previews) _forms.Items.Add(preview.Name);
        _forms.SelectedIndex = 0;
        _forms.DoubleClick += (_, _) => OpenSelected();

        var note = new Label
        {
            Text = "Chọn một màn hình để xem. Các nút xác nhận chỉ đóng cửa sổ; không chạy xuất/Convert và không tạo file.",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        var open = new Button { Text = "Mở xem trước", AutoSize = true };
        open.Click += (_, _) => OpenSelected();
        var close = new Button { Text = "Đóng", AutoSize = true, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttons.Controls.Add(close);
        buttons.Controls.Add(open);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.Controls.Add(note, 0, 0);
        layout.Controls.Add(_forms, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
        AcceptButton = open;
        CancelButton = close;
    }

    private void OpenSelected()
    {
        if (_forms.SelectedIndex >= 0) _previews[_forms.SelectedIndex].Open();
    }

    private void ShowDialog(Form form)
    {
        using (form) form.ShowDialog(this);
    }

    private void ShowProgressPreview()
    {
        using var form = new OperationProgressForm("Xem trước tiến trình");
        using var timer = new System.Windows.Forms.Timer { Interval = 750 };
        using var cancellationRegistration = form.CancellationToken.Register(() =>
        {
            timer.Stop();
            form.MarkCanceled();
        });
        var sampleItems = new[] { "Khung trái.ipt", "Nắp trên.ipt", "Tai gá.ipt", "Thanh giằng.ipt" };
        var completed = 0;
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (form.CancellationToken.IsCancellationRequested)
            {
                form.MarkCanceled();
                return;
            }
            completed++;
            form.Report(new OperationProgress(completed, sampleItems.Length, sampleItems[completed - 1], "Đang xử lý mẫu"));
            if (completed == sampleItems.Length) form.MarkCompleted();
            else timer.Start();
        };
        form.Shown += (_, _) => timer.Start();
        form.ShowDialog(this);
        timer.Stop();
    }

    private static ExportSettings CreateSettings()
    {
        var settings = new ExportSettings
        {
            OutputRoot = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            FilePattern = ExportSettings.DefaultFilePattern,
            AddInfoText = true,
            BendMode = "Chỉ đánh dấu 2 đầu",
            TextAlignment = "Căn giữa",
            TextHeight = 10,
            TextLineGap = 15,
            TextYOffset = -250,
            ConvFilter = "BRACKET",
            ConvMode = 2,
            ConvSkipExisting = true,
            ConvTolerance = 0.01,
            ConvLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };
        settings.LayerSettings["IV_OUTER_PROFILE"] = "True";
        settings.LayerSettings["IV_INTERIOR_PROFILES"] = "True";
        settings.LayerSettings["IV_BEND_UP"] = "True";
        settings.LayerSettings["IV_BEND_DOWN"] = "True";
        return settings;
    }

    private static IReadOnlyList<ExportItem> CreateItems() =>
    [
        new(@"C:\Preview\Khung trai.ipt", "Khung trái", "SS400", "2.0", 2),
        new(@"C:\Preview\Nap tren.ipt", "Nắp trên", "SUS304", "1.5", 1),
        new(@"C:\Preview\Tai ga.ipt", "Tai gá", "SS400", "3.0", 4)
    ];

    private static ExportPreflightResult CreatePreflightResult() =>
        new(
            new[]
            {
                new ExportPreflightItem("Khung trái:1", true, true, false, false, null),
                new ExportPreflightItem("Nắp trên:1", true, false, false, false, null),
                new ExportPreflightItem("Tai gá:1", true, true, false, true, null),
                new ExportPreflightItem("Chi tiết nhập:1", false, null, false, false, "Không thể kiểm tra document mẫu.")
            },
            new[]
            {
                "Sheet Metal occurrence 'Nắp trên:1' thiếu Flat Pattern.",
                "Tên đầu ra dự kiến bị trùng."
            },
            new[] { "Inventor SelectSet chưa được áp dụng tự động." });
}
