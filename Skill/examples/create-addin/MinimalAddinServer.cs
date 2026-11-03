using System.Runtime.InteropServices;
using Inventor;

namespace Inventor2027AICodingKit.Examples;

[ComVisible(true)]
[Guid("8D537E99-070F-4F4F-9D03-30F3BFB68EA1")]
[ClassInterface(ClassInterfaceType.None)]
public sealed class MinimalAddinServer : ApplicationAddInServer
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
