using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui.MainWindow;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.CustomCombos.Padlockable;
using GagSpeak.CustomCombos.PairActions;
using GagSpeak.CustomCombos.Moodles;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.CkCommons.Gui.Permissions;

/// <summary>
///     TODO: Refactor this into a class that is not spawned by a factory, allowing us to make static accessors
///     and make it so it does not need to recreate all sub-files of this class object every time.
/// </summary>
public partial class PairStickyUI : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly KinksterRequests _globals;
    private readonly PresetLogicDrawer _presets;
    private readonly PairManager _pairs;
    private readonly ClientMonitor _monitor;
    private readonly PiShockProvider _shockies;

    // Private variables for the sticky UI and its respective combos.
    private float WindowMenuWidth = -1;
    private PairGagCombo _pairGags;
    private PairGagPadlockCombo _pairGagPadlocks;
    private PairRestrictionCombo _pairRestrictionItems;
    private PairRestrictionPadlockCombo _pairRestrictionPadlocks;
    private PairRestraintCombo _pairRestraintSets;
    private PairRestraintPadlockCombo _pairRestraintSetPadlocks;
    private PairMoodleStatusCombo _pairMoodleStatuses;
    private PairMoodlePresetCombo _pairMoodlePresets;
    private PairPatternCombo _pairPatterns;
    private PairAlarmCombo _pairAlarmToggles;
    private PairTriggerCombo _pairTriggerToggles;
    private OwnMoodleStatusToPairCombo _moodleStatuses;
    private OwnMoodlePresetToPairCombo _moodlePresets;
    private EmoteCombo _emoteCombo;
    private PairMoodleStatusCombo _activePairStatusCombo;
    private Dictionary<SPPID, string> _timespanCache = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private static string DisplayName;

    public PairStickyUI(
        ILogger<PairStickyUI> logger,
        GagspeakMediator mediator, 
        Pair pair,
        StickyWindowType drawType,
        MainHub hub,
        KinksterRequests globals,
        PresetLogicDrawer presets,
        IconDisplayer iconDisplayer,
        PairManager pairs,
        ClientMonitor monitor,
        PiShockProvider shocks)
        : base(logger, mediator, $"PairStickyUI-{pair.UserData.UID}")
    {
        _presets = presets;
        _hub = hub;
        _globals = globals;
        _pairs = pairs;
        _monitor = monitor;
        _shockies = shocks;

        Flags = WFlags.NoCollapse | WFlags.NoTitleBar | WFlags.NoResize | WFlags.NoScrollbar;
        IsOpen = true;

        // Define the pair and window type.
        SPair = pair;
        DrawType = drawType;
        OpenedInteraction = InteractionType.None;

        // Init the combos and stuff.
        _pairGags = new PairGagCombo(pair, hub, logger);
        _pairGagPadlocks = new PairGagPadlockCombo(pair, hub, logger);
        _pairRestrictionItems = new PairRestrictionCombo(pair, hub, logger);
        _pairRestrictionPadlocks = new PairRestrictionPadlockCombo(pair, hub, logger);
        _pairRestraintSets = new PairRestraintCombo(pair, hub, logger);
        _pairRestraintSetPadlocks = new PairRestraintPadlockCombo(pair, hub, logger);
        _pairMoodleStatuses = new PairMoodleStatusCombo(1.3f, iconDisplayer, pair, hub, logger);
        _pairMoodlePresets = new PairMoodlePresetCombo(1.3f, iconDisplayer, pair, hub, logger);
        _pairPatterns = new PairPatternCombo(pair, hub, logger);
        _pairAlarmToggles = new PairAlarmCombo(pair, hub, logger);
        _pairTriggerToggles = new PairTriggerCombo(pair, hub, logger);
        _emoteCombo = new EmoteCombo(1.3f, iconDisplayer, logger, () => [
            ..pair.PairPerms.AllowForcedEmote ? EmoteExtensions.LoopedEmotes() : EmoteExtensions.SittingEmotes()
        ]);
        _moodleStatuses = new OwnMoodleStatusToPairCombo(1.3f, iconDisplayer, pair, hub, logger);
        _moodlePresets = new OwnMoodlePresetToPairCombo(1.3f, iconDisplayer, pair, hub, logger);

        _activePairStatusCombo = new PairMoodleStatusCombo(1.3f, iconDisplayer, pair, hub, logger, () => [
            .. pair.LastIpcData.DataInfo.Values.OrderBy(x => x.Title)
        ]);

        // Publish a mediator event to let us know a new pair was made for the stickyUI.
        Mediator.Publish(new StickyPairWindowCreated(pair));
    }

    public Pair SPair { get; init; }
    public StickyWindowType DrawType = StickyWindowType.None;
    public InteractionType OpenedInteraction = InteractionType.None;

    /// <summary> Task that blocks UI Interaction during a transaction update to prevent spamming and shiz. </summary>
    public static Task? UiTask { get; private set; }
    public static bool DisableUI => UiTask is not null && !UiTask.IsCompleted;

    private void OpenOrClose(InteractionType type) => OpenedInteraction = (type == OpenedInteraction) ? InteractionType.None : type;
    private void CloseInteraction() => OpenedInteraction = InteractionType.None;

    protected override void PreDrawInternal()
    {
        // Magic that makes the sticky pair window move with the main UI.
        var position = MainUI.LastPos;
        position.X += MainUI.LastSize.X;
        position.Y += ImGui.GetFrameHeightWithSpacing();
        ImGui.SetNextWindowPos(position);

        Flags |= WFlags.NoMove;

        var width = (DrawType == StickyWindowType.PairPerms) ? 160 * ImGuiHelpers.GlobalScale : 110 * ImGuiHelpers.GlobalScale;
        var size = new Vector2(7 * ImGui.GetFrameHeight() + 3 * ImGui.GetStyle().ItemInnerSpacing.X + width, MainUI.LastSize.Y - ImGui.GetFrameHeightWithSpacing() * 2);
        ImGui.SetNextWindowSize(size);
    }

    protected override void DrawInternal()
    {
        WindowMenuWidth = ImGui.GetContentRegionAvail().X;
        switch (DrawType)
        {
            case StickyWindowType.PairPerms:
                ImGuiUtil.Center($"{DisplayName}'s Permissions for You");
                ImGui.Separator();
                using (ImRaii.Child("PairPermsContent", new Vector2(0, ImGui.GetContentRegionAvail().Y), false, WFlags.NoScrollbar))
                    DrawPairPermsForClient();
                break;
            case StickyWindowType.ClientPermsForPair:
                ImGuiUtil.Center($"Your Permissions for {DisplayName}");
                CkGui.SetCursorXtoCenter(225f);
                _presets.DrawPresetList(SPair, 225f);

                ImGui.Separator();
                using (ImRaii.Child("ClientPermsForPairContent", new Vector2(0, ImGui.GetContentRegionAvail().Y), false, WFlags.NoScrollbar))
                    DrawClientPermsForPair();
                break;
            case StickyWindowType.PairActionFunctions:
                using (ImRaii.Child("##StickyWinActs", new Vector2(0, ImGui.GetContentRegionAvail().Y), false, WFlags.NoScrollbar))
                    DrawPairActionFunctions();
                break;
        }
    }
    protected override void PostDrawInternal()
    { }

    public override void OnClose()
    {
        Mediator.Publish(new RemoveWindowMessage(this));
    }
}
