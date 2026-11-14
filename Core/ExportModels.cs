namespace InventorDxfExporter.Core;

public sealed class ExportSettings
{
    public const string DefaultFilePattern = "{DoDay}_{Qty}_{VatLieu}_{TenChiTiet}";

    public string OutputRoot { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public string FilePattern { get; set; } = DefaultFilePattern;
    public string OverwriteMode { get; set; } = "Hỏi từng file";
    public bool FastMode { get; set; } = true;
    public bool CleanupSuppressed { get; set; }
    public bool UpdateInfoAndBom { get; set; } = true;
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
    public int ActionIndex { get; set; } = 1;
    public bool DoConvert { get; set; }
    public bool DoExport { get; set; } = true;
    public string ConvFilter { get; set; } = string.Empty;
    public int ConvMode { get; set; } = 2;
    public bool ConvSkipExisting { get; set; } = true;
    public double ConvTolerance { get; set; } = 0.01;
    public string ConvLogFolder { get; set; } = string.Empty;
    public Dictionary<string, string> LayerSettings { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, LayerExportStyle> LayerStyles { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record LayerExportStyle(int LineType = 37648, double LineWeightMm = 0.5);

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
    string? FlatPatternSizeIssue = null);

public sealed record OperationResult(int Success, int Skipped, int Errors, string LogFile, IReadOnlyList<string> Messages);
