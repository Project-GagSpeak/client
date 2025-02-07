using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;

namespace GagSpeak.UI.Components.Combos;
public class PadlockGagsPair : PadlockBase<GagSlot>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PadlockGagsPair(Pair pairData, MainHub mainHub, ILogger log, UiSharedService uiShared, string label)
        : base(log, uiShared, label)
    {
        _mainHub = mainHub;
        _pairRef = pairData;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks() => GsPadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override Padlocks GetLatestPadlock() => _pairRef.LastAppearanceData?.GagSlots[PairCombos.GagLayer].Padlock.ToPadlock() ?? Padlocks.None;
    protected override GagSlot GetLatestActiveItem() => _pairRef.LastAppearanceData?.GagSlots[PairCombos.GagLayer] ?? new GagSlot();
    protected override string ToActiveItemString(GagSlot item) => item.GagType;
    protected override bool DisableCondition() 
        => _pairRef.LastAppearanceData is null || _pairRef.LastAppearanceData.GagSlots[PairCombos.GagLayer].GagType.ToGagType() is GagType.None;

    protected override void OnLockButtonPress()
    {
        if (_pairRef.LastAppearanceData is null) return;

        // create a deep clone our of appearance data to analyze and modify.
        var newAppearanceData = _pairRef.LastAppearanceData.DeepCloneData();

        _logger.LogDebug("Verifying lock for padlock: " + SelectedLock.ToName(), LoggerType.PadlockHandling);
        var slotToUpdate = newAppearanceData.GagSlots[PairCombos.GagLayer];

        // see if things are valid through the lock helper extensions.
        PadlockReturnCode validationResult = GsPadlockEx.ValidateLockUpdate(slotToUpdate, SelectedLock, _password, _timer, MainHub.UID, _pairRef.PairPerms);
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to lock padlock: " + SelectedLock.ToName() + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
            ResetInputs();
            return;
        }
        // update the padlock.
        GsPadlockEx.PerformLockUpdate(ref slotToUpdate, SelectedLock, _password, _timer, MainHub.UID);

        // update the appearance data with the new slot.
        newAppearanceData.GagSlots[PairCombos.GagLayer] = slotToUpdate;
        _ = _mainHub.UserPushPairDataAppearanceUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newAppearanceData, (GagLayer)PairCombos.GagLayer, GagUpdateType.GagLocked, Padlocks.None, UpdateDir.Other));
        _logger.LogDebug("Locking Gag with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
        PairCombos.Opened = InteractionType.None;
        ResetSelection();
    }

    protected override void OnUnlockButtonPress()
    {
        if (_pairRef.LastAppearanceData is null) return;

        // create a deep clone our of appearance data to analyze and modify.
        var appearanceData = _pairRef.LastAppearanceData.DeepCloneData();
        if (appearanceData is null) return;

        var slotToUpdate = appearanceData.GagSlots[PairCombos.GagLayer];
        _logger.LogDebug("Verifying unlock for padlock: " + slotToUpdate.Padlock, LoggerType.PadlockHandling);

        // safely store the preivous password in the case of success.
        var prevLock = slotToUpdate.Padlock.ToPadlock();

        // verify if we can unlock.
        PadlockReturnCode validationResult = GsPadlockEx.ValidateUnlockUpdate(slotToUpdate, _pairRef.UserData, _password, MainHub.UID, _pairRef.PairPerms);
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to unlock padlock: " + slotToUpdate.Padlock + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
            return;
        }
        
        // update the Padlock.
        GsPadlockEx.PerformUnlockUpdate(ref slotToUpdate);
        // publish.
        _ = _mainHub.UserPushPairDataAppearanceUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, appearanceData, (GagLayer)PairCombos.GagLayer, GagUpdateType.GagUnlocked, prevLock, UpdateDir.Other));
        _logger.LogDebug("Unlocking Gag with GagPadlock " + slotToUpdate.Padlock + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
        PairCombos.Opened = InteractionType.None;
        ResetSelection();
    }
}
