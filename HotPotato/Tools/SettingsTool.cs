using HotPotato.Options;
using PeterHan.PLib.Options;

namespace HotPotato.Tools;

// A tool that does nothing on the map. Choosing it shows the checkbox panel the vanilla dig / deconstruct tools use for
// their filters, with the two settings that are worth changing in the middle of a game.
public class SettingsTool : InterfaceTool
{
    public const string ToolName = "HotPotatoSettingsTool";

    // The panel looks the labels up as STRINGS.UI.TOOLS.FILTERLAYERS.<name>.NAME / .TOOLTIP.
    internal const string PauseKey = "HOTPOTATO_PAUSE_GAME";
    internal const string NotifyKey = "HOTPOTATO_SHOW_NOTIFICATION";
    private const string LabelPrefix = "STRINGS.UI.TOOLS.FILTERLAYERS.";

    private ToolParameterMenu.ToggleData pause;
    private ToolParameterMenu.ToggleData notify;

    protected override void OnActivateTool()
    {
        base.OnActivateTool();

        // Registered on every activation so a language change is picked up.
        AddLabel(PauseKey, STRINGS.OPTIONS.PAUSE_GAME.NAME, STRINGS.OPTIONS.PAUSE_GAME.TOOLTIP);
        AddLabel(NotifyKey, STRINGS.OPTIONS.SHOW_NOTIFICATION.NAME, STRINGS.OPTIONS.SHOW_NOTIFICATION.TOOLTIP);

        var options = ModOptions.Instance;
        pause = Toggle(PauseKey, options.PauseGame);
        notify = Toggle(NotifyKey, options.ShowNotification);

        var menu = ToolMenu.Instance.toolParameterMenu;
        menu.onParametersChanged += OnParametersChanged;
        menu.PopulateMenu(new[] { pause, notify });
    }

    protected override void OnDeactivateTool(InterfaceTool new_tool)
    {
        var menu = ToolMenu.Instance.toolParameterMenu;
        menu.onParametersChanged -= OnParametersChanged;
        menu.ClearMenu();
        base.OnDeactivateTool(new_tool);
    }

    private void OnParametersChanged()
    {
        var options = ModOptions.Instance;
        options.PauseGame = pause.IsOn;
        options.ShowNotification = notify.IsOn;
        POptions.WriteSettings(options);
    }

    private static void AddLabel(string key, string name, string tooltip)
    {
        Strings.Add(LabelPrefix + key + ".NAME", name);
        Strings.Add(LabelPrefix + key + ".TOOLTIP", tooltip);
    }

    // Inclusive toggles are independent checkboxes; exclusive ones would behave like radio buttons.
    private static ToolParameterMenu.ToggleData Toggle(string key, bool on) =>
        new ToolParameterMenu.ToggleData(key, on ? ToolParameterMenu.ToggleState.On : ToolParameterMenu.ToggleState.Off, true);
}
