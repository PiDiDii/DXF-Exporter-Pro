using Inventor;

namespace InventorDxfExporter.Core;

public enum StepSaveMode
{
    AskEachTime,
    FixedFolder,
    NextToDocument
}

public enum StepDuplicateMode
{
    Ask,
    Overwrite,
    Skip
}

public enum StepDuplicateAnswer
{
    Overwrite,
    OverwriteAll,
    Skip,
    SkipAll,
    Cancel
}

public sealed class StepExportSettings
{
    public StepSaveMode SaveMode { get; set; } = StepSaveMode.AskEachTime;
    public string FixedFolder { get; set; } = string.Empty;
    public StepDuplicateMode DuplicateMode { get; set; } = StepDuplicateMode.Ask;
    public string LastFolder { get; set; } = string.Empty;
}

public sealed class StepPartItem
{
    public required Document Document { get; init; }
    public required string DocumentKey { get; init; }
    public required string PartNumber { get; init; }
    public string Material { get; init; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public bool Export { get; set; } = true;
    public string Status { get; set; } = string.Empty;
}

public sealed class StepCollectResult
{
    public List<StepPartItem> Items { get; } = [];
    public int SkippedLibrary { get; set; }
    public int SkippedOther { get; set; }
    public List<string> Warnings { get; } = [];
}

public sealed class StepExportResult
{
    public int Success { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public bool Cancelled { get; set; }
    public List<string> Errors { get; } = [];
}
