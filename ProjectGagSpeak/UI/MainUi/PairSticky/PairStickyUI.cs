using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.UI.Permissions;

public partial class PairStickyUI : WindowMediatorSubscriberBase
{
    private PermissionsDrawer _drawer;

    private readonly PairCombos _pairCombos;
    private readonly PermActData _permData;
    private readonly PresetLogic _presets;

    private readonly MainHub _hub;
    private readonly GlobalData _globals;
    private readonly PairManager _pairs;
    private readonly ClientMonitor _monitor;
    private readonly UiSharedService _ui;

    public PairStickyUI(ILogger<PairStickyUI> logger, GagspeakMediator mediator, Pair pair,
        StickyWindowType drawType, PairCombos pairCombos, PermActData permData, PresetLogic presets,
        MainHub hub, GlobalData globals, PiShockProvider shocks, PairManager pairs,
        ClientMonitor monitor, UiSharedService ui) : base(logger, mediator, "PairStickyUI for " + pair.UserData.UID + "pair.")
    {
        _pairCombos = pairCombos;
        _permData = permData;
        _presets = presets;
        _hub = hub;
        _globals = globals;
        _pairs = pairs;
        _monitor = monitor;
        _ui = ui;

        // Define the pair.
        SPair = pair;

        // reset the opened interaction and paircombo drawers.
        _drawer = new PermissionsDrawer(hub, permData, shocks, ui);
        _pairCombos.UpdateCombosForPair(pair);
        _permData.InitForPair(pair.UserData, pair.GetNickAliasOrUid());
        PairCombos.Opened = InteractionType.None;

        // set the type of window we're drawing
        DrawType = drawType;

        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;
        IsOpen = true;
    }

    public Pair SPair { get; init; }
    public StickyWindowType DrawType = StickyWindowType.None;
    private float WindowMenuWidth = -1;

    protected override void PreDrawInternal()
    {
        // Magic that makes the sticky pair window move with the main UI.
        var position = _ui.LastMainUIWindowPosition;
        position.X += _ui.LastMainUIWindowSize.X;
        position.Y += ImGui.GetFrameHeightWithSpacing();
        ImGui.SetNextWindowPos(position);

        Flags |= ImGuiWindowFlags.NoMove;

        var width = (DrawType == StickyWindowType.PairPerms) ? 160 * ImGuiHelpers.GlobalScale : 110 * ImGuiHelpers.GlobalScale;
        var size = new Vector2(7 * ImGui.GetFrameHeight() + 3 * ImGui.GetStyle().ItemInnerSpacing.X + width, _ui.LastMainUIWindowSize.Y - ImGui.GetFrameHeightWithSpacing() * 2);
        ImGui.SetNextWindowSize(size);
    }

    protected override void DrawInternal()
    {
        WindowMenuWidth = ImGui.GetContentRegionAvail().X;

        switch (DrawType)
        {
            case StickyWindowType.PairPerms:
                ImGuiUtil.Center(PermActData.DispName + "'s Permissions for You");
                ImGui.Separator();
                using (ImRaii.Child("PairPermsContent", new Vector2(0, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoScrollbar))
                    DrawPairPermsForClient();
                break;
            case StickyWindowType.ClientPermsForPair:
                ImGuiUtil.Center("Your Permissions for " + PermActData.DispName);
                _ui.SetCursorXtoCenter(225f);
                _presets.DrawPresetList(SPair, 225f);

                ImGui.Separator();
                using (ImRaii.Child("ClientPermsForPairContent", new Vector2(0, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoScrollbar))
                    DrawClientPermsForPair();
                break;
            case StickyWindowType.PairActionFunctions:
                using (ImRaii.Child("##StickyWinActs", new Vector2(0, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.NoScrollbar))
                    DrawPairActionFunctions();
                break;
        }
    }

    protected override void PostDrawInternal() { }

    public override void OnClose()
    {
        Mediator.Publish(new RemoveWindowMessage(this));
    }
}
