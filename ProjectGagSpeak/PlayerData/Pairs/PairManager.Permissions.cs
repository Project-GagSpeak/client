using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Extensions;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentNumericInput.Delegates;

namespace GagSpeak.PlayerData.Pairs;

/// <summary>
/// General note to self, pairs used to have "own permissions" and "other permissions" but they were removed.
/// <para> If down the line something like this is an answer to a problem of mine, then find a way to utilize it.</para>
/// </summary>
public sealed partial class PairManager : DisposableMediatorSubscriberBase
{
    /// <summary>
    /// Updates all permissions of a client pair user.
    /// Edit access is checked server-side to prevent abuse, so these should be all accurate
    /// </summary>
    public void UpdateOtherPairAllPermissions(BulkUpdatePermsAllDto dto)
    {
        var MoodlesChanged = false;
        if (!_allClientPairs.TryGetValue(dto.User, out var pair))
        {
            throw new InvalidOperationException("No such pair for " + dto);
        }

        if (pair.UserPair == null) throw new InvalidOperationException("No direct pair for " + dto);

        // check to see if the user just paused themselves.
        if (pair.UserPair.OtherPairPerms.IsPaused != dto.PairPermissions.IsPaused)
            Mediator.Publish(new ClearProfileDataMessage(dto.User));

        MoodlesChanged = (dto.PairPermissions.MoodlePerms != pair.PairPerms.MoodlePerms)
            || (dto.PairPermissions.MaxMoodleTime != pair.PairPerms.MaxMoodleTime);

        // set the permissions.
        pair.UserPair.OtherGlobalPerms = dto.GlobalPermissions;
        pair.UserPair.OtherPairPerms = dto.PairPermissions;
        pair.UserPair.OtherEditAccessPerms = dto.EditAccessPermissions;

        Logger.LogTrace("Fresh update >> Paused: "+ pair.UserPair.OtherPairPerms.IsPaused, LoggerType.PairDataTransfer);

        RecreateLazy(true);

        // push notify after recreating lazy.
        if (MoodlesChanged && GetOnlineUserDatas().Contains(pair.UserData))
            Mediator.Publish(new MoodlesPermissionsUpdated(pair.PlayerNameWithWorld));
    }

    public void UpdatePairUpdateOwnAllUniquePermissions(BulkUpdatePermsUniqueDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        // Find new permissions enabled that were not enabled before
        var prevPerms = pair.OwnPerms.PuppetPerms;
        var newlyEnabledPermissions = (dto.UniquePerms.PuppetPerms & ~prevPerms);

        // update the permissions.
        pair.UserPair.OwnPairPerms = dto.UniquePerms;
        pair.UserPair.OwnEditAccessPerms = dto.UniqueAccessPerms;

        if (newlyEnabledPermissions is not PuppetPerms.None)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, newlyEnabledPermissions);

        Logger.LogDebug($"Updated own unique permissions for '{pair.GetNickname() ?? pair.UserData.AliasOrUID}'", LoggerType.PairDataTransfer);
    }


    public void UpdatePairUpdateOtherAllGlobalPermissions(BulkUpdatePermsGlobalDto dto)
    {
        // update the pairs permissions.
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }
        pair.UserPair.OtherGlobalPerms = dto.GlobalPermissions;
        Logger.LogDebug($"Updated global permissions for '{pair.GetNickname() ?? pair.UserData.AliasOrUID}'", LoggerType.PairDataTransfer);
    }

    public void UpdatePairUpdateOtherAllUniquePermissions(BulkUpdatePermsUniqueDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }
        pair.UserPair.OtherPairPerms = dto.UniquePerms;
        pair.UserPair.OtherEditAccessPerms = dto.UniqueAccessPerms;
        Logger.LogDebug($"Updated pairs unique permissions for '{pair.GetNickname() ?? pair.UserData.AliasOrUID}'", LoggerType.PairDataTransfer);
    }

    /// <summary>
    /// Updates a global permission of a client pair user.
    /// Edit access is checked server-side to prevent abuse, so these should be all accurate.
    /// </summary>>
    public void UpdateOtherPairGlobalPermission(UserGlobalPermChangeDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var ChangedPermission = dto.ChangedPermission.Key;
        var ChangedValue = dto.ChangedPermission.Value;
        var propertyInfo = typeof(UserGlobalPermissions).GetProperty(ChangedPermission);
        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        // Get the Hardcore Change Type before updating the property (if it is not valid it wont return anything but none anyways)
        var changeType = pair.PairGlobals.PermChangeType(ChangedPermission, ChangedValue?.ToString() ?? string.Empty);

        // Special conversions
        object? convertedValue = propertyInfo.PropertyType switch
        {
            Type t when t.IsEnum =>
                ChangedValue?.GetType() == Enum.GetUnderlyingType(t)
                    ? Enum.ToObject(t, ChangedValue)
                    : Convert.ChangeType(ChangedValue, t), // If newValue type matches enum underlying type, convert it directly.
            Type t when t == typeof(TimeSpan) && ChangedValue is ulong u => TimeSpan.FromTicks((long)u),
            Type t when t == typeof(char) && ChangedValue is byte b => Convert.ToChar(b),
            _ => Convert.ChangeType(ChangedValue, propertyInfo.PropertyType)
        };

        if (convertedValue is null)
            return;

        propertyInfo.SetValue(pair.UserPair.OtherGlobalPerms, convertedValue);
        Logger.LogDebug($"Updated other pair global permission '{ChangedPermission}' to '{ChangedValue}'", LoggerType.PairDataTransfer);

        RecreateLazy(false);

        // Handle hardcore changes here.
        if (changeType is InteractionType.None || changeType is InteractionType.ForcedPermChange)
            return;

        // log the new state, the hardcore change, and the new value.
        var newState = string.IsNullOrEmpty(ChangedValue?.ToString()) ? NewState.Disabled : NewState.Enabled;
        Logger.LogDebug(changeType.ToString() + " has changed, and is now " + ChangedValue, LoggerType.PairDataTransfer);
        UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, changeType, newState, dto.Enactor.UID, pair.UserData.UID);
    }

    /// <summary>
    /// Updates one of the paired users pair permissions they have set for you. (their permission for you)
    /// </summary>>
    public void UpdateOtherPairPermission(UserPairPermChangeDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var ChangedPermission = dto.ChangedPermission.Key;
        var ChangedValue = dto.ChangedPermission.Value;
        var propertyInfo = typeof(UserPairPermissions).GetProperty(ChangedPermission);

        // has the person just paused us.
        if (ChangedPermission == "IsPaused")
            if (pair.UserPair.OtherPairPerms.IsPaused != (bool)ChangedValue)
                Mediator.Publish(new ClearProfileDataMessage(dto.User));

        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        // conversions
        object? convertedValue = propertyInfo.PropertyType switch
        {
            Type t when t.IsEnum =>
                ChangedValue?.GetType() == Enum.GetUnderlyingType(t)
                    ? Enum.ToObject(t, ChangedValue)
                    : Convert.ChangeType(ChangedValue, t), // If newValue type matches enum underlying type, convert it directly.
            Type t when t == typeof(TimeSpan) && ChangedValue is ulong u => TimeSpan.FromTicks((long)u),
            Type t when t == typeof(char) && ChangedValue is byte b => Convert.ToChar(b),
            _ => Convert.ChangeType(ChangedValue, propertyInfo.PropertyType)
        };

        if (convertedValue is null)
            return;
        
        propertyInfo.SetValue(pair.UserPair.OtherPairPerms, convertedValue);
        Logger.LogDebug($"Updated other pair permission '{ChangedPermission}' to '{ChangedValue}'", LoggerType.PairDataTransfer);
        
        RecreateLazy(false);

        // push notify after recreating lazy.
        if (ChangedPermission is nameof(UserPairPermissions.MoodlePerms) || ChangedPermission is nameof(UserPairPermissions.MaxMoodleTime))
            if (GetOnlineUserDatas().Contains(pair.UserData))
                Mediator.Publish(new MoodlesPermissionsUpdated(pair.PlayerNameWithWorld));
    }

    /// <summary>
    /// Updates an edit access permission for the paired user, reflecting what they are giving you access to.
    /// </summary>>
    public void UpdateOtherPairAccessPermission(UserPairAccessChangeDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var ChangedPermission = dto.ChangedAccessPermission.Key;
        var ChangedValue = dto.ChangedAccessPermission.Value;
        var propertyInfo = typeof(UserEditAccessPermissions).GetProperty(ChangedPermission);

        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        object? convertedValue = propertyInfo.PropertyType switch
        {
            Type t when t.IsEnum =>
                ChangedValue?.GetType() == Enum.GetUnderlyingType(t)
                    ? Enum.ToObject(t, ChangedValue)
                    : Convert.ChangeType(ChangedValue, t), // If newValue type matches enum underlying type, convert it directly.
            Type t when t == typeof(TimeSpan) && ChangedValue is ulong u => TimeSpan.FromTicks((long)u),
            Type t when t == typeof(char) && ChangedValue is byte b => Convert.ToChar(b),
            _ => Convert.ChangeType(ChangedValue, propertyInfo.PropertyType)
        };

        if (convertedValue is null)
            return;

        propertyInfo.SetValue(pair.UserPair.OtherEditAccessPerms, convertedValue);
        Logger.LogDebug($"Updated other pair access permission '{ChangedPermission}' to '{ChangedValue}'", LoggerType.PairDataTransfer);

        RecreateLazy(false);
    }


    /// <summary>
    /// Updates one of your unique pair permissions you have set with the paired user.
    /// </summary>>
    public void UpdateSelfPairPermission(UserPairPermChangeDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var ChangedPermission = dto.ChangedPermission.Key;
        var ChangedValue = dto.ChangedPermission.Value;
        var propertyInfo = typeof(UserPairPermissions).GetProperty(ChangedPermission);

        if (ChangedPermission is "IsPaused" && (pair.UserPair.OwnPairPerms.IsPaused != (bool)ChangedValue))
            Mediator.Publish(new ClearProfileDataMessage(dto.User));
        
        var prevPerms = pair.OwnPerms.PuppetPerms;
        var puppetChanged = ChangedPermission == nameof(UserPairPermissions.PuppetPerms);

        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        object? convertedValue = propertyInfo.PropertyType switch
        {
            Type t when t.IsEnum =>
                ChangedValue?.GetType() == Enum.GetUnderlyingType(t)
                    ? Enum.ToObject(t, ChangedValue)
                    : Convert.ChangeType(ChangedValue, t), // If newValue type matches enum underlying type, convert it directly.
            Type t when t == typeof(TimeSpan) && ChangedValue is ulong u => TimeSpan.FromTicks((long)u),
            Type t when t == typeof(char) && ChangedValue is byte b => Convert.ToChar(b),
            _ => Convert.ChangeType(ChangedValue, propertyInfo.PropertyType)
        };

        if (convertedValue is null)
            return;

        propertyInfo.SetValue(pair.UserPair.OwnPairPerms, convertedValue);
        Logger.LogDebug($"Updated self pair permission '{ChangedPermission}' to '{ChangedValue}'", LoggerType.PairDataTransfer);

        var newEnabledPuppetPerms = (pair.OwnPerms.PuppetPerms & ~prevPerms);
        if (newEnabledPuppetPerms is not PuppetPerms.None)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, newEnabledPuppetPerms);

        RecreateLazy(false);

        // push notify after recreating lazy.
        if (ChangedPermission is nameof(UserPairPermissions.MoodlePerms) || ChangedPermission is nameof(UserPairPermissions.MaxMoodleTime))
            Mediator.Publish(new MoodlesPermissionsUpdated(pair.PlayerNameWithWorld));
    }

    /// <summary>
    /// Updates an edit access permission that you've set for the paired user.
    /// </summary>>
    public void UpdateSelfPairAccessPermission(UserPairAccessChangeDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var ChangedPermission = dto.ChangedAccessPermission.Key;
        var ChangedValue = dto.ChangedAccessPermission.Value;

        var propertyInfo = typeof(UserEditAccessPermissions).GetProperty(ChangedPermission);
        if (propertyInfo != null)
        {
            if (propertyInfo.CanWrite)
            {
                // convert the value to the appropriate type before setting.
                var value = Convert.ChangeType(ChangedValue, propertyInfo.PropertyType);
                propertyInfo.SetValue(pair.UserPair.OwnEditAccessPerms, value);
                Logger.LogDebug($"Updated self pair access permission '{ChangedPermission}' to '{ChangedValue}'", LoggerType.PairDataTransfer);
            }
            else
            {
                Logger.LogError($"Property '{ChangedPermission}' not found or cannot be updated.");
            }
        }

        RecreateLazy(false);
    }
}
