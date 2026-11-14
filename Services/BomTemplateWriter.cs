using ClosedXML.Excel;
using InventorDxfExporter.Core;
using IOPath = System.IO.Path;

namespace InventorDxfExporter.Services;

public static class BomTemplateWriter
{
    public static string Write(
        BomExportSettings settings,
        IReadOnlyCollection<BomTemplateRow> rows,
        DateTime exportDate)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(rows);
        Validate(settings);
        if (rows.Count > 1_048_576 - settings.StartRow + 1)
            throw new ArgumentOutOfRangeException(nameof(rows), "Số dòng BOM vượt quá giới hạn worksheet Excel.");

        var templatePath = IOPath.GetFullPath(settings.TemplatePath);
        var outputDirectory = IOPath.GetFullPath(settings.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        var outputPath = CreateOutputPath(outputDirectory, exportDate);
        using var workbook = new XLWorkbook(templatePath);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("File mẫu không có worksheet.");

        var rowNumber = settings.StartRow;
        foreach (var row in rows)
        {
            foreach (var mapping in settings.Columns.Where(column => column.Enabled))
            {
                var cell = worksheet.Cell(rowNumber, ColumnNumber(mapping.Column));
                switch (mapping.Field.Trim().ToUpperInvariant())
                {
                    case "STT": cell.Value = row.Number; break;
                    case "TÊN CHI TIẾT": cell.Value = row.DetailName; break;
                    case "KÍCH THƯỚC": cell.Value = row.Dimensions; break;
                    case "VẬT LIỆU": cell.Value = row.Material; break;
                    case "SỐ LƯỢNG": cell.Value = row.Quantity; break;
                    default: throw new InvalidOperationException($"Trường BOM không được hỗ trợ: {mapping.Field}");
                }
            }
            rowNumber++;
        }

        workbook.SaveAs(outputPath);
        return outputPath;
    }

    public static int ColumnNumber(string column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(column);
        column = column.Trim();
        if (column.Length > 3)
            throw new ArgumentOutOfRangeException(nameof(column), $"Cột Excel vượt XFD: {column}");
        var result = 0;
        foreach (var character in column.ToUpperInvariant())
        {
            if (character is < 'A' or > 'Z')
                throw new ArgumentException($"Tên cột Excel không hợp lệ: {column}", nameof(column));
            result = checked(result * 26 + character - 'A' + 1);
        }
        if (result > 16384)
            throw new ArgumentOutOfRangeException(nameof(column), $"Cột Excel vượt XFD: {column}");
        return result;
    }

    private static void Validate(BomExportSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TemplatePath) || !File.Exists(settings.TemplatePath))
            throw new FileNotFoundException("Chọn một file Excel mẫu (.xlsx) tồn tại.", settings.TemplatePath);
        if (string.IsNullOrWhiteSpace(settings.OutputDirectory))
            throw new InvalidOperationException("Chọn thư mục lưu file BOM.");
        if (settings.StartRow < 1 || settings.StartRow > 1_048_576)
            throw new ArgumentOutOfRangeException(nameof(settings.StartRow), "Dòng bắt đầu phải từ 1 đến 1048576.");

        var enabledColumns = settings.Columns.Where(column => column.Enabled).ToArray();
        foreach (var mapping in enabledColumns)
        {
            if (mapping.Field.Equals("Hình ảnh", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Xuất ảnh sẽ được bổ sung ở phiên bản sau.");
            _ = ColumnNumber(mapping.Column);
        }
        var supportedFields = new HashSet<string>(
            ["STT", "Tên chi tiết", "Kích thước", "Vật liệu", "Số lượng"],
            StringComparer.OrdinalIgnoreCase);
        var unsupported = enabledColumns.FirstOrDefault(mapping => !supportedFields.Contains(mapping.Field));
        if (unsupported is not null)
            throw new InvalidOperationException($"Trường BOM chưa được hỗ trợ: {unsupported.Field}");
        if (enabledColumns.GroupBy(mapping => ColumnNumber(mapping.Column)).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Mỗi trường BOM phải được gán vào một cột Excel khác nhau.");
    }

    private static string CreateOutputPath(string outputDirectory, DateTime exportDate)
    {
        var baseName = "BOM_" + exportDate.ToString("yyyyMMdd_HHmmss_fff");
        var path = IOPath.Combine(outputDirectory, baseName + ".xlsx");
        var suffix = 1;
        while (File.Exists(path))
            path = IOPath.Combine(outputDirectory, $"{baseName}_{suffix++}.xlsx");
        return path;
    }
}
