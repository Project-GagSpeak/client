using GagSpeak.Kinksters.Pairs;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;

namespace GagSpeak.CustomCombos.Padlock;

public class PairGagPadlockCombo : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairGagPadlockCombo(Pair pair, MainHub hub, ILogger log)
        : base([ ..pair.LastGagData.GagSlots ], log)
    {
        _mainHub = hub;
        _pairRef = pair;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();
    
    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].GagItem is GagType.None;

    protected override async Task<bool> OnLockButtonPress(int layerIdx)
    {
        if (Items[layerIdx].CanLock() && _pairRef.PairPerms.LockGags)
        {
            var dto = new PushKinksterGagSlotUpdate(_pairRef.UserData, DataUpdateType.Locked)
            {
                Layer = layerIdx,
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterGagState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform LockGag with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}, Reason:{result}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Locking Gag with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }

    protected override async Task<bool> OnUnlockButtonPress(int layerIdx)
    {
        if (Items[layerIdx].CanUnlock() && _pairRef.PairPerms.UnlockGags)
        {
            var dto = new PushKinksterGagSlotUpdate(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Layer = layerIdx,
                Padlock = Items[layerIdx].Padlock,
                Password = Password, // Our guessed password.
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterGagState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform UnlockGag with {Items[layerIdx].Padlock.ToName()} on {_pairRef.GetNickAliasOrUid()}, Reason:{result}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Unlocking Gag with {Items[layerIdx].Padlock.ToName()} on {_pairRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }
}
