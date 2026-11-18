# Inventor API Verification Rules

This is the evidence gate for code, examples, reviews, and AI-generated API guidance. A plausible member name, an API from another Inventor release, or a successful web search is not verification.

## Required verification workflow

Before using an Inventor API:

1. Confirm the target is Inventor 2027 and inspect the actual project target framework, references, add-in manifest, and architecture. Do not infer project configuration from installed SDKs.
2. Find the exact 2027 object/interface and member in the installed Inventor 2027 API Help or SDK.
3. Confirm the C#-visible spelling, overload, parameter order/types, optional arguments, return type, enum names/values, and applicable document/object context. For COM APIs, check the interop projection rather than translating VBA syntax by guesswork.
4. Check preconditions, state requirements, lifecycle/thread constraints, exceptions, and failure behavior where documented.
5. Prefer a version-specific Autodesk SDK sample when usage patterns or object navigation are involved. Samples supplement, but do not replace, the API reference.
6. Record the source, exact topic/member, Inventor version, and evidence status in the example or API index.
7. Build the actual code against the intended project configuration. A metadata lookup is not a compile test.
8. Run host-dependent behavior in Inventor 2027 when available. Record it separately from compile and automated-test results.

If authoritative evidence for the exact claim is unavailable, state exactly:

> API này chưa được xác minh trong tài liệu Inventor 2027.

Do not publish an invented signature as a working example. You may describe the intended behavior at a high level while clearly marking the API as unverified.

## Evidence status labels

Assign statuses per API/member or example, not to an entire document. Multiple statuses may apply independently:

- **Verified from Inventor 2027 Reference** — the exact member/signature was located in the installed 2027 API Help or an accessible, clearly versioned Inventor 2027 API reference. Record the page/topic and evidence.
- **Verified from Inventor 2027 SDK Sample** — the named member and usage were observed in a version-specific Autodesk Inventor 2027 SDK sample. Record the sample path/name.
- **Compile Verified** — the relevant code was built against the intended project configuration and Inventor 2027 interop/reference assemblies. Record the command/result. This does not imply host runtime success.
- **Inventor Runtime Tested** — the behavior was executed in Inventor 2027. Record the host version/build and test conditions.
- **Unverified** — one or more required facts lack sufficient evidence. Do not present an exact signature or behavior as confirmed.

Never mark an API **Compile Verified** based only on reflection or XML docs, and never mark it **Inventor Runtime Tested** without actually running it in Inventor.

## Sprint 01 evidence ledger

Environment inspected on 2026-11-02. Local paths are machine-specific.

| API / topic | Inventor 2027 evidence | Result | Status |
|---|---|---|---|
| `Inventor.ApplicationAddInServer` | Installed `Autodesk.Inventor.Interop.xml`, topic `T:Inventor.ApplicationAddInServer`; `Autodesk.Inventor.Interop.dll` version `31.10.26700.0` | Interface exists in the installed 2027 interop assembly. | Verified from Inventor 2027 Reference |
| `ApplicationAddInServer.Activate` | XML topic `M:Inventor.ApplicationAddInServer.Activate(Inventor.ApplicationAddInSite,System.Boolean)` and reflection on installed interop assembly | `void Activate(Inventor.ApplicationAddInSite AddInSiteObject, bool FirstTime)` | Verified from Inventor 2027 Reference |
| `ApplicationAddInServer.Deactivate` | XML topic `M:Inventor.ApplicationAddInServer.Deactivate` and reflection on installed interop assembly | `void Deactivate()` | Verified from Inventor 2027 Reference |
| `ApplicationAddInServer.ExecuteCommand` | XML topic `M:Inventor.ApplicationAddInServer.ExecuteCommand(System.Int32)` and reflection on installed interop assembly | `void ExecuteCommand(int CommandID)` | Verified from Inventor 2027 Reference |
| `ApplicationAddInServer.Automation` | XML topic `P:Inventor.ApplicationAddInServer.Automation` and reflection on installed interop assembly | C# property returns `object`; XML describes it as the add-in's automation interface object. | Verified from Inventor 2027 Reference |
| `Inventor.ApplicationAddInSite.Application` | XML topic `P:Inventor.ApplicationAddInSite.Application` and reflection on installed interop assembly | C# property returns `Inventor.Application`; XML notes that the top-level application depends on whether the context is Inventor or Apprentice. | Verified from Inventor 2027 Reference |

The local API Help file was present at `C:\Users\Public\Documents\Autodesk\Inventor 2027\Local Help\admapi_31_0.chm`. The interop XML is installed beside the DLL in `C:\Program Files\Autodesk\Inventor 2027\Bin`. Reflection confirmed the signatures above. The DeveloperTools MSI was present but its samples were not installed/inspected.

At the Sprint 01 checkpoint, **Compile Verified** and **Inventor Runtime Tested** were unclaimed. No sample project or API test harness existed at that checkpoint. Sprint 02 added the shared project and compile evidence below; it still did not load or run an add-in in Inventor.

The installed 2027 `What's New` topic includes a separate 2027.1 section. Reflection against interop version `31.10.26700.0` confirmed symbol presence for representative update members `ComponentDefinition.HasDesignProblems`, `AssemblyComponentDefinition.IsContentMember`, `BOMView.Renumber2`, `BOMRow.Thumbnail`, `PresentationDocument.InsertModel`, `BreakOperations.AddWithOptions`, and `BreakOperation.AlignedCurve`. This confirms symbol presence only, not complete signatures or runtime behavior.

## Sprint 02 compile evidence

The shared sample project targets `net10.0-windows` and references the inspected Inventor 2027 interop assembly (`31.10.26700.0`) from `C:\Program Files\Autodesk\Inventor 2027\Bin`.

| API / example surface | Compile evidence | Status |
|---|---|---|
| `ApplicationAddInServer` methods and `ApplicationAddInSite.Application` | `MinimalAddinServer.cs` compiled in `examples/Inventor2027.Examples.csproj` | Compile Verified |
| `Application.CommandManager.ControlDefinitions.AddButtonDefinition` and `ButtonDefinitionSink_Event.OnExecute` | `RibbonCommandExample.cs` compiled, including the event delegate subscription | Compile Verified |
| `Ribbon`, `RibbonTabs.Add`, `RibbonPanels.Add`, and `CommandControls.AddButton` | `RibbonCommandExample.cs` compiled against the 2027 interop assembly | Compile Verified |
| `ComponentOccurrences.AllLeafOccurrences[Type.Missing]`, enumerator indexer, and `ComponentOccurrence.Suppressed` | `AssemblyTraversalExample.cs` compiled; this confirmed the C# collection indexer projection | Compile Verified |
| `PartDocument.ComponentDefinition`, `SheetMetalComponentDefinition.HasFlatPattern`, `.FlatPattern`, `.DataIO`, and `DataIO.WriteDataToFile` | Flat-pattern and DXF examples compiled against Inventor 2027 interop | Compile Verified |
| `DrawingDocument.ActiveSheet.PartsLists.Count` | `DrawingBomExample.cs` compiled against Inventor 2027 interop | Compile Verified |

Build command:

```powershell
dotnet build .\examples\Inventor2027.Examples.csproj
```

Result: **Build succeeded; 0 warnings, 0 errors.** This only confirms compile compatibility with the installed interop assembly and .NET 10 reference packs. The examples were not registered, loaded, or executed inside Inventor; all **Inventor Runtime Tested** statuses remain unclaimed. The indexer has a separate Python unit-test suite; the Inventor examples have no automated host tests.

## Sprint 03 validation

The standalone template at `templates/addin-template/` targets `net10.0-windows` and references the same installed Inventor 2027 interop assembly. Its `Inventor2027AddinServer.cs` implements the verified `ApplicationAddInServer` contract. The `.addin` XML was parsed, and its `ClassId`/`ClientId` were checked against the class `Guid` attribute.

| Validation | Command/check | Result |
|---|---|---|
| Examples build | `dotnet build .\examples\Inventor2027.Examples.csproj` | Succeeded; 0 warnings, 0 errors |
| Standalone template build | `dotnet build .\templates\addin-template\Inventor2027Addin.csproj` | Succeeded; 0 warnings, 0 errors |
| Indexer unit tests | `python -m unittest discover -s .\tools\api-documentation-indexer -p 'test_*.py' -v` | 3 tests passed |
| Python syntax | `python -m py_compile .\tools\api-documentation-indexer\index_api_docs.py .\tools\api-documentation-indexer\test_index_api_docs.py` | Succeeded |
| Markdown and Skill checks | Local links and Agent Skill frontmatter across the kit | 33 Markdown files checked; no broken local links; frontmatter valid |
| Add-in manifest | XML parse and manifest/class GUID consistency | Passed |

Builds used Inventor 2027 interop version `31.10.26700.0`. Neither project was loaded or runtime-tested in Inventor. The SDK DeveloperTools samples were not installed/inspected; compilation and manifest parsing do not prove host loading or deployment behavior.

The public Autodesk Help links in the catalog were opened during the final link review. The third-party `inventor.autodeskapi.dev` reference still returned HTTP 402 and was not used as verification evidence. Direct network checks from the local shell were blocked by the environment; this is not evidence that an external link is broken.

## Ribbon cleanup validation

The retired Assembly/Part utility tabs are removed by exact internal name through `RibbonTabs.Count`, its 1-based `Item` indexer, and `RibbonTab.Delete`. These members were verified in the installed Inventor 2027 `Autodesk.Inventor.Interop.xml` topics `P:Inventor.RibbonTabs.Count`, `P:Inventor.RibbonTabs.Item`, and `M:Inventor.RibbonTab.Delete`, and the add-in compiled against interop version `31.10.26700.0`.

| Validation | Command/check | Result |
|---|---|---|
| Main add-in build after UI refresh | `dotnet build E:\DXF Exporter Pro\InventorDxfExporter.csproj --no-restore` with isolated temporary intermediate/output paths and the existing workspace assets file | Succeeded; 0 warnings, 0 errors |
| Ribbon tab cleanup runtime | Not run inside Inventor 2027 | Unverified; restart/reload the add-in in Inventor to confirm the persisted tab is removed |

## Flat-pattern bounding-size display

| API / topic | Inventor 2027 evidence | Result | Status |
|---|---|---|---|
| `FlatPattern.RangeBox`, `Box.MinPoint` / `MaxPoint`, `Point.X` / `Y` / `Z` | Autodesk Inventor 2027 API Help and installed `Autodesk.Inventor.Interop.xml` topics `P:Inventor.FlatPattern.RangeBox`, `P:Inventor.Box.MinPoint`, `P:Inventor.Box.MaxPoint`, and `P:Inventor.Point.X` / `Y` / `Z` | Read the existing flat-pattern bounding box without creating or changing a flat pattern; display the two largest of its three extents. | Verified from Inventor 2027 Reference |
| `Document.UnitsOfMeasure`, `UnitsOfMeasure.ConvertUnits`, `UnitsTypeEnum.kDatabaseLengthUnits`, `UnitsTypeEnum.kMillimeterLengthUnits` | Autodesk Inventor 2027 API Help and installed interop XML topics `P:Inventor.Document.UnitsOfMeasure`, `M:Inventor.UnitsOfMeasure.ConvertUnits(System.Double,System.Object,System.Object)`, and the two enum fields | Convert database-unit X/Y extents to millimeters. The 2027 enum reference identifies database length units as centimeters. | Verified from Inventor 2027 Reference; Compile Verified |

The export list reports “Chưa có Flat Pattern” when a part has no existing flat pattern. It does not call `Unfold` merely to populate the list. COM failures reading the bounding box appear in that row's tooltip.

## BOM document-thumbnail export

| API / topic | Inventor 2027 evidence | Result | Status |
|---|---|---|---|
| `Document.Thumbnail` | Autodesk Inventor 2027 API Help, [`Document.Thumbnail`](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/Document_Thumbnail.htm), and installed `Autodesk.Inventor.Interop.xml` topic `P:Inventor.Document.Thumbnail`; the installed interop projects the property as `stdole.IPictureDisp` | Reads the saved document preview in the in-process add-in; it is not a fresh render of the current view. `stdole.dll` from the Inventor 2027 Bin is referenced for the COM type. | Verified from Inventor 2027 Reference; Compile Verified |
| `Documents.Open` and `Document.Close` | Autodesk Inventor 2027 API Help and installed interop XML topics `M:Inventor.Documents.Open(System.String,System.Boolean)` and `M:Inventor.Document.Close(System.Boolean)` | Opens only parts not already open, with `OpenVisible = false`; closes only parts opened for this export, using `SkipSave = true`. | Verified from Inventor 2027 Reference; Compile Verified |

| Validation | Command/check | Result |
|---|---|---|
| Add-in build | `dotnet build .\InventorDxfExporter.csproj --no-restore` | Succeeded; 0 warnings, 0 errors |
| BOM/DXF validation | `dotnet run --project .\Tests\DxfProcessorValidation\DxfProcessorValidation.csproj` | Passed, including workbook picture embedding and migration of legacy image settings |
| Inventor runtime | Not run inside Inventor 2027 | Unverified; confirm thumbnail availability and appearance in the host, especially for unsaved or thumbnail-less parts |

## External iLogic command integration

| API / topic | Inventor 2027 evidence | Result | Status |
|---|---|---|---|
| `IiLogicAutomation.RunExternalRule` | Installed Autodesk Inventor 2027 `Autodesk.iLogic.Interfaces.xml` topic `M:Autodesk.iLogic.Interfaces.IiLogicAutomation.RunExternalRule(Inventor.Document,System.String)` and reflection on `Autodesk.iLogic.Interfaces.dll` | `int RunExternalRule(Inventor.Document, System.String)`; zero means success; accepts a rule filename with a valid extension and absolute path. Add-in build compiled the new command against the installed 2027 interface assembly. | Verified from Inventor 2027 Reference; Compile Verified |
| `ApplicationAddIns` / `ApplicationAddIn` | Installed `Autodesk.Inventor.Interop.dll`, version `31.10.26700.0`, inspected by reflection | `ApplicationAddIns.Count`, `Item[int]`, and `ApplicationAddIn.DisplayName`, `ShortDisplayName`, `Description`, `Activated`, `Automation`, and `Activate()` are present and used to find/activate iLogic. Add-in build compiled the discovery and activation code. | Verified from Inventor 2027 Reference; Compile Verified |

## Part Number / Browser Name command integration

| API / topic | Inventor 2027 evidence | Result | Status |
|---|---|---|---|
| `ComponentOccurrence.Name` | Autodesk Inventor 2027 API Help, [`ComponentOccurrence.Name`](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ComponentOccurrence_Name.htm), plus installed interop XML topic `P:Inventor.ComponentOccurrence.Name` | Read/write string property used to preview and update the browser label while preserving the occurrence suffix. | Verified from Inventor 2027 Reference; Compile Verified |
| `Document.PropertySets` / `Property.Value` | Autodesk Inventor 2027 API Help, [`Document.PropertySets`](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/Document_PropertySets.htm) and [`Property.Value`](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/Property_Value.htm); installed interop reflection/XML | `Document.PropertySets` returns `PropertySets`; the Design Tracking Properties and Part Number items return a `Property`; `Property.Value` is `object` and read/write. | Verified from Inventor 2027 Reference; Compile Verified |
| `Document.FullDocumentName`, `Document.SubType`, `ComponentOccurrence.Definition` / `SubOccurrences`, `ComponentOccurrencesEnumerator.Count` / `Item` | Installed Inventor 2027 `Autodesk.Inventor.Interop.xml` and reflection on interop version `31.10.26700.0` | Used to deduplicate documents, identify sheet-metal subtype, and recursively traverse unsuppressed assembly occurrences in stable browser order. | Verified from Inventor 2027 Reference; Compile Verified |
| `DesignProject.LibraryPaths` / `ProjectPaths` / `ProjectPath.Path` / `DesignProject.ContentCenterPath` | Installed Inventor 2027 `Autodesk.Inventor.Interop.xml` topics `P:Inventor.DesignProject.LibraryPaths`, `P:Inventor.ProjectPaths.Count`, `P:Inventor.ProjectPaths.Item`, `P:Inventor.ProjectPath.Path`, and `P:Inventor.DesignProject.ContentCenterPath` | `ProjectPaths` is read by its 1-based `Count`/`Item` collection; each `ProjectPath.Path` and the Content Center path are normalized and compared to component document paths. | Verified from Inventor 2027 Reference; Compile Verified |
| `PartComponentDefinition.IsiPartMember` / `IsiPartFactory` / `IsContentMember`; `AssemblyComponentDefinition.IsiAssemblyMember` / `IsiAssemblyFactory` / `IsContentMember` | Installed Inventor 2027 `Autodesk.Inventor.Interop.xml` topics for the named properties | Boolean properties used to exclude iPart/iAssembly factories and members, and identify Content Center parts/assemblies. | Verified from Inventor 2027 Reference; Compile Verified |
| Modeless WinForms owner (`Application.MainFrameHWND`) | Installed Inventor 2027 interop metadata; add-in build | The Part Number form is shown modeless with an `IWin32Window` owner wrapping Inventor's main frame handle; the preview form supplies sample data and never accesses Inventor COM. | Verified from Inventor 2027 Reference; Compile Verified; not Inventor Runtime Tested |

Build command:

```powershell
dotnet build .\InventorDxfExporter.csproj --no-restore
```

Result: **Build succeeded; 0 warnings, 0 errors.** The command has not been loaded or runtime-tested in Inventor 2027. The 3D document-open/view workflow was intentionally removed; the grid now shows text and status only.

## Evidence record template

```text
API:
Inventor version:
Topic/member:
Exact C# signature (only after verification):
Applicable document/object context:
Source title and URL or installed file/topic:
Evidence status:
Compile command/result:
Inventor runtime test/result:
Limitations:
```
