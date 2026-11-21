using System.Text.Json;
using InventorDxfExporter.Core;

namespace InventorDxfExporter.Settings;

public sealed class StepExportSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string FilePath { get; }

    public StepExportSettingsStore() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Inventor_DXF_Exporter",
        "StepExportSettings.json"))
    {
    }

    internal StepExportSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public StepExportSettings Load()
    {
        if (!File.Exists(FilePath))
            return new StepExportSettings();

        return JsonSerializer.Deserialize<StepExportSettings>(File.ReadAllText(FilePath), JsonOptions)
            ?? new StepExportSettings();
    }

    public void Save(StepExportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
