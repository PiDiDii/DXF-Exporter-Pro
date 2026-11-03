using System.Diagnostics;
using Inventor;

namespace Inventor2027AICodingKit.Examples;

public sealed class RibbonCommandExample
{
    private const string ClientId = "{8D537E99-070F-4F4F-9D03-30F3BFB68EA1}";
    private const string ButtonInternalName = "Inventor2027AICodingKit.RunSample";
    private const string TabInternalName = "Inventor2027AICodingKit.Tab";
    private const string PanelInternalName = "Inventor2027AICodingKit.Panel";

    private ButtonDefinition? _button;
    private ButtonDefinitionSink_Event? _buttonEvents;

    public void Activate(Application application, bool firstTime)
    {
        ArgumentNullException.ThrowIfNull(application);

        _button = application.CommandManager.ControlDefinitions.AddButtonDefinition(
            "Run sample",
            ButtonInternalName,
            CommandTypesEnum.kNonShapeEditCmdType,
            ClientId,
            "Runs the sample command",
            "Run sample",
            null,
            null,
            ButtonDisplayEnum.kAlwaysDisplayText);

        _buttonEvents = (ButtonDefinitionSink_Event)_button;
        _buttonEvents.OnExecute += OnExecute;

        if (!firstTime)
        {
            return;
        }

        var ribbon = application.UserInterfaceManager.Ribbons["Part"];
        var tab = ribbon.RibbonTabs.Add(
            "AI Coding Kit",
            TabInternalName,
            ClientId,
            string.Empty,
            false,
            false);
        var panel = tab.RibbonPanels.Add(
            "Examples",
            PanelInternalName,
            ClientId,
            string.Empty,
            false);
        panel.CommandControls.AddButton(_button, false, true, string.Empty, false);
    }

    public void Deactivate()
    {
        if (_buttonEvents is not null)
        {
            _buttonEvents.OnExecute -= OnExecute;
        }

        _buttonEvents = null;
        _button = null;
    }

    private static void OnExecute(NameValueMap Context)
    {
        Trace.WriteLine("Inventor sample command executed.");
    }
}
