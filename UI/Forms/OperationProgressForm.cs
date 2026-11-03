using InventorDxfExporter.Core;
using System.Windows.Forms;

namespace InventorDxfExporter.UI.Forms;

/// <summary>Modeless progress surface. Inventor COM remains on the caller's STA; report between items only.</summary>
public sealed class OperationProgressForm : Form, IOperationMonitor
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ProgressBar _bar = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };
    private readonly Label _stage = new() { Dock = DockStyle.Fill, AutoEllipsis = true, Text = "Đang chuẩn bị..." };
    private readonly Label _item = new() { Dock = DockStyle.Fill, AutoEllipsis = true };
    private readonly Button _cancel = new() { Text = "Hủy", AutoSize = true };
    private bool _completed;

    public OperationProgressForm(string title)
    {
        Text = title; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false; MaximizeBox = false; ControlBox = false; ShowInTaskbar = false;
        ClientSize = new System.Drawing.Size(520, 135);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_stage, 0, 0); layout.Controls.Add(_bar, 0, 1); layout.Controls.Add(_item, 0, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        _cancel.Click += (_, _) =>
        {
            if (_completed || _cancellation.IsCancellationRequested)
            {
                Close();
                return;
            }
            _cancel.Enabled = false;
            _stage.Text = "Đang dừng sau item hiện tại...";
            _cancellation.Cancel();
        };
        buttons.Controls.Add(_cancel); layout.Controls.Add(buttons, 0, 3); Controls.Add(layout);
        CancelButton = _cancel;
    }

    public CancellationToken CancellationToken => _cancellation.Token;

    public void Report(OperationProgress progress)
    {
        if (IsDisposed || !IsHandleCreated) return;
        _bar.Value = progress.Percent;
        _stage.Text = $"{progress.Stage} — {progress.Completed}/{progress.Total}";
        _item.Text = progress.CurrentItem;
        // Processes the Cancel click while keeping the Inventor call sequence on its original STA.
        System.Windows.Forms.Application.DoEvents();
        _cancellation.Token.ThrowIfCancellationRequested();
    }

    public void MarkCompleted()
    {
        _completed = true;
        if (!IsDisposed) { _bar.Value = 100; _stage.Text = "Hoàn tất."; _cancel.Text = "Đóng"; _cancel.Enabled = true; }
    }

    public void MarkCanceled()
    {
        _completed = true;
        if (!IsDisposed)
        {
            _stage.Text = "Đã dừng.";
            _cancel.Text = "Đóng";
            _cancel.Enabled = true;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_completed && !_cancellation.IsCancellationRequested) _cancellation.Cancel();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _cancellation.Dispose();
        base.Dispose(disposing);
    }
}
