# Inventor API notes for remaining G3 wiring

## Assembly selection

Autodesk Support states that assembly selection belongs to the top-level assembly document, even when a subassembly is being edited in-place; the example reads `ThisApplication.ActiveDocument.SelectSet.Count` rather than the edited component's `SelectSet`. This confirms the correct document context for a future selected-occurrence implementation, but this note does **not** verify the C# `SelectSet.Item` projection or the runtime type returned for a selected occurrence. Do not add that loop until those signatures are verified against the installed Inventor 2027 interop/API Help.

Source: [Autodesk Support — SelectSet is empty on edited components in an Inventor assembly when using the API](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/SelectSet-is-empty-on-edited-components-in-an-Inventor-assembly-when-using-the-API.html), accessed 2026-11-03.

## Part save event

The local Inventor 2027 API index records `DocumentEvents.OnSave(EventTimingEnum BeforeOrAfter, NameValueMap Context, ref HandlingCodeEnum HandlingCode)` and links to Autodesk Help. The skill reference notes that the generated 2027 event delegate must be inspected because COM events may have `ref` parameters. Direct Help fetch for the guessed topic URL returned Page Not Found; therefore the current code deliberately does not subscribe to OnSave until the exact interop event source/delegate signatures can be checked on the Windows Inventor 2027 build machine.

Sources: local workspace `skills/inventor-2027-addin/api/API_INDEX.md`, row `Inventor.DocumentEvents`; local workspace `skills/inventor-2027-addin/skills/inventor-2027-addin/references/events.md`; attempted Autodesk Help URL: https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DocumentEvents_OnSave.htm (Page Not Found at fetch time).

## Flat-pattern DXF layer styling

The Inventor 2027 `DataIO.WriteDataToFile` flat-pattern DXF options accept per-layer keys in the form `{LayerArgument}Color=R;G;B`, `{LayerArgument}LineType=<LineTypeEnum value>`, and `{LayerArgument}LineWeight=<double in centimeters>`. The exporter keeps lineweight values in millimeters in settings and converts them to centimeters for the API option string. DXF 2000 is selected so lineweight metadata is supported.

The listed line-type values (default 37648, continuous 37633, dashed 37634, dotted 37636, dash-dotted 37638, dashed-hidden 37641, chain 37644) were verified against the installed Inventor 2027 interop and Autodesk's `LineTypeEnum` reference. This verifies option construction only; DXF export has not been runtime-tested in Inventor.

Sources: [Autodesk Inventor 2027 — WriteFlatPatternAsDXF sample](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WriteFlatPatternAsDXF_Sample.htm), [Autodesk Inventor 2027 — LineTypeEnum](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/LineTypeEnum.htm), [Autodesk Inventor 2027 — DataIO.WriteDataToFile](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DataIO_WriteDataToFile.htm).
