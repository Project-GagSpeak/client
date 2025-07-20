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
    public PairRestraintPadlockCombo(ILogger log, MainHub hub, Kinkster k)
        : base(() => [k.LastRestraintData], () => [..PadlockEx.GetLocksForPair(k.PairPerms)], log)
    {
        _mainHub = hub;
        _ref = k;
    }

    protected override string ItemName(CharaActiveRestraint item)
        => _ref.LastLightStorage.Restraints.FirstOrDefault(r => r.Id == item.Identifier) is { } restraint
            ? restraint.Label : "None";
    protected override bool DisableCondition(int _)
        => !_ref.PairPerms.ApplyRestraintSets || SelectedLock == Items[0].Padlock || !Items[0].CanLock();

    protected override async Task<bool> OnLockButtonPress(int _)
    {
        // return if we cannot lock.
        if (!Items[0].CanLock() || !_ref.PairPerms.LockRestraintSets)
            return false;

        // we know it was valid, so begin assigning the new data to send off.
        var finalTime = SelectedLock == Padlocks.FiveMinutesPadlock
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

        var dto = new PushKinksterRestraintUpdate(_ref.UserData, DataUpdateType.Locked)
        {
            Padlock = SelectedLock,
            Password = Password,
            Timer = finalTime,
            PadlockAssigner = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterRestraintState(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform LockRestraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            DisplayToastErrorAndReset(result.ErrorCode, SelectedLock);
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

    protected override async Task<bool> OnUnlockButtonPress(int _)
    {
        if (!Items[0].CanUnlock() || !_ref.PairPerms.UnlockRestraintSets)
            return false;

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
            DisplayToastErrorAndReset(result.ErrorCode, Items[0].Padlock);
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

    private bool DisplayToastErrorAndReset(GagSpeakApiEc errorCode, Padlocks padlock)
    {
        // Determine if we have access to unlock.
        switch (errorCode)
        {
            case GagSpeakApiEc.BadUpdateKind:
                Svc.Toasts.ShowError("Invalid Update Kind. Please try again.");
                break;

            case GagSpeakApiEc.InvalidLayer:
                Svc.Toasts.ShowError("Attempted to apply to a layer that was invalid.");
                break;

            case GagSpeakApiEc.InvalidPassword when padlock is Padlocks.CombinationPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4 digits (0-9).");
                break;

            case GagSpeakApiEc.InvalidPassword when padlock is Padlocks.PasswordPadlock or Padlocks.TimerPasswordPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4-20 characters.");
                break;

            case GagSpeakApiEc.InvalidTime when padlock is Padlocks.TimerPadlock or Padlocks.TimerPasswordPadlock:
                Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 0h2m7s).");
                break;

            case GagSpeakApiEc.LackingPermissions:
                Svc.Toasts.ShowError("You do not have permission to perform this action.");
                break;

            case GagSpeakApiEc.ItemIsLocked:
                Svc.Toasts.ShowError("This item is already locked.");
                break;

            case GagSpeakApiEc.ItemNotLocked:
                Svc.Toasts.ShowError("Cannot unlock item, as it is not yet locked.");
                break;

            case GagSpeakApiEc.NoActiveItem:
                Svc.Toasts.ShowError("Cannot remove this item as you are did not apply it.");
                break;

            case GagSpeakApiEc.NotItemAssigner:
                Svc.Toasts.ShowError("Cannot remove lock, it can only be removed by it's assigner.");
                break;

            default:
                Svc.Logger.Warning($"UNK Padlock Error: {errorCode}.");
                break;
        }
        ResetSelection();
        ResetInputs();
        return false;
    }
}
