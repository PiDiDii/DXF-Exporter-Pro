# Inventor API notes for remaining G3 wiring

## Assembly selection

Autodesk Support states that assembly selection belongs to the top-level assembly document, even when a subassembly is being edited in-place; the example reads `ThisApplication.ActiveDocument.SelectSet.Count` rather than the edited component's `SelectSet`. This confirms the correct document context for a future selected-occurrence implementation, but this note does **not** verify the C# `SelectSet.Item` projection or the runtime type returned for a selected occurrence. Do not add that loop until those signatures are verified against the installed Inventor 2027 interop/API Help.

Source: [Autodesk Support — SelectSet is empty on edited components in an Inventor assembly when using the API](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/SelectSet-is-empty-on-edited-components-in-an-Inventor-assembly-when-using-the-API.html), accessed 2026-11-03.

## Part save event

The Inventor 2027 API index records `DocumentEvents.OnSave(EventTimingEnum BeforeOrAfter, NameValueMap Context, ref HandlingCodeEnum HandlingCode)` and links to Autodesk Help. The add-in still does not subscribe to `OnSave`; automatic metadata updates during user saves remain a separate feature and require host-runtime validation.

Sources: [Skill/api/API_INDEX.md](./Skill/api/API_INDEX.md), row `Inventor.DocumentEvents`; [Skill/references/events.md](./Skill/references/events.md).

## Ribbon reset event

The add-in subscribes to `UserInterfaceEventsSink_Event.OnResetRibbonInterface` and recreates only missing add-in tabs. Its C# event handler is `void Handler(NameValueMap Context)`. The signature was confirmed from the installed Inventor 2027 interop assembly (`31.10.26700.0`) and `Autodesk.Inventor.Interop.xml`, member `M:Inventor.UserInterfaceEvents.OnResetRibbonInterface(Inventor.NameValueMap)`. The callback is detached during add-in deactivation.

Source: [Autodesk Inventor 2027 — UserInterfaceEvents.OnResetRibbonInterface](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/UserInterfaceEvents_OnResetRibbonInterface.htm). Reference and compile verification do not imply that Ribbon reset behavior has been runtime-tested in Inventor.

## Flat-pattern DXF layer styling

The Inventor 2027 `DataIO.WriteDataToFile` flat-pattern DXF options accept per-layer keys in the form `{LayerArgument}Color=R;G;B`, `{LayerArgument}LineType=<LineTypeEnum value>`, and `{LayerArgument}LineWeight=<double in centimeters>`. The exporter targets `AcadVersion=R12`, does not emit custom layer colors, and omits the lineweight option because the R12 target predates DXF lineweight metadata.

The `InvisibleLayers` option is a semicolon-separated list of layer names. The R12 option string includes the same layer mappings and hidden-layer list for each DataIO provider. Because Inventor can still write a named layer record into the DXF table, post-processing also removes entities on unchecked layers from `ENTITIES` and `BLOCKS` (while retaining block delimiters) and removes their `LAYER` table records. This cleanup is covered by DXF fixture tests; final output still requires an Inventor 2027 export and CAD-open check.

`IV_MARK_SURFACE` is a user-requested visibility option: when disabled, its name is added to `InvisibleLayers`; when enabled, it is omitted from that list. The Autodesk sample verifies the option syntax, not that every Inventor document produces this exact layer. Confirm the layer name and visibility in Inventor 2027 before release.

The production option string omits line types: the recorded assembly test showed all 60 parts failing the first `LineType` attempt before succeeding with options without `LineType`. Omitting the known-failing option avoids repeating that failure. The user has confirmed a successful Inventor 2027 run; host regression should be repeated when changing DXF export behavior.

Sources: [Autodesk Inventor 2027 — WriteFlatPatternAsDXF sample](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WriteFlatPatternAsDXF_Sample.htm), [Autodesk Inventor 2027 — LineTypeEnum](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/LineTypeEnum.htm), [Autodesk Inventor 2027 — DataIO.WriteDataToFile](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DataIO_WriteDataToFile.htm).
