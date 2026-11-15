using System.Globalization;

namespace InventorDxfExporter.Core;

public static class BomDimensionFormatter
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static string Format(double? longSideMm, double? shortSideMm, string thickness)
    {
        if (longSideMm is not double longSide || shortSideMm is not double shortSide
            || !double.IsFinite(longSide) || !double.IsFinite(shortSide)
            || longSide <= 0 || shortSide <= 0)
            return string.Empty;

        var formattedThickness = FormatThickness(thickness);
        var longSideText = longSide.ToString("0.##", VietnameseCulture);
        var shortSideText = shortSide.ToString("0.##", VietnameseCulture);
        return $"{longSideText} x {shortSideText} x {formattedThickness}";
    }

    public static string FormatThicknessDisplay(string value)
    {
        var displayValue = value.StartsWith('T') ? value[1..] : value;
        if (TryParseThickness(value, out var thickness))
            return thickness.ToString("0.###", CultureInfo.CurrentCulture);

        return displayValue.Equals("_Khong_Xac_Dinh", StringComparison.OrdinalIgnoreCase)
            || displayValue.Equals("Khong_Xac_Dinh", StringComparison.OrdinalIgnoreCase)
            ? "Chưa xác định"
            : displayValue;
    }

    private static string FormatThickness(string value)
    {
        if (TryParseThickness(value, out var thickness))
            return thickness.ToString("0.##", VietnameseCulture);

        return "Chưa xác định";
    }

    private static bool TryParseThickness(string value, out double thickness)
    {
        var numericValue = value.StartsWith('T') ? value[1..] : value;
        return double.TryParse(numericValue, NumberStyles.Float, CultureInfo.InvariantCulture, out thickness)
            && double.IsFinite(thickness)
            && thickness > 0;
    }
}
