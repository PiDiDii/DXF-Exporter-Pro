using InventorDxfExporter.Core;
using InventorDxfExporter.Services;
using InventorDxfExporter.Settings;
using ClosedXML.Excel;
using System.Globalization;

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
Require(FileNameBuilder.BuildFileName("T2", 3, "SS400", "Bracket") == "T2_Q3_SS400_Bracket.dxf",
    "The standard DXF filename format changed.");

var layerSettings = new ExportSettings();
Require(!layerSettings.ExportBomAfterDxf,
    "DXF export must not create BOM output by default.");
layerSettings.LayerSettings["IV_BEND_UP"] = "True";
layerSettings.LayerSettings["IV_BEND_DOWN"] = "True";
var fallbackOptions = DxfLayerOptionsBuilder.Build(layerSettings);
Require(fallbackOptions.Contains("AcadVersion=R12", StringComparison.Ordinal), "R12 was not selected.");
Require(fallbackOptions.Contains("BendUpLayer=IV_BEND_UP", StringComparison.Ordinal), "The selected bend-up layer was not mapped.");
Require(fallbackOptions.Contains("BendDownLayer=IV_BEND_DOWN", StringComparison.Ordinal), "The selected bend-down layer was not mapped.");
Require(fallbackOptions.Contains("InvisibleLayers=IV_TOOL_CENTER;IV_Tool_center;IV_ARC_CENTERS;IV_TANGENT;IV_FEATURE_PROFILES;IV_UNCONSUMED_SKETCHES;IV_ROLL_TANGENT;IV_ROLL;IV_MARK_SURFACE", StringComparison.Ordinal),
    "The fallback options do not hide every unchecked layer.");
Require(!fallbackOptions.Contains("LineType=", StringComparison.Ordinal), "DXF options must omit unsupported line-type settings.");
var surfaceLayerEnabled = new ExportSettings();
surfaceLayerEnabled.LayerSettings["IV_MARK_SURFACE"] = "True";
Require(!DxfLayerOptionsBuilder.Build(surfaceLayerEnabled).Contains("IV_MARK_SURFACE", StringComparison.Ordinal),
    "An enabled surface-mark layer was incorrectly added to InvisibleLayers.");
var toolCenterEnabled = new ExportSettings();
toolCenterEnabled.LayerSettings["IV_TOOL_CENTER"] = "True";
var visibleToolCenterOptions = DxfLayerOptionsBuilder.Build(toolCenterEnabled);
Require(!visibleToolCenterOptions.Contains("InvisibleLayers=IV_TOOL_CENTER", StringComparison.Ordinal)
    && !visibleToolCenterOptions.Contains("IV_Tool_center", StringComparison.Ordinal),
    "An enabled tool-center layer was incorrectly hidden.");
Require(BomDimensionFormatter.Format(239.47, 105, "T1.5") == "239,47 x 105 x 1,5",
    "BOM dimensions must include thickness, omit units, and use decimal commas.");
Require(BomDimensionFormatter.Format(239, 105, "T_Khong_Xac_Dinh") == "239 x 105 x Chưa xác định",
    "An unknown sheet-metal thickness should be explicit in BOM dimensions.");
Require(BomDimensionFormatter.Format(null, 105, "T1.5") == string.Empty,
    "BOM dimensions should be blank when the flat-pattern size is unavailable.");
var vietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
Require(UiNumberParser.ParseDecimal("2,5", vietnameseCulture) == 2.5M,
    "Localized decimal-comma input was not accepted.");
Require(UiNumberParser.ParseDecimal("2.5", vietnameseCulture) == 2.5M,
    "Invariant decimal-point input was not accepted under vi-VN.");
var callerSettings = new ExportSettings { OutputRoot = "original" };
callerSettings.LayerSettings["IV_TOOL_CENTER"] = "False";
var workingSettings = new ExportSettings();
ExportSettingsSnapshot.From(callerSettings).Apply(workingSettings);
workingSettings.OutputRoot = "draft";
workingSettings.LayerSettings["IV_TOOL_CENTER"] = "True";
Require(callerSettings.OutputRoot == "original" && callerSettings.LayerSettings["IV_TOOL_CENTER"] == "False",
    "Editing an ExportSettings working copy changed the caller's settings.");
var previousCulture = CultureInfo.CurrentCulture;
try
{
    CultureInfo.CurrentCulture = vietnameseCulture;
    Require(BomDimensionFormatter.FormatThicknessDisplay("T1.5") == "1,5",
        "Preview thickness formatting did not use the shared culture-aware formatter.");
}
finally
{
    CultureInfo.CurrentCulture = previousCulture;
}
BomTemplateWriter.ValidateCellAddress("XFD1048576");
try
{
    BomTemplateWriter.ValidateCellAddress("XFE1");
    throw new InvalidOperationException("Cell addresses beyond XFD must be rejected.");
}
catch (ArgumentOutOfRangeException)
{
}

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

    var layeredDxf = Path.Combine(tempDirectory, "layered.dxf");
    File.WriteAllLines(layeredDxf, CreateLayeredDxf(), DxfProcessor.DxfEncoding());
    var layerOptions = options with { ExcludedLayers = DxfLayerOptionsBuilder.GetInvisibleLayers(layerSettings) };
    new DxfProcessor().Process(layeredDxf, output, layerOptions);
    var filteredDxf = File.ReadAllLines(output, DxfProcessor.DxfEncoding());
    Require(DxfProcessor.ValidateDxfLines(filteredDxf), "Layer filtering produced an invalid DXF.");
    Require(DxfProcessor.CountDxfEntities(filteredDxf) == 1
        && !filteredDxf.Any(line => line.Equals("IV_TOOL_CENTER", StringComparison.OrdinalIgnoreCase)
            || line.Equals("IV_Tool_center", StringComparison.OrdinalIgnoreCase)),
        "Unchecked tool-center geometry or layer records were retained in the output.");
    Require(filteredDxf.Contains("IV_OUTER_PROFILE"),
        "Layer filtering removed an enabled layer.");
    var layerTableCountIndex = Array.FindIndex(filteredDxf, line => line == "70");
    Require(layerTableCountIndex >= 0 && filteredDxf[layerTableCountIndex + 1] == "1",
        "Layer filtering did not update the LAYER table record count.");

    var blockLayeredDxf = Path.Combine(tempDirectory, "block-layered.dxf");
    File.WriteAllLines(blockLayeredDxf, CreateBlockLayeredDxf(), DxfProcessor.DxfEncoding());
    new DxfProcessor().Process(blockLayeredDxf, output, layerOptions);
    var filteredBlockDxf = File.ReadAllLines(output, DxfProcessor.DxfEncoding());
    Require(DxfProcessor.ValidateDxfLines(filteredBlockDxf)
        && !filteredBlockDxf.Contains("IV_TOOL_CENTER", StringComparer.OrdinalIgnoreCase)
        && filteredBlockDxf.Contains("IV_OUTER_PROFILE")
        && filteredBlockDxf.Contains("BLOCK")
        && filteredBlockDxf.Contains("ENDBLK")
        && filteredBlockDxf.Count(line => line == "LINE") == 1,
        "Layer filtering did not preserve block structure while removing excluded block geometry.");

    var bendDxf = new List<string>
    {
        "0", "SECTION", "2", "HEADER", "0", "ENDSEC",
        "0", "SECTION", "2", "ENTITIES",
        "0", "LINE", "8", "IV_BEND_UP", "10", "0", "20", "0", "11", "20", "21", "0",
        "0", "LINE", "8", "IV_BEND_UP", "10", "20", "20", "0", "11", "40", "21", "0",
        "0", "ENDSEC", "0", "EOF"
    };
    Require(DxfProcessor.ConvertBendLinesToEndMarks(bendDxf, options) == 1,
        "Contiguous bend lines were not grouped into one bend mark.");
    Require(DxfProcessor.ValidateDxfLines(bendDxf), "Bend-line post-processing produced an invalid DXF.");
    Require(DxfProcessor.CountDxfEntities(bendDxf) == 2, "Bend-line conversion did not produce the expected end marks.");

    var textDxf = CreateDxfWithTables();
    DxfProcessor.AddInfoTextToDxf(textDxf,
        new ExportItem("part.ipt", "Bracket", "SS400", "T2", 3),
        "Căn giữa", 10, 15, -20);
    Require(DxfProcessor.ValidateDxfLines(textDxf), "TEXT insertion produced an invalid DXF.");
    Require(DxfProcessor.CountDxfEntities(textDxf) == 5, "TEXT insertion did not add exactly four entities.");
    Require(textDxf.Contains("DXF_INFO") && textDxf.Contains("DXF_INFO_STYLE"),
        "TEXT layer/style entries were not added to the DXF tables.");

    var retiredIniPath = Path.Combine(tempDirectory, "retired-settings.ini");
    File.WriteAllLines(retiredIniPath, ["OutputRoot=old-output"]);
    var exportSettingsPath = Path.Combine(tempDirectory, "ExportSettings.json");
    var exportSettingsStore = new ExportSettingsStore(exportSettingsPath);
    var exportSettings = exportSettingsStore.Load();
    Require(exportSettings.OutputRoot != "old-output" && !exportSettings.ExportBomAfterDxf,
        "Settings from the retired INI format must not be imported.");
    exportSettings.ExportBomAfterDxf = false;
    exportSettings.ConvFilter = "BRACKET";
    exportSettings.ConvMode = 1;
    exportSettings.ConvWriteLog = false;
    exportSettings.LayerSettings["IV_BEND_UP"] = "True";
    exportSettings.LayerSettings["IV_MARK_SURFACE"] = "True";
    exportSettingsStore.Save(exportSettings);
    var reloadedSettings = exportSettingsStore.Load();
    Require(!reloadedSettings.ExportBomAfterDxf
        && reloadedSettings.ConvFilter == "BRACKET" && reloadedSettings.ConvMode == 1
        && !reloadedSettings.ConvWriteLog && reloadedSettings.LayerSettings["iv_bend_up"] == "True"
        && reloadedSettings.LayerSettings["iv_mark_surface"] == "True",
        "Application settings were not persisted in the current JSON format.");
    var legacySettingsPath = Path.Combine(tempDirectory, "legacy-ExportSettings.json");
    File.WriteAllText(legacySettingsPath, "{\"ConvFilter\":\"LEGACY\"}");
    Require(new ExportSettingsStore(legacySettingsPath).Load().ConvWriteLog,
        "Existing settings without a log option must retain the previous log-enabled behavior.");

    var profileDirectory = Path.Combine(tempDirectory, "profiles");
    Directory.CreateDirectory(profileDirectory);
    var profileStore = new SettingsProfileStore(profileDirectory);
    var profileSettings = new ExportSettings { ExportBomAfterDxf = false };
    profileStore.Save("separated", profileSettings);
    var reloadedProfile = new ExportSettings();
    Require(profileStore.TryLoad("separated", reloadedProfile)
        && !reloadedProfile.ExportBomAfterDxf,
        "A profile did not preserve the independent options.");

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
        StartRow = 11,
        ProjectNameCell = "A7",
        ExportDateCell = "M8"
    };
    bomSettings.Columns.Add(new BomColumnMapping("Nhà cung cấp", false, "B"));
    foreach (var field in new[] { "Tên file", "Đường dẫn file", "Độ dày" })
        bomSettings.Columns.First(mapping => mapping.Field == field).Enabled = true;
    var bomSettingsPath = Path.Combine(tempDirectory, "saved-bom-settings.json");
    var bomSettingsStore = new BomExportSettingsStore(bomSettingsPath);
    bomSettingsStore.Save(bomSettings);
    var reloadedBomSettings = bomSettingsStore.Load();
    Require(reloadedBomSettings.SchemaVersion == 2
        && reloadedBomSettings.ProjectNameCell == "A7"
        && reloadedBomSettings.ExportDateCell == "M8"
        && reloadedBomSettings.Columns.All(mapping =>
            !mapping.Field.Equals("Nhà cung cấp", StringComparison.OrdinalIgnoreCase)),
        "BOM metadata cells or the updated settings schema were not persisted.");
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
        [new BomTemplateRow(
            1,
            "Bracket",
            BomDimensionFormatter.Format(100, 50, "T2.5"),
            "SS400",
            3,
            CreateTestImage(),
            "Bracket.ipt",
            @"C:\parts\Bracket.ipt",
            "2,5")],
        "LV2.0",
        new DateTime(2026, 11, 13));
    Require(File.Exists(bomOutput), "BOM template output was not created.");
    using (var result = new XLWorkbook(bomOutput))
    {
        var firstSheet = result.Worksheet(1);
        Require(firstSheet.Cell("A7").GetString() == "LV2.0", "Project metadata was not written to its configured cell.");
        Require(firstSheet.Cell("M8").GetString() == "13/11/2026", "Date metadata was not formatted or written to its configured cell.");
        Require(firstSheet.Cell("A11").GetValue<int>() == 1, "Sequence number was not written.");
        Require(firstSheet.Cell("C11").GetString() == "Bracket", "Detail name was not written.");
        Require(firstSheet.Cell("D11").GetString() == "image placeholder", "Image placeholder should remain untouched.");
        Require(firstSheet.Cell("E11").GetString() == "100 x 50 x 2,5",
            "The formatted dimensions were not written to the configured column.");
        Require(firstSheet.Cell("K11").GetString() == "SS400", "Material was not written to its configured column.");
        Require(firstSheet.Cell("M11").GetValue<int>() == 3, "Quantity was not written.");
        Require(firstSheet.Cell("N11").GetString() == "Bracket.ipt", "The source filename mapping was not written.");
        Require(firstSheet.Cell("O11").GetString() == @"C:\parts\Bracket.ipt", "The source path mapping was not written.");
        Require(firstSheet.Cell("P11").GetString() == "2,5", "The thickness mapping was not written.");
        Require(firstSheet.Pictures.Count == 1, "The BOM image was not embedded in the workbook.");
        Require(firstSheet.Row(11).Height >= 50, "The image row was not enlarged to display the image.");
        Require(firstSheet.Cell("Z11").GetString() == "untouched", "Unmapped template cells should remain untouched.");
        Require(result.Worksheets.Count == 2 && result.Worksheet(2).Cell("A1").GetString() == "untouched sheet",
            "The template's other worksheets should remain untouched.");
    }
    using (var original = new XLWorkbook(templatePath))
        Require(original.Worksheet(1).Cell("A11").GetString() == "template STT"
            && original.Worksheet(1).Cell("A7").GetString() == "Project placeholder",
            "The source template was modified.");

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

static List<string> CreateDxfWithTables() =>
[
    "0", "SECTION", "2", "HEADER", "0", "ENDSEC",
    "0", "SECTION", "2", "TABLES",
    "0", "TABLE", "2", "LAYER", "70", "0", "0", "ENDTAB",
    "0", "TABLE", "2", "STYLE", "70", "0", "0", "ENDTAB",
    "0", "ENDSEC",
    "0", "SECTION", "2", "ENTITIES",
    "0", "LINE", "8", "0", "10", "0", "20", "0", "11", "100", "21", "50",
    "0", "ENDSEC", "0", "EOF"
];

static List<string> CreateLayeredDxf() =>
[
    "0", "SECTION", "2", "HEADER", "0", "ENDSEC",
    "0", "SECTION", "2", "TABLES",
    "0", "TABLE", "2", "LAYER", "70", "3",
    "0", "LAYER", "2", "IV_OUTER_PROFILE", "70", "0", "62", "7", "6", "CONTINUOUS",
    "0", "LAYER", "2", "IV_TOOL_CENTER", "70", "0", "62", "7", "6", "CONTINUOUS",
    "0", "LAYER", "2", "IV_Tool_center", "70", "0", "62", "7", "6", "CONTINUOUS",
    "0", "ENDTAB", "0", "ENDSEC",
    "0", "SECTION", "2", "ENTITIES",
    "0", "LINE", "8", "IV_OUTER_PROFILE", "10", "0", "20", "0", "11", "100", "21", "50",
    "0", "LINE", "8", "IV_TOOL_CENTER", "10", "1", "20", "1", "11", "2", "21", "2",
    "0", "LINE", "8", "IV_Tool_center", "10", "3", "20", "3", "11", "4", "21", "4",
    "0", "ENDSEC", "0", "EOF"
];

static List<string> CreateBlockLayeredDxf() =>
[
    "0", "SECTION", "2", "HEADER", "0", "ENDSEC",
    "0", "SECTION", "2", "BLOCKS",
    "0", "BLOCK", "8", "0", "2", "SAMPLE", "70", "0", "10", "0", "20", "0", "30", "0",
    "0", "LINE", "8", "IV_TOOL_CENTER", "10", "0", "20", "0", "11", "1", "21", "1",
    "0", "LINE", "8", "IV_OUTER_PROFILE", "10", "2", "20", "2", "11", "3", "21", "3",
    "0", "ENDBLK", "8", "0",
    "0", "ENDSEC",
    "0", "SECTION", "2", "ENTITIES",
    "0", "INSERT", "8", "IV_OUTER_PROFILE", "2", "SAMPLE", "10", "0", "20", "0",
    "0", "ENDSEC", "0", "EOF"
];
