namespace InventorDxfExporter.Core;

public sealed class ExportSettings
{
    public const string DefaultFilePattern = "{DoDay}_{Qty}_{VatLieu}_{PartNumber}";

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
    public Dictionary<string, string> LayerSettings { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record LayerRow(string Name, bool Enabled);

/// <summary>Document identity is a file path; Inventor COM objects stay within service adapters.</summary>
public sealed record ExportItem(
    string DocumentPath,
    string DetailName,
    string Material,
    string Thickness,
    int Quantity,
    double? FlatPatternXmm = null,
    double? FlatPatternYmm = null,
    string? FlatPatternSizeIssue = null,
    string? PartNumber = null);

public sealed record OperationResult(int Success, int Skipped, int Errors, string LogFile, IReadOnlyList<string> Messages);
