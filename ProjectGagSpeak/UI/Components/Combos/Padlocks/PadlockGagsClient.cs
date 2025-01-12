using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Components.Combos;
public class PadlockGagsClient : PadlockBase<GagSlot>
{
    private readonly GagspeakMediator _mediator;
    private readonly ClientData _gagData;
    public int GagSlotLayer { get; init; }

    public PadlockGagsClient(GagspeakMediator mediator, ClientData gagData, ILogger log, 
        UiSharedService uiShared, string label) : base(log, uiShared, label)
    {
        _mediator = mediator;
        _gagData = gagData;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks() => LockHelperExtensions.ClientLocks;
    protected override Padlocks GetLatestPadlock() => _gagData.AppearanceData?.GagSlots[GagSlotLayer].Padlock.ToPadlock() ?? Padlocks.None;
    protected override GagSlot GetLatestActiveItem() => _gagData.AppearanceData?.GagSlots[GagSlotLayer] ?? new GagSlot();
    protected override string ToActiveItemString(GagSlot item) => item.GagType;

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
        // do not do anything if appearance data is null.
        if (_gagData.AppearanceData is null) return;

        // take our current gag slot, and turn it into its IPadlockable form, so we can pass it by ref without affecting original data.
        var slotToUpdate = _gagData.AppearanceData.GagSlots[GagSlotLayer].DeepCloneData();

        // see if things are valid through the lock helper extensions.
        PadlockReturnCode validationResult = LockHelperExtensions.VerifyLock(ref slotToUpdate, _selectedLock, _password, _timer, MainHub.UID);

        // if the validation result is anything but successful, log it and return.
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to lock padlock: " + _selectedLock.ToName() + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
            ResetInputs();
            return;
        }

        // it was successful (we reached this point), send off new data to the server.
        var newData = _gagData.CompileAppearanceToAPI();
        var previousLock = newData.GagSlots[GagSlotLayer].Padlock.ToPadlock();
        // update the current data's slot with the new updated information.
        newData.GagSlots[GagSlotLayer] = slotToUpdate;
        // publish off the change in appearance for the online player manager to handle.
        _logger.LogTrace("Sending off Lock Applied Event to server!", LoggerType.PadlockHandling);
        _mediator.Publish(new PlayerCharAppearanceChanged(newData, (GagLayer)GagSlotLayer, GagUpdateType.GagLocked, previousLock));
        ResetSelection();
    }

    protected override void OnUnlockButtonPress()
    {
        // do not do anything if appearance data is null.
        if (_gagData.AppearanceData is null) return;

        // take our current gag slot, and turn it into its IPadlockable form, so we can pass it by ref without affecting original data.
        var slotToUpdate = _gagData.AppearanceData.GagSlots[GagSlotLayer].DeepCloneData();

        // see if things are valid through the lock helper extensions.
        PadlockReturnCode validationResult = LockHelperExtensions.VerifyUnlock(ref slotToUpdate, MainHub.PlayerUserData, _password, MainHub.UID);

        // if the validation result is anything but successful, log it and return.
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to unlock padlock: " + slotToUpdate.Padlock + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
            ResetInputs();
            return;
        }

        // it was successful (we reached this point), send off new data to the server.
        var newData = _gagData.CompileAppearanceToAPI();
        var previousLock = newData.GagSlots[GagSlotLayer].Padlock.ToPadlock();
        // update the current data's slot with the new updated information.
        newData.GagSlots[GagSlotLayer] = slotToUpdate;
        // publish off the change in appearnace for the online player manager to handle.
        _logger.LogTrace("Sending off Unlock Applied Event to server!", LoggerType.PadlockHandling);
        _mediator.Publish(new PlayerCharAppearanceChanged(newData, (GagLayer)GagSlotLayer, GagUpdateType.GagUnlocked, previousLock));
        ResetSelection();
    }
}
