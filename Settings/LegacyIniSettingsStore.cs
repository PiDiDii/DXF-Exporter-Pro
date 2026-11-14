using System.Globalization;
using InventorDxfExporter.Core;

namespace InventorDxfExporter.Settings;

/// <summary>Reads/writes the V14 key=value file in %AppData% without changing its location or key names.</summary>
public sealed class LegacyIniSettingsStore
{
    public string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Inventor_DXF_Export_Form.ini");

    public ExportSettings Load()
    {
        var settings = new ExportSettings();
        if (!File.Exists(FilePath)) return settings;
        try
        {
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var split = line.IndexOf('=');
                if (split <= 0) continue;
                var key = line[..split];
                var value = line[(split + 1)..];
                switch (key)
                {
                    case "OutputRoot": settings.OutputRoot = value; break;
                    case "OverwriteMode": settings.OverwriteMode = value; break;
                    case "FastMode": settings.FastMode = ParseBool(value); break;
                    case "CleanupSuppressed": settings.CleanupSuppressed = ParseBool(value); break;
                    case "UpdateInfoAndBom": settings.UpdateInfoAndBom = ParseBool(value); break;
                    case "AddInfoText": settings.AddInfoText = ParseBool(value); break;
                    case "TextAlignment": settings.TextAlignment = value; break;
                    case "TextHeight": settings.TextHeight = ParseDouble(value, settings.TextHeight); break;
                    case "TextLineGap": settings.TextLineGap = ParseDouble(value, settings.TextLineGap); break;
                    case "TextYOffset": settings.TextYOffset = ParseDouble(value, settings.TextYOffset); break;
                    case "BendMode": settings.BendMode = value; break;
                    case "ActionIndex": settings.ActionIndex = ParseInt(value, settings.ActionIndex); break;
                    case "ConvFilter": settings.ConvFilter = value; break;
                    case "ConvMode": settings.ConvMode = ParseInt(value, settings.ConvMode); break;
                    case "ConvSkipExisting": settings.ConvSkipExisting = ParseBool(value); break;
                    case "ConvTolerance": settings.ConvTolerance = ParseDouble(value, settings.ConvTolerance); break;
                    case "ConvLogFolder": settings.ConvLogFolder = value; break;
                    case "BendUpLen": settings.BendUpLen = ParseDouble(value, settings.BendUpLen); break;
                    case "BendUpInset": settings.BendUpInset = ParseDouble(value, settings.BendUpInset); break;
                    case "BendDownLen": settings.BendDownLen = ParseDouble(value, settings.BendDownLen); break;
                    case "BendDownInset": settings.BendDownInset = ParseDouble(value, settings.BendDownInset); break;
                    default:
                        if (key.StartsWith("Layer.", StringComparison.Ordinal)) settings.LayerSettings[key[6..]] = value;
                        else if (key.StartsWith("LayerStyle.", StringComparison.Ordinal) && TryParseLayerStyle(value, out var style))
                            settings.LayerStyles[key["LayerStyle.".Length..]] = style;
                        break;
                }
            }
        }
        catch (IOException) { /* Match V14's tolerant load behavior; defaults remain available. */ }
        return settings;
    }

    public void Save(ExportSettings s)
    {
        ArgumentNullException.ThrowIfNull(s);
        var lines = new List<string>
        {
            $"OutputRoot={s.OutputRoot}", $"FilePattern={ExportSettings.DefaultFilePattern}", $"OverwriteMode={s.OverwriteMode}",
            $"FastMode={s.FastMode}", $"CleanupSuppressed={s.CleanupSuppressed}",
            $"UpdateInfoAndBom={s.UpdateInfoAndBom}", $"AddInfoText={s.AddInfoText}", $"TextAlignment={s.TextAlignment}",
            $"TextHeight={Format(s.TextHeight)}", $"TextLineGap={Format(s.TextLineGap)}", $"TextYOffset={Format(s.TextYOffset)}",
            $"BendMode={s.BendMode}", $"ActionIndex={s.ActionIndex}", $"ConvFilter={s.ConvFilter}", $"ConvMode={s.ConvMode}",
            $"ConvSkipExisting={s.ConvSkipExisting}", $"ConvTolerance={Format(s.ConvTolerance)}", $"ConvLogFolder={s.ConvLogFolder}",
            $"BendUpLen={Format(s.BendUpLen)}", $"BendUpInset={Format(s.BendUpInset)}",
            $"BendDownLen={Format(s.BendDownLen)}", $"BendDownInset={Format(s.BendDownInset)}"
        };
        foreach (var layer in s.LayerSettings) lines.Add($"Layer.{layer.Key}={layer.Value}");
        foreach (var layer in s.LayerStyles)
        {
            var style = layer.Value;
            lines.Add($"LayerStyle.{layer.Key}={style.LineType}|{Format(style.LineWeightMm)}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllLines(FilePath, lines, System.Text.Encoding.UTF8);
    }

    private static bool TryParseLayerStyle(string value, out LayerExportStyle style)
    {
        var parts = value.Split('|');
        var typeIndex = parts.Length == 3 ? 1 : 0;
        var weightIndex = parts.Length - 1;
        if (parts.Length is 2 or 3
            && int.TryParse(parts[typeIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineType)
            && double.TryParse(parts[weightIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var lineWeightMm)
            && lineWeightMm > 0)
        {
            style = new LayerExportStyle(lineType, lineWeightMm);
            return true;
        }
        style = new LayerExportStyle();
        return false;
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
    private static bool ParseBool(string value) => value.Equals("true", StringComparison.OrdinalIgnoreCase);
    private static double ParseDouble(string value, double fallback) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;
    private static int ParseInt(string value, int fallback) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : fallback;
}
