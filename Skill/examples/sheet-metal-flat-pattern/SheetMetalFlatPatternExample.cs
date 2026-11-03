using Inventor;

namespace Inventor2027AICodingKit.Examples;

public static class SheetMetalFlatPatternExample
{
    public static FlatPattern? GetExistingFlatPattern(PartDocument part)
    {
        ArgumentNullException.ThrowIfNull(part);

        if (part.ComponentDefinition is not SheetMetalComponentDefinition sheetMetal)
        {
            return null;
        }

        return sheetMetal.HasFlatPattern ? sheetMetal.FlatPattern : null;
    }
}
