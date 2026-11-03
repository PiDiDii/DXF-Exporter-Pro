# Sheet Metal and Flat Patterns

## Verified members

The installed Inventor 2027 reference documents:

- `SheetMetalComponentDefinition.HasFlatPattern : bool`
- `SheetMetalComponentDefinition.FlatPattern : FlatPattern` (the help says it returns Nothing when no flat pattern exists)
- `SheetMetalComponentDefinition.Unfold()`
- `SheetMetalComponentDefinition.Unfold2(Face)`
- `SheetMetalComponentDefinition.DataIO : DataIO`
- `FlatPattern.DataIO : DataIO`

Source: [SheetMetalComponentDefinition](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SheetMetalComponentDefinition.htm), [FlatPattern.DataIO](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/FlatPattern_DataIO.htm), and installed 2027 interop XML. The exact C# signature for `Unfold2` is `FlatPattern Unfold2(Face BaseFace)` in the interop metadata; confirm the reference topic's preconditions before calling it.

## Safe workflow

1. Verify the input document is a part and its component definition is sheet metal.
2. Check `HasFlatPattern` before reading `FlatPattern`.
3. If missing, decide whether to create one. `Unfold` is a model operation; do not create it silently in a read-only exporter.
4. If choosing a base face, verify the `Unfold2(Face)` behavior and face validity for the current model.
5. Export from the documented DataIO context; file formats are context-dependent.
6. Report errors and per-part outcomes. Do not swallow COM exceptions.

## Example

[`examples/sheet-metal-flat-pattern/SheetMetalFlatPatternExample.cs`](../../../examples/sheet-metal-flat-pattern/SheetMetalFlatPatternExample.cs) only inspects an existing flat pattern and does not create one.

## Sources

- Autodesk Inventor 2027 [SheetMetalComponentDefinition.HasFlatPattern](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SheetMetalComponentDefinition_HasFlatPattern.htm), [SheetMetalComponentDefinition.Unfold](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SheetMetalComponentDefinition_Unfold.htm), [FlatPattern.DataIO](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/FlatPattern_DataIO.htm).
- Autodesk Inventor 2027 [What's New](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WhatsNew.htm).
