[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$InventorBinPath = 'C:\Program Files\Autodesk\Inventor 2027\Bin',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'),
    [string]$ProductVersion = '1.0.0'
)

$ErrorActionPreference = 'Stop'

if ($ProductVersion -notmatch '^\d+\.\d+\.\d+$') {
    throw 'ProductVersion must have three numeric parts, for example 1.0.0.'
}
$versionParts = $ProductVersion.Split('.') | ForEach-Object { [int]$_ }
if ($versionParts[0] -gt 255 -or $versionParts[1] -gt 255 -or $versionParts[2] -gt 65535) {
    throw 'ProductVersion parts must not exceed 255.255.65535.'
}

$project = Join-Path $PSScriptRoot 'InventorDxfExporter.csproj'
$wixProject = Join-Path $PSScriptRoot 'Installer\InventorDxfExporter.Setup.wixproj'
$interop = Join-Path $InventorBinPath 'Autodesk.Inventor.Interop.dll'
if (-not (Test-Path $interop)) {
    throw "Inventor interop was not found: $interop. Install Inventor 2027 or pass -InventorBinPath."
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK was not found in PATH. Install the .NET 10 SDK.'
}

$stagingRoot = Join-Path $env:TEMP ("InventorDxfExporter-Msi-" + [guid]::NewGuid().ToString('N'))
$payloadDirectory = Join-Path $stagingRoot 'payload'
$payloadWxs = Join-Path $stagingRoot 'PayloadFiles.wxs'
$wixOutputDirectory = Join-Path $stagingRoot 'wix-bin'
$wixIntermediateDirectory = Join-Path $stagingRoot 'wix-obj'
$installPackage = Join-Path $OutputDirectory 'InventorDxfExporter-Setup.msi'

try {
    New-Item -ItemType Directory -Path $payloadDirectory -Force | Out-Null
    $msbuildProperty = "-p:InventorBinPath=$InventorBinPath"

    dotnet restore $project $msbuildProperty
    if ($LASTEXITCODE -ne 0) { throw "Add-in restore failed ($LASTEXITCODE)." }

    $payloadOutput = $payloadDirectory.Replace('\', '/') + '/'
    dotnet build $project -c $Configuration --no-restore $msbuildProperty "-p:OutputPath=$payloadOutput"
    if ($LASTEXITCODE -ne 0) { throw "Add-in build failed ($LASTEXITCODE)." }

    $addInAssembly = Join-Path $payloadDirectory 'InventorDxfExporter.dll'
    $closedXml = Join-Path $payloadDirectory 'ClosedXML.dll'
    if (-not (Test-Path $addInAssembly)) { throw "Add-in assembly was not produced: $addInAssembly" }
    if (-not (Test-Path $closedXml)) { throw "ClosedXML.dll was not produced: $closedXml" }
    Copy-Item (Join-Path $PSScriptRoot 'InventorDxfExporter.addin') -Destination $payloadDirectory

    $componentXml = [System.Text.StringBuilder]::new()
    [void]$componentXml.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"><Fragment><ComponentGroup Id="AddinFiles">')
    $componentIndex = 0
    foreach ($file in Get-ChildItem -LiteralPath $payloadDirectory -File |
        Where-Object { $_.Extension -ne '.pdb' } | Sort-Object Name) {
        $identity = [System.Text.Encoding]::UTF8.GetBytes("PiDiDii.DXFExporter:$($file.Name)")
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try { $guidBytes = $sha256.ComputeHash($identity)[0..15] }
        finally { $sha256.Dispose() }
        $guidBytes[7] = [byte](($guidBytes[7] -band 0x0F) -bor 0x50)
        $guidBytes[8] = [byte](($guidBytes[8] -band 0x3F) -bor 0x80)
        $componentGuid = ([guid]::new([byte[]]$guidBytes)).ToString('B').ToUpperInvariant()
        $source = [System.Security.SecurityElement]::Escape($file.FullName)
        [void]$componentXml.AppendLine("<Component Id=`"PayloadComponent$componentIndex`" Guid=`"$componentGuid`" Directory=`"AddinsFolder`">")
        [void]$componentXml.AppendLine("<RegistryValue Root=`"HKCU`" Key=`"Software\PiDiDii\InventorDxfExporter`" Name=`"PayloadFile$componentIndex`" Type=`"integer`" Value=`"1`" KeyPath=`"yes`" />")
        if ($componentIndex -eq 0) {
            [void]$componentXml.AppendLine('<RemoveFolder Id="RemoveAddinsFolder" Directory="AddinsFolder" On="uninstall" /><RemoveFolder Id="RemoveInventorFolder" Directory="InventorFolder" On="uninstall" /><RemoveFolder Id="RemoveAutodeskFolder" Directory="AutodeskFolder" On="uninstall" />')
        }
        [void]$componentXml.AppendLine("<File Id=`"PayloadFile$componentIndex`" Source=`"$source`" /></Component>")
        $componentIndex++
    }
    [void]$componentXml.AppendLine('</ComponentGroup></Fragment></Wix>')
    [System.IO.File]::WriteAllText($payloadWxs, $componentXml.ToString(), [System.Text.Encoding]::UTF8)

    $wixIntermediate = $wixIntermediateDirectory.Replace('\', '/') + '/'
    $wixOutput = $wixOutputDirectory.Replace('\', '/') + '/'
    dotnet build $wixProject -c Release `
        "-p:PayloadDir=$payloadDirectory" `
        "-p:PayloadWxs=$payloadWxs" `
        "-p:ProductVersion=$ProductVersion" `
        '-p:Platform=x64' `
        "-p:BaseIntermediateOutputPath=$wixIntermediate" `
        "-p:MSBuildProjectExtensionsPath=$wixIntermediate" `
        "-p:OutputPath=$wixOutput"
    if ($LASTEXITCODE -ne 0) { throw "MSI build failed ($LASTEXITCODE)." }

    $builtPackage = Join-Path $wixOutputDirectory 'InventorDxfExporter-Setup.msi'
    if (-not (Test-Path $builtPackage)) { throw "MSI output was not produced: $builtPackage" }
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    Copy-Item $builtPackage -Destination $installPackage -Force

    Write-Host "MSI package created: $installPackage"
    Write-Host 'Close Inventor before installing or upgrading the add-in.'
    Write-Host "Install with: msiexec.exe /i `"$installPackage`""
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}
