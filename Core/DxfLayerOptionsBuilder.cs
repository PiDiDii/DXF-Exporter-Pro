using System.Globalization;
using System.Text;

namespace InventorDxfExporter.Core;

public static class DxfLayerOptionsBuilder
{
    private static readonly (string Key, string Name)[] LayerMap =
    {
        ("OuterProfileLayer", "IV_OUTER_PROFILE"), ("InteriorProfilesLayer", "IV_INTERIOR_PROFILES"),
        ("BendUpLayer", "IV_BEND_UP"), ("BendDownLayer", "IV_BEND_DOWN"),
        ("ToolCenterLayer", "IV_TOOL_CENTER"), ("ArcCentersLayer", "IV_ARC_CENTERS"),
        ("TangentLayer", "IV_TANGENT"), ("FeatureProfilesLayer", "IV_FEATURE_PROFILES"),
        ("UnconsumedSketchesLayer", "IV_UNCONSUMED_SKETCHES"), ("TangentRollLinesLayer", "IV_ROLL_TANGENT"),
        ("RollLinesLayer", "IV_ROLL")
    };

    public static string Build(ExportSettings settings, bool includeLineTypes)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var options = new StringBuilder("FLAT PATTERN DXF?AcadVersion=R12");
        var hiddenLayers = new List<string>();
        for (var index = 0; index < LayerMap.Length; index++)
        {
            var (key, name) = LayerMap[index];
            var enabled = IsLayerEnabled(settings, name, index < 2);
            if (!enabled)
                hiddenLayers.Add(name);

            options.Append('&').Append(key).Append('=').Append(name);
            if (includeLineTypes)
            {
                var style = settings.LayerStyles.TryGetValue(name, out var configuredStyle)
                    ? configuredStyle
                    : new LayerExportStyle();
                options.Append('&').Append(key).Append("LineType=")
                    .Append(style.LineType.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (hiddenLayers.Count > 0)
            options.Append("&InvisibleLayers=").Append(string.Join(";", hiddenLayers));
        options.Append("&RebaseGeometry=True");
        return options.ToString();
    }

    private static bool IsLayerEnabled(ExportSettings settings, string name, bool defaultValue)
    {
        if (!settings.LayerSettings.TryGetValue(name, out var value))
            return defaultValue;

        var flag = value.Split('|')[0].Trim();
        return flag.Equals("true", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("hiện", StringComparison.OrdinalIgnoreCase)
            || flag.Equals("show", StringComparison.OrdinalIgnoreCase);
    }
}
