using Inventor;

namespace Inventor2027AICodingKit.Examples;

public static class DrawingBomExample
{
    public static int GetActiveSheetPartsListCount(DrawingDocument drawing)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        return drawing.ActiveSheet.PartsLists.Count;
    }
}
