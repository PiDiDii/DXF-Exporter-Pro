using Inventor;

namespace Inventor2027AICodingKit.Examples;

public static class DxfExportExample
{
    private const string FlatPatternDxfFormat =
        "FLAT PATTERN DXF?AcadVersion=R12&OuterProfileLayer=Outer" +
        "&TrimCenterlinesAtContour=True" +
        "&InvisibleLayers=IV_UNCONSUMED_SKETCH_CONSTRUCTION";

    public static void ExportExistingFlatPattern(PartDocument part, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (part.ComponentDefinition is not SheetMetalComponentDefinition sheetMetal)
        {
            throw new ArgumentException(
                "The document is not a sheet-metal part.",
                nameof(part));
        }

        if (!sheetMetal.HasFlatPattern)
        {
            throw new InvalidOperationException(
                "The part has no flat pattern. Create one explicitly before export.");
        }

        sheetMetal.DataIO.WriteDataToFile(FlatPatternDxfFormat, outputPath);
    }
}
