# Assembly Leaf Occurrence Traversal

Reads leaf occurrences from a standard `AssemblyDocument`, excludes suppressed occurrences, and returns their names. It does not open referenced files or modify the assembly. It counts occurrence instances, not unique source documents.

The optional COM argument on `AllLeafOccurrences` is passed as `Type.Missing` in C#. The collection is 1-based; the exact property/indexer projection was checked against the installed 2027 interop assembly and the sample is compiled with the shared project.

Sources: Autodesk Inventor 2027 [ComponentOccurrences.AllLeafOccurrences](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ComponentOccurrences_AllLeafOccurrences.htm), [ComponentOccurrence](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ComponentOccurrence.htm), and installed `Autodesk.Inventor.Interop.dll` version `31.10.26700.0`.
