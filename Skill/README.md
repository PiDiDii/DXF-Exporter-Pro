# Inventor 2027 AI Coding Kit

A source-grounded technical kit for AI-assisted Autodesk Inventor 2027 Add-in development. API evidence, sample compilation, and Inventor-host testing are tracked separately; an unverified signature is not presented as working code.

## Contents

- `AGENTS.md`, `CLAUDE.md`, `PROJECT_GUIDE.md` — coding-agent rules and engineering workflow
- `skills/inventor-2027-addin/SKILL.md` — reusable Agent Skill; specialized guidance is in its `references/` directory
- `references/API_SOURCES.md`, `references/API_VERIFICATION.md` — source precedence and verification ledger
- `api/API_INDEX.md`, `api/API_SOURCES.md`, `api/API_VERIFICATION.md`, `api/API_2027_CHANGES.md` — curated API catalog, source links, verification pointers, and release notes
- `api/object-model/README.md` — add-in, document, assembly, sheet-metal, DXF, and drawing/BOM object hierarchy
- `examples/` — C# samples and a shared compile-check project for add-in lifecycle, Ribbon, assembly traversal, flat patterns, DXF, and drawing parts lists
- `templates/addin-template/` — minimal .NET 10 C# Add-in starter with a registry-free `.addin` manifest
- `tools/api-documentation-indexer/` — offline Python standard-library indexer for installed interop XML

## Use in a coding-agent repository

1. Copy the kit files into the root of the Add-in repository, preserving their directory structure.
2. Keep `skills/inventor-2027-addin/SKILL.md` at that relative path. Register the skill folder with agents that support Agent Skills; keep `AGENTS.md` and `CLAUDE.md` as the repository entrypoints for agents that read those files.
3. Before implementation, inspect the real solution/project, manifest, target framework, and installed Inventor version.
4. Read the skill references and `references/API_SOURCES.md` / `references/API_VERIFICATION.md`; verify each API used against the installed Inventor 2027 reference/SDK and report its evidence status.
5. Build and run tests available in the target repository. Test host-dependent behavior in Inventor 2027 when possible, and report build, test, and host results separately.

## Build and index the examples

On a Windows machine with Inventor 2027 and the .NET 10 SDK installed:

```powershell
dotnet build .\examples\Inventor2027.Examples.csproj
```

Build the standalone starter template with:

```powershell
dotnet build .\templates\addin-template\Inventor2027Addin.csproj
```

Override `InventorBinPath` if the installation is elsewhere:

```powershell
dotnet build .\examples\Inventor2027.Examples.csproj -p:InventorBinPath="D:\Autodesk\Inventor 2027\Bin"
dotnet build .\templates\addin-template\Inventor2027Addin.csproj -p:InventorBinPath="D:\Autodesk\Inventor 2027\Bin"
```

This checks compilation against the local interop assembly; it does not register/load the add-in or run Inventor. See the [API documentation indexer](tools/api-documentation-indexer/README.md) for offline XML indexing; generated entries remain unverified until reviewed.

The starter manifest is configured for manual loading. Follow its README and the Inventor 2027 deployment reference before installing it; a successful build does not verify manifest discovery, host policy, or runtime behavior.

## Sprint 01 environment findings

On the machine inspected for Sprint 01:

- Inventor 2027 is installed. `Inventor.exe` reports ProductVersion `Autodesk Inventor 2027`; `Autodesk.Inventor.Interop.dll` reports version `31.10.26700.0`.
- Inventor 2027 API Help is installed at `C:\Users\Public\Documents\Autodesk\Inventor 2027\Local Help\admapi_31_0.chm`.
- The SDK's `developertools.msi` and `usertools.msi` are present, but DeveloperTools samples and tools were not installed or inspected during Sprint 01.
- At the Sprint 01 checkpoint, the installed Inventor interop XML and reflected metadata verified the lifecycle signatures recorded in `references/API_VERIFICATION.md`; the example had not yet been compiled or runtime-tested. Sprint 02 compile evidence is recorded separately.
- The installed 2027 API Help `What's New` topic separates base 2027 and 2027.1. Reflection confirmed representative 2027.1 symbols in the installed interop assembly; see `api/API_2027_CHANGES.md`.
- At the Sprint 01 checkpoint, the starter kit had no solution/project, so there was no established Add-in target framework or product architecture.
- The searchable third-party API site returned HTTP 402 during inspection. Use the installed API Help as the version-specific reference instead.

These are observations from this machine, not portable assumptions about another user's installation. The .NET SDK versions installed locally do not determine the target framework for an Add-in project.

## Framework note

The installed Inventor 2027 API Help `What's New` topic recommends updating add-ins to .NET 10; the SDK readme contains older Visual Studio wizard notes referring to .NET 8. This kit's shared example project targets .NET 10 to compile-check against the installed 2027 interop assembly. Treat that as an example setting, not a deployment guarantee: verify the current 2027 hosting guidance, migration implications, and actual project before changing an existing target.
