using InventorDxfExporter.Core;
using System.Globalization;

namespace InventorDxfExporter.Services;

public sealed class BomServiceImplementation : IBomService
{
    private readonly List<string> _errors = new();

    public IReadOnlyList<string> Errors => _errors;

    public string Export(IReadOnlyCollection<ExportItem> items, BomExportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(settings);
        _errors.Clear();

        try
        {
            var rows = new List<BomTemplateRow>(items.Count);
            var number = 1;
            foreach (var item in items)
            {
                ArgumentNullException.ThrowIfNull(item);
                var dimensions = FormatDimensions(item);
                if (string.IsNullOrWhiteSpace(dimensions) && !string.IsNullOrWhiteSpace(item.FlatPatternSizeIssue))
                    _errors.Add($"{item.DetailName} (Kích thước): {item.FlatPatternSizeIssue}");

                rows.Add(new BomTemplateRow(
                    number++,
                    item.DetailName,
                    dimensions,
                    item.Material,
                    item.Quantity));
            }

            var path = BomTemplateWriter.Write(settings, rows, DateTime.Now);
            return path;
        }
        catch (Exception ex)
        {
            _errors.Add("Xuất BOM Excel: " + ex.Message);
            return string.Empty;
        }
    }

    private static string FormatDimensions(ExportItem item)
    {
        if (item.FlatPatternXmm is not double x || item.FlatPatternYmm is not double y)
            return string.Empty;
        return $"{x.ToString("0.##", CultureInfo.InvariantCulture)} x {y.ToString("0.##", CultureInfo.InvariantCulture)} mm";
    }
}
