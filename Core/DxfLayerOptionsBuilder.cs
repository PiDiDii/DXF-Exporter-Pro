using System.Text;

namespace InventorDxfExporter.Core;

public static class DxfLayerOptionsBuilder
{
    public static IReadOnlyList<DxfLayerOption> LayerOptions { get; } = Array.AsReadOnly(new DxfLayerOption[]
    {
        new("OuterProfileLayer", "IV_OUTER_PROFILE", "Đường bao ngoài", true),
        new("InteriorProfilesLayer", "IV_INTERIOR_PROFILES", "Đường bao trong / lỗ", true),
        new("BendUpLayer", "IV_BEND_UP", "Đường gấp lên (bend up)", true),
        new("BendDownLayer", "IV_BEND_DOWN", "Đường gấp xuống (bend down)", true),
        new("ToolCenterLayer", "IV_TOOL_CENTER", "Tâm dụng cụ (tool center)", false),
        new("ArcCentersLayer", "IV_ARC_CENTERS", "Tâm cung tròn", false),
        new("TangentLayer", "IV_TANGENT", "Đường tiếp tuyến", false),
        new("FeatureProfilesLayer", "IV_FEATURE_PROFILES", "Đường bao chi tiết đặc trưng", false),
        new("UnconsumedSketchesLayer", "IV_UNCONSUMED_SKETCHES", "Sketch chưa dùng đến", false),
        new("TangentRollLinesLayer", "IV_ROLL_TANGENT", "Tiếp tuyến đường cán", false),
        new("RollLinesLayer", "IV_ROLL", "Đường cán (roll line)", false),
        new(null, "IV_MARK_SURFACE", "Đánh dấu bề mặt", false)
    });

    public static string Build(ExportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var options = new StringBuilder("FLAT PATTERN DXF?AcadVersion=R12");
        foreach (var layer in LayerOptions)
        {
            if (layer.OptionKey is not null)
                options.Append('&').Append(layer.OptionKey).Append('=').Append(layer.Name);
        }

        var hiddenLayers = GetInvisibleLayers(settings);
        if (hiddenLayers.Count > 0)
            options.Append("&InvisibleLayers=").Append(string.Join(";", hiddenLayers));
        options.Append("&RebaseGeometry=True");
        return options.ToString();
    }

    public static IReadOnlyList<string> GetInvisibleLayers(ExportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var hiddenLayers = new List<string>();
        foreach (var layer in LayerOptions)
        {
            if (IsLayerEnabled(settings, layer.Name, layer.DefaultEnabled))
                continue;

            hiddenLayers.Add(layer.Name);
            if (layer.Name == "IV_TOOL_CENTER")
                hiddenLayers.Add("IV_Tool_center");
        }

        return hiddenLayers;
    }

    private static bool IsLayerEnabled(ExportSettings settings, string name, bool defaultValue)
    {
        if (!settings.LayerSettings.TryGetValue(name, out var value))
            return defaultValue;

        return bool.TryParse(value, out var enabled) && enabled;
    }
}

public sealed record DxfLayerOption(string? OptionKey, string Name, string Description, bool DefaultEnabled);
