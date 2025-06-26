using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.CustomCombos.Padlock;

// only has one item in it? idk if it works it works lol.
public class PairRestraintPadlockCombo : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly MainHub _mainHub;
    private Kinkster _ref;
    public PairRestraintPadlockCombo(ILogger log, MainHub hub, Kinkster kinkster)
        : base(() => [ kinkster.LastRestraintData ], log)
    {
        _mainHub = hub;
        _ref = kinkster;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.GetLocksForPair(_ref.PairPerms);
    protected override string ItemName(CharaActiveRestraint item)
        => _ref.LastLightStorage.Restraints.FirstOrDefault(r => r.Id == item.Identifier) is { } restraint
            ? restraint.Label : "None";
    protected override bool DisableCondition(int _)
        => !_ref.PairPerms.ApplyRestraintSets || SelectedLock == Items[0].Padlock || !Items[0].CanLock();

    protected override async Task<bool> OnLockButtonPress(int _)
    {
        if (Items[0].CanLock() && _ref.PairPerms.LockRestraintSets)
        {
            var dto = new PushKinksterRestraintUpdate(_ref.UserData, DataUpdateType.Locked)
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterRestraintState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform LockRestraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Locking Restraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }

    protected override async Task<bool> OnUnlockButtonPress(int _)
    {
        if (Items[0].CanUnlock() && _ref.PairPerms.UnlockRestraintSets)
        {
            var dto = new PushKinksterRestraintUpdate(_ref.UserData, DataUpdateType.Unlocked)
            {
                Padlock = Items[0].Padlock,
                Password = Password,
                PadlockAssigner = MainHub.UID,
            };

            var result = await _mainHub.UserChangeKinksterRestraintState(dto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Log.LogDebug($"Failed to perform UnlockRestraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
                ResetSelection();
                ResetInputs();
                return false;
            }
            else
            {
                Log.LogDebug($"Unlocking Restraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
                ResetSelection();
                ResetInputs();
                return true;
            }
        }
        return false;
    }
}
