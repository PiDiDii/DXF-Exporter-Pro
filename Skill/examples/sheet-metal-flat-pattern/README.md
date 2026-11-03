# Inspect an Existing Sheet-Metal Flat Pattern

This read-only helper confirms the part definition is sheet metal and checks `HasFlatPattern` before accessing `FlatPattern`. It deliberately does not call `Unfold`; creating a missing flat pattern is a model change and must be an explicit workflow choice.

Sources: Autodesk Inventor 2027 [SheetMetalComponentDefinition.HasFlatPattern](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SheetMetalComponentDefinition_HasFlatPattern.htm), [FlatPattern](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SheetMetalComponentDefinition_FlatPattern.htm), and installed interop metadata.
