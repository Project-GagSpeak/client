using CkCommons.DrawSystem;
using Dalamud.Bindings.ImGui;
using GagSpeak.DrawSystem;
using GagSpeak.PlayerClient;

namespace GagSpeak.Gui.MainWindow;

// Idk if we even need the tabs anymore lol.
public class RequestsTab
{
    private readonly MainConfig _config;
    private readonly RequestsInDrawer _incoming;
    private readonly RequestsOutDrawer _outgoing;
    public RequestsTab(MainConfig config, RequestsInDrawer incoming, RequestsOutDrawer outgoing)
    {
        _config = config;
        _incoming = incoming;
        _outgoing = outgoing;
    }

    public void DrawSection()
    {
        var width = ImGui.GetContentRegionAvail().X;
        if (_config.Current.ViewingIncoming)
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
