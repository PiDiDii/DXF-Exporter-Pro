using System.Text.Json;
using InventorDxfExporter.Core;

namespace InventorDxfExporter.Settings;

/// <summary>Named project/customer profiles stored separately from the legacy V14 settings file.</summary>
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
        var snapshot = ProfileSnapshot.From(settings);
        File.WriteAllText(PathFor(profileName), JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    public bool TryLoad(string profileName, ExportSettings target)
    {
        ValidateName(profileName); ArgumentNullException.ThrowIfNull(target);
        var path = PathFor(profileName);
        if (!File.Exists(path)) return false;
        var snapshot = JsonSerializer.Deserialize<ProfileSnapshot>(File.ReadAllText(path));
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

    private sealed class ProfileSnapshot
    {
        public string OutputRoot { get; set; } = string.Empty;
        public string FilePattern { get; set; } = string.Empty;
        public string OverwriteMode { get; set; } = string.Empty;
        public bool FastMode { get; set; }
        public bool CleanupSuppressed { get; set; }
        public bool UpdateInfoAndBom { get; set; }
        public bool AddInfoText { get; set; }
        public string TextAlignment { get; set; } = string.Empty;
        public double TextHeight { get; set; }
        public double TextLineGap { get; set; }
        public double TextYOffset { get; set; }
        public string BendMode { get; set; } = string.Empty;
        public double BendUpLen { get; set; }
        public double BendUpInset { get; set; }
        public double BendDownLen { get; set; }
        public double BendDownInset { get; set; }
        public Dictionary<string, string> LayerSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, LayerExportStyle> LayerStyles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static ProfileSnapshot From(ExportSettings s) => new()
        {
            OutputRoot=s.OutputRoot, FilePattern=s.FilePattern, OverwriteMode=s.OverwriteMode, FastMode=s.FastMode,
            CleanupSuppressed=s.CleanupSuppressed, UpdateInfoAndBom=s.UpdateInfoAndBom, AddInfoText=s.AddInfoText,
            TextAlignment=s.TextAlignment, TextHeight=s.TextHeight, TextLineGap=s.TextLineGap, TextYOffset=s.TextYOffset,
            BendMode=s.BendMode, BendUpLen=s.BendUpLen, BendUpInset=s.BendUpInset, BendDownLen=s.BendDownLen,
            BendDownInset=s.BendDownInset, LayerSettings=new Dictionary<string, string>(s.LayerSettings, StringComparer.OrdinalIgnoreCase),
            LayerStyles=new Dictionary<string, LayerExportStyle>(s.LayerStyles, StringComparer.OrdinalIgnoreCase)
        };

        public void Apply(ExportSettings s)
        {
            s.OutputRoot=OutputRoot; s.FilePattern=ExportSettings.DefaultFilePattern; s.OverwriteMode=OverwriteMode; s.FastMode=FastMode;
            s.CleanupSuppressed=CleanupSuppressed; s.UpdateInfoAndBom=UpdateInfoAndBom; s.AddInfoText=AddInfoText;
            s.TextAlignment=TextAlignment; s.TextHeight=TextHeight; s.TextLineGap=TextLineGap; s.TextYOffset=TextYOffset;
            s.BendMode=BendMode; s.BendUpLen=BendUpLen; s.BendUpInset=BendUpInset; s.BendDownLen=BendDownLen; s.BendDownInset=BendDownInset;
            s.LayerSettings.Clear(); foreach (var pair in LayerSettings) s.LayerSettings[pair.Key]=pair.Value;
            s.LayerStyles.Clear(); foreach (var pair in LayerStyles) s.LayerStyles[pair.Key]=pair.Value;
        }
    }
}
