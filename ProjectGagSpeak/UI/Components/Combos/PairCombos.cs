using Dalamud.Plugin.Services;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.CustomCombos.Padlockable;
using GagSpeak.CustomCombos.PairActions;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Components.Combos;

// responsible for updating and unifying custom pair combos.
public class PairCombos
{
    private const int MaxGagLayers = 3;
    private const int MaxRestrictionLayers = 5;
    private const int MaxRestraintSets = 1;
    private const int MaxRestraintSetLayers = 5;

    private readonly ILogger<PairCombos> _logger;
    private readonly MainHub _hub;
    private readonly UiSharedService _uiShared;
    private readonly ITextureProvider _tp;

    // Make the following static and public to allow global access throughout custom combos.
    public static InteractionType Opened = InteractionType.None;
    private int _gagLayer = 0;
    private int _restrictionLayer = 0;
    private int _restraintLayer = 0;
    public int CurGagLayer => _gagLayer;
    public int CurRestrictionLayer => _restrictionLayer;
    public int CurRestraintLayer => _restraintLayer;

    public PairCombos(ILogger<PairCombos> logger, MainHub hub, UiSharedService uiShared, ITextureProvider tp)
    {
        _logger = logger;
        _hub = hub;
        _uiShared = uiShared;
        _tp = tp;
    }

    public PairGagCombo GagApplyCombo { get; private set; }
    public PairGagPadlockCombo GagPadlockCombo { get; private set; }
    public PairRestrictionCombo RestrictionApplyCombo { get; private set; }
    public PairRestrictionPadlockCombo RestrictionPadlockCombo { get; private set; }
    public PairRestraintCombo RestraintApplyCombo { get; private set; }
    public PairRestraintPadlockCombo RestraintPadlockCombo { get; private set; }
    public PairPatternCombo PatternCombo { get; private set; }
    public PairAlarmCombo AlarmToggleCombo { get; private set; }
    public PairTriggerCombo TriggerToggleCombo { get; private set; }
    public EmoteCombo EmoteCombo { get; private set; }

    private static readonly string[] GagLayerNames = new string[] { "Layer 1", "Layer 2", "Layer 3" };
    private static readonly string[] FiveLayerNames = new string[] { "Layer 1", "Layer 2", "Layer 3", "Layer 4", "Layer 5" };

    public void DrawGagLayerSelection(float comboWidth)
    {
        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.Combo("##GagLayer", ref _gagLayer, GagLayerNames, 3))
            GagApplyCombo.SetLayer(_gagLayer);
        UiSharedService.AttachToolTip("Select the layer to apply a Gag to.");
    }

    public void DrawRestrictionLayerSelection(float comboWidth)
    {
        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.Combo("##RestrictionLayer", ref _restrictionLayer, FiveLayerNames, 5))
            RestrictionApplyCombo.SetLayer(_restrictionLayer);
        UiSharedService.AttachToolTip("Select the layer to apply a Restriction to.");
    }

    public void DrawRestraintLayerSelection(float comboWidth)
    {
        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.Combo("##RestraintLayer", ref _restraintLayer, FiveLayerNames, 5)) { }
        UiSharedService.AttachToolTip("Select the layer to apply a Restraint to.");
    }



    public void UpdateCombosForPair(Pair pair)
    {
        _gagLayer = 0;
        _restrictionLayer = 0;
        _restraintLayer = 0;

        GagApplyCombo = new PairGagCombo(_gagLayer, pair, _hub, _logger, _uiShared, "Apply", "Apply a Gag to " + pair.GetNickAliasOrUid());
        GagPadlockCombo = new PairGagPadlockCombo(_gagLayer, pair, _hub, _logger, _uiShared, "GagPadlock");


        // Create the Restraint Combos for the Pair.
        RestraintApplyCombo = new PairRestraintCombo(pair, _hub, _logger, _uiShared, "Apply", "Apply a Restraint to " + pair.GetNickAliasOrUid());
        RestraintPadlockCombo = new PairRestraintPadlockCombo(pair, _hub, _logger, _uiShared);

        // Create the Pattern Combo for the Pair.
        PatternCombo = new PairPatternCombo(pair, _hub, _logger, _uiShared, "Execute", "Apply a Pattern to " + pair.GetNickAliasOrUid());

        // Create the pattern Combo for alarm Toggling.
        AlarmToggleCombo = new PairAlarmCombo(pair, _hub, _logger, _uiShared, "Enable", "Toggle this Alarm for " + pair.GetNickAliasOrUid());

        // Create the Triggers combo for trigger toggling.
        TriggerToggleCombo = new PairTriggerCombo(pair, _hub, _logger, _uiShared, "Enable", "Toggle this Trigger for " + pair.GetNickAliasOrUid());

        EmoteCombo = new EmoteCombo(_tp, _logger, () => 
        [
            ..pair.PairPerms.AllowForcedEmote
                ? EmoteMonitor.ValidEmotes.OrderBy(e => e.RowId)
                : EmoteMonitor.SitEmoteComboList.OrderBy(e => e.RowId)
        ]);

        Opened = InteractionType.None;
    }
}
