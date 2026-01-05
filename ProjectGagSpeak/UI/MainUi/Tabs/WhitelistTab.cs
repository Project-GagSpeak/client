using Dalamud.Bindings.ImGui;
using GagSpeak.DrawSystem;
using GagSpeak.PlayerClient;

namespace GagSpeak.Gui.MainWindow;

/// <summary> 
/// Sub-class of the main UI window. Handles drawing the whitelist/contacts tab of the main UI.
/// </summary>
public class WhitelistTab
{
    private readonly MainConfig _config;
    private readonly WhitelistDrawer _drawer;

    public WhitelistTab(MainConfig config, WhitelistDrawer drawer)
    {
        _config = config;
        _drawer = drawer;
    }

    public void DrawSection()
    {
        var width = ImGui.GetContentRegionAvail().X;
        _drawer.DrawFilterRow(width, 64);
        _drawer.DrawContents(width);
    }
}
