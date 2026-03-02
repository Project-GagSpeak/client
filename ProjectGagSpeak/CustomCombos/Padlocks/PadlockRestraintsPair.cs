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
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _ref;

    public PairRestraintPadlockCombo(ILogger log, MainHub hub, Kinkster k, Action postButtonPress)
        : base(() => [..PadlockEx.GetLocksForPair(k.PairPerms)], log)
    {
        _mainHub = hub;
        _ref = k;
        PostButtonPress = postButtonPress;
    }

    protected override string ItemName(CharaActiveRestraint item)
        => _ref.LightCache.Restraints.TryGetValue(item.Identifier, out var restraint) ? restraint.Label : "None";

    protected override bool DisableCondition(int _)
        => ActiveItem.Identifier == Guid.Empty;

    public override void DrawLockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip, bool isTwoRow)
    {
        ActiveItem = _ref.ActiveRestraint;
        base.DrawLockCombo(label, width, layerIdx, buttonTxt, tooltip, isTwoRow);
    }

    public override void DrawUnlockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip)
    {
        ActiveItem = _ref.ActiveRestraint;
        base.DrawUnlockCombo(label, width, layerIdx, buttonTxt, tooltip);
    }

    protected override async Task OnLockButtonPress(string label, int _)
    {
        // return if we cannot lock.
        if (!ActiveItem.CanLock() || !_ref.PairPerms.LockRestraintSets)
            return;

        // we know it was valid, so begin assigning the new data to send off.
        var finalTime = SelectedLock == Padlocks.FiveMinutes
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5))
            : Timer.GetEndTimeUTC();

        var dto = new PushKinksterActiveRestraint(_ref.UserData, DataUpdateType.Locked)
        {
            Padlock = SelectedLock,
            Password = Password,
            Timer = finalTime,
            PadlockAssigner = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterActiveRestraint(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform LockRestraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            DisplayToastErrorAndReset(result.ErrorCode, SelectedLock, false);
            return;
        }

        Log.LogDebug($"Locking Restraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
        ResetSelection();
        ResetInputs();
        ActiveItem = new CharaActiveRestraint();
        PostButtonPress.Invoke();
    }

    protected override async Task OnUnlockButtonPress(string label, int _)
    {
        if (!ActiveItem.CanUnlock() || !_ref.PairPerms.UnlockRestraintSets)
            return;

        var dto = new PushKinksterActiveRestraint(_ref.UserData, DataUpdateType.Unlocked)
        {
            Padlock = ActiveItem.Padlock,
            Password = Password,
            PadlockAssigner = MainHub.UID,
        };

        Svc.Logger.Debug($"Attempting to unlock restraint with {dto.ToString()}");

        var result = await _mainHub.UserChangeKinksterActiveRestraint(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform UnlockRestraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            DisplayToastErrorAndReset(result.ErrorCode, ActiveItem.Padlock, true);
            return;
        }

        Log.LogDebug($"Unlocking Restraint with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
        ResetSelection();
        ResetInputs();
        ActiveItem = new CharaActiveRestraint();
        SelectedLock = Padlocks.None;
        PostButtonPress.Invoke();
    }

    private bool DisplayToastErrorAndReset(GagSpeakApiEc errorCode, Padlocks padlock, bool unlocking)
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

            case GagSpeakApiEc.InvalidPassword when unlocking:
                Svc.Toasts.ShowError("Incorrectly guessed this padlocks password!");
                break;

            case GagSpeakApiEc.InvalidPassword when padlock is Padlocks.Combination:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4 digits (0-9).");
                break;

            case GagSpeakApiEc.InvalidPassword when padlock.IsPasswordLock():
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4-20 characters.");
                break;

            case GagSpeakApiEc.InvalidTime when padlock.IsTimerLock():
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
                Svc.Toasts.ShowError("No active item is present.");
                break;

            case GagSpeakApiEc.NotItemAssigner:
                Svc.Toasts.ShowError("This padlock can only be removed by its assigner.");
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
