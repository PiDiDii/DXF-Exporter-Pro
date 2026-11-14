using System.Text.Json;
using InventorDxfExporter.Core;

namespace InventorDxfExporter.Settings;

public sealed class BomExportSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Inventor_DXF_Exporter",
        "BomExportSettings.json");

    public BomExportSettings Load()
    {
        if (!File.Exists(FilePath))
            return new BomExportSettings();

        var settings = JsonSerializer.Deserialize<BomExportSettings>(File.ReadAllText(FilePath), JsonOptions)
            ?? throw new InvalidDataException($"Không đọc được cấu hình BOM: {FilePath}");
        MergeDefaultColumns(settings);
        return settings;
    }

    public void Save(BomExportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        MergeDefaultColumns(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static void MergeDefaultColumns(BomExportSettings settings)
    {
        settings.Columns ??= [];
        settings.Columns.RemoveAll(column =>
            column.Field.Equals("Nhà cung cấp", StringComparison.OrdinalIgnoreCase));
        foreach (var defaultColumn in BomExportSettings.CreateDefaultColumns())
        {
            var existing = settings.Columns.FirstOrDefault(column =>
                column.Field.Equals(defaultColumn.Field, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
                settings.Columns.Add(defaultColumn);
        }
    }
}
