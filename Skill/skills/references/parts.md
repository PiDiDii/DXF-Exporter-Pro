# Parts

## Generic part vs sheet metal

`PartDocument.ComponentDefinition` is a part component definition. Do not assume every part is sheet metal. Check the actual definition type before using `SheetMetalComponentDefinition` members.

Keep read-only inspection separate from edits:

- Read document path/type and component-definition properties first.
- Identify whether the operation changes model geometry, feature state, or saved output.
- Use a transaction/undo pattern only after verifying the relevant 2027 transaction API and desired UX.
- Avoid saving or changing a model as an incidental effect of an export command.

## Sheet-metal-specific work

Flat-pattern creation (`Unfold`) changes model state. Before invoking it in a production command, decide whether to create a missing flat pattern, require one to exist, or operate on a copy. Clearly report which behavior was chosen.

## Sources

- Autodesk Inventor 2027 [PartDocument.ComponentDefinition](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/PartDocument_ComponentDefinition.htm).
- Autodesk Inventor 2027 [SheetMetalComponentDefinition](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/SheetMetalComponentDefinition.htm).
- Example project: [sheet-metal-flat-pattern](../../../examples/sheet-metal-flat-pattern/README.md).
