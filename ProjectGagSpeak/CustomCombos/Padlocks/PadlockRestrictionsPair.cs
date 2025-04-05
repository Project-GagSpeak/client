using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PairRestrictionPadlockCombo : CkPadlockComboBase<ActiveRestriction>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairRestrictionPadlockCombo(ILogger log, Pair pair, MainHub hub, Func<int, ActiveRestriction> generator)
        : base(generator, log)
    {
        _mainHub = hub;
        _pairRef = pair;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(ActiveRestriction item)
        => _pairRef.LastLightStorage.Restrictions.FirstOrDefault(r => r.Id == item.Identifier) is { } restriction
            ? restriction.Label : "None";
    protected override bool DisableCondition()
        => !_pairRef.PairPerms.ApplyRestrictions || SelectedLock == MonitoredItem.Padlock || !MonitoredItem.CanLock();

    protected override void OnLockButtonPress(int layerIdx)
    {
        if (MonitoredItem.CanLock() && _pairRef.PairPerms.LockRestrictions)
        {
            var dto = new PushPairRestrictionDataUpdateDto(_pairRef.UserData, DataUpdateType.Locked)
            {
                Layer = layerIdx,
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID,
            };

            _mainHub.UserPushPairDataRestrictions(dto).ConfigureAwait(false);
            Log.LogDebug("Locking Restriction with " + SelectedLock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }

    protected override void OnUnlockButtonPress(int layerIdx)
    {
        if (MonitoredItem.CanUnlock() && _pairRef.PairPerms.UnlockRestrictions)
        {
            var dto = new PushPairRestrictionDataUpdateDto(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Layer = layerIdx,
                Padlock = MonitoredItem.Padlock,
                Password = Password,
                PadlockAssigner = MainHub.UID,
            };
            _mainHub.UserPushPairDataRestrictions(dto).ConfigureAwait(false);
            Log.LogDebug("Unlocking Restriction with " + MonitoredItem.Padlock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }
}
