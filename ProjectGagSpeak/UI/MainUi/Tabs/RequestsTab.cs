using CkCommons.DrawSystem;
using Dalamud.Bindings.ImGui;
using GagSpeak.DrawSystem;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;

namespace GagSpeak.Gui.MainWindow;

// Idk if we even need the tabs anymore lol.
public class RequestsTab
{
    private readonly MainConfig _config;
    private readonly RequestTabs _tabs;
    private readonly RequestsInDrawer _incoming;
    private readonly RequestsOutDrawer _outgoing;
    public RequestsTab(MainConfig config, RequestTabs tabs, RequestsInDrawer incoming, RequestsOutDrawer outgoing)
    {
        _config = config;
        _tabs = tabs;
        _incoming = incoming;
        _outgoing = outgoing;
    }

    public void DrawSection()
    {
        var width = ImGui.GetContentRegionAvail().X;
        _tabs.Draw(width);
        if (_tabs.TabSelection is RequestTabs.SelectedTab.Incoming)
        {
            _incoming.DrawFilterRow(width, 100);
            _incoming.DrawRequests(width, DynamicFlags.Selectable);
        }
        else
        {
            _outgoing.DrawFilterRow(width, 100);
            _outgoing.DrawRequests(width, DynamicFlags.Selectable);
        }
    }
}
