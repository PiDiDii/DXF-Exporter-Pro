# Inventor 2027 API and Add-in Changes

**Primary evidence:** installed Autodesk Inventor 2027 API Help, `What's New` topic (`WhatsNew.htm`) from `admapi_31_0.chm`, release metadata `INVNTOR / 2027`; matching interop assembly `31.10.26700.0`. Public reference: [What's New in the Inventor 2027 API](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WhatsNew.htm).

This file is a release-oriented index, not permission to assume a signature. Open the linked topic and confirm all parameters, return types, and context before coding.

## Notable changes recorded by the 2027 API Help

| Change | 2027 evidence | Practical guidance | Status |
|---|---|---|---|
| `.NET 10` recommendation for .NET/.NET Framework-based add-ins | Local `What's New` says updating to .NET 10 is recommended. `CreatingAnAddIn_Overview.htm` explains `UseInventorAssemblyContext`. | Check the actual project's current target and migration guide before changing it. The SDK readme also contains older wizard text referring to .NET 8; do not treat that text as the definitive 2027 recommendation. | Verified from Inventor 2027 Reference |
| `UseInventorAssemblyContext` manifest setting | Local `What's New` and Creating an Add-in topic describe shared Inventor vs add-in-specific assembly-load-context behavior. | Test dependency resolution and the deployed location. The documented behavior applies to .NET add-ins and has location/context caveats. | Verified from Inventor 2027 Reference |
| Irregular pattern distributions | `IrregularDistributionSet`; `RectangularPatternFeatureDefinition.XDirectionIrregularDistributionSet`, `.YDirectionIrregularDistributionSet`; `CircularPatternFeatureDefinition.IrregularDistributionSet`. | Confirm supported pattern definition and collection values before editing a feature. | Verified from Inventor 2027 Reference |
| Slot feature APIs | `SlotFeature` and slot feature collections/API topics are present in the local 2027 reference and release notes. | Resolve the exact definition/placement APIs from the referenced 2027 topic; do not infer creation signatures from the feature name. | Verified from Inventor 2027 Reference |
| Part-document browser folders | `BrowserPane.AddBrowserFolderWithOptions` and `BrowserFolder.PartFeatureFolder` are called out by the release notes. | Confirm target browser pane, part-feature grouping, and option types in the member topics. | Verified from Inventor 2027 Reference |
| Drawing BOM view replacement | `Balloon.ReplaceBOMView` and `PartsList.ReplaceBOMView` are named by the release notes and present in the interop XML. | Check optional `object` parameters and required BOM level/numbering inputs in each exact member topic. | Verified from Inventor 2027 Reference |
| Model-state member document generation | `ModelStates.GenerateOnSaveStatus`, `ModelState.GenerateOnSaveStatus`, and `ModelState.MemberDocumentStatus`; enum values are in `ModelStateMemberGenerationStatusEnum`. | Verify property mutability and enum semantics for the exact object before changing save/generation behavior. | Verified from Inventor 2027 Reference |
| Face feature bend definition | Release notes mention settings for `FaceFeatureDefinition.BendDefinition` and related bend-definition properties. | Verify available setters, valid values, and geometry preconditions in the exact topics. | Verified from Inventor 2027 Reference |
| Break operations | Release notes name `BreakOperations.AddWithOptions`, describe `Add` as hidden, and add `BreakOperation.AlignedCurve`. | Do not call the hidden member in new work; inspect `AddWithOptions` parameter documentation and drawing-curve requirements. | Verified from Inventor 2027 Reference |
| Registry-free Apprentice sample | Release notes list `SimpleApprenticeRegFree` examples in C++, C#, and VB.NET. | Samples were not installed/inspected in this pass. Use the 2027 DeveloperTools sample when testing Apprentice-specific workflows. | Verified from Inventor 2027 Reference; SDK sample not inspected |

The local 2027 API Help also records that `OnRestart32BitHost` is hidden. Do not build new behavior around it without a documented supported replacement.

## Inventor 2027.1 additions

The shipped 2027 `What's New` help topic includes a separate **New Features of Inventor 2027.1** section. The inspected `Autodesk.Inventor.Interop.dll` (`31.10.26700.0`) contains representative 2027.1 symbols below, confirmed by reflection. The help/release notes establish that the members are named for 2027.1; this does not verify each full signature or runtime behavior.

| 2027.1 change named by the help | Evidence/notes |
|---|---|
| `ComponentDefinition.HasDesignProblems` | Named by 2027.1 release notes; member exists in inspected interop assembly. |
| `AssemblyComponentDefinition.IsContentMember` | Named by 2027.1 release notes; member exists in inspected interop assembly. |
| Assembly model annotation / datum-target APIs, including `ModelDatums.CreateDatumTargetsDefinition`, `ModelDatumTargets.Definition`, and `ModelDatumTargets.Delete` | Release-note description only here; inspect exact 2027.1 topics before use. |
| `BOMRow.Thumbnail` | Named by 2027.1 release notes; member exists in inspected interop assembly. |
| `SketchedSymbol.RangeBox` and `SketchedSymbol.AssociativeEntity` | Release-note description only here; verify property types and mutability before use. |
| `Parameters.ImportFromXMLWithStatus` and `Parameters.ExportToXMLWithStatus` | Named by 2027.1 release notes; exact signatures/status-return semantics are not reproduced here. |
| `PresentationDocument.InsertModel` | Named by 2027.1 release notes; member exists in inspected interop assembly. |
| `FileManager.GetDocumentThumbnail` | Release-note description only here; verify argument/model-state requirements. |
| `AssemblyOptions.OpenComponentsUsingCurrentCamera` | Release-note description only here; verify applicability and default value. |
| `ContentTableColumn.Delete` and `ContentTableColumns.DeleteColumns` | Release-note description only here; verify document/context requirements. |
| `ColorSchemes.GetThumbnailBackgroundColorSettings` and `SetThumbnailBackgroundColorSettings` | Release-note description only here; verify types and valid settings. |
| `BOMView.Renumber2` | Named by 2027.1 release notes; member exists in inspected interop assembly. |
| `DrawingView.ActivePresentationView` becomes writable | Release-note description only here; verify the property type and allowed values before assignment. |
| `BreakOperations.AddWithOptions` and `BreakOperation.AlignedCurve` | Named by 2027.1 release notes; both members exist in inspected interop assembly. |

These are **Verified from Inventor 2027 Reference** as release-note entries, and the specifically noted reflection checks confirm symbol presence in the inspected interop assembly. Full signatures/semantics must still be checked in the member topics. None is marked **Compile Verified** by the samples in this kit.

## Version and source caveats

- This release list is based on the 2027 `What's New` topic shipped with the installed API Help, not on inference from 2025/2026 documentation.
- That topic distinguishes base 2027 from 2027.1; verify the actual target/update level before using 2027.1-only members.
- A release note proves a feature is called out for the release; it does not by itself establish the full C# signature or behavior.
- `inventor.autodeskapi.dev` returned HTTP 402 during Sprint 01 and is not used as evidence here.
- DeveloperTools MSI is present locally but its samples were not installed or reviewed. No item above is marked **Verified from Inventor 2027 SDK Sample**.

## Further release-specific reading

- [Creating an Inventor Add-in](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/CreatingAnAddIn_Overview.htm)
- [Port .NET Framework-based projects to .NET](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/PortToNetCore_Overview.htm)
- [IrregularDistributionSet](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/IrregularDistributionSet.htm)
- [SlotFeature](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SlotFeature.htm)
- [PartsList.ReplaceBOMView](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/PartsList_ReplaceBOMView.htm)
- [ModelState.GenerateOnSaveStatus](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ModelState_GenerateOnSaveStatus.htm)
