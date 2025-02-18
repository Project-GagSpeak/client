using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PairRestraintPadlockCombo : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairRestraintPadlockCombo(Pair pair, MainHub hub, ILogger log, UiSharedService ui)
        : base(() => pair.LastRestraintData, log, ui, "PairRestraintPadlock")
    {
        _mainHub = hub;
        _pairRef = pair;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => GsPadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(CharaActiveRestraint item)
        => _pairRef.LastLightStorage.Restraints.FirstOrDefault(r => r.Id == item.Identifier) is { } restraint
            ? restraint.Label : "None";
    protected override bool DisableCondition()
        => _pairRef.PairPerms.ApplyRestraintSets is false
        || SelectedLock == MonitoredItem.Padlock
        || MonitoredItem.CanLock() is false;

    protected override void OnLockButtonPress()
    {
        if (MonitoredItem.CanLock() && _pairRef.PairPerms.LockRestraintSets)
        {
            var dto = new PushPairRestraintDataUpdateDto(_pairRef.UserData, DataUpdateType.Locked)
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                Assigner = MainHub.UID,
            };

            _ = _mainHub.UserPushPairDataRestraint(dto);
            Log.LogDebug("Locking Restraint with " + SelectedLock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }

    protected override void OnUnlockButtonPress()
    {
        if (MonitoredItem.CanUnlock() && _pairRef.PairPerms.UnlockRestraintSets)
        {
            var dto = new PushPairRestraintDataUpdateDto(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Padlock = MonitoredItem.Padlock,
                Password = Password,
                Assigner = MainHub.UID,
            };
            _ = _mainHub.UserPushPairDataRestraint(dto);
            Log.LogDebug("Unlocking Restraint with " + MonitoredItem.Padlock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }
}
