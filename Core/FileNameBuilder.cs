using System.IO;
using System.Text;

namespace InventorDxfExporter.Core;

/// <summary>Pure string and file-name helpers with no Inventor dependency.</summary>
public static class FileNameBuilder
{
    public static string CleanName(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "Khong_Xac_Dinh";
        var result = value.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars()) result = result.Replace(invalid, '_');
        return result.Replace(" ", "_");
    }

    public static string BuildFileName(string thickness, int quantity, string material, string detailName)
    {
        var result = ExportSettings.DefaultFilePattern.Replace("{DoDay}", CleanName(thickness), StringComparison.Ordinal)
            .Replace("{Qty}", "Q" + quantity.ToString(), StringComparison.Ordinal)
            .Replace("{VatLieu}", CleanName(material), StringComparison.Ordinal)
            .Replace("{TenChiTiet}", CleanName(detailName), StringComparison.Ordinal);
        if (!result.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase)) result += ".dxf";
        foreach (var invalid in Path.GetInvalidFileNameChars()) result = result.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(result) ? "FlatPattern.dxf" : result;
    }

    public static string CleanTextForDxf(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var text = value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', ' ').Replace('đ', 'd').Replace('Đ', 'D');
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var normalized = sb.ToString().Normalize(NormalizationForm.FormC);
        var ascii = new StringBuilder(normalized.Length);
        foreach (var ch in normalized) ascii.Append(ch is >= (char)32 and <= (char)126 ? ch : '?');
        return ascii.ToString();
    }
}
