namespace InventorDxfExporter.Core;

public sealed record OperationProgress(int Completed, int Total, string CurrentItem, string Stage)
{
    public int Percent => Total <= 0 ? 0 : Math.Clamp((int)Math.Round(100.0 * Completed / Total), 0, 100);
}

/// <summary>Thread-affine callback contract for Inventor COM work; implementations must report between items.</summary>
public interface IOperationMonitor
{
    CancellationToken CancellationToken { get; }
    void Report(OperationProgress progress);
}

public sealed class NullOperationMonitor : IOperationMonitor
{
    public static NullOperationMonitor Instance { get; } = new();
    private NullOperationMonitor() { }
    public CancellationToken CancellationToken => CancellationToken.None;
    public void Report(OperationProgress progress) { }
}
