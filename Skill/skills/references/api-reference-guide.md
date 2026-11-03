# Inventor 2027 API Reference Guide

## Choose evidence in this order

1. Installed Inventor 2027 API Help (`admapi_31_0.chm`).
2. The matching installed `Autodesk.Inventor.Interop.dll` and `.xml`.
3. Version-specific DeveloperTools samples and Object Model Chart.
4. Autodesk-hosted 2027 Programming Interface Help and Autodesk DevBlog.
5. Third-party references/community code for discovery only.

Read [`references/API_SOURCES.md`](../../../references/API_SOURCES.md) for source locations and authority. The inspected machine has DeveloperTools MSI but its samples are not installed; do not claim to have checked them.

## Verification procedure

For each API call, record:

- Release: Inventor 2027.
- Namespace and object/interface.
- Exact member and C# signature; parameter names/order/types, return type, enum values.
- Applicable document or object context and state preconditions.
- Source title, direct URL (or installed file/topic), source class, and verification status.
- Compile result and Inventor runtime result as separate checks.

Use the local interop metadata to confirm C#-visible COM types and delegate/event interfaces. Do not mechanically translate VBA's omitted optional arguments into C#; imported COM methods may expose all arguments.

## Statuses

- **Verified from Inventor 2027 Reference**
- **Verified from Inventor 2027 SDK Sample**
- **Compile Verified**
- **Inventor Runtime Tested**
- **Unverified**

Statuses are independent. In particular, reference verification does not prove compile success or runtime behavior. Unknown details remain unknown; if no exact 2027 source is available, state: "API này chưa được xác minh trong tài liệu Inventor 2027."

## Core flow reference map

| Workflow | Start here |
|---|---|
| Add-in entry/lifetime | `ApplicationAddInServer`, `ApplicationAddInSite.Application` |
| Current document | `Application.ActiveDocument`, `DocumentTypeEnum`, specific `PartDocument` / `AssemblyDocument` / `DrawingDocument` |
| Assembly iteration | `AssemblyDocument.ComponentDefinition` -> `AssemblyComponentDefinition.Occurrences` -> `ComponentOccurrences.AllLeafOccurrences` |
| Sheet-metal flat pattern | `PartDocument.ComponentDefinition` -> `SheetMetalComponentDefinition.HasFlatPattern` / `FlatPattern` / `Unfold` |
| Data export | Context-specific `DataIO`; query supported formats before calling `WriteDataToFile` |
| Translator export | `TranslatorAddIn.HasSaveCopyAsOptions` -> configure translation context/options/data target -> `SaveCopyAs` |
| Commands and Ribbon | `CommandManager.ControlDefinitions` -> `ButtonDefinition`; `UserInterfaceManager.Ribbons` -> tabs -> panels -> controls |
| Events | `ApplicationEvents`, `DocumentEvents`, and the matching `_Sink_Event` interface in C# interop |

Use [`API_INDEX.md`](../../../api/API_INDEX.md) for signatures and links, and [`object-model/README.md`](../../../api/object-model/README.md) for the object hierarchy.
