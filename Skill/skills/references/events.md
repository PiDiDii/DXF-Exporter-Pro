# Events

## Event scopes

- `ApplicationEvents` is application-centric; `OnOpenDocument` and `OnCloseDocument` fire for document lifecycle.
- `DocumentEvents` is associated with a specific document; examples include `OnSave` and `OnChange`.
- `ButtonDefinitionSink_Event.OnExecute(NameValueMap Context)` is the command-button callback used by the Ribbon example.
- `UserInterfaceEventsSink_Event.OnResetRibbonInterface` is relevant when recreating customized Ribbon controls after a reset.

The exact Inventor 2027 signatures and event wrappers are in the local API Help and `Autodesk.Inventor.Interop.dll`. Event delegates include COM `ref` parameters such as `HandlingCodeEnum`; inspect the generated 2027 delegate type before writing a C# handler.

## Subscription lifecycle

1. Subscribe after the relevant application/document/control exists.
2. Keep the event source and delegate strongly referenced while subscribed.
3. Avoid duplicate subscriptions during reactivation or repeated UI setup.
4. Detach document events when that document closes; detach all add-in-owned subscriptions during `Deactivate`.
5. Keep callbacks short. Do not block the UI with file I/O or long assembly traversal.
6. Do not let exceptions cross a COM event boundary unreported. Log/notify appropriately and set handling codes only according to the documented event contract.

## Verified examples

- Button event delegate: `void OnExecute(Inventor.NameValueMap Context)` (compiled in `RibbonCommandExample.cs`).
- Application document event reference: [ApplicationEvents.OnOpenDocument](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationEvents_OnOpenDocument.htm).
- Document save event reference: [DocumentEvents.OnSave](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/DocumentEvents_OnSave.htm).

Application/document event handlers are not compiled in the sample project. Use the generated delegate signatures from the matching 2027 interop assembly; do not infer them from the button callback.
