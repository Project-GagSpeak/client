using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.CustomCombos.Padlock;

public class PairRestrictionPadlockCombo : CkPadlockComboBase<ActiveRestriction>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _ref;
    public PairRestrictionPadlockCombo(ILogger log, MainHub hub, Kinkster k, Action postButtonPress)
        : base(() => [..PadlockEx.GetLocksForPair(k.PairPerms)], log)
    {
        _mainHub = hub;
        _ref = k;
        PostButtonPress = postButtonPress;
    }

    protected override string ItemName(ActiveRestriction item)
        => _ref.LightCache.Restrictions.TryGetValue(item.Identifier, out var bind) ? bind.Label : "None";
    protected override bool DisableCondition(int layerIdx)
        => ActiveItem.Identifier == Guid.Empty;

    public override void DrawLockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip, bool isTwoRow)
    {
        ActiveItem = _ref.ActiveRestrictions.Restrictions[layerIdx];
        base.DrawLockCombo(label, width, layerIdx, buttonTxt, tooltip, isTwoRow);
    }

    public override void DrawUnlockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip)
    {
        ActiveItem = _ref.ActiveRestrictions.Restrictions[layerIdx];
        base.DrawUnlockCombo(label, width, layerIdx, buttonTxt, tooltip);
    }

    protected override async Task OnLockButtonPress(string label, int layerIdx)
    {
        // return if we cannot lock.
        if (!ActiveItem.CanLock() || !_ref.PairPerms.LockRestrictions)
            return;

        // we know it was valid, so begin assigning the new data to send off.
        var finalTime = SelectedLock == Padlocks.FiveMinutes
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

        var dto = new PushKinksterActiveRestriction(_ref.UserData, DataUpdateType.Locked)
        {
            Layer = layerIdx,
            Padlock = SelectedLock,
            Password = Password,
            Timer = finalTime,
            PadlockAssigner = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterActiveRestriction(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform LockRestriction with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            DisplayToastErrorAndReset(result.ErrorCode, SelectedLock, false);
        }
        else
        {
            Log.LogDebug($"Locking Restriction with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            ActiveItem = new ActiveRestriction();
            PostButtonPress?.Invoke();
        }
    }

    protected override async Task OnUnlockButtonPress(string label, int layerIdx)
    {
        if (!ActiveItem.CanUnlock() || !_ref.PairPerms.UnlockRestrictions)
            return;

        var dto = new PushKinksterActiveRestriction(_ref.UserData, DataUpdateType.Unlocked)
        {
            Layer = layerIdx,
            Padlock = ActiveItem.Padlock,
            Password = Password,
            PadlockAssigner = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterActiveRestriction(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform UnlockRestriction with {ActiveItem.Padlock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            DisplayToastErrorAndReset(result.ErrorCode, ActiveItem.Padlock, true);
        }
        else
        {
            Log.LogDebug($"Unlocking Restriction with {ActiveItem.Padlock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            ActiveItem = new ActiveRestriction();
            SelectedLock = Padlocks.None;
            PostButtonPress?.Invoke();
        }
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
