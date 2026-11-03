# BOM service port (V14)

`Services/BomServiceImplementation.cs` implements `IBomService.Export(IReadOnlyCollection<ExportItem>, string)` using ClosedXML. It writes the V14-style `BOM` worksheet with columns `STT`, `Part number`, `Số lượng`, `Thickness`, `Description`, a bold header, auto-fit columns and filename `BOM_yyyyMMdd_HHmmss.xlsx`.

The service receives grouped `ExportItem` rows from `DxfExportServiceImplementation.CollectItems`. It reuses an already-open part where possible; otherwise it opens the referenced file invisibly and closes only that document after reading. Part Number, Thickness and Description come from `IPartInfoService.Read`, not from the export preview values. Per-row errors are available through `BomServiceImplementation.Errors`.

`PartInfoServiceImplementation` reads and updates the V14 fields. The standalone BOM button and the DXF pipeline both call the same service composition in `Services/ServiceContainer.cs`. The project references ClosedXML 0.104.2. Excel is not automated or required.

The environment does not include Windows Inventor 2027 interop or a .NET SDK, so this service and workbook output have not been compiled/runtime-tested here. On the target machine, verify workbook contents, timestamp collision behavior if multiple exports occur within one second, and COM open/close behavior for references that were already open versus newly opened.
