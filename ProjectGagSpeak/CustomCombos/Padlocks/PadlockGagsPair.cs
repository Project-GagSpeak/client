using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PairGagPadlockCombo : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    private int CurrentLayer;
    public PairGagPadlockCombo(int layer, Pair pairData, MainHub mainHub, ILogger log, string label)
        : base(() => pairData.LastGagData.GagSlots[layer], log, label)
    {
        CurrentLayer = layer;
        _mainHub = mainHub;
        _pairRef = pairData;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => GsPadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();
    protected override bool DisableCondition()
        => _pairRef.LastGagData.GagSlots[CurrentLayer].GagItem is GagType.None || _pairRef.PairPerms.ApplyGags is false;

    protected override void OnLockButtonPress()
    {
        if (MonitoredItem.CanLock() && _pairRef.PairPerms.LockGags)
        {
            var dto = new PushPairGagDataUpdateDto(_pairRef.UserData, DataUpdateType.Locked)
            {
                Layer = (GagLayer)CurrentLayer,
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                Assigner = MainHub.UID,
            };

            _ = _mainHub.UserPushPairDataGags(dto);
            Log.LogDebug("Locking Gag with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
            PairCombos.Opened = InteractionType.None;
            ResetSelection();
            return;
        }

        ResetInputs();
    }

    protected override void OnUnlockButtonPress()
    {
        if (MonitoredItem.CanUnlock() && _pairRef.PairPerms.UnlockGags)
        {
            var dto = new PushPairGagDataUpdateDto(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Layer = (GagLayer)CurrentLayer,
                Padlock = MonitoredItem.Padlock,
                Password = Password, // Our guessed password.
                Assigner = MainHub.UID,
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
