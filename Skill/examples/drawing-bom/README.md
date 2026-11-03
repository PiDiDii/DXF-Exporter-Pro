# Inspect Drawing Parts Lists

Reads the number of parts lists on the active drawing sheet. It does not create, edit, or replace a BOM view. This is intentionally a small, read-only example; a full BOM workflow must distinguish drawing `PartsList` objects from the source assembly's `BOM` and validate the referenced document/view.

Sources: Autodesk Inventor 2027 [DrawingDocument.Sheets](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DrawingDocument_Sheets.htm), [PartsLists.Count](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/PartsLists_Count.htm), and installed interop metadata. The example is compiled by the shared project; Inventor runtime behavior is untested.
