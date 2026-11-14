# Inventor 2027 Add-in Lifecycle

## Entry contract

The add-in server implements `Inventor.ApplicationAddInServer`. Verified C# members:

```csharp
void Activate(Inventor.ApplicationAddInSite AddInSiteObject, bool FirstTime);
void Deactivate();
void ExecuteCommand(int CommandID);
object Automation { get; }
```

Evidence: installed Autodesk Inventor 2027 `Autodesk.Inventor.Interop.xml`, topics `T:Inventor.ApplicationAddInServer`, `M:...Activate(...)`, `M:...Deactivate`, `M:...ExecuteCommand(System.Int32)`, and `P:...Automation`; reflected from interop version `31.10.26700.0`. See the direct [ApplicationAddInServer API Help topic](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationAddInServer.htm) and [ApplicationAddInSite.Application](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationAddInSite_Application.htm).

## Lifecycle responsibilities

### `Activate`

- Save the `ApplicationAddInSite.Application` reference only for the lifetime in which it is needed.
- Initialize application-level services and add-in state.
- Create command definitions on each activation as required by the 2027 help/sample lifecycle.
- Create persistent Ribbon/UI controls only on first registration (`FirstTime`), and handle UI reset events if the add-in customizes resettable UI.
- Subscribe to events after the objects they need are ready; keep explicit references to event sources and handlers.
- If initialization fails, surface/log the exception and undo any event/UI setup already made. Do not report a successful activation after partial setup.

The `FirstTime` Boolean is supplied by Inventor. Do not reinterpret it as a generic "this process has started" flag.

### While loaded

- Treat command callbacks as entry points: validate active document/type/state before operating.
- Keep document-sensitive object references tied to their valid document lifetime.
- Do not block the Inventor UI with unbounded work.
- For geometry or file-writing operations, provide visible failure reporting and avoid destructive side effects unless the command explicitly requests them.

### `Deactivate`

- Detach events before releasing references to their source objects.
- Stop timers/background work owned by the add-in.
- Release managed references and clear add-in state. Do not blindly call `Marshal.ReleaseComObject` on shared Inventor RCWs.
- Make cleanup safe if activation only completed partially.

### `ExecuteCommand`

This server method receives an integer command ID and is distinct from a Ribbon button's `ButtonDefinitionSink_Event.OnExecute` callback. Use it only when the installed API/manifest mechanism routes commands through it; do not assume every UI button invokes it.

## Verified minimal server example

See [`examples/create-addin/MinimalAddinServer.cs`](../../../examples/create-addin/MinimalAddinServer.cs). The example is compiled against the installed 2027 interop assembly; it is not registered, loaded, or runtime-tested in Inventor.

## Sources

- Autodesk Inventor 2027 API Help, [Creating an Inventor Add-in](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/CreatingAnAddIn_Overview.htm), and [ApplicationAddInServer](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationAddInServer.htm).
- Installed Inventor 2027 API interop XML/DLL; local evidence ledger in [`API_VERIFICATION.md`](../../../references/API_VERIFICATION.md).
