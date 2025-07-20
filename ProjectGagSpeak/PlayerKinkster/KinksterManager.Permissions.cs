using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;

namespace GagSpeak.Kinksters;

/// <summary>
/// General note to self, pairs used to have "own permissions" and "other permissions" but they were removed.
/// <para> If down the line something like this is an answer to a problem of mine, then find a way to utilize it.</para>
/// </summary>
public sealed partial class KinksterManager : DisposableMediatorSubscriberBase
{
    /// <summary>
    /// Updates all permissions of a client pair user.
    /// Edit access is checked server-side to prevent abuse, so these should be all accurate
    /// </summary>
    public void UpdateOtherPairAllPermissions(BulkChangeAll dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair))
            throw new InvalidOperationException("No such pair for " + dto);

        if (pair.UserPair is null)
            throw new InvalidOperationException("No direct pair for " + dto);

        // check to see if the user just paused themselves.
        if (pair.PairPerms.IsPaused != dto.Unique.IsPaused)
            Mediator.Publish(new ClearProfileDataMessage(dto.User));

        var MoodlesChanged = (dto.Unique.MoodlePerms != pair.PairPerms.MoodlePerms) || (dto.Unique.MaxMoodleTime != pair.PairPerms.MaxMoodleTime);

        // set the permissions.
        pair.UserPair.Globals = dto.Globals;
        pair.UserPair.Perms = dto.Unique;
        pair.UserPair.Access = dto.Access;

        Logger.LogTrace("Fresh update >> Paused: "+ pair.PairPerms.IsPaused, LoggerType.PairDataTransfer);

        RecreateLazy(true);

        // push notify after recreating lazy.
        if (MoodlesChanged && GetOnlineUserDatas().Contains(pair.UserData))
            Mediator.Publish(new MoodlesPermissionsUpdated(pair.PlayerNameWithWorld));
    }

    public void UpdatePairUpdateOwnAllUniquePermissions(BulkChangeUnique dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair))
            throw new InvalidOperationException("No such pair for " + dto);

        // Find new permissions enabled that were not enabled before
        var prevPerms = pair.OwnPerms.PuppetPerms;
        var newlyEnabledPermissions = (dto.NewPerms.PuppetPerms & ~prevPerms);

        // update the permissions.
        pair.UserPair.OwnPerms = dto.NewPerms;
        pair.UserPair.OwnAccess = dto.NewAccess;

        if (newlyEnabledPermissions is not PuppetPerms.None)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, newlyEnabledPermissions);

        Logger.LogDebug($"Updated own unique permissions for '{pair.GetNickname() ?? pair.UserData.AliasOrUID}'", LoggerType.PairDataTransfer);
    }


    public void UpdatePairUpdateOtherAllGlobalPermissions(BulkChangeGlobal dto)
    {
        // update the pairs permissions.
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }
        pair.UserPair.Globals = dto.NewPerms;
        Logger.LogDebug($"Updated global permissions for '{pair.GetNickname() ?? pair.UserData.AliasOrUID}'", LoggerType.PairDataTransfer);
    }

    public void UpdatePairUpdateOtherAllUniquePermissions(BulkChangeUnique dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair))
            throw new InvalidOperationException("No such pair for " + dto);

        pair.UserPair.Perms = dto.NewPerms;
        pair.UserPair.Access = dto.NewAccess;
        Logger.LogDebug($"Updated pairs unique permissions for '{pair.GetNickname() ?? pair.UserData.AliasOrUID}'", LoggerType.PairDataTransfer);
    }

    /// <summary>
    /// Updates a global permission of a client pair user.
    /// Edit access is checked server-side to prevent abuse, so these should be all accurate.
    /// </summary>>
    public void UpdateOtherPairGlobalPermission(SingleChangeGlobal dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var NewPerm = dto.NewPerm.Key;
        var ChangedValue = dto.NewPerm.Value;
        var propertyInfo = typeof(GlobalPerms).GetProperty(NewPerm);
        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        // Special conversions
        var convertedValue = propertyInfo.PropertyType switch
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

        propertyInfo.SetValue(pair.PairGlobals, convertedValue);
        Logger.LogDebug($"Updated other pair global permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);

        RecreateLazy(false);

        // Handle hardcore changes here. (DO THIS LATER)
        //if (changeType is InteractionType.None || changeType is InteractionType.ForcedPermChange)
        //    return;

        //// log the new state, the hardcore change, and the new value.
        //var newState = string.IsNullOrEmpty(ChangedValue?.ToString()) ? NewState.Disabled : NewState.Enabled;
        //Logger.LogDebug(changeType.ToString() + " has changed, and is now " + ChangedValue, LoggerType.PairDataTransfer);
        //GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, changeType, newState, dto.Enactor.UID, pair.UserData.UID);
    }

    /// <summary>
    /// Updates one of the paired users pair permissions they have set for you. (their permission for you)
    /// </summary>>
    public void UpdateOtherPairPermission(SingleChangeUnique dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var NewPerm = dto.NewPerm.Key;
        var ChangedValue = dto.NewPerm.Value;
        var propertyInfo = typeof(PairPerms).GetProperty(NewPerm);

        // has the person just paused us.
        if (NewPerm == nameof(PairPerms.IsPaused))
            if (pair.PairPerms.IsPaused != (bool)ChangedValue)
                Mediator.Publish(new ClearProfileDataMessage(dto.User));

        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        // conversions
        var convertedValue = propertyInfo.PropertyType switch
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
        
        propertyInfo.SetValue(pair.PairPerms, convertedValue);
        Logger.LogDebug($"Updated other pair permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);
        
        RecreateLazy(false);

        // push notify after recreating lazy.
        if (NewPerm is nameof(PairPerms.MoodlePerms) || NewPerm is nameof(PairPerms.MaxMoodleTime))
            if (GetOnlineUserDatas().Contains(pair.UserData))
                Mediator.Publish(new MoodlesPermissionsUpdated(pair.PlayerNameWithWorld));
    }

    /// <summary>
    /// Updates an edit access permission for the paired user, reflecting what they are giving you access to.
    /// </summary>>
    public void UpdateOtherPairAccessPermission(SingleChangeAccess dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No such pair for " + dto);

        var NewPerm = dto.NewPerm.Key;
        var ChangedValue = dto.NewPerm.Value;
        var propertyInfo = typeof(PairPermAccess).GetProperty(NewPerm);

        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        var convertedValue = propertyInfo.PropertyType switch
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

        propertyInfo.SetValue(pair.PairPermAccess, convertedValue);
        Logger.LogDebug($"Updated other pair access permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);

        RecreateLazy(false);
    }


    /// <summary>
    /// Updates one of your unique pair permissions you have set with the paired user.
    /// </summary>>
    public void UpdateSelfPairPermission(SingleChangeUnique dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var NewPerm = dto.NewPerm.Key;
        var ChangedValue = dto.NewPerm.Value;
        var propertyInfo = typeof(PairPerms).GetProperty(NewPerm);

        if (NewPerm is "IsPaused" && (pair.UserPair.OwnPerms.IsPaused != (bool)ChangedValue))
            Mediator.Publish(new ClearProfileDataMessage(dto.User));
        
        var prevPerms = pair.OwnPerms.PuppetPerms;
        var puppetChanged = NewPerm == nameof(PairPerms.PuppetPerms);

        if (propertyInfo is null || !propertyInfo.CanWrite)
            return;

        var convertedValue = propertyInfo.PropertyType switch
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

        propertyInfo.SetValue(pair.UserPair.OwnPerms, convertedValue);
        Logger.LogDebug($"Updated self pair permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);

        var newEnabledPuppetPerms = (pair.OwnPerms.PuppetPerms & ~prevPerms);
        if (newEnabledPuppetPerms is not PuppetPerms.None)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, newEnabledPuppetPerms);

        RecreateLazy(false);

        // push notify after recreating lazy.
        if (NewPerm is nameof(PairPerms.MoodlePerms) || NewPerm is nameof(PairPerms.MaxMoodleTime))
            Mediator.Publish(new MoodlesPermissionsUpdated(pair.PlayerNameWithWorld));
    }

    /// <summary>
    /// Updates an edit access permission that you've set for the paired user.
    /// </summary>>
    public void UpdateSelfPairAccessPermission(SingleChangeAccess dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) { throw new InvalidOperationException("No such pair for " + dto); }

        var NewPerm = dto.NewPerm.Key;
        var ChangedValue = dto.NewPerm.Value;

        var propertyInfo = typeof(PairPermAccess).GetProperty(NewPerm);
        if (propertyInfo != null)
        {
            if (propertyInfo.CanWrite)
            {
                // convert the value to the appropriate type before setting.
                var value = propertyInfo.PropertyType switch
                {
                    Type t when t.IsEnum =>
                        ChangedValue?.GetType() == Enum.GetUnderlyingType(t)
                            ? Enum.ToObject(t, ChangedValue)
                            : Convert.ChangeType(ChangedValue, t), // If newValue type matches enum underlying type, convert it directly.
                    _ => Convert.ChangeType(ChangedValue, propertyInfo.PropertyType)
                };
                propertyInfo.SetValue(pair.OwnPermAccess, value);
                Logger.LogDebug($"Updated self pair access permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);
            }
            else
            {
                Logger.LogError($"Property '{NewPerm}' not found or cannot be updated.");
            }
        }

        RecreateLazy(false);
    }
}
