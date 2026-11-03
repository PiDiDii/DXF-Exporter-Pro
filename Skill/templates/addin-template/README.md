# Inventor 2027 Add-in starter template

This minimal C# project implements the Inventor 2027 `ApplicationAddInServer` entry contract and includes a registry-free `.addin` manifest. It intentionally adds no commands, Ribbon UI, automation object, or document operations.

## Build

On Windows with the .NET 10 SDK and Inventor 2027 installed:

```powershell
dotnet build .\templates\addin-template\Inventor2027Addin.csproj
```

For a different Inventor installation path:

```powershell
dotnet build .\templates\addin-template\Inventor2027Addin.csproj -p:InventorBinPath="D:\Autodesk\Inventor 2027\Bin"
```

## Manual local deployment

1. Build the project.
2. Copy the contents of `bin\Debug\net10.0-windows\` (or the matching configuration output directory) to `%APPDATA%\Autodesk\Inventor 2027\Addins\`.
3. Restart Inventor 2027 and load **Inventor 2027 Add-in Template** from the Add-In Manager.
4. Remove the copied files to uninstall it.

The manifest uses a generated example GUID; replace it in both the `.addin` file and the class `Guid` attribute before adapting this starter into a distributable product. `UseInventorAssemblyContext` is omitted, leaving the documented Inventor 2027 default in effect. Review dependency and load-context guidance before adding third-party assemblies.

This is a compile-checked starter, not a deployment certification. It has not been registered, loaded, or runtime-tested in Inventor. Installation, host policy, and dependency resolution must be validated on the target machine.

## Verified API source

- Autodesk Inventor 2027 API Help, [Creating an Inventor Add-in](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/CreatingAnAddIn_Overview.htm), `Addin` manifest and discovery locations; version checked: Inventor 2027.
- Autodesk Inventor 2027 API Help, [ApplicationAddInServer](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationAddInServer.htm) and [ApplicationAddInSite.Application](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationAddInSite_Application.htm); lifecycle contract; version checked: Inventor 2027.
- The project targets `net10.0-windows` per the installed Inventor 2027 API Help `What's New` guidance; the template must be retested if the target framework or host version changes.
