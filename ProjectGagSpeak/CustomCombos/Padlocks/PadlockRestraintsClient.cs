using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestraintsClient : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly GagspeakMediator _mediator;
    private readonly RestraintManager _manager;
    public PadlockRestraintsClient(ILogger log, GagspeakMediator mediator, RestraintManager manager)
        : base(() => [ manager.ServerData ?? new CharaActiveRestraint() ], log)
    {
        _mediator = mediator;
        _manager = manager;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.ClientLocks;

    protected override string ItemName(CharaActiveRestraint item)
        => _manager.Storage.TryGetRestraint(item.Identifier, out var restraint) ? restraint.Label : "None";

    protected override bool DisableCondition(int _)
        => Items[0].Identifier == Guid.Empty;

    public void DrawLockCombo(float width, string tooltip)
        => DrawLockCombo("##ClientRestraintLock", width, 0, string.Empty, tooltip, true);

    public void DrawUnlockCombo(float width, string tooltip)
        => DrawUnlockCombo("##ClientRestraintUnlock", width, 0, string.Empty, tooltip);

    protected override Task<bool> OnLockButtonPress(int _)
    {
        if (!Items[0].CanLock())
        {
            ResetInputs();
            return Task.FromResult(false);
        }
        if (!ValidateLock())
            return Task.FromResult(false);

        var newData = new CharaActiveRestraint()
        {
            Padlock = SelectedLock,
            Password = Password,
            Timer = Timer.GetEndTimeUTC(),
            PadlockAssigner = MainHub.UID
        };
        _mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Locked, newData));
        ResetSelection();
        ResetInputs();
        return Task.FromResult(true);
    }
    protected override Task<bool> OnUnlockButtonPress(int _)
    {

        if (!Items[0].CanUnlock())
        {
            ResetInputs();
            return Task.FromResult(false);
        }
        if (!ValidateUnlock())
            return Task.FromResult(false);

        var newData = new CharaActiveRestraint()
        {
            Padlock = Items[0].Padlock,
            Password = Items[0].Password,
            PadlockAssigner = MainHub.UID
        };

        _mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Unlocked, newData));
        ResetSelection();
        ResetInputs();
        return Task.FromResult(true);
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
        int layerIdx = 0;  // Unecessary but why not
        // Determine if we have access to unlock.
        bool valid = Items[layerIdx].Padlock switch
        {
            Padlocks.MetalPadlock or Padlocks.FiveMinutesPadlock => true,
            Padlocks.CombinationPadlock => Items[layerIdx].Password == Password,
            Padlocks.PasswordPadlock => Items[layerIdx].Password == Password,
            Padlocks.TimerPadlock => Items[layerIdx].PadlockAssigner == MainHub.UID,
            Padlocks.TimerPasswordPadlock => Items[layerIdx].Password == Password,
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
