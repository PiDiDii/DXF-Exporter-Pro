[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$InventorBinPath = 'C:\Program Files\Autodesk\Inventor 2027\Bin',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist\InventorDxfExporter')
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'InventorDxfExporter.csproj'
$interop = Join-Path $InventorBinPath 'Autodesk.Inventor.Interop.dll'

if (-not (Test-Path $interop)) {
    throw "Inventor interop was not found: $interop. Install Inventor 2027 or pass -InventorBinPath."
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK was not found in PATH. Install the .NET 10 SDK.'
}

$msbuildProperty = "-p:InventorBinPath=$InventorBinPath"
dotnet restore $project $msbuildProperty
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed ($LASTEXITCODE)." }

dotnet build $project -c $Configuration --no-restore $msbuildProperty
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE). Package was not created." }

$buildOutput = Join-Path $PSScriptRoot "bin\$Configuration\net10.0-windows"
if (-not (Test-Path (Join-Path $buildOutput 'InventorDxfExporter.dll'))) {
    throw "Build output DLL was not found in: $buildOutput"
}
if (-not (Test-Path (Join-Path $buildOutput 'ClosedXML.dll'))) {
    throw "ClosedXML.dll was not copied to the build output: $buildOutput. Ensure runtime package dependencies are copied."
}
if (Test-Path $OutputDirectory) { Remove-Item $OutputDirectory -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Get-ChildItem $buildOutput -File |
    Where-Object { $_.Extension -ne '.pdb' } |
    Copy-Item -Destination $OutputDirectory -Force
Copy-Item (Join-Path $PSScriptRoot 'InventorDxfExporter.addin') -Destination $OutputDirectory -Force
if (-not (Test-Path (Join-Path $OutputDirectory 'ClosedXML.dll'))) {
    throw "ClosedXML.dll is missing from the add-in package: $OutputDirectory"
}

$installPath = Join-Path $env:APPDATA 'Autodesk\Inventor 2027\Addins'
Write-Host "Add-in package created: $OutputDirectory"
Write-Host "Copy the package files to: $installPath"
Write-Host 'Then load DXF Exporter Pro from Inventor Add-In Manager.'
