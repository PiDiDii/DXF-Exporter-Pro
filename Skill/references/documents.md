# Documents

## Start with the actual document

`Application.ActiveDocument` may be unavailable when no document is open. Check for null before using it, then use the document-type API or a typed COM cast to confirm the supported type. Do not cast an arbitrary active document to a part or assembly.

Verified 2027 entry points include `Application.ActiveDocument`, `Application.Documents`, and `Documents.Open(string FullDocumentName, bool OpenVisible)`. `Documents.Open` opens a file and has visible/open-state effects; it is not a read-only query.

## Lifecycle and ownership

- Decide whether the operation acts on an already-open document, opens a new document, or changes the active document.
- Do not close a document the user already had open unless the command explicitly owns that lifecycle.
- For batch processing, preserve the user's active document and restore application state only through documented APIs.
- Before saving, exporting, or changing geometry, report which file is being acted on.
- Handle unsaved documents, missing references, non-modifiable documents, and cancelled user interactions as explicit outcomes.

## Document-specific branches

- Part: `PartDocument.ComponentDefinition` gives the part component definition; check for `SheetMetalComponentDefinition` before sheet-metal operations.
- Assembly: `AssemblyDocument.ComponentDefinition` returns an assembly definition for a standard assembly; weldment definitions may differ.
- Drawing: access `DrawingDocument.Sheets` and sheet-specific collections such as `PartsLists`.

## Example

[`examples/drawing-bom/DrawingBomExample.cs`](../../../examples/drawing-bom/DrawingBomExample.cs) checks a typed drawing and reads the active sheet's parts-list count. It does not modify or create a BOM.

## Sources

- Autodesk Inventor 2027 [Application](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/Application.htm), [Documents.Open](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/Documents_Open.htm), [PartDocument.ComponentDefinition](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/PartDocument_ComponentDefinition.htm), and [AssemblyDocument.ComponentDefinition](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/AssemblyDocument_ComponentDefinition.htm).
- Member evidence and status are in [`API_INDEX.md`](../../../api/API_INDEX.md).
