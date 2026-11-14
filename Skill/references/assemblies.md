# Assemblies

## Occurrence semantics

An assembly contains **occurrences** of component definitions/documents. The same part document can be used by multiple occurrences. Decide whether the requested operation is per occurrence (placement/context-specific) or per unique source document (deduplicated).

For a standard assembly, the verified traversal chain is:

```text
AssemblyDocument.ComponentDefinition
  -> AssemblyComponentDefinition.Occurrences
    -> ComponentOccurrences.AllLeafOccurrences
      -> ComponentOccurrencesEnumerator.Count / Item(index)
```

`AllLeafOccurrences` returns leaf occurrences in the assembly context. `ComponentOccurrence.Suppressed` is available; `SubOccurrences` applies to occurrences that represent a subassembly. A flat leaf walk is not a substitute for preserving parent/occurrence context when that context matters.

## Robust traversal checklist

- Confirm an assembly document and a compatible assembly component definition.
- Decide whether to visit all occurrences or unique documents.
- Check `Suppressed` before accessing data that requires an active/resolved component.
- Anticipate unresolved/missing references and COM errors.
- Do not assume suppressed occurrences have the same accessible definition behavior as active occurrences.
- Avoid mutating assembly state during a traversal unless the command explicitly does so.

## Example

[`examples/assembly-traversal/AssemblyTraversalExample.cs`](../../../examples/assembly-traversal/AssemblyTraversalExample.cs) reads leaf occurrence names and excludes suppressed occurrences. It intentionally does not open or modify referenced files.

## Sources

- Autodesk Inventor 2027 [ComponentOccurrences.AllLeafOccurrences](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ComponentOccurrences_AllLeafOccurrences.htm).
- Autodesk Inventor 2027 [ComponentOccurrence](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ComponentOccurrence.htm).
- [Inventor 2027 Object Model Map](../../../api/object-model/README.md).
