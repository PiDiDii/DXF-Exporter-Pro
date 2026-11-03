# Inventor 2027 Add-in Deployment

## Registry-free manifest

The Inventor 2027 API Help's **Creating an Inventor Add-in** topic describes a registry-free `.addin` XML manifest and the supported discovery locations. Prefer registry-free deployment for new add-ins; do not copy manifest XML from a different host release without checking 2027 documentation.

The documented manifest includes identity and load fields such as `ClassId`, `ClientId`, `DisplayName`, `Assembly`, and `LoadAutomatically`. The 2027 release adds `UseInventorAssemblyContext` for .NET add-ins; the help says it defaults to the Inventor assembly load context when omitted. Setting it to `0` opts into an add-in-specific context, with dependencies first sought in the add-in directory and then the Inventor installation folder. This setting does not apply when the binary is located in Inventor's Bin folder.

## Documented discovery locations

The local 2027 API Help lists these locations:

- All users, version independent: `%ALLUSERSPROFILE%\Autodesk\Inventor Addins\`
- All users, version dependent: `%PROGRAMFILES%\Autodesk\Inventor 20xx\Bin\Addins\`
- Per user, version dependent: `%APPDATA%\Autodesk\Inventor 20xx\Addins\`
- Per user, version independent: `%APPDATA%\Autodesk\ApplicationPlugins\`

For a version-dependent all-user manifest, Inventor 2024+ uses the `Bin\Addins` location described by the reference rather than the older legacy directory. All-user installation generally requires administrative privileges.

## Framework and interop

- Inventor 2027 API Help `What's New` recommends updating .NET/.NET Framework-based add-ins to .NET 10.
- The SDK readme's older wizard note describes Visual Studio 2022 add-in templates targeting .NET 8 (introduced for Inventor 2025). Prefer the release-specific 2027 `What's New` guidance, but inspect the template and migration guide available in the actual environment.
- Match the interop assembly to the Inventor version and project. The installed 2027 SDK readme states that the .NET add-in should reference the Inventor `Bin` interop assembly or ship the correct local copy; for a manual re-reference it advises Embed Interop Types = false and Copy Local = true.
- Build and test the actual deployment layout. Successful compilation does not prove manifest discovery or dependency resolution.

## Deployment checklist

- Generate a unique add-in `ClassId`/`ClientId`; keep manifest identity and code attributes aligned.
- Use the correct manifest XML element names/casing from the 2027 reference.
- Verify assembly path resolution, dependencies, architecture, target framework, and `UseInventorAssemblyContext`.
- Place the manifest only in an intended discovery location.
- Test install, load-on-startup, manual load/unload, update, and removal in Inventor 2027.
- Keep customer/project files out of logs and deployment packages.

## Sources

- Autodesk Inventor 2027 [Creating an Inventor Add-in](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/CreatingAnAddIn_Overview.htm).
- Autodesk Inventor 2027 [What's New](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WhatsNew.htm).
- Autodesk Inventor 2027 installed `SDK_Readme.htm` and `Autodesk.Inventor.Interop.dll`.
