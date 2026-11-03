using System.Runtime.InteropServices;
using Inventor;

namespace Inventor2027Addin;

[ComVisible(true)]
[Guid("91949afe-d3ca-457f-bcf9-752b61f14af4")]
[ClassInterface(ClassInterfaceType.None)]
public sealed class Inventor2027AddinServer : ApplicationAddInServer
{
    private Application? _application;

    public void Activate(ApplicationAddInSite AddInSiteObject, bool FirstTime)
    {
        ArgumentNullException.ThrowIfNull(AddInSiteObject);
        _application = AddInSiteObject.Application;
    }

    public void Deactivate()
    {
        _application = null;
    }

    public void ExecuteCommand(int CommandID)
    {
    }

    public object Automation => null!;
}
