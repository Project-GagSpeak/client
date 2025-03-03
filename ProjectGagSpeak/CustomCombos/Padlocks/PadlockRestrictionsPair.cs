using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PairRestrictionPadlockCombo : CkPadlockComboBase<ActiveRestriction>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    private int CurrentLayer;
    public PairRestrictionPadlockCombo(int layer, Pair pair, MainHub hub, ILogger log)
        : base(() => pair.LastRestrictionsData.Restrictions[layer], log, "PairRestrictionPadlock" + layer)
    {
        CurrentLayer = layer;
        _mainHub = hub;
        _pairRef = pair;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => GsPadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(ActiveRestriction item)
        => _pairRef.LastLightStorage.Restrictions.FirstOrDefault(r => r.Id == item.Identifier) is { } restriction
            ? restriction.Label : "None";
    protected override bool DisableCondition()
        => _pairRef.PairPerms.ApplyRestrictions is false
        || SelectedLock == MonitoredItem.Padlock
        || MonitoredItem.CanLock() is false;

    protected override void OnLockButtonPress()
    {
        if (MonitoredItem.CanLock() && _pairRef.PairPerms.LockRestrictions)
        {
            var dto = new PushPairRestrictionDataUpdateDto(_pairRef.UserData, DataUpdateType.Locked)
            {
                AffectedIndex = CurrentLayer,
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                Assigner = MainHub.UID,
            };

            _ = _mainHub.UserPushPairDataRestrictions(dto);
            Log.LogDebug("Locking Restriction with " + SelectedLock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }

    protected override void OnUnlockButtonPress()
    {
        if (MonitoredItem.CanUnlock() && _pairRef.PairPerms.UnlockRestrictions)
        {
            var dto = new PushPairRestrictionDataUpdateDto(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                AffectedIndex = CurrentLayer,
                Padlock = MonitoredItem.Padlock,
                Password = Password,
                Assigner = MainHub.UID,
            };
            _ = _mainHub.UserPushPairDataRestrictions(dto);
            Log.LogDebug("Unlocking Restriction with " + MonitoredItem.Padlock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }
}
