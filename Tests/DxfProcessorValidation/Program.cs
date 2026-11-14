using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using InventorDxfExporter.Settings;
using ClosedXML.Excel;

var valid = new[]
{
    "0", "SECTION", "2", "HEADER", "0", "ENDSEC",
    "0", "SECTION", "2", "ENTITIES",
    "0", "LINE", "8", "0", "10", "0", "20", "0", "11", "1", "21", "1",
    "0", "ENDSEC", "0", "EOF"
};
Require(DxfProcessor.ValidateDxfLines(valid), "A structurally valid DXF was rejected.");

var oddLineCount = valid.Concat(new[] { "trailing" }).ToArray();
Require(!DxfProcessor.ValidateDxfLines(oddLineCount), "An unpaired group value was accepted.");

var invalidGroupCode = valid.ToArray();
invalidGroupCode[0] = "not-a-code";
Require(!DxfProcessor.ValidateDxfLines(invalidGroupCode), "A nonnumeric group code was accepted.");

var emptyEntities = new[]
{
    "0", "SECTION", "2", "HEADER", "0", "ENDSEC",
    "0", "SECTION", "2", "ENTITIES", "0", "ENDSEC", "0", "EOF"
};
Require(!DxfProcessor.ValidateDxfLines(emptyEntities), "An empty ENTITIES section was accepted.");

var layerSettings = new ExportSettings();
layerSettings.LayerSettings["IV_BEND_UP"] = "True";
layerSettings.LayerSettings["IV_BEND_DOWN"] = "True";
var fallbackOptions = DxfLayerOptionsBuilder.Build(layerSettings, includeLineTypes: false);
Require(fallbackOptions.Contains("AcadVersion=R12", StringComparison.Ordinal), "R12 was not selected.");
Require(fallbackOptions.Contains("BendUpLayer=IV_BEND_UP", StringComparison.Ordinal), "The selected bend-up layer was not mapped.");
Require(fallbackOptions.Contains("BendDownLayer=IV_BEND_DOWN", StringComparison.Ordinal), "The selected bend-down layer was not mapped.");
Require(fallbackOptions.Contains("InvisibleLayers=IV_TOOL_CENTER;IV_ARC_CENTERS;IV_TANGENT;IV_FEATURE_PROFILES;IV_UNCONSUMED_SKETCHES;IV_ROLL_TANGENT;IV_ROLL", StringComparison.Ordinal),
    "The fallback options do not hide every unchecked layer.");
Require(!fallbackOptions.Contains("LineType=", StringComparison.Ordinal), "Fallback options should omit optional line-type settings.");

var tempDirectory = Path.Combine(Path.GetTempPath(), "DxfProcessorValidation-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempDirectory);
try
{
    var input = Path.Combine(tempDirectory, "input.dxf");
    var output = Path.Combine(tempDirectory, "output.dxf");
    File.WriteAllLines(input, valid, DxfProcessor.DxfEncoding());
    File.WriteAllText(output, "previous output");

    var options = new DxfProcessOptions("", "Giữ nguyên đường dài", 10, 3, 3, 3, false, "Căn giữa", 10, 15, -250);
    var malformedInput = Path.Combine(tempDirectory, "malformed.dxf");
    File.WriteAllLines(malformedInput, oddLineCount, DxfProcessor.DxfEncoding());
    try
    {
        new DxfProcessor().Process(malformedInput, output, options);
        throw new InvalidOperationException("Malformed DXF input should fail post-processing.");
    }
    catch (InvalidDataException)
    {
    }

    Require(File.ReadAllText(output) == "previous output", "A malformed DXF replaced the existing output.");
    new DxfProcessor().Process(input, output, options);
    Require(new DxfProcessor().Validate(output), "Post-processed DXF did not pass validation.");

    var templatePath = Path.Combine(tempDirectory, "template.xlsx");
    var outputDirectory = Path.Combine(tempDirectory, "bom-output");
    using (var template = new XLWorkbook())
    {
        var firstSheet = template.Worksheets.Add("Template");
        firstSheet.Cell("A7").Value = "Project placeholder";
        firstSheet.Cell("M8").Value = "Date placeholder";
        firstSheet.Cell("A11").Value = "template STT";
        firstSheet.Cell("D11").Value = "image placeholder";
        firstSheet.Cell("Z11").Value = "untouched";
        template.Worksheets.Add("Other").Cell("A1").Value = "untouched sheet";
        template.SaveAs(templatePath);
    }

    var bomSettings = new BomExportSettings
    {
        TemplatePath = templatePath,
        OutputDirectory = outputDirectory,
        StartRow = 11
    };
    var settingsPath = Path.Combine(tempDirectory, "bom-settings.json");
    File.WriteAllText(settingsPath,
        "{\"TemplatePath\":\"template.xlsx\",\"OutputDirectory\":\"\",\"StartRow\":11,\"Columns\":[{\"Field\":\"Hình ảnh\",\"Enabled\":false,\"Column\":\"D\"}]}");
    var settingsStore = new BomExportSettingsStore(settingsPath);
    var migratedSettings = settingsStore.Load();
    Require(migratedSettings.Columns.First(mapping => mapping.Field == "Hình ảnh").Enabled,
        "An existing BOM configuration was not migrated to enable image export.");
    settingsStore.Save(migratedSettings);
    migratedSettings.Columns.First(mapping => mapping.Field == "Hình ảnh").Enabled = false;
    settingsStore.Save(migratedSettings);
    Require(!settingsStore.Load().Columns.First(mapping => mapping.Field == "Hình ảnh").Enabled,
        "A user's choice to disable image export was not preserved.");

    var bomOutput = BomTemplateWriter.Write(
        bomSettings,
        [new BomTemplateRow(1, "Bracket", "100 x 50 mm", "SS400", 3, CreateTestImage())],
        new DateTime(2026, 11, 13));
    Require(File.Exists(bomOutput), "BOM template output was not created.");
    using (var result = new XLWorkbook(bomOutput))
    {
        var firstSheet = result.Worksheet(1);
        Require(firstSheet.Cell("A7").GetString() == "Project placeholder", "Project metadata in the template should remain untouched.");
        Require(firstSheet.Cell("M8").GetString() == "Date placeholder", "Date metadata in the template should remain untouched.");
        Require(firstSheet.Cell("A11").GetValue<int>() == 1, "Sequence number was not written.");
        Require(firstSheet.Cell("C11").GetString() == "Bracket", "Detail name was not written.");
        Require(firstSheet.Cell("D11").GetString() == "image placeholder", "Image placeholder should remain untouched.");
        Require(firstSheet.Cell("E11").GetString() == "100 x 50 mm", "Dimensions were not written.");
        Require(firstSheet.Cell("K11").GetString() == "SS400", "Material was not written to its configured column.");
        Require(firstSheet.Cell("M11").GetValue<int>() == 3, "Quantity was not written.");
        Require(firstSheet.Pictures.Count == 1, "The BOM image was not embedded in the workbook.");
        Require(firstSheet.Row(11).Height >= 50, "The image row was not enlarged to display the image.");
        Require(firstSheet.Cell("Z11").GetString() == "untouched", "Unmapped template cells should remain untouched.");
        Require(result.Worksheets.Count == 2 && result.Worksheet(2).Cell("A1").GetString() == "untouched sheet",
            "The template's other worksheets should remain untouched.");
    }
    using (var original = new XLWorkbook(templatePath))
        Require(original.Worksheet(1).Cell("A11").GetString() == "template STT", "The source template was modified.");

    Require(BomTemplateWriter.ColumnNumber("M") == 13 && BomTemplateWriter.ColumnNumber("AA") == 27,
        "Excel column conversion returned an incorrect index.");
    Require(BomExportSettings.CreateDefaultColumns().All(mapping =>
        !mapping.Field.Equals("Nhà cung cấp", StringComparison.OrdinalIgnoreCase)),
        "The supplier field should not be included in the default BOM mapping.");
    try
    {
        _ = BomTemplateWriter.ColumnNumber("XFE");
        throw new InvalidOperationException("Columns beyond XFD must be rejected.");
    }
    catch (ArgumentOutOfRangeException)
    {
    }
}
finally
{
    Directory.Delete(tempDirectory, recursive: true);
}

Console.WriteLine("DXF structure and BOM template validation checks passed.");

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static byte[] CreateTestImage()
{
    using var image = new System.Drawing.Bitmap(2, 2);
    using var stream = new MemoryStream();
    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
    return stream.ToArray();
}
