using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.UI.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MainUiMoodlesHub : DisposableMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;

    public MainUiMoodlesHub(ILogger<MainUiMoodlesHub> logger, GagspeakMediator mediator,
        MainHub apiHubMain) : base(logger, mediator)
    {
        _apiHubMain = apiHubMain;
    }

    public void DrawMoodlesHub()
    {
        using var scrollbar = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        // grab the content region of the current section
        var CurrentRegion = ImGui.GetContentRegionAvail();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
        {
            ImGuiUtil.Center("Moodles Hub COMING SOON!");
        }
        ImGui.Separator();
    }
}

