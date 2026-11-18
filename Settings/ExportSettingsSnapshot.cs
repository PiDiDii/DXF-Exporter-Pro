using InventorDxfExporter.Core;

namespace InventorDxfExporter.Settings;

internal sealed class ExportSettingsSnapshot
{
    public string OutputRoot { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public string OverwriteMode { get; set; } = "Hỏi từng file";
    public bool FastMode { get; set; } = true;
    public bool CleanupSuppressed { get; set; }
    public bool ExportBomAfterDxf { get; set; }
    public bool AddInfoText { get; set; }
    public string TextAlignment { get; set; } = "Căn giữa";
    public double TextHeight { get; set; } = 10;
    public double TextLineGap { get; set; } = 15;
    public double TextYOffset { get; set; } = -250;
    public string BendMode { get; set; } = "Giữ nguyên đường dài";
    public double BendUpLen { get; set; } = 10;
    public double BendUpInset { get; set; } = 3;
    public double BendDownLen { get; set; } = 3;
    public double BendDownInset { get; set; } = 3;
    public string ConvFilter { get; set; } = string.Empty;
    public int ConvMode { get; set; } = 2;
    public bool ConvSkipExisting { get; set; } = true;
    public bool ConvAssignInnerFaceAsASide { get; set; } = true;
    public double ConvTolerance { get; set; } = 0.01;
    public string ConvLogFolder { get; set; } = string.Empty;
    public bool ConvWriteLog { get; set; } = true;
    public bool ShowDxfReport { get; set; } = true;
    public Dictionary<string, string> LayerSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static ExportSettingsSnapshot From(ExportSettings settings) => new()
    {
        OutputRoot = settings.OutputRoot,
        OverwriteMode = settings.OverwriteMode,
        FastMode = settings.FastMode,
        CleanupSuppressed = settings.CleanupSuppressed,
        ExportBomAfterDxf = settings.ExportBomAfterDxf,
        AddInfoText = settings.AddInfoText,
        TextAlignment = settings.TextAlignment,
        TextHeight = settings.TextHeight,
        TextLineGap = settings.TextLineGap,
        TextYOffset = settings.TextYOffset,
        BendMode = settings.BendMode,
        BendUpLen = settings.BendUpLen,
        BendUpInset = settings.BendUpInset,
        BendDownLen = settings.BendDownLen,
        BendDownInset = settings.BendDownInset,
        ConvFilter = settings.ConvFilter,
        ConvMode = settings.ConvMode,
        ConvSkipExisting = settings.ConvSkipExisting,
        ConvAssignInnerFaceAsASide = settings.ConvAssignInnerFaceAsASide,
        ConvTolerance = settings.ConvTolerance,
        ConvLogFolder = settings.ConvLogFolder,
        ConvWriteLog = settings.ConvWriteLog,
        ShowDxfReport = settings.ShowDxfReport,
        LayerSettings = new Dictionary<string, string>(settings.LayerSettings, StringComparer.OrdinalIgnoreCase)
    };

    public void Apply(ExportSettings settings)
    {
        settings.OutputRoot = OutputRoot;
        settings.OverwriteMode = OverwriteMode;
        settings.FastMode = FastMode;
        settings.CleanupSuppressed = CleanupSuppressed;
        settings.ExportBomAfterDxf = ExportBomAfterDxf;
        settings.AddInfoText = AddInfoText;
        settings.TextAlignment = TextAlignment;
        settings.TextHeight = TextHeight;
        settings.TextLineGap = TextLineGap;
        settings.TextYOffset = TextYOffset;
        settings.BendMode = BendMode;
        settings.BendUpLen = BendUpLen;
        settings.BendUpInset = BendUpInset;
        settings.BendDownLen = BendDownLen;
        settings.BendDownInset = BendDownInset;
        settings.ConvFilter = ConvFilter;
        settings.ConvMode = ConvMode;
        settings.ConvSkipExisting = ConvSkipExisting;
        settings.ConvAssignInnerFaceAsASide = ConvAssignInnerFaceAsASide;
        settings.ConvTolerance = ConvTolerance;
        settings.ConvLogFolder = ConvLogFolder;
        settings.ConvWriteLog = ConvWriteLog;
        settings.ShowDxfReport = ShowDxfReport;
        settings.LayerSettings.Clear();
        foreach (var pair in LayerSettings)
            settings.LayerSettings[pair.Key] = pair.Value;
    }
}
