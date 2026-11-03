$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'Preview\ExportSettingsPreview.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK was not found in PATH. Install the .NET 10 SDK.'
}

dotnet run --project $project
if ($LASTEXITCODE -ne 0) {
    throw "UI preview suite failed ($LASTEXITCODE)."
}
