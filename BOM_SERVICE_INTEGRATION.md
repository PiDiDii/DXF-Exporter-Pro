# BOM service integration

`Services/BomServiceImplementation.cs` implements `IBomService.Export(IReadOnlyCollection<ExportItem>, BomExportSettings, string, IOperationMonitor?)` using ClosedXML. It writes into a new workbook copied from the user's `.xlsx` template, using the first worksheet, configured start row, column mappings, and optional title-cell mappings for project name and export date. Unmapped cells and other worksheets remain unchanged.

The service receives grouped `ExportItem` rows from `DxfExportServiceImplementation.CollectItems`. Both collection and row preparation report progress through `IOperationMonitor` and observe cancellation between items. If the image column is enabled, it reuses an already-open part where possible; otherwise it opens the referenced file invisibly and closes only that document after reading its thumbnail. The table values come from the collected `ExportItem`; dimensions are emitted as `length x width x thickness`, without `mm`, using a comma for decimal fractions. Per-row errors are available through `BomServiceImplementation.Errors`.

The optional title fields are written only when their cell addresses are configured. Project name is the source document filename without its extension; export date is written as `dd/MM/yyyy`. The writer rejects duplicate title cells and title cells that overlap the data rows. Workbook `SaveAs` is synchronous and cannot report intermediate progress.

`PartInfoServiceImplementation` reads and updates Inventor document properties. The standalone BOM button and the DXF pipeline both call the same service composition in `Services/ServiceContainer.cs`. ClosedXML 0.104.2 is used; Microsoft Excel is not automated or required.

The user has confirmed that the add-in has been run successfully in Inventor 2027. The repository's automated validation also checks workbook output, image embedding, preservation of unmapped cells and worksheets, and that the source template is not modified. Re-run host checks when changing Inventor document open/close behavior or BOM export.
