# Sheet-Metal DXF Export

Exports an **existing** flat pattern with Inventor's documented sheet-metal DataIO format. It refuses to unfold the model implicitly and surfaces invalid input/state as exceptions to the caller. A production command should additionally validate output directory permissions and collisions, report per-document outcomes, and decide whether DXF R12 is the required target.

The `FLAT PATTERN DXF?...` options are from the Autodesk Inventor 2027 API Help sample. `WriteDataToFile(string Format, string FileName)` was checked in the matching interop metadata. The reference sample is in VBA/iLogic, not C#; this C# translation is compile-verified by the shared project only. It has **not** been run in Inventor or validated against a generated DXF file.

Sources:

- Autodesk Inventor 2027 [Translate - Sheet Metal to DXF](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WriteFlatPatternAsDXF_Sample.htm).
- Autodesk Inventor 2027 [DataIO.WriteDataToFile](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DataIO_WriteDataToFile.htm).
- Autodesk Inventor 2027 [SheetMetalComponentDefinition.DataIO](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SheetMetalComponentDefinition_DataIO.htm).
