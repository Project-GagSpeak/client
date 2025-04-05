using Dalamud.Plugin.Services;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.CustomCombos.Padlockable;
using GagSpeak.CustomCombos.PairActions;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using ImGuiNET;

namespace GagSpeak.UI.Components;

public class PairCombos : DisposableMediatorSubscriberBase
{
    private const int MaxGagLayers = 3;
    private const int MaxRestrictionLayers = 5;
    private const int MaxRestraintSets = 1;
    private const int MaxRestraintSetLayers = 5;

    private readonly MainHub _hub;
    private readonly ITextureProvider _tp;

    // Make the following static and public to allow global access throughout custom combos.
    public static InteractionType Opened = InteractionType.None;
    private int _gagLayer = 0;
    private int _restrictionLayer = 0;
    private int _restraintLayer = 0;

    public PairCombos(ILogger<PairCombos> logger, GagspeakMediator mediator, MainHub hub, ITextureProvider tp) : base(logger, mediator)
    {
        _hub = hub;
        _tp = tp;

        Mediator.Subscribe<StickyPairWindowCreated>(this, (pair) => UpdateCombosForPair(pair.newPair));
    }

    public int CurGagLayer => _gagLayer;
    public int CurRestrictionLayer => _restrictionLayer;
    public int CurRestraintLayer => _restraintLayer;

    public PairGagCombo GagItemCombo { get; private set; }
    public PairGagPadlockCombo GagPadlockCombo { get; private set; }
    public PairRestrictionCombo RestrictionItemCombo { get; private set; }
    public PairRestrictionPadlockCombo RestrictionPadlockCombo { get; private set; }
    public PairRestraintCombo RestraintItemCombo { get; private set; }
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
        ImGui.Combo("##int", ref _gagLayer, GagLayerNames, 3);
        CkGui.AttachToolTip("Select the layer to apply a Gag to.");
    }

    public void DrawRestrictionLayerSelection(float comboWidth)
    {
        ImGui.SetNextItemWidth(comboWidth);
        ImGui.Combo("##RestrictionLayer", ref _restrictionLayer, FiveLayerNames, 5);
        CkGui.AttachToolTip("Select the layer to apply a Restriction to.");
    }

    public void DrawRestraintLayerSelection(float comboWidth)
    {
        ImGui.SetNextItemWidth(comboWidth);
        ImGui.Combo("##RestraintLayer", ref _restraintLayer, FiveLayerNames, 5);
        CkGui.AttachToolTip("Select the layer to apply a Restraint to.");
    }

    private void UpdateCombosForPair(Pair pair)
    {
        _gagLayer = 0;
        _restrictionLayer = 0;
        _restraintLayer = 0;

        GagItemCombo = new PairGagCombo(Logger, pair, _hub, () => [ ..Enum.GetValues<GagType>().Skip(1)]);
        GagPadlockCombo = new PairGagPadlockCombo(Logger, pair, _hub, (idx) => pair.LastGagData.GagSlots[idx]);

        RestrictionItemCombo = new PairRestrictionCombo(Logger, pair, _hub, () => [.. pair.LastLightStorage.Restrictions.OrderBy(x => x.Label)]);
        RestrictionPadlockCombo = new PairRestrictionPadlockCombo(Logger, pair, _hub, (idx) => pair.LastRestrictionsData.Restrictions[idx]);

        RestraintItemCombo = new PairRestraintCombo(Logger, pair, _hub, () => [.. pair.LastLightStorage.Restraints.OrderBy(x => x.Label)]);
        RestraintPadlockCombo = new PairRestraintPadlockCombo(Logger, pair, _hub, (_) => pair.LastRestraintData);

        RestraintItemCombo = new PairRestraintCombo(Logger, pair, _hub, () => [ ..pair.LastLightStorage.Restraints.OrderBy(x => x.Label)]);
        RestraintPadlockCombo = new PairRestraintPadlockCombo(Logger, pair, _hub, (_) => pair.LastRestraintData);


        PatternCombo = new PairPatternCombo(pair, _hub, Logger, "Execute", "Apply a Pattern to " + pair.GetNickAliasOrUid());
        AlarmToggleCombo = new PairAlarmCombo(pair, _hub, Logger, "Enable", "Toggle this Alarm for " + pair.GetNickAliasOrUid());
        TriggerToggleCombo = new PairTriggerCombo(pair, _hub, Logger, "Enable", "Toggle this Trigger for " + pair.GetNickAliasOrUid());

        EmoteCombo = new EmoteCombo(_tp, Logger, () => 
        [
            ..pair.PairPerms.AllowForcedEmote
                ? EmoteMonitor.ValidEmotes.OrderBy(e => e.RowId)
                : EmoteMonitor.SitEmoteComboList.OrderBy(e => e.RowId)
        ]);

        Opened = InteractionType.None;
    }
}
