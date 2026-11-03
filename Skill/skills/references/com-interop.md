# COM Interop for Inventor 2027 Add-ins

Inventor's API is COM-based. C# sees imported interfaces and runtime-callable wrappers from the version-matched interop assembly. Use the C# projection shown by the 2027 reference/interop metadata, not an assumed VBA translation.

## Practical rules

- Reference the intended Inventor version explicitly. The inspected 2027 interop assembly is `Autodesk.Inventor.Interop.dll`, version `31.10.26700.0`, in the Inventor `Bin` folder. Inventor 2026+ SDK readme notes that .NET add-ins should reference the installed `Bin` assembly or ship a local copy; it is not in the GAC.
- When manually re-referencing the interop assembly, the SDK readme says to set **Embed Interop Types = false** and **Copy Local = true**. Verify your project output/deployment strategy before choosing local vs host assembly.
- COM collections commonly use 1-based indices. Confirm each collection's `Item` signature and bounds in the exact topic; do not assume .NET zero-based indexing.
- Collection `Item` parameters may be projected as `object` or `int` depending on the collection. Use the declared C# type, not a guessed indexer.
- Retain event source and delegate/event-interface references for as long as subscribed. Unsubscribe deterministically during document close or add-in deactivation.
- Never blanket-release every RCW. Inventor may share COM objects across API properties; premature `ReleaseComObject` can break other code. Release explicitly only when ownership/lifetime is understood and documented.
- Do not call Inventor COM APIs from arbitrary background threads. Keep UI-bound API calls on Inventor's supported thread; marshal work back using a verified host-supported pattern.
- COM exceptions and failed HRESULTs are meaningful. Report actionable errors; do not silently continue with a partially written export.

## C# event wrappers

The 2027 interop assembly exposes event source interfaces such as `ButtonDefinitionSink_Event` and `ApplicationEventsSink_Event`, with generated delegate types such as `ButtonDefinitionSink_OnExecuteEventHandler`. For the button event, reflection on the installed 2027 interop metadata reports:

```csharp
void OnExecute(Inventor.NameValueMap Context);
```

The compile-verified example uses this event interface. Application/document event argument lists differ; confirm each delegate before implementing a handler.

## Sources

- Autodesk [Inventor API overview](https://aps.autodesk.com/developer/overview/inventor-api) identifies the Inventor API as COM-based.
- Installed `Autodesk.Inventor.Interop.dll` and SDK `SDK_Readme.htm` in Inventor 2027.
- Autodesk Inventor 2027 [Creating an Inventor Add-in](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/CreatingAnAddIn_Overview.htm).
