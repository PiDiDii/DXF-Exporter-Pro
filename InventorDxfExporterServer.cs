using System.Runtime.InteropServices;
using Inventor;
using InventorDxfExporter.Commands;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter;

[ComVisible(true)]
[Guid("6B93E5C1-9A6A-4D1B-8FA9-47567B4A1D20")]
[ClassInterface(ClassInterfaceType.None)]
public sealed class InventorDxfExporterServer : ApplicationAddInServer
{
    private InventorApplication? _application;
    private RibbonCommandSet? _commands;

    public void Activate(ApplicationAddInSite AddInSiteObject, bool FirstTime)
    {
        ArgumentNullException.ThrowIfNull(AddInSiteObject);
        _application = AddInSiteObject.Application;
        try
        {
            _commands = new RibbonCommandSet(_application);
            _commands.Activate(FirstTime);
        }
        catch
        {
            _commands?.Deactivate();
            _commands = null;
            _application = null;
            throw;
        }
    }

    public void Deactivate()
    {
        _commands?.Deactivate();
        _commands = null;
        _application = null;
    }

    public void ExecuteCommand(int CommandID) { }

    public object Automation => this;
}
