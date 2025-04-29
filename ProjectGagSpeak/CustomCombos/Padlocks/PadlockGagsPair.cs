using GagSpeak.PlayerData.Pairs;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PairGagPadlockCombo : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairGagPadlockCombo(ILogger log, Pair pairData, MainHub mainHub, Func<int, ActiveGagSlot> generator)
        : base(generator, log)
    {
        _mainHub = mainHub;
        _pairRef = pairData;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();
    protected override bool DisableCondition()
        => MonitoredItem.GagItem is GagType.None;

    protected override void OnLockButtonPress(int layerIdx)
    {
        if (MonitoredItem.CanLock() && _pairRef.PairPerms.LockGags)
        {
            var dto = new PushPairGagDataUpdateDto(_pairRef.UserData, DataUpdateType.Locked)
            {
                Layer = (int)layerIdx,
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID,
            };

            _ = _mainHub.UserPushPairDataGags(dto);
            Log.LogDebug("Locking Gag with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }

    protected override void OnUnlockButtonPress(int layerIdx)
    {
        if (MonitoredItem.CanUnlock() && _pairRef.PairPerms.UnlockGags)
        {
            var dto = new PushPairGagDataUpdateDto(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Layer = layerIdx,
                Padlock = MonitoredItem.Padlock,
                Password = Password, // Our guessed password.
                PadlockAssigner = MainHub.UID,
            };
            _ = _mainHub.UserPushPairDataGags(dto);
            Log.LogDebug("Unlocking Gag with GagPadlock " + MonitoredItem.Padlock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }
}
