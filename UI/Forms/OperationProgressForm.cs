using InventorDxfExporter.Core;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

/// <summary>Modeless progress surface. Inventor COM remains on the caller's STA; report between items only.</summary>
public sealed class OperationProgressForm : Form, IOperationMonitor
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly IntPtr _inventorHandle;
    private readonly int _uiThreadId = Environment.CurrentManagedThreadId;
    private readonly ProgressBar _bar = new()
    {
        Dock = DockStyle.Fill,
        Minimum = 0,
        Maximum = 100,
        Style = ProgressBarStyle.Continuous
    };
    private readonly Label _stage = new() { Dock = DockStyle.Fill, AutoEllipsis = true, Text = "Đang chuẩn bị..." };
    private readonly Label _item = new() { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = UiStyles.MutedText };
    private readonly Button _cancel = UiStyles.CreateButton("Hủy");
    private bool _completed;
    private bool _pumpingMessages;

    public OperationProgressForm(string title, IWin32Window? inventorOwner = null)
    {
        _inventorHandle = inventorOwner?.Handle ?? IntPtr.Zero;
        UiStyles.Initialize(this);
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ControlBox = false;
        ShowInTaskbar = false;
        ClientSize = new System.Drawing.Size(500, 260);

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
        root.Controls.Add(UiStyles.CreateHeader(title, "Có thể hủy thao tác; tiến trình sẽ dừng sau mục hiện tại."), 0, 0);

        var section = UiStyles.CreateSection("Tiến trình hiện tại");
        section.Margin = new Padding(18, 18, 18, 8);
        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 8),
            ColumnCount = 1,
            RowCount = 3
        };
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _stage.Font = UiStyles.ProgressFont;
        details.Controls.Add(_stage, 0, 0);
        details.Controls.Add(_bar, 0, 1);
        details.Controls.Add(_item, 0, 2);
        section.Controls.Add(details);
        root.Controls.Add(section, 0, 1);

        _cancel.Click += (_, _) =>
        {
            if (_completed || _cancellation.IsCancellationRequested)
            {
                Close();
                return;
            }
            _cancel.Enabled = false;
            _stage.Text = "Đang dừng sau mục hiện tại...";
            _cancellation.Cancel();
        };
        root.Controls.Add(UiStyles.CreateFooter(_cancel), 0, 2);
        Controls.Add(root);
        CancelButton = _cancel;
    }

    public CancellationToken CancellationToken => _cancellation.Token;

    public void Report(OperationProgress progress)
    {
        if (IsDisposed || !IsHandleCreated)
            return;
        _bar.Value = progress.Percent;
        _stage.Text = $"{progress.Stage} — {progress.Completed}/{progress.Total}";
        _item.Text = progress.CurrentItem;
        _stage.Refresh();
        _item.Refresh();
        _bar.Refresh();
        PumpProgressMessages();
        _cancellation.Token.ThrowIfCancellationRequested();
    }

    private void PumpProgressMessages()
    {
        if (Environment.CurrentManagedThreadId != _uiThreadId)
            throw new InvalidOperationException("Tiến trình Inventor phải được cập nhật trên luồng UI đã tạo form.");
        if (_pumpingMessages)
            throw new InvalidOperationException("Không cho phép gọi lồng cập nhật tiến trình.");

        _pumpingMessages = true;
        var hostWasEnabled = _inventorHandle != IntPtr.Zero
            && IsWindow(_inventorHandle)
            && IsWindowEnabled(_inventorHandle);
        if (hostWasEnabled)
            EnableWindow(_inventorHandle, false);
        try
        {
            // Keep cancellation responsive without allowing ribbon commands to re-enter Inventor work.
            System.Windows.Forms.Application.DoEvents();
        }
        finally
        {
            if (hostWasEnabled && IsWindow(_inventorHandle))
                EnableWindow(_inventorHandle, true);
            _pumpingMessages = false;
        }
    }

    public void MarkCompleted()
    {
        _completed = true;
        if (IsDisposed)
            return;
        _bar.Value = 100;
        _stage.Text = "Hoàn tất.";
        _item.Text = "Thao tác đã hoàn tất.";
        _cancel.Text = "Đóng";
        _cancel.Enabled = true;
    }

    public void MarkCanceled()
    {
        _completed = true;
        if (IsDisposed)
            return;
        _stage.Text = "Đã dừng.";
        _item.Text = "Các mục đã xử lý trước đó được giữ nguyên.";
        _cancel.Text = "Đóng";
        _cancel.Enabled = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_completed && !_cancellation.IsCancellationRequested)
            _cancellation.Cancel();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cancellation.Dispose();
        base.Dispose(disposing);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
