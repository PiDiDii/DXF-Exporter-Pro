using System.Text.Json;
using InventorDxfExporter.Core;

namespace InventorDxfExporter.Settings;

public sealed class ExportSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string FilePath { get; }

    public ExportSettingsStore() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Inventor_DXF_Exporter",
        "ExportSettings.json"))
    {
    }

    internal ExportSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public ExportSettings Load()
    {
        if (!File.Exists(FilePath))
            return new ExportSettings();

        var snapshot = JsonSerializer.Deserialize<ExportSettingsSnapshot>(File.ReadAllText(FilePath), JsonOptions)
            ?? throw new InvalidDataException($"Không đọc được cấu hình xuất: {FilePath}");
        var settings = new ExportSettings();
        snapshot.Apply(settings);
        return settings;
    }

    public void Save(ExportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(ExportSettingsSnapshot.From(settings), JsonOptions));
    }
}
