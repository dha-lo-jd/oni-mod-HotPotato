using HotPotato.Ui;

namespace HotPotato.Tools;

// A tool that does nothing on the map. Choosing it shows the settings panel, where every setting can be changed while
// the game runs; leaving the tool hides the panel.
public class SettingsTool : InterfaceTool
{
    public const string ToolName = "HotPotatoSettingsTool";

    protected override void OnActivateTool()
    {
        base.OnActivateTool();
        SettingsPanel.Show();
    }

    protected override void OnDeactivateTool(InterfaceTool new_tool)
    {
        SettingsPanel.Hide();
        base.OnDeactivateTool(new_tool);
    }
}
