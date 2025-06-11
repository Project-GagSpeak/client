using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.CustomCombos.Padlockable;

public class PairRestraintPadlockCombo : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PairRestraintPadlockCombo(Pair pair, MainHub hub, ILogger log)
        : base([ pair.LastRestraintData ], log)
    {
        _mainHub = hub;
        _pairRef = pair;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override string ItemName(CharaActiveRestraint item)
        => _pairRef.LastLightStorage.Restraints.FirstOrDefault(r => r.Id == item.Identifier) is { } restraint
            ? restraint.Label : "None";
    protected override bool DisableCondition(int _)
        => !_pairRef.PairPerms.ApplyRestraintSets || SelectedLock == Items[0].Padlock || !Items[0].CanLock();

    protected override async Task<bool> OnLockButtonPress(int _)
    {
        if (Items[0].CanLock() && _pairRef.PairPerms.LockRestraintSets)
        {
            var dto = new PushKinksterRestraintUpdate(_pairRef.UserData, DataUpdateType.Locked)
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterRestraintState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform LockRestraint with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Locking Restraint with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }

    protected override async Task<bool> OnUnlockButtonPress(int _)
    {
        if (Items[0].CanUnlock() && _pairRef.PairPerms.UnlockRestraintSets)
        {
            var dto = new PushKinksterRestraintUpdate(_pairRef.UserData, DataUpdateType.Unlocked)
            {
                Padlock = Items[0].Padlock,
                Password = Password,
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterRestraintState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform UnlockRestraint with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Unlocking Restraint with {SelectedLock.ToName()} on {_pairRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }
}
