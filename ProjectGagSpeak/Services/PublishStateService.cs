using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.ConfigurationServices;
using GagspeakAPI.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.StateManagers;

/// <summary>
/// Primarily designed to assist the Appearnace Managers initial logic checks, 
/// and also for other components to check before publishing updates.
/// </summary>
public sealed class PublishStateService
{
    private readonly ILogger<PublishStateService> _logger;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientData _playerData;

    public PublishStateService(ILogger<PublishStateService> logger, ClientConfigurationManager clientConfigs, ClientData playerData)
    {
        _logger = logger;
        _playerData = playerData;
        _clientConfigs = clientConfigs;
    }

    public bool CanApplyRestraint(Guid restraintID, [NotNullWhen(true)] out RestraintSet? setRef)
    {
        setRef = null;

        // Reject if wardrobe permissions not enabled.
        if (!_playerData.WardrobeEnabled)
        {
            _logger.LogTrace("Wardrobe disabled, cannot perform.");
            return false;
        }

        // reject if the active set is already enabled.
        if (_clientConfigs.GetActiveSet() is not null)
        {
            _logger.LogError("You must Disable the active Set before calling this!");
            return false;
        }

        // reject if the set we are trying to enable no longer exists.
        var setIdx = _clientConfigs.StoredRestraintSets.FindIndex(x => x.RestraintId == restraintID);
        if (setIdx is -1)
        {
            _logger.LogTrace("Attempted to enable a restraint set that does not exist.");
            return false;
        }

        setRef = _clientConfigs.StoredRestraintSets.First(x => x.RestraintId == restraintID);
        // success, we can enable the set.
        return true;
    }

    public bool CanLockRestraint(Guid restraintId, [NotNullWhen(true)] out RestraintSet? setRef)
    {
        setRef = null;

        // Reject if wardrobe permissions not enabled.
        if (!_playerData.WardrobeEnabled)
        {
            _logger.LogTrace("Wardrobe disabled, cannot perform.");
            return false;
        }

        // if the id is guid.empty return false.
        if (restraintId == Guid.Empty)
        {
            _logger.LogTrace("Attempted to lock a restraint with an empty guid.");
            return false;
        }

        // reject if the set we are trying to lock no longer exists.
        var setIdx = _clientConfigs.StoredRestraintSets.FindIndex(x => x.RestraintId == restraintId);
        if (setIdx == -1)
        {
            _logger.LogTrace("Set Does not Exist, Skipping.");
            return false;
        }

        // if the set is not the active set, log that this is invalid, as we should only be locking / unlocking the active set.
        if (setIdx != _clientConfigs.GetActiveSetIdx())
        {
            _logger.LogTrace("Attempted to lock a set that is not the active set. Skipping.");
            return false;
        }

        // Grab the set reference.
        setRef = _clientConfigs.StoredRestraintSets[setIdx];
        if (setRef.IsLocked())
        {
            _logger.LogDebug(setRef.Name + " is already locked. Skipping!", LoggerType.AppearanceState);
            return false;
        }

        // success, we can lock the set.
        return true;
    }

    public bool CanUnlockRestraint(Guid restraintId, [NotNullWhen(true)] out RestraintSet? setRef)
    {
        setRef = null;

        // reject if wardrobe permissions not enabled.
        if (!_playerData.WardrobeEnabled)
        {
            _logger.LogTrace("Wardrobe disabled, cannot perform.");
            return false;
        }

        // reject if the set we are trying to unlock no longer exists.
        var setIdx = _clientConfigs.StoredRestraintSets.FindIndex(x => x.RestraintId == restraintId);
        if (setIdx == -1)
        {
            _logger.LogTrace("Set Does not Exist, Skipping.");
            return false;
        }

        // if the set is not the active set, log that this is invalid, as we should only be locking / unlocking the active set.
        if (setIdx != _clientConfigs.GetActiveSetIdx())
        {
            _logger.LogTrace("Attempted to unlock a set that is not the active set. Skipping.");
            return false;
        }

        // if the set is not locked, dont allow unlocking.
        setRef = _clientConfigs.StoredRestraintSets[setIdx];
        if (!setRef.IsLocked())
        {
            _logger.LogDebug(setRef.Name + " is not even locked. Skipping!", LoggerType.AppearanceState);
            return false;
        }

        // success, we can unlock the set.
        return true;
    }

    public bool CanRemoveRestraint(Guid restraintID, [NotNullWhen(true)] out RestraintSet? setRef)
    {
        setRef = null;

        // Reject if wardrobe permissions not enabled.
        if (!_playerData.WardrobeEnabled)
        {
            _logger.LogTrace("Wardrobe disabled, cannot perform.");
            return false;
        }

        // Reject if the set we are trying to disable no longer exists.
        var setIdx = _clientConfigs.StoredRestraintSets.FindIndex(x => x.RestraintId == restraintID);
        if (setIdx is -1)
        {
            _logger.LogTrace("Set Does not Exist, Skipping.");
            return false;
        }

        // reject if the set at this index is not enabled, or is still locked.
        setRef = _clientConfigs.StoredRestraintSets[setIdx];
        if (!setRef.Enabled || setRef.IsLocked())
        {
            _logger.LogTrace(setRef.Name + " is already disabled or locked. Skipping!");
            return false;
        }

        // success, we can disable the set.
        return true;
    }

    public bool CanApplyGag(GagLayer layer)
    {
        // reject if the appearance data is null.
        if (_playerData.AppearanceData is null)
        {
            _logger.LogTrace("Appearance Data is Null, Skipping.");
            return false;
        }

        // reject if the gagslot is locked.
        if (_playerData.AppearanceData.GagSlots[(int)layer].IsLocked())
        {
            _logger.LogTrace("Gag Slot is Locked, Skipping.");
            return false;
        }

        // allow update.
        return true;
    }

    public bool CanLockGag(GagLayer layer)
    {
        // reject if the appearance data is null.
        if (_playerData.AppearanceData is null)
        {
            _logger.LogTrace("Appearance Data is Null, Skipping.");
            return false;
        }

        // reject if there is no gag on the layer.
        if (_playerData.AppearanceData.GagSlots[(int)layer].GagType.ToGagType() is GagType.None)
        {
            _logger.LogTrace("No Gag on Slot, Skipping.");
            return false;
        }

        // reject if the gag is already locked.
        if (_playerData.AppearanceData.GagSlots[(int)layer].IsLocked())
        {
            _logger.LogTrace("Gag Slot is Locked, Skipping.");
            return false;
        }

        // allow update.
        return true;
    }

    public bool CanUnlockGag(GagLayer layer)
    {
        // reject if the appearance data is null.
        if (_playerData.AppearanceData is null)
        {
            _logger.LogTrace("Appearance Data is Null, Skipping.");
            return false;
        }

        // reject if there is no gag on the layer.
        if (_playerData.AppearanceData.GagSlots[(int)layer].GagType.ToGagType() is GagType.None)
        {
            _logger.LogTrace("No Gag on Slot, Skipping.");
            return false;
        }

        // reject if the gag is not locked.
        if (!_playerData.AppearanceData.GagSlots[(int)layer].IsLocked())
        {
            _logger.LogTrace("Gag Slot is not Locked, Skipping.");
            return false;
        }

        // allow update.
        return true;
    }

    public bool CanRemoveGag(GagLayer layer)
    {
        // reject if the appearance data is null.
        if (_playerData.AppearanceData is null)
        {
            _logger.LogTrace("Appearance Data is Null, Skipping.");
            return false;
        }

        // reject if locked.
        if (_playerData.AppearanceData.GagSlots[(int)layer].IsLocked())
        {
            _logger.LogTrace("Gag Slot is Locked, Skipping.");
            return false;
        }

        // reject if the current gagtype is the same as the new gagtype.
        if (_playerData.AppearanceData.GagSlots[(int)layer].GagType.ToGagType() is GagType.None)
        {
            _logger.LogTrace("Gag is already empty, skipping removing nothing.");
            return false;
        }

        // allow update.
        return true;
    }
}
