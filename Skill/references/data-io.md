# DataIO and File Export

## DataIO is context-dependent

The Autodesk Inventor 2027 API Help states that supported formats depend on the object that provides the `DataIO` instance. Query `GetInputFormats` / `GetOutputFormats` and the paired storage types for the actual context; never assume a format is available merely because another object can write it.

Verified C# signature:

```csharp
void WriteDataToFile(string Format, string FileName);
```

Source: [DataIO.WriteDataToFile](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DataIO_WriteDataToFile.htm), [DataIO overview](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DataIO_Overview.htm), and local interop XML.

## DXF example

The 2027 API Help's `Translate - Sheet Metal to DXF` sample documents `FLAT PATTERN DXF` and its options. It gives the R12 example:

```text
FLAT PATTERN DXF?AcadVersion=R12&OuterProfileLayer=Outer&TrimCenterlinesAtContour=True&InvisibleLayers=IV_UNCONSUMED_SKETCH_CONSTRUCTION
```

It also lists layer names/defaults, `AcadVersion` choices (`2018`, `2013`, `2010`, `2007`, `2004`, `2000`, `R12`), `SimplifySplines`, `SplineTolerance`, `RebaseGeometry`, and other format options. These options belong to the documented sheet-metal DataIO context; do not apply them to unrelated DataIO objects.

Use the verified C# call shape `WriteDataToFile(format, fileName)`. The official sample is VBA/iLogic, so a C# translation must still be compiled against the 2027 interop assembly and tested with actual parts/output. See [`examples/dxf-export/README.md`](../../../examples/dxf-export/README.md).

## Production checks

- Validate output directory, naming collisions, access rights, and existing-file policy before export.
- Keep the requested output path separate from the format/options string.
- Do not modify the model by unfolding a part unless that side effect is an explicit product decision.
- Report each failed file with its source document and exception; do not return a success-shaped empty file.
- Use the appropriate Translator Add-in only when its format/options behavior is documented for the required workflow. `HasSaveCopyAsOptions` and `SaveCopyAs` are not interchangeable with `DataIO`.
