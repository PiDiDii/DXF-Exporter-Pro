# Ribbon Command

Creates a button definition, retains the generated 2027 COM event interface, subscribes to its `OnExecute` event, and adds a tab/panel on first UI creation. The example uses the `Part` ribbon; an actual add-in must choose and test the appropriate ribbon/environment(s).

This is a focused API example, not a deployable Add-in on its own. It has no reset handler or manifest. Its event subscription is detached by `Deactivate`; the example does not remove its persistent Ribbon tab/panel.

Build all samples with:

```powershell
dotnet build .\examples\Inventor2027.Examples.csproj
```

Sources: Autodesk Inventor 2027 [Ribbon guide](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/RibbonUI_Overview.htm), [AddButtonDefinition](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ControlDefinitions_AddButtonDefinition.htm), and installed interop definitions for `ButtonDefinitionSink_Event`, `RibbonTabs.Add`, `RibbonPanels.Add`, and `CommandControls.AddButton`.
