using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestraintsClient : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly DataDistributionService _dds;
    private readonly RestraintManager _manager;
    public PadlockRestraintsClient(ILogger log, DataDistributionService dds, RestraintManager manager)
        : base(() => [ manager.ServerData ?? new CharaActiveRestraint() ], PadlockEx.ClientLocks, log)
    {
        _dds = dds;
        _manager = manager;
    }

    protected override string ItemName(CharaActiveRestraint item)
        => _manager.Storage.TryGetRestraint(item.Identifier, out var restraint) ? restraint.Label : "None";

    protected override bool DisableCondition(int _)
        => Items[0].Identifier == Guid.Empty;

    public void DrawLockCombo(float width, string tooltip)
        => DrawLockCombo("##ClientRestraintLock", width, 0, string.Empty, tooltip, true);

    public void DrawUnlockCombo(float width, string tooltip)
        => DrawUnlockCombo("##ClientRestraintUnlock", width, 0, string.Empty, tooltip);

    protected override async Task<bool> OnLockButtonPress(string label, int _)
    {
        if (!Items[0].CanLock())
            return false;

        if (!ValidateLock())
            return false;

        var finalTime = SelectedLock == Padlocks.FiveMinutesPadlock
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

        var newData = new CharaActiveRestraint()
        {
            Padlock = SelectedLock,
            Password = Password,
            Timer = finalTime,
            PadlockAssigner = MainHub.UID
        };

        if (await _dds.PushActiveRestraintUpdate(newData, DataUpdateType.Locked) is { } res && res is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform LockRestraint with {SelectedLock.ToName()} on self. Reason:{res}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            return false;
        }
        else
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            return true;
        }
    }
    protected override async Task<bool> OnUnlockButtonPress(string label, int _)
    {
        if (!Items[0].CanUnlock())
            return false;

        if (!ValidateUnlock())
            return false;

        var newData = new CharaActiveRestraint()
        {
            Padlock = Items[0].Padlock,
            Password = Items[0].Password,
            PadlockAssigner = MainHub.UID,
        };

        if (await _dds.PushActiveRestraintUpdate(newData, DataUpdateType.Unlocked) is { } res && res is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform UnlockRestraint with {Items[0].Padlock.ToName()} on self. Reason:{res}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            return false;
        }
        else
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            SelectedLock = Padlocks.None;
            return true;
        }
    }

    private bool ValidateLock()
    {
        // Determine if we have access to unlock.
        bool valid = SelectedLock switch
        {
            Padlocks.MetalPadlock or Padlocks.FiveMinutesPadlock => true,
            Padlocks.CombinationPadlock => PadlockValidation.IsValidCombo(Password),
            Padlocks.PasswordPadlock => PadlockValidation.IsValidPass(Password),
            Padlocks.TimerPadlock => PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)),
            Padlocks.TimerPasswordPadlock => PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)) && PadlockValidation.IsValidPass(Password),
            _ => false
        };

        if (valid)
            return true;

        // If we don't, display the appropriate error and reset inputs.
        switch (SelectedLock)
        {
            case Padlocks.CombinationPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4 digits (0-9).");
                break;

            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock when !PadlockValidation.IsValidPass(Password):
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4-20 characters.");
                break;

            case Padlocks.TimerPadlock:
            case Padlocks.TimerPasswordPadlock when !PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)):
                Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 0h2m7s).");
                break;

            default:
                Svc.Toasts.ShowError("Can't lock this Padlock.");
                break;
        }

        ResetInputs();
        return false;
    }

    private bool ValidateUnlock()
    {
        // Determine if we have access to unlock.
        bool valid = Items[0].Padlock switch
        {
            Padlocks.MetalPadlock or Padlocks.FiveMinutesPadlock => true,
            Padlocks.CombinationPadlock => Items[0].Password == Password,
            Padlocks.PasswordPadlock => Items[0].Password == Password,
            Padlocks.TimerPadlock => Items[0].PadlockAssigner == MainHub.UID,
            Padlocks.TimerPasswordPadlock => Items[0].Password == Password,
            _ => false
        };

        if (valid)
            return true;

        // If we don't, display the appropriate error and reset inputs.
        switch (SelectedLock)
        {
            case Padlocks.CombinationPadlock:
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                Svc.Toasts.ShowError("Password does not match!");
                break;

            case Padlocks.TimerPadlock:
                Svc.Toasts.ShowError("Can only be removed early by Assigner!");
                break;

            default:
                Svc.Toasts.ShowError("Can't unlock this padlock!");
                break;
        }

        ResetInputs();
        return false;
    }
}
