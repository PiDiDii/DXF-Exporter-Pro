using InventorDxfExporter.Core;

namespace InventorDxfExporter.Services;

/// <summary>Builds preflight results from the collection snapshot without traversing Inventor documents again.</summary>
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

    public ExportPreflightResult Inspect(
        IReadOnlyCollection<ExportItem> exportItems,
        IReadOnlyCollection<string> skippedItems,
        IReadOnlyCollection<string> collectionErrors,
        IReadOnlyCollection<ExportItem> suppressedItems)
    {
        ArgumentNullException.ThrowIfNull(exportItems);
        ArgumentNullException.ThrowIfNull(skippedItems);
        ArgumentNullException.ThrowIfNull(collectionErrors);
        ArgumentNullException.ThrowIfNull(suppressedItems);
        var warnings = new List<string>();
        warnings.AddRange(skippedItems
            .Where(item => item.StartsWith("Suppressed:", StringComparison.OrdinalIgnoreCase))
            .Select(item => item + "; sẽ không xuất part này."));
        warnings.AddRange(collectionErrors.Select(error => "Không thể kiểm tra đầy đủ: " + error));
        var duplicateNames = exportItems
            .GroupBy(item => item.DetailName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = exportItems.Select(item =>
        {
            bool? hasFlatPattern = item.FlatPatternSizeIssue switch
            {
                null => true,
                "Chưa có Flat Pattern" => false,
                _ => null
            };
            if (hasFlatPattern == false)
                warnings.Add($"Sheet Metal part '{item.DetailName}' thiếu Flat Pattern.");
            else if (item.FlatPatternSizeIssue is not null)
                warnings.Add($"'{item.DetailName}': {item.FlatPatternSizeIssue}");
            if (duplicateNames.Contains(item.DetailName))
                warnings.Add($"Trùng tên chi tiết '{item.DetailName}'. Kiểm tra tên file xuất dự kiến.");
            return new ExportPreflightItem(item.DetailName, true, hasFlatPattern, false,
                duplicateNames.Contains(item.DetailName), item.FlatPatternSizeIssue);
        }).Concat(suppressedItems.Select(item =>
            new ExportPreflightItem(item.DetailName, true, null, true, false, null))).ToArray();

        return CreateResult(items, warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static ExportPreflightResult CreateResult(IReadOnlyList<ExportPreflightItem> items, IReadOnlyList<string> warnings) =>
        new(items, warnings, new[] { SelectionCheck });
}
