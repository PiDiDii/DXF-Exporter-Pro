namespace InventorDxfExporter.Core;

public sealed class BomExportSettings
{
    public int SchemaVersion { get; set; }
    public string TemplatePath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public int StartRow { get; set; } = 11;
    public string ProjectNameCell { get; set; } = string.Empty;
    public string ExportDateCell { get; set; } = string.Empty;
    public List<BomColumnMapping> Columns { get; set; } = CreateDefaultColumns();

    public static IReadOnlyList<string> SupportedFields { get; } =
    [
        "STT", "Tên chi tiết", "Hình ảnh", "Kích thước", "Vật liệu", "Số lượng",
        "Tên file", "Đường dẫn file", "Độ dày"
    ];

    public static List<BomColumnMapping> CreateDefaultColumns() =>
    [
        new("STT", true, "A"),
        new("Tên chi tiết", true, "C"),
        new("Hình ảnh", true, "D"),
        new("Kích thước", true, "E"),
        new("Vật liệu", true, "K"),
        new("Số lượng", true, "M"),
        new("Tên file", false, "N"),
        new("Đường dẫn file", false, "O"),
        new("Độ dày", false, "P")
    ];
}

public sealed class BomColumnMapping
{
    public BomColumnMapping() { }

    public BomColumnMapping(string field, bool enabled, string column)
    {
        Field = field;
        Enabled = enabled;
        Column = column;
    }

    public string Field { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Column { get; set; } = string.Empty;
}

public sealed record BomTemplateRow(
    int Number,
    string DetailName,
    string Dimensions,
    string Material,
    int Quantity,
    byte[]? ImageData = null,
    string? FileName = null,
    string? DocumentPath = null,
    string? Thickness = null);
