using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Components.Combos;
public class PadlockRestraintsClient : PadlockBase<RestraintSet>
{
    private readonly GagspeakMediator _mediator;
    private readonly ClientConfigurationManager _restraintData;
    public PadlockRestraintsClient(GagspeakMediator mediator, ClientConfigurationManager restraintData,
        ILogger log, UiSharedService uiShared, string label) : base(log, uiShared, label)
    {
        _mediator = mediator;
        _restraintData = restraintData;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks() => LockHelperExtensions.ClientLocks;
    protected override Padlocks GetLatestPadlock() => GetLatestActiveItem().Padlock.ToPadlock();
    protected override RestraintSet GetLatestActiveItem() => _restraintData.GetActiveSet() ?? new RestraintSet();
    protected override string ToActiveItemString(RestraintSet item) => item.Name;

    public void DrawPadlockComboSection(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // grab the latest padlock. If it is not none, we should draw the unlock base, otherwise, draw the lock base.
        if (GetLatestPadlock() is not Padlocks.None)
            DrawUnlockCombo(width, tt, btt, flags);
        else
            DrawLockCombo(width, tt, btt, flags);
    }

    protected override void OnLockButtonPress()
    {
        // dont do anything if there is no active set.
        if (_restraintData.GetActiveSet() is null) return;

        // grab our current data, and compile it for the api sendoff.
        var newWardrobeData = _restraintData.CompileWardrobeToAPI();

        // see if things are valid through the lock helper extensions.
        PadlockReturnCode validationResult = LockHelperExtensions.VerifyLock(ref newWardrobeData, _selectedLock, _password, _timer, MainHub.UID);

        // if the validation result is anything but successful, log it and return.
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to lock padlock: " + _selectedLock.ToName() + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
            ResetInputs();
            return;
        }

        // it was successful (we reached this point), send off new data to the server.
        _logger.LogTrace("Sending off Lock Applied Event to server!", LoggerType.PadlockHandling);
        _mediator.Publish(new PlayerCharWardrobeChanged(newWardrobeData, WardrobeUpdateType.RestraintLocked, Padlocks.None));
        ResetSelection();
    }

    protected override void OnUnlockButtonPress()
    {
        // dont do anything if there is no active set.
        if (_restraintData.GetActiveSet() is null) return;

        // grab our current data, and compile it for the api sendoff.
        var newWardrobeData = _restraintData.CompileWardrobeToAPI();

        // get the previous lock before we update it.
        var prevLock = newWardrobeData.Padlock.ToPadlock();

        _logger.LogDebug("Verifying unlock for padlock: " + _selectedLock.ToName(), LoggerType.PadlockHandling);

        // verify if we can unlock.
        PadlockReturnCode validationResult = LockHelperExtensions.VerifyUnlock(ref newWardrobeData, MainHub.PlayerUserData, _password, MainHub.UID);

        // if the validation result is anything but successful, log it and return.
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to unlock padlock: " + _selectedLock.ToName() + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
            ResetInputs();
            return;
        }

        // update the wardrobe data on the server with the new information.
        _logger.LogDebug("Unlocking Restraint Set with GagPadlock " + _selectedLock.ToName(), LoggerType.Permissions);
        _mediator.Publish(new PlayerCharWardrobeChanged(newWardrobeData, WardrobeUpdateType.RestraintUnlocked, prevLock));
        ResetSelection();
    }
}
