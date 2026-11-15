using System.Globalization;

namespace InventorDxfExporter.Core;

public static class UiNumberParser
{
    private const NumberStyles Styles =
        NumberStyles.AllowLeadingWhite
        | NumberStyles.AllowTrailingWhite
        | NumberStyles.AllowLeadingSign
        | NumberStyles.AllowDecimalPoint;

    public static bool TryParseDecimal(string? text, CultureInfo culture, out decimal value)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return decimal.TryParse(text, Styles, culture, out value)
            || decimal.TryParse(text, Styles, CultureInfo.InvariantCulture, out value);
    }

    public static decimal ParseDecimal(string text, CultureInfo culture)
    {
        if (TryParseDecimal(text, culture, out var value))
            return value;

        throw new FormatException($"'{text}' không phải là số hợp lệ.");
    }
}
