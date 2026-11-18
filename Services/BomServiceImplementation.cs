using InventorDxfExporter.Core;
using System.Drawing.Imaging;
using Inventor;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Services;

public sealed class BomServiceImplementation : IBomService
{
    private readonly InventorApplication _application;
    private readonly List<string> _errors = new();

    public BomServiceImplementation(InventorApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public IReadOnlyList<string> Errors => _errors;

    public static string GetProjectName(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var documentName = string.IsNullOrWhiteSpace(document.FullFileName)
            ? document.DisplayName
            : document.FullFileName;
        return System.IO.Path.GetFileNameWithoutExtension(documentName);
    }

    public string Export(
        IReadOnlyCollection<ExportItem> items,
        BomExportSettings settings,
        string projectName,
        IOperationMonitor? monitor = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(settings);
        monitor ??= NullOperationMonitor.Instance;
        _errors.Clear();

        try
        {
            var rows = new List<BomTemplateRow>(items.Count);
            var number = 1;
            foreach (var item in items)
            {
                monitor.CancellationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(item);
                var dimensions = FormatDimensions(item);
                if (string.IsNullOrWhiteSpace(dimensions) && !string.IsNullOrWhiteSpace(item.FlatPatternSizeIssue))
                    _errors.Add($"{item.DetailName} (Kích thước): {item.FlatPatternSizeIssue}");

                byte[]? imageData = null;
                if (settings.Columns.Any(mapping =>
                    mapping.Enabled && mapping.Field.Equals("Hình ảnh", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        imageData = ReadThumbnail(item.DocumentPath);
                    }
                    catch (Exception ex)
                    {
                        _errors.Add($"{item.DetailName} (Hình ảnh): {ex.Message}");
                    }
                }

                rows.Add(new BomTemplateRow(
                    number++,
                    item.PartNumber ?? item.DetailName,
                    dimensions,
                    item.Material,
                    item.Quantity,
                    imageData,
                    System.IO.Path.GetFileName(item.DocumentPath),
                    item.DocumentPath,
                    BomDimensionFormatter.FormatThicknessDisplay(item.Thickness)));
                monitor.Report(new OperationProgress(rows.Count, items.Count + 1,
                    item.DetailName, "Đang thu thập dữ liệu BOM"));
            }

            var path = BomTemplateWriter.Write(settings, rows, projectName, DateTime.Now, monitor);
            return path;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Add("Xuất BOM Excel: " + ex.Message);
            return string.Empty;
        }
    }

    private static string FormatDimensions(ExportItem item) =>
        BomDimensionFormatter.Format(item.FlatPatternXmm, item.FlatPatternYmm, item.Thickness);

    private byte[] ReadThumbnail(string documentPath)
    {
        var fullPath = System.IO.Path.GetFullPath(documentPath);
        var part = FindOpenPart(fullPath);
        var openedHere = part is null;
        if (openedHere)
        {
            var openedDocument = _application.Documents.Open(fullPath, false);
            part = openedDocument as PartDocument;
            if (part is null)
            {
                ((Document)openedDocument).Close(true);
                throw new InvalidDataException("Tài liệu không phải Part.");
            }
        }

        try
        {
            var thumbnail = part!.Thumbnail
                ?? throw new InvalidDataException("Tài liệu không có thumbnail.");
            using var image = PictureDispBridge.ToImage(thumbnail);
            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        finally
        {
            if (openedHere)
                part!.Close(true);
        }
    }

    private PartDocument? FindOpenPart(string fullPath)
    {
        for (var index = 1; index <= _application.Documents.Count; index++)
        {
            if (_application.Documents[index] is not PartDocument part
                || string.IsNullOrWhiteSpace(part.FullFileName))
                continue;

            if (string.Equals(
                System.IO.Path.GetFullPath(part.FullFileName),
                fullPath,
                StringComparison.OrdinalIgnoreCase))
                return part;
        }

        return null;
    }
}
