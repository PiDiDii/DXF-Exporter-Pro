using Inventor;
using InventorDxfExporter.Core;

namespace InventorDxfExporter.Services;

public interface IConvertService
{
    OperationResult Convert(AssemblyDocument assembly, ExportSettings settings);
}

public interface IDxfExportService
{
    OperationResult Export(Document document, ExportSettings settings);
}

public interface IBomService
{
    string Export(IReadOnlyCollection<ExportItem> items, BomExportSettings settings);
}

public interface IPartInfoService
{
    PartInfo Read(PartDocument part);
    PartInfo UpdateFromFlatPattern(PartDocument part, bool saveIfDirty);
}

public sealed record PartInfo(string PartNumber, string Description, string Thickness, string Material);
