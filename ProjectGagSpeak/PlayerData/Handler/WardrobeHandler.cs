using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagspeakAPI.Extensions;
using System.Diagnostics.CodeAnalysis;

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
    private readonly ClientData _playerManager;
    private readonly PairManager _pairManager;

    public WardrobeHandler(ILogger<WardrobeHandler> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfiguration, ClientData playerManager,
        AppearanceManager appearanceHandler, PairManager pairManager) : base(logger, mediator)
    {
        _clientConfigs = clientConfiguration;
        _appearanceHandler = appearanceHandler;
        _playerManager = playerManager;
        _pairManager = pairManager;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckLockedSet());
    }

    public RestraintSet? ClonedSetForEdit { get; private set; } = null;
    public bool WardrobeEnabled => !_playerManager.CoreDataNull && _playerManager.GlobalPerms!.WardrobeEnabled;
    public bool RestraintSetsEnabled => !_playerManager.CoreDataNull && _playerManager.GlobalPerms!.RestraintSetAutoEquip;
    public int RestraintSetCount => _clientConfigs.WardrobeConfig.WardrobeStorage.RestraintSets.Count;

    public bool TryGetActiveSet([MaybeNullWhen(false)] out RestraintSet activeSet)
    {
        _clientConfigs.TryGetActiveSet(out activeSet);
        return activeSet != null;
    }

    public RestraintSet? GetActiveSet() => _clientConfigs.GetActiveSet();
    public void StartEditingSet(RestraintSet set)
    {
        ClonedSetForEdit = set.DeepCloneSet();
        Guid originalID = set.RestraintId; // Prevent storing the set ID by reference.
        ClonedSetForEdit.RestraintId = originalID; // Ensure the ID remains the same here.
    }

    public void CancelEditingSet() => ClonedSetForEdit = null;

    public void SaveEditedSet()
    {
        if (ClonedSetForEdit is null)
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

    public EquipDrawData GetBlindfoldDrawData() => _clientConfigs.GetBlindfoldItem();
    public void SetBlindfoldDrawData(EquipDrawData drawData) => _clientConfigs.SetBlindfoldItem(drawData);

    private void CheckLockedSet()
    {
        if (_clientConfigs.TryGetActiveSet(out var activeSet))
        {
            // if the set is locked return.
            if (!activeSet.IsLocked())
                return;

            // check if the lock is expired and should be removed, if so, remove it.
            if (activeSet.Padlock.ToPadlock().IsTimerLock() && activeSet.Timer - DateTimeOffset.UtcNow <= TimeSpan.Zero)
            {
                Logger.LogInformation("Active Set [" + activeSet.Name + "] has expired its lock, unlocking and removing restraint set.", LoggerType.Restraints);
                if (activeSet.Padlock.ToPadlock() is Padlocks.TimerPadlock)
                    _appearanceHandler.UnlockRestraintSet(activeSet.RestraintId, activeSet.Password, "Client", true, false);
                else
                    _appearanceHandler.UnlockRestraintSet(activeSet.RestraintId, activeSet.Password, activeSet.Assigner, true, false);
            }
        }
    }
}
