# Inventor 2027 Object Model Map

This map describes the portion of the `Inventor` namespace needed for an Add-in and a sheet-metal DXF workflow. Arrows mean "obtained through" or "returns"; they are not claims that every relationship is valid in every document context.

## Application and Add-in

```text
Inventor.ApplicationAddInServer
  Activate(ApplicationAddInSite, bool)
    ApplicationAddInSite.Application -> Inventor.Application
      Application.ActiveDocument -> Inventor._Document (COM interface)
      Application.Documents -> Inventor.Documents
      Application.CommandManager -> Inventor.CommandManager
        CommandManager.ControlDefinitions -> Inventor.ControlDefinitions
          AddButtonDefinition(...) -> Inventor.ButtonDefinition
            ButtonDefinitionSink_Event.OnExecute(NameValueMap)
      Application.UserInterfaceManager -> Inventor.UserInterfaceManager
        Ribbons -> Ribbon -> RibbonTabs -> RibbonTab
          -> RibbonPanels -> RibbonPanel -> CommandControls
  Deactivate()
  ExecuteCommand(int)
```

`ApplicationAddInServer` is the add-in contract, not the user command callback. The button callback is surfaced through the button-definition event interface. See [add-in lifecycle](../../skills/inventor-2027-addin/references/addin-lifecycle.md), [commands and Ribbon](../../skills/inventor-2027-addin/references/commands-ribbon.md), and [events](../../skills/inventor-2027-addin/references/events.md).

## Documents and components

```text
Application.ActiveDocument : Document
  AssemblyDocument
    ComponentDefinition : AssemblyComponentDefinition
      Occurrences : ComponentOccurrences
        AllLeafOccurrences : ComponentOccurrencesEnumerator
          Item(index) : ComponentOccurrence
            Definition : ComponentDefinition
            Suppressed
            SubOccurrences : ComponentOccurrencesEnumerator (subassembly occurrence context)
  PartDocument
    ComponentDefinition : PartComponentDefinition
      [sheet-metal part] SheetMetalComponentDefinition
        HasFlatPattern
        FlatPattern : FlatPattern (only when present)
          DataIO : DataIO
        DataIO : DataIO
```

Do not treat an assembly occurrence as the same thing as its referenced document. A leaf-occurrence collection represents occurrences; repeated references may appear more than once. For unique source-file processing, define an explicit de-duplication key and handle unresolved/suppressed references.

## DXF output

```text
PartDocument.ComponentDefinition
  -> SheetMetalComponentDefinition (only after the part is confirmed as sheet metal)
    -> DataIO
      -> WriteDataToFile(formatString, outputPath)
```

The official Inventor 2027 API Help includes a sheet-metal DXF example using the component-definition DataIO context and a `FLAT PATTERN DXF?...` format string. `FlatPattern.DataIO` is a separate available property. Determine which DataIO context is appropriate for the required output; query supported formats and do not assume every DataIO object supports every format.

See [sheet metal](../../skills/inventor-2027-addin/references/sheet-metal.md), [data I/O](../../skills/inventor-2027-addin/references/data-io.md), and [DXF export example](../../examples/dxf-export/README.md).

## Drawings and BOM

```text
DrawingDocument
  ActiveSheet : Sheet
  Sheets -> Sheet
    PartsLists -> PartsList
      ReplaceBOMView(PartsListLevelEnum, object, object, object)
```

Use the sheet's parts-list collection to inspect drawing BOM tables. `ReplaceBOMView` is a separate 2027 API addition; its broad `object` parameters must be verified against the exact topic and use case before modification.

## Source evidence

- Autodesk Inventor 2027 API Help, `Object Model Chart` / SDK documentation shipped with DeveloperTools. DeveloperTools is not installed in the inspected machine, so no chart artifact was reviewed in this pass.
- Autodesk Inventor 2027 API Help topics and matching interop metadata listed in [`API_INDEX.md`](../API_INDEX.md).
- Autodesk's [Inventor API overview](https://aps.autodesk.com/developer/overview/inventor-api) identifies the SDK Object Model Chart as the hierarchy reference.

All exact members in this map should be checked against the corresponding 2027 topic before being used outside the reviewed examples. No Inventor runtime behavior is implied.
