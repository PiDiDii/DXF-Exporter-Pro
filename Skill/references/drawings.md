# Drawings and BOM

## Drawing collections

The verified 2027 object route for read-only inspection is:

```text
DrawingDocument.Sheets -> Sheets.Item(index) -> DrawingSheet.PartsLists
  -> PartsLists.Count / PartsLists.Item(index) -> PartsList
```

`PartsList` is the drawing's displayed parts list; it should not be conflated with an underlying assembly BOM or with a different sheet's parts list.

## BOM view changes in 2027

The Inventor 2027 `What's New` topic names:

- `Balloon.ReplaceBOMView(PartsListLevelEnum, object, object, object)`
- `PartsList.ReplaceBOMView(PartsListLevelEnum, object, object, object)`

These members exist in the 2027 interop reference. The latter's broad `object` parameters are not self-describing; look up the exact topic and valid values before changing a drawing. Do not infer the meaning/order of optional arguments from another release.

## Example

[`examples/drawing-bom/DrawingBomExample.cs`](../../../examples/drawing-bom/DrawingBomExample.cs) reads the active sheet's parts-list count without editing the drawing.

## Sources

- Autodesk Inventor 2027 [DrawingDocument.Sheets](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DrawingDocument_Sheets.htm), [PartsLists.Item](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/PartsLists_Item.htm), [PartsList.ReplaceBOMView](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/PartsList_ReplaceBOMView.htm), and [What's New](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WhatsNew.htm).
