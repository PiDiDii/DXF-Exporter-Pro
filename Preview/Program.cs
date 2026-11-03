namespace InventorDxfExporter.Preview;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new PreviewDashboardForm());
    }
}
