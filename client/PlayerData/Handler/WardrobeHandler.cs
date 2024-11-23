using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;

namespace GagSpeak.PlayerData.Handlers;
/// <summary>
/// The wardrobe Handler is designed to store a variety of public reference variables for other classes to use.
/// Primarily, they will store values that would typically be required to iterate over a heavy list like all client pairs
/// to find, and only update them when necessary.
/// </summary>
public class WardrobeHandler : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly AppearanceManager _appearanceHandler;
    private readonly PlayerCharacterData _playerManager;
    private readonly PairManager _pairManager;

    public WardrobeHandler(ILogger<WardrobeHandler> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfiguration, PlayerCharacterData playerManager,
        AppearanceManager appearanceHandler, PairManager pairManager) : base(logger, mediator)
    {
        _clientConfigs = clientConfiguration;
        _appearanceHandler = appearanceHandler;
        _playerManager = playerManager;
        _pairManager = pairManager;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckLockedSet());
    }

    public RestraintSet? ActiveSet => _clientConfigs.GetActiveSet();
    public RestraintSet? ClonedSetForEdit { get; private set; } = null;
    public bool WardrobeEnabled => !_playerManager.CoreDataNull && _playerManager.GlobalPerms!.WardrobeEnabled;
    public bool RestraintSetsEnabled => !_playerManager.CoreDataNull && _playerManager.GlobalPerms!.RestraintSetAutoEquip;
    public int RestraintSetCount => _clientConfigs.WardrobeConfig.WardrobeStorage.RestraintSets.Count;

    public void StartEditingSet(RestraintSet set)
    {
        ClonedSetForEdit = set.DeepCloneSet();
        Guid originalID = set.RestraintId; // Prevent storing the set ID by reference.
        ClonedSetForEdit.RestraintId = originalID; // Ensure the ID remains the same here.
    }

    public void CancelEditingSet() => ClonedSetForEdit = null;
    
    public void SaveEditedSet()
    {
        if(ClonedSetForEdit is null) 
            return;
        // locate the restraint set that contains the matching guid.
        var setIdx = _clientConfigs.GetSetIdxByGuid(ClonedSetForEdit.RestraintId);
        // update that set with the new cloned set.
        _clientConfigs.UpdateRestraintSet(ClonedSetForEdit, setIdx);
        // make the cloned set null again.
        ClonedSetForEdit = null;
    }

    // For copying and pasting parts of the restraint set.
    public void CloneRestraintSet(RestraintSet setToClone) => _clientConfigs.CloneRestraintSet(setToClone);
    public void AddNewRestraintSet(RestraintSet newSet)
    {
        _clientConfigs.AddNewRestraintSet(newSet);
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintUpdated, newSet);
    }

    public void RemoveRestraintSet(Guid idToRemove)
    {
        var idxToRemove = _clientConfigs.GetSetIdxByGuid(idToRemove);
        _clientConfigs.RemoveRestraintSet(idxToRemove);
        CancelEditingSet();
    }

    public List<RestraintSet> GetAllSetsForSearch() => _clientConfigs.StoredRestraintSets;
    public RestraintSet GetRestraintSet(int idx) => _clientConfigs.GetRestraintSet(idx);

    public async Task EnableRestraintSet(Guid id, string assignerUID, bool pushToServer = true)
    {
        if (!WardrobeEnabled || !RestraintSetsEnabled) {
            Logger.LogInformation("Wardrobe or Restraint Sets are disabled, cannot enable restraint set.", LoggerType.Restraints);
            return;
        }

        // check to see if there is any active set currently. If there is, disable it.
        if (ActiveSet is not null)
        {
            Logger.LogInformation("Disabling Active Set ["+ActiveSet.Name+"] before enabling new set.", LoggerType.Restraints);
            await _appearanceHandler.DisableRestraintSet(ActiveSet.RestraintId, assignerUID); // maybe add push to server here to prevent double send?
        }
        // Enable the new set.
        await _appearanceHandler.EnableRestraintSet(id, assignerUID, pushToServer);
    }
    public async Task DisableRestraintSet(Guid id, string assignerUID, bool pushToServer = true)
    {
        if (!WardrobeEnabled || !RestraintSetsEnabled) 
        {
            Logger.LogInformation("Wardrobe or Restraint Sets are disabled, cannot disable restraint set.", LoggerType.Restraints);
            return;
        }
        await _appearanceHandler.DisableRestraintSet(id, assignerUID, pushToServer);
    }

    public void LockRestraintSet(Guid id, Padlocks padlock, string pwd, DateTimeOffset endLockTimeUTC, string assignerUID)
        => _appearanceHandler.LockRestraintSet(id, padlock, pwd, endLockTimeUTC, assignerUID).ConfigureAwait(false);

    public void UnlockRestraintSet(Guid id, string lockRemoverUID) 
        => _appearanceHandler.UnlockRestraintSet(id, lockRemoverUID).ConfigureAwait(false);

    public int GetActiveSetIndex() => _clientConfigs.GetActiveSetIdx();
    public int GetRestraintSetIndexByName(string setName) => _clientConfigs.GetRestraintSetIdxByName(setName);
    public List<Guid> GetAssociatedMoodles(int setIndex) => _clientConfigs.WardrobeConfig.WardrobeStorage.RestraintSets[setIndex].AssociatedMoodles;
    public EquipDrawData GetBlindfoldDrawData() => _clientConfigs.GetBlindfoldItem();
    public void SetBlindfoldDrawData(EquipDrawData drawData) => _clientConfigs.SetBlindfoldItem(drawData);

    private void CheckLockedSet()
    {
        if (ActiveSet == null) return;

        // we have an active set, but dont check if it is not locked. We should keep it on if it is simply active.
        if (!ActiveSet.Locked) return;

        // check if the locked time minus the current time in UTC is less than timespan.Zero ... if it is, we should push an unlock set update.
        if (GenericHelpers.TimerPadlocks.Contains(ActiveSet.LockType) && ActiveSet.LockedUntil - DateTimeOffset.UtcNow <= TimeSpan.Zero)
        {
            _appearanceHandler.UnlockRestraintSet(ActiveSet.RestraintId, ActiveSet.LockedBy, fromTimer: true).ConfigureAwait(false);
            Logger.LogInformation("Active Set ["+ActiveSet.Name+"] has expired its lock, unlocking and removing restraint set.", LoggerType.Restraints);
        }
    }
}
