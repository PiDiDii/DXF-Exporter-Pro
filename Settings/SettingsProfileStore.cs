using System.Text.Json;
using InventorDxfExporter.Core;

namespace InventorDxfExporter.Settings;

/// <summary>Named project/customer profiles stored separately from application settings.</summary>
public sealed class SettingsProfileStore
{
    private readonly string _directory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsProfileStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Inventor_DXF_Exporter", "Profiles");
    }

    public IReadOnlyList<string> List() => Directory.Exists(_directory)
        ? Directory.EnumerateFiles(_directory, "*.json").Select(Path.GetFileNameWithoutExtension).Where(x => x is not null).Cast<string>().OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToArray()
        : Array.Empty<string>();

    public void Save(string profileName, ExportSettings settings)
    {
        ValidateName(profileName); ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(_directory);
        var snapshot = ExportSettingsSnapshot.From(settings);
        File.WriteAllText(PathFor(profileName), JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    public bool TryLoad(string profileName, ExportSettings target)
    {
        ValidateName(profileName); ArgumentNullException.ThrowIfNull(target);
        var path = PathFor(profileName);
        if (!File.Exists(path)) return false;
        var snapshot = JsonSerializer.Deserialize<ExportSettingsSnapshot>(File.ReadAllText(path));
        if (snapshot is null) return false;
        snapshot.Apply(target);
        return true;
    }

    public bool Delete(string profileName)
    {
        ValidateName(profileName);
        var path = PathFor(profileName);
        if (!File.Exists(path)) return false;
        File.Delete(path); return true;
    }

    private string PathFor(string name) => Path.Combine(_directory, name.Trim() + ".json");
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên cấu hình không được để trống.", nameof(name));
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\'))
            throw new ArgumentException("Tên cấu hình có ký tự không hợp lệ.", nameof(name));
    }
}
