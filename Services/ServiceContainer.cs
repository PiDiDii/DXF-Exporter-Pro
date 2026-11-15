using Inventor;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Services;

/// <summary>Application-lifetime composition root for Inventor export services.</summary>
public sealed class ServiceContainer
{
    public IPartInfoService PartInfo { get; }
    public BomServiceImplementation Bom { get; }
    public DxfExportServiceImplementation DxfExport { get; }
    public ConvertServiceImplementation Convert { get; }

    public ServiceContainer(InventorApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        PartInfo = new PartInfoServiceImplementation();
        Bom = new BomServiceImplementation(application);
        DxfExport = new DxfExportServiceImplementation(application, Bom);
        Convert = new ConvertServiceImplementation(application);
    }
}
