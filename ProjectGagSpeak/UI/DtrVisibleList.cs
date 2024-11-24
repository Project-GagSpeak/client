using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using ImGuiNET;
using Microsoft.VisualBasic.ApplicationServices;
using OtterGui;
using System.Numerics;

namespace GagSpeak.UI.Components;
internal class DtrVisibleWindow : WindowMediatorSubscriberBase
{
    private readonly UiSharedService _uiSharedService;
    private readonly DtrBarService _dtrBarService;
    private bool ThemePushed = false;
    public DtrVisibleWindow(ILogger<DtrVisibleWindow> logger, GagspeakMediator mediator,
        DtrBarService dtrService, UiSharedService uiSharedService) : base(logger, mediator, "##DtrLinker")
    {
        _dtrBarService = dtrService;
        _uiSharedService = uiSharedService;
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;
    }

    private List<IPlayerCharacter> NonGagspeakUsers => _dtrBarService._visiblePlayers;
    private int SelectedIndex = -1;
    private Vector2 LastRecordedPos = Vector2.Zero;
    public override void OnOpen() => LastRecordedPos = ImGui.GetMousePos();

    protected override void PreDrawInternal() 
    {
        var posX = LastRecordedPos.X - 100;
        var posY = LastRecordedPos.Y + ImGui.GetFrameHeight();
        ImGui.SetNextWindowPos(new Vector2(posX, posY));

        Flags |= ImGuiWindowFlags.NoMove;

        int cnt = NonGagspeakUsers.Count > 10 ? 10+2 : NonGagspeakUsers.Count+2;
        var size = new Vector2(200f, (ImGui.GetTextLineHeightWithSpacing() * cnt) - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y + ImGuiHelpers.GlobalScale);

        ImGui.SetNextWindowSize(size);

        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
            ThemePushed = true;
        }
    }
    protected override void DrawInternal()
    {
        // draw a list of tree nodes, for each player. When they are selected, display the map coordinates of them.
        var displayedPlayers = NonGagspeakUsers.Take(10).ToList();
        var remainingCount = NonGagspeakUsers.Count - displayedPlayers.Count;

        for(var i=0; i<displayedPlayers.Count; i++)
        {
            string text = displayedPlayers[i].GetName() + "  " + displayedPlayers[i].HomeWorldName();
            if(ImGui.Selectable(text, SelectedIndex == i))
            {
                SelectedIndex = i;
                _dtrBarService.LocatePlayer(displayedPlayers[i]);
            }
        }
        if (remainingCount > 0)
        {
            UiSharedService.ColorTextCentered("And " + remainingCount + " more...", ImGuiColors.ParsedPink);
        }

        // close window if its not focused.
        if (!ImGui.IsWindowFocused())
        {
            this.IsOpen = false;
        }
    }
    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }
}
