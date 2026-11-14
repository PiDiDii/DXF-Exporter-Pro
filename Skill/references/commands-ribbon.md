# Commands and Ribbon

## Object model

```text
Application.CommandManager.ControlDefinitions
  -> AddButtonDefinition(...) -> ButtonDefinition
    -> ButtonDefinitionSink_Event.OnExecute(NameValueMap)
Application.UserInterfaceManager.Ribbons
  -> Ribbon.RibbonTabs
    -> RibbonTab.RibbonPanels
      -> RibbonPanel.CommandControls.AddButton(...)
```

`ControlDefinitions.AddButtonDefinition` exact 2027 C# signature:

```csharp
ButtonDefinition AddButtonDefinition(
    string DisplayName, string InternalName, CommandTypesEnum Classification,
    object ClientId, string DescriptionText, string ToolTipText,
    object StandardIcon, object LargeIcon, ButtonDisplayEnum ButtonDisplay);
```

The interop metadata projects optional COM arguments as required C# parameters; pass valid values for all of them. The 2027 help's Ribbon example omits optional VBA arguments, so do not copy that call shape verbatim into C#.

`RibbonPanels.Add` and `CommandControls.AddButton` signatures are recorded in [`API_INDEX.md`](../../../api/API_INDEX.md). Ribbon internal names and active ribbon depend on the document/environment; `ZeroDoc`, `Part`, `Assembly`, and `Drawing` are distinct contexts in the 2027 Ribbon guide.

## UI creation and persistence

- Define the command during activation so its definition and handler exist for the current add-in lifetime.
- The 2027 Add-in/Ribbon guide treats UI controls as persistent and gates initial Ribbon creation on the add-in's `FirstTime` flag.
- Handle `UserInterfaceEvents.OnResetRibbonInterface` if the add-in needs to recreate UI after a user resets the Ribbon.
- Use a unique stable internal name and the add-in client ID. Do not create duplicates on every activation.
- Keep command enablement and active-document assumptions explicit. Test each supported ribbon/document context in Inventor.
- When retiring a previously persisted custom tab, enumerate only the affected document-context ribbons and delete only exact add-in-owned internal names. Never remove a tab by its localized display name.

## Example

See [`examples/ribbon-command/RibbonCommandExample.cs`](../../../examples/ribbon-command/RibbonCommandExample.cs). It compiles against the installed 2027 interop assembly. Its UI placement and event callback have not been executed in Inventor.

## Sources

- Autodesk Inventor 2027 [Customizing the Ribbon using the API](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/RibbonUI_Overview.htm).
- Autodesk Inventor 2027 [ControlDefinitions.AddButtonDefinition](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ControlDefinitions_AddButtonDefinition.htm).
- Installed Inventor 2027 interop metadata for Ribbon collection signatures and `ButtonDefinitionSink_Event`.
