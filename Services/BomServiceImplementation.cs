using ClosedXML.Excel;
using Inventor;
using InventorDxfExporter.Core;
using System.Runtime.InteropServices;
using InventorApplication = Inventor.Application;
using IOPath = System.IO.Path;

namespace InventorDxfExporter.Services;

/// <summary>
/// V14-compatible BOM writer. ExportItem carries a document path (not an Inventor
/// document/PartInfo), so this adapter opens each part through the active Inventor
/// application and reads its properties via the existing IPartInfoService contract.
/// </summary>
public sealed class BomServiceImplementation : IBomService
{
    private readonly InventorApplication _application;
    private readonly IPartInfoService _partInfoService;

    /// <summary>Per-row and overall diagnostics from the most recent export.</summary>
    public IReadOnlyList<string> Errors => _errors;
    private readonly List<string> _errors = new();

    public BomServiceImplementation(InventorApplication application, IPartInfoService partInfoService)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _partInfoService = partInfoService ?? throw new ArgumentNullException(nameof(partInfoService));
    }

    public string Export(IReadOnlyCollection<ExportItem> items, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        _errors.Clear();

        try
        {
            Directory.CreateDirectory(outputDirectory);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("BOM");
            sheet.Cell(1, 1).Value = "STT";
            sheet.Cell(1, 2).Value = "Part number";
            sheet.Cell(1, 3).Value = "Số lượng";
            sheet.Cell(1, 4).Value = "Thickness";
            sheet.Cell(1, 5).Value = "Description";
            sheet.Range(1, 1, 1, 5).Style.Font.Bold = true;

            var row = 2;
            foreach (var item in items)
            {
                ArgumentNullException.ThrowIfNull(item);
                sheet.Cell(row, 1).Value = row - 1;
                sheet.Cell(row, 3).Value = item.Quantity;
                try
                {
                    if (string.IsNullOrWhiteSpace(item.DocumentPath))
                        throw new InvalidOperationException("ExportItem.DocumentPath is empty.");

                    var part = FindOpenPart(item.DocumentPath);
                    var openedHere = part is null;
                    if (openedHere)
                        part = _application.Documents.Open(item.DocumentPath, false) as PartDocument;
                    if (part is null)
                        throw new InvalidOperationException("Document path did not resolve to an Inventor PartDocument.");

                    try
                    {
                        // Part number / Thickness / Description deliberately use the existing
                        // IPartInfoService contract rather than inferring from Material/Thickness DTO fields.
                        var info = _partInfoService.Read(part);
                        sheet.Cell(row, 2).Value = info.PartNumber ?? string.Empty;
                        sheet.Cell(row, 4).Value = info.Thickness ?? string.Empty;
                        sheet.Cell(row, 5).Value = info.Description ?? string.Empty;
                    }
                    finally
                    {
                        if (openedHere)
                            part.Close(true);
                    }
                }
                catch (Exception ex)
                {
                    _errors.Add($"{item.DetailName} (BOM): {ex.Message}");
                }
                row++;
            }

            sheet.Columns(1, 5).AdjustToContents();
            var path = IOPath.Combine(outputDirectory, "BOM_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");
            workbook.SaveAs(path);
            return path;
        }
        catch (Exception ex)
        {
            _errors.Add("Xuất BOM Excel: " + ex.Message);
            return string.Empty;
        }
    }

    private PartDocument? FindOpenPart(string documentPath)
    {
        // Avoid opening a second copy when the part is already open in Inventor.
        foreach (Document document in _application.Documents)
        {
            try
            {
                if (string.Equals(document.FullFileName, IOPath.GetFullPath(documentPath), StringComparison.OrdinalIgnoreCase))
                    return document as PartDocument;
            }
            catch (COMException)
            {
                // Ignore transient/unavailable COM documents and continue searching.
            }
        }
        return null;
    }
}
