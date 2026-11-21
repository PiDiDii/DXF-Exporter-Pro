using Inventor;
using InventorDxfExporter.Core;
using InventorDxfExporter.UI.Forms;
using System.Windows.Forms;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Commands;

public sealed class ExportStepCommand
{
    private readonly InventorApplication _application;

    public ExportStepCommand(InventorApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public void Execute()
    {
        var document = _application.ActiveDocument;
        if (document is not AssemblyDocument && document is not PartDocument)
        {
            MessageBox.Show(new InventorWindowOwner(_application.MainFrameHWND),
                "Xuất STEP chỉ chạy trên Assembly hoặc Part.",
                "Xuất STEP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new StepExportForm(_application, document);
        form.ShowDialog(new InventorWindowOwner(_application.MainFrameHWND));
    }
}
