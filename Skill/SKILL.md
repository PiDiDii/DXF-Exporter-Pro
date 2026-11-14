---
name: inventor-2027-addin
description: Develop, debug, review, and test Autodesk Inventor 2027 C# Add-ins using verified Inventor API references and SDK samples. Use for Add-in lifecycle, commands/Ribbon, document/assembly/part/drawing/sheet-metal automation, DXF export, COM interop, and deployment.
---

# Autodesk Inventor 2027 Add-in Skill

## Start here
1. Read repository `AGENTS.md` and `PROJECT_GUIDE.md`.
2. Read the repository's [`references/API_SOURCES.md`](../../references/API_SOURCES.md) and [`references/API_VERIFICATION.md`](../../references/API_VERIFICATION.md).
3. Inspect the actual solution before suggesting changes.
4. Confirm Inventor release, target framework, SDK/API assemblies, Add-in manifest format, and deployment approach from project files and installed SDK.

For domain work, read the matching reference before editing: [lifecycle](references/addin-lifecycle.md), [COM interop](references/com-interop.md), [commands/Ribbon](references/commands-ribbon.md), [documents](references/documents.md), [assemblies](references/assemblies.md), [parts](references/parts.md), [sheet metal](references/sheet-metal.md), [drawings](references/drawings.md), [DataIO](references/data-io.md), [events](references/events.md), [deployment](references/deployment.md), and [troubleshooting](references/troubleshooting.md). Use the curated [API index](../../api/API_INDEX.md) and [object model](../../api/object-model/README.md) to locate exact topics.

## Grounding protocol
For each API used:
- Locate the exact 2027 API topic.
- Verify the exact type/member name, parameter order/types, return type, enum values, and applicable document context.
- Prefer installed Inventor 2027 SDK/API Help for definitive version-specific details.
- Use official Autodesk sources first; use third-party reference sites only as navigational aids or corroboration.
- Record one or more exact evidence statuses from `references/API_VERIFICATION.md`; metadata lookup is not compile verification, and compile verification is not host-runtime verification.
- If a page is inaccessible or the signature is not confirmed, say so. Do not make up a plausible API.
- Include direct references in technical explanations and API notes.

## Add-in implementation protocol
- Confirm the Add-in entry point and lifecycle expected by the SDK (commonly `ApplicationAddInServer`).
- Verify registration/manifest, GUID, load behavior, and deployment details from 2027 documentation/sample.
- Check command creation, Ribbon placement, event hookup/unhooking, and cleanup.
- Inspect whether project runtime/framework changes are necessary. Do not migrate an existing project just because a newer target is available.
- Check dependency loading and manifest settings, including `UseInventorAssemblyContext` where relevant to 2027, against SDK guidance.
- Handle COM exceptions and object lifetime deliberately; avoid indiscriminate `Marshal.ReleaseComObject` patterns unless justified by verified ownership/lifetime.
- Do not copy manifest paths or framework settings from a blog sample without checking the installed Inventor 2027 reference and actual project.

## Domain workflow
### Documents and assemblies
- Verify active document type before casting.
- For assembly traversal, account for subassemblies, suppressed/unresolved occurrences, patterns, and occurrence/document distinctions as relevant to the requested behavior.
- Avoid duplicate processing when the requirement is unique document processing rather than occurrence processing.

### Sheet metal and DXF
- Confirm the component is sheet metal using documented API.
- Verify Flat Pattern availability/state and the documented way to create or access it.
- Confirm export route (`DataIO` vs Translator Add-in), format options, units, layer mapping, and output path against 2027 examples/reference.
- Never assume DXF option strings are universal; source and test the exact options.
- Keep export errors and skipped parts visible in a report.

### Testing
- Build first.
- Run unit/integration tests that exist in the repository.
- Test host-dependent behavior in Inventor 2027 if available.
- Distinguish compile success, automated tests, and manual Inventor runtime verification.
- Do not report a test as passed unless executed.

## Expected response
For a coding task, provide:
1. Brief plan or implementation summary.
2. API members used and their reference links.
3. Files changed.
4. Build/test commands and actual results.
5. Limitations, assumptions, or Inventor-host validation still needed.
