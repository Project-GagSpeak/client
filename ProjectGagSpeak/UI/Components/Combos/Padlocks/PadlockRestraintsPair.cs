using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;

namespace GagSpeak.UI.Components.Combos;
public class PadlockRestraintsPair : PadlockBase<CharaWardrobeData>
{
    private readonly MainHub _mainHub;
    private Pair _pairRef;
    public PadlockRestraintsPair(Pair pairRef, MainHub mainHub, ILogger log, UiSharedService uiShared,
        string label) : base(log, uiShared, label)
    {
        _mainHub = mainHub;
        _pairRef = pairRef;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks() => GsPadlockEx.GetLocksForPair(_pairRef.PairPerms);
    protected override Padlocks GetLatestPadlock() => GetLatestActiveItem().Padlock.ToPadlock();
    protected override CharaWardrobeData GetLatestActiveItem() => _pairRef.LastWardrobeData ?? new CharaWardrobeData() { ActiveSetId = Guid.Empty };
    protected override bool DisableCondition() => GetLatestActiveItem().ActiveSetId == Guid.Empty;
    protected override string ToActiveItemString(CharaWardrobeData item)
    {
        return _pairRef.LastLightStorage?.Restraints
            .FirstOrDefault(x => x.Identifier == item.ActiveSetId)?.Name
            ?? item.ActiveSetId.ToString();
    }

    protected override void OnLockButtonPress()
    {
        if (_pairRef.LastWardrobeData is null) return;

        // create a deep clone our of wardrobe data to analyze and modify.
        var newWardrobeData = _pairRef.LastWardrobeData.DeepCloneData();

        // see if things are valid through the lock helper extensions.
        _logger.LogDebug("Verifying lock for padlock: " + SelectedLock.ToName(), LoggerType.PadlockHandling);
        PadlockReturnCode validationResult = GsPadlockEx.ValidateLockUpdate(newWardrobeData, SelectedLock, _password, _timer, MainHub.UID, _pairRef.PairPerms);
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to lock padlock: " + SelectedLock.ToName() + " due to: " + validationResult.ToFlagString());
            ResetInputs();
            return;
        }
        // update the padlock.
        GsPadlockEx.PerformLockUpdate(ref newWardrobeData, SelectedLock, _password, _timer, MainHub.UID);

        // update the wardrobe data with the new slot.
        _ = _mainHub.UserPushPairDataWardrobeUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newWardrobeData, WardrobeUpdateType.RestraintLocked, string.Empty, UpdateDir.Other));
        _logger.LogDebug("Locking Restraint Set with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid());
        PairCombos.Opened = InteractionType.None;
        ResetSelection();
    }

    protected override void OnUnlockButtonPress()
    {
        if (_pairRef.LastWardrobeData is null) return;

        // create a deep clone our of wardrobe data to analyze and modify.
        var newWardrobeData = _pairRef.LastWardrobeData.DeepCloneData();
        // get the previous lock before we update it.
        var prevLock = newWardrobeData.Padlock.ToPadlock();

        _logger.LogDebug("Verifying unlock for padlock: " + SelectedLock.ToName(), LoggerType.PadlockHandling);
        PadlockReturnCode validationResult = GsPadlockEx.ValidateUnlockUpdate(newWardrobeData, _pairRef.UserData, _password, MainHub.UID, _pairRef.PairPerms);
        if (validationResult is not PadlockReturnCode.Success)
        {
            _logger.LogError("Failed to unlock padlock: " + SelectedLock.ToName() + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
            ResetInputs();
            return;
        }

        // update the padlock.
        GsPadlockEx.PerformUnlockUpdate(ref newWardrobeData);
        // update the wardrobe data on the server with the new information.
        _ = _mainHub.UserPushPairDataWardrobeUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, newWardrobeData, WardrobeUpdateType.RestraintUnlocked, prevLock.ToName(), UpdateDir.Other));
        _logger.LogDebug("Unlocking Restraint Set with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
        PairCombos.Opened = InteractionType.None;
        ResetSelection();
    }
}
