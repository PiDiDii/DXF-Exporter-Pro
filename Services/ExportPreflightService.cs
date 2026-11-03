using Inventor;

namespace InventorDxfExporter.Services;

/// <summary>Read-only document checks; output metadata/name checks are added by the export command.</summary>
public sealed record ExportPreflightResult(
    IReadOnlyList<ExportPreflightItem> Items,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> UncheckedChecks);

public sealed record ExportPreflightItem(
    string Label,
    bool IsSheetMetal,
    bool? HasFlatPattern,
    bool IsSuppressed,
    bool HasDuplicateName,
    string? InspectionError);

public sealed class ExportPreflightService
{
    private const string SelectionCheck = "Inventor SelectSet chưa được áp dụng tự động; có thể chọn/bỏ chọn từng part trong danh sách xem trước.";

    public ExportPreflightResult Inspect(PartDocument part)
    {
        ArgumentNullException.ThrowIfNull(part);
        var items = new List<ExportPreflightItem>(); var warnings = new List<string>();
        try
        {
            if (part.ComponentDefinition is not SheetMetalComponentDefinition sheetMetal)
            {
                items.Add(new ExportPreflightItem(part.DisplayName, false, null, false, false, null));
                warnings.Add("Part không phải Sheet Metal; kiểm tra Flat Pattern không áp dụng.");
            }
            else
            {
                bool hasFlatPattern = sheetMetal.HasFlatPattern;
                items.Add(new ExportPreflightItem(part.DisplayName, true, hasFlatPattern, false, false, null));
                if (!hasFlatPattern) warnings.Add("Sheet Metal part thiếu Flat Pattern.");
            }
        }
        catch (Exception ex)
        {
            items.Add(new ExportPreflightItem(part.DisplayName, false, null, false, false, ex.Message));
            warnings.Add("Không thể kiểm tra đầy đủ part: " + ex.Message);
        }
        return CreateResult(items, warnings);
    }

    public ExportPreflightResult Inspect(AssemblyDocument assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var items = new List<ExportPreflightItem>(); var warnings = new List<string>();
        try
        {
            var leaves = assembly.ComponentDefinition.Occurrences.AllLeafOccurrences[Type.Missing];
            for (var index = 1; index <= leaves.Count; index++)
            {
                try
                {
                    var occurrence = (ComponentOccurrence)leaves[index];
                    var label = occurrence.Name;
                    if (occurrence.Suppressed)
                    {
                        items.Add(new ExportPreflightItem(label, false, null, true, false, null));
                        warnings.Add($"Occurrence '{label}' bị suppressed; không truy cập definition.");
                        continue;
                    }
                    if (occurrence.Definition is not SheetMetalComponentDefinition sheetMetal)
                    {
                        items.Add(new ExportPreflightItem(label, false, null, false, false, null));
                        continue;
                    }
                    bool hasFlatPattern = sheetMetal.HasFlatPattern;
                    items.Add(new ExportPreflightItem(label, true, hasFlatPattern, false, false, null));
                    if (!hasFlatPattern) warnings.Add($"Sheet Metal occurrence '{label}' thiếu Flat Pattern.");
                }
                catch (Exception ex)
                {
                    items.Add(new ExportPreflightItem($"Occurrence #{index}", false, null, false, false, ex.Message));
                    warnings.Add($"Không thể kiểm tra occurrence #{index}: {ex.Message}");
                }
            }
        }
        catch (Exception ex) { warnings.Add("Không thể duyệt assembly hoặc có reference chưa resolve: " + ex.Message); }
        MarkDuplicateOccurrenceNames(items, warnings);
        return CreateResult(items, warnings);
    }

    private static void MarkDuplicateOccurrenceNames(List<ExportPreflightItem> items, List<string> warnings)
    {
        var duplicateNames = items.Where(x => !x.IsSuppressed && x.InspectionError is null)
            .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1)
            .Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (duplicateNames.Count == 0) return;
        for (int i = 0; i < items.Count; i++) if (duplicateNames.Contains(items[i].Label)) items[i] = items[i] with { HasDuplicateName = true };
        foreach (var name in duplicateNames) warnings.Add($"Trùng occurrence name '{name}'. Đây không nhất thiết là trùng tên file xuất.");
    }

    private static ExportPreflightResult CreateResult(IReadOnlyList<ExportPreflightItem> items, IReadOnlyList<string> warnings) =>
        new(items, warnings, new[] { SelectionCheck });
}
