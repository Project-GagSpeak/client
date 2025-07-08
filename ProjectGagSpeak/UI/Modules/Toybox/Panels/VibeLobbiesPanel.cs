using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using ImGuiNET;

namespace GagSpeak.Gui.Toybox;

public class VibeLobbiesPanel
{
    private readonly ILogger<VibeLobbiesPanel> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly VibeLobbyManager _lobbyManager;
    private readonly IpcCallerIntiface _ipc;
    private readonly TutorialService _guides;

    public VibeLobbiesPanel(
        ILogger<VibeLobbiesPanel> logger,
        GagspeakMediator mediator,
        VibeLobbyManager lobbyManager,
        IpcCallerIntiface ipc,
        TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _lobbyManager = lobbyManager;
        _ipc = ipc;
        _guides = guides;

        // grab path to the intiface
        if (IntifaceCentral.AppPath == string.Empty)
            IntifaceCentral.GetApplicationPath();
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("VibeLobbiesTL", drawRegions.TopLeft.Size))
            ImGui.Text("Im Top Left Area!");

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("VibeLobbiesBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            ImGui.Text("Im Bottom Left Area!");

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("VibeLobbiesTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawRightPanel(drawRegions.BotRight, curveSize);
    }

    private void DrawRightPanel(CkHeader.DrawRegion region, float curveSize)
    {
        DrawCurrentLobby(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawCurrentLobby(CkHeader.DrawRegion region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);

        using (CkRaii.ChildLabelButton(region.Size, .6f, "I'm a label!", ImGui.GetFrameHeight(), BeginEdits, string.Empty, DFlags.RoundCornersRight, LabelFlags.SizeIncludesHeader))
        {
            ImGui.Text("Im this lobby's internal view");
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left)
                return;
        }
    }
}
