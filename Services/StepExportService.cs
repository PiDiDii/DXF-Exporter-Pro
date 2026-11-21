using Inventor;
using InventorDxfExporter.Core;
using System.Security.Cryptography;
using System.Text;
using InventorApplication = Inventor.Application;

namespace InventorDxfExporter.Services;

public sealed class StepExportService
{
    private const string StepTranslatorId = "{90AF7F40-0C01-11D5-8E83-0010B541CD80}";
    private const int Ap214Automotive = 3;
    private readonly InventorApplication _application;

    public StepExportService(InventorApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public StepCollectResult Collect(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var result = new StepCollectResult();
        var items = new Dictionary<string, StepPartItem>(StringComparer.OrdinalIgnoreCase);
        var libraryPaths = LoadLibraryPaths();

        if (document is PartDocument)
        {
            AddPart(document, null, false, items, result);
        }
        else if (document is AssemblyDocument assembly)
        {
            WalkOccurrences(assembly.ComponentDefinition.Occurrences, items, result, libraryPaths);
        }
        return result;
    }

    private void WalkOccurrences(ComponentOccurrences occurrences,
        Dictionary<string, StepPartItem> items, StepCollectResult result,
        IReadOnlyCollection<string> libraryPaths)
    {
        var enumerator = occurrences.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var occurrence = enumerator.Current as ComponentOccurrence;
            if (occurrence is null)
                continue;

            try
            {
                if (occurrence.Suppressed)
                    continue;
                if (occurrence.Definition is VirtualComponentDefinition)
                    continue;

                if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    WalkOccurrences(occurrence.SubOccurrences, items, result, libraryPaths);
                }
                else if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kPartDocumentObject)
                {
                    var part = occurrence.Definition.Document as PartDocument;
                    if (part is not null)
                        AddPart(part, occurrence, true, items, result, libraryPaths);
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"{occurrence.Name}: {ex.Message}");
            }
        }
    }

    private static void AddPart(Document document, ComponentOccurrence? occurrence, bool applyFilter,
        Dictionary<string, StepPartItem> items, StepCollectResult result,
        IReadOnlyCollection<string>? libraryPaths = null)
    {
        var key = document.FullDocumentName;
        if (items.TryGetValue(key, out var existing))
        {
            existing.Quantity++;
            return;
        }

        if (applyFilter)
        {
            if (occurrence is not null && IsMember(occurrence.Definition))
            {
                result.SkippedOther++;
                return;
            }
            if (IsLibraryDocument(document, occurrence?.Definition, libraryPaths))
            {
                result.SkippedLibrary++;
                return;
            }
        }

        items[key] = new StepPartItem
        {
            Document = document,
            DocumentKey = key,
            PartNumber = ReadPartNumber(document),
            Material = ReadMaterial(document)
        };
        result.Items.Add(items[key]);
    }

    private static bool IsMember(ComponentDefinition definition)
    {
        foreach (var propertyName in new[] { "IsiPartMember", "IsiPartFactory", "IsiAssemblyMember", "IsiAssemblyFactory" })
        {
            try
            {
                var property = definition.GetType().GetProperty(propertyName);
                if (property?.GetValue(definition) is true)
                    return true;
            }
            catch
            {
            }
        }
        return false;
    }

    private static bool IsLibraryDocument(Document document, ComponentDefinition? definition,
        IReadOnlyCollection<string>? libraryPaths)
    {
        try
        {
            var property = definition?.GetType().GetProperty("IsContentMember");
            if (property?.GetValue(definition) is true)
                return true;
        }
        catch
        {
        }

        if (libraryPaths is null)
            return false;

        var path = document.FullFileName;
        return libraryPaths.Any(libraryPath => path.StartsWith(libraryPath, StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyCollection<string> LoadLibraryPaths()
    {
        var paths = new List<string>();
        try
        {
            var project = _application.DesignProjectManager.ActiveDesignProject;
            var projectDirectory = Path.GetDirectoryName(project.FullFileName);
            foreach (var path in project.LibraryPaths.Cast<ProjectPath>())
            {
                AddLibraryPath(paths, path.Path, projectDirectory);
            }
            AddLibraryPath(paths, project.ContentCenterPath, projectDirectory);
        }
        catch
        {
        }
        return paths;
    }

    private static void AddLibraryPath(ICollection<string> paths, string? path, string? projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        try
        {
            if (!Path.IsPathRooted(path) && !string.IsNullOrWhiteSpace(projectDirectory))
                path = Path.Combine(projectDirectory, path);
            path = Path.GetFullPath(path);
            if (!path.EndsWith(Path.DirectorySeparatorChar))
                path += Path.DirectorySeparatorChar;
            paths.Add(path);
        }
        catch
        {
        }
    }

    private static string ReadPartNumber(Document document)
    {
        try
        {
            var value = document.PropertySets["Design Tracking Properties"]["Part Number"].Value as string;
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        catch
        {
        }
        try
        {
            return Path.GetFileNameWithoutExtension(document.FullFileName);
        }
        catch
        {
            return document.DisplayName;
        }
    }

    private static string ReadMaterial(Document document)
    {
        try
        {
            var value = document.PropertySets["Design Tracking Properties"]["Material"].Value as string;
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        catch
        {
        }
        try
        {
            return (document as PartDocument)?.ActiveMaterial.Name ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public StepExportResult Export(IReadOnlyList<StepPartItem> items, string folder,
        StepDuplicateMode duplicateMode, Func<string, StepDuplicateAnswer>? askDuplicate,
        Action<int, int, StepPartItem>? progress, Func<bool>? isCancelled)
    {
        var result = new StepExportResult();
        Directory.CreateDirectory(folder);

        var translator = GetStepTranslator();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mode = duplicateMode;

        for (var index = 0; index < items.Count; index++)
        {
            if (isCancelled?.Invoke() == true)
            {
                result.Cancelled = true;
                break;
            }

            var item = items[index];
            item.Status = "Đang xuất...";
            progress?.Invoke(index, items.Count, item);

            var baseName = SanitizeName(item.PartNumber);
            var fileName = baseName + ".stp";
            var duplicateNumber = 1;
            while (usedNames.Contains(fileName))
                fileName = baseName + "_" + (++duplicateNumber) + ".stp";
            usedNames.Add(fileName);

            var fullPath = Path.Combine(folder, fileName);
            var skip = false;
            if (File.Exists(fullPath))
            {
                if (mode == StepDuplicateMode.Ask)
                {
                    var answer = askDuplicate?.Invoke(fullPath) ?? StepDuplicateAnswer.Cancel;
                    switch (answer)
                    {
                        case StepDuplicateAnswer.OverwriteAll:
                            mode = StepDuplicateMode.Overwrite;
                            break;
                        case StepDuplicateAnswer.Skip:
                            skip = true;
                            break;
                        case StepDuplicateAnswer.SkipAll:
                            mode = StepDuplicateMode.Skip;
                            skip = true;
                            break;
                        case StepDuplicateAnswer.Cancel:
                            result.Cancelled = true;
                            item.Status = string.Empty;
                            progress?.Invoke(index, items.Count, item);
                            return result;
                    }
                }
                else if (mode == StepDuplicateMode.Skip)
                {
                    skip = true;
                }
            }

            if (skip)
            {
                item.Status = "Bỏ qua (đã tồn tại)";
                result.Skipped++;
            }
            else
            {
                try
                {
                    ExportOne(translator, item.Document, fullPath);
                    if (!File.Exists(fullPath))
                        throw new InvalidOperationException("Không tìm thấy file STEP sau khi xuất.");
                    item.Status = "Đã xuất";
                    result.Success++;
                }
                catch (Exception ex)
                {
                    item.Status = "Lỗi";
                    result.Failed++;
                    result.Errors.Add($"{item.PartNumber}: {ex.Message}");
                }
            }
            progress?.Invoke(index + 1, items.Count, item);
        }
        return result;
    }

    private TranslatorAddIn GetStepTranslator()
    {
        var addIn = _application.ApplicationAddIns.ItemById(StepTranslatorId);
        if (addIn is null)
            throw new InvalidOperationException("Không tìm thấy STEP Translator của Inventor.");
        if (!addIn.Activated)
            addIn.Activate();
        return (TranslatorAddIn)addIn;
    }

    private void ExportOne(TranslatorAddIn translator, Document document, string fullPath)
    {
        var context = _application.TransientObjects.CreateTranslationContext();
        context.Type = IOMechanismEnum.kFileBrowseIOMechanism;

        var options = _application.TransientObjects.CreateNameValueMap();
        translator.HasSaveCopyAsOptions(document, context, options);
        options.Value["ApplicationProtocolType"] = Ap214Automotive;

        var medium = _application.TransientObjects.CreateDataMedium();
        medium.FileName = fullPath;
        translator.SaveCopyAs(document, context, options, medium);
    }

    public static string SanitizeName(string name)
    {
        var result = (name ?? string.Empty).Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
            result = result.Replace(character, '_');
        result = result.TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(result) ? "NoName" : result;
    }

    public string GetPreview(StepPartItem item)
    {
        var directory = Path.Combine(Path.GetTempPath(), "InventorDXFExporter", "StepPreview");
        Directory.CreateDirectory(directory);

        using var hash = SHA256.Create();
        var name = Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(item.DocumentKey)))
            .Substring(0, 24) + ".png";
        var previewPath = Path.Combine(directory, name);
        RenderPreview(item.Document, previewPath);
        return previewPath;
    }

    private void RenderPreview(Document document, string previewPath)
    {
        var homeDocument = _application.ActiveDocument;
        var openedForPreview = false;
        View? view = null;
        Window? window = null;

        try
        {
            if (document.Views.Count == 0)
            {
                document = _application.Documents.Open(document.FullFileName, true);
                openedForPreview = true;
            }
            else
            {
                document.Activate();
            }

            view = _application.ActiveView;
            window = view?.Parent;
            var camera = view!.Camera;
            camera.ViewOrientationType = ViewOrientationTypeEnum.kIsoTopRightViewOrientation;
            camera.Apply();
            view.Fit();
            var color = _application.TransientObjects.CreateColor(255, 255, 255);
            view.SaveAsBitmap(previewPath, 900, 640, color, color);
        }
        finally
        {
            if (openedForPreview)
                window?.Close();
            if (homeDocument is not null)
                homeDocument.Activate();
        }
    }
}
