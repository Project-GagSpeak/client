using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using GagSpeak.UI.MainWindow;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PairRestraintPadlockCombo : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairRestraintPadlockCombo(ILogger log, Pair pair, MainHub hub, Func<int, CharaActiveRestraint> generator)
        : base(generator, log)
    {
        _mainHub = hub;
        _pairRef = pair;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(CharaActiveRestraint item)
        => _pairRef.LastLightStorage.Restraints.FirstOrDefault(r => r.Id == item.Identifier) is { } restraint
            ? restraint.Label : "None";
    protected override bool DisableCondition()
        => !_pairRef.PairPerms.ApplyRestraintSets || SelectedLock == MonitoredItem.Padlock || !MonitoredItem.CanLock();

    protected override void OnLockButtonPress(int _)
    {
        if (MonitoredItem.CanLock() && _pairRef.PairPerms.LockRestraintSets)
        {
            var dto = new PushPairRestraintDataUpdateDto(_pairRef.UserData, DataUpdateType.Locked)
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID,
            };

            _mainHub.UserPushPairDataRestraint(dto).ConfigureAwait(false);
            Log.LogDebug("Locking Restraint with " + SelectedLock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }

    protected override void OnUnlockButtonPress(int _)
    {
        if (MonitoredItem.CanUnlock() && _pairRef.PairPerms.UnlockRestraintSets)
        {
            var dto = new PushPairRestraintDataUpdateDto(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Padlock = MonitoredItem.Padlock,
                Password = Password,
                PadlockAssigner = MainHub.UID,
            };
            _mainHub.UserPushPairDataRestraint(dto).ConfigureAwait(false);
            Log.LogDebug("Unlocking Restraint with " + MonitoredItem.Padlock.ToName() + " on " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }
}
