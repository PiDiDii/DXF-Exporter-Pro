using Inventor;

namespace InventorDxfExporter.Services;

internal static class FlatPatternDimensionsReader
{
    public static (double LongSideMm, double ShortSideMm) Read(
        PartDocument part,
        SheetMetalComponentDefinition sheetMetal)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(sheetMetal);

        var bounds = sheetMetal.FlatPattern.RangeBox;
        var units = part.UnitsOfMeasure;
        var extents = new[]
        {
            units.ConvertUnits(Math.Abs(bounds.MaxPoint.X - bounds.MinPoint.X),
                UnitsTypeEnum.kDatabaseLengthUnits, UnitsTypeEnum.kMillimeterLengthUnits),
            units.ConvertUnits(Math.Abs(bounds.MaxPoint.Y - bounds.MinPoint.Y),
                UnitsTypeEnum.kDatabaseLengthUnits, UnitsTypeEnum.kMillimeterLengthUnits),
            units.ConvertUnits(Math.Abs(bounds.MaxPoint.Z - bounds.MinPoint.Z),
                UnitsTypeEnum.kDatabaseLengthUnits, UnitsTypeEnum.kMillimeterLengthUnits)
        }.OrderByDescending(extent => extent).Take(2).ToArray();

        if (extents.Any(extent => !double.IsFinite(extent) || extent <= 0))
            throw new InvalidDataException("Kích thước Flat Pattern không hợp lệ.");

        return (extents[0], extents[1]);
    }
}
