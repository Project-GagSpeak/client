using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentNumericInput.Delegates;

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
    //public void UpdateOtherPairAllPermissions(BulkChangeAll dto)
    //{
    //    if (!_allClientPairs.TryGetValue(dto.User, out var pair))
    //        throw new InvalidOperationException("No such pair for " + dto);

    //    if (pair.UserPair is null)
    //        throw new InvalidOperationException("No direct pair for " + dto);

    //    // check to see if the user just paused themselves.
    //    if (pair.PairPerms.IsPaused != dto.Unique.IsPaused)
    //        Mediator.Publish(new ClearProfileDataMessage(dto.User));

    //    var MoodlesChanged = (dto.Unique.MoodlePerms != pair.PairPerms.MoodlePerms) || (dto.Unique.MaxMoodleTime != pair.PairPerms.MaxMoodleTime);

    //    // set the permissions.
    //    pair.UserPair.Globals = dto.Globals;
    //    pair.UserPair.Perms = dto.Unique;
    //    pair.UserPair.Access = dto.Access;

    //    Logger.LogTrace("Fresh update >> Paused: "+ pair.PairPerms.IsPaused, LoggerType.PairDataTransfer);

    //    RecreateLazy(true);

    //    // push notify after recreating lazy.
    //    if (MoodlesChanged && GetOnlineUserDatas().Contains(pair.UserData))
    //        Mediator.Publish(new MoodlesPermissionsUpdated(pair.PlayerNameWithWorld));
    //}

    public void UpdateAllUniqueForKinkster(UserData target, PairPerms newUniquePerms, PairPermAccess newAccessPerms)
    {
        if (!_allClientPairs.TryGetValue(target, out var pair))
            throw new InvalidOperationException("No such pair for " + target.AliasOrUID);

        // Find new permissions enabled that were not enabled before
        var prevPerms = pair.OwnPerms.PuppetPerms;
        var newlyEnabledPermissions = (newUniquePerms.PuppetPerms & ~prevPerms);

        // update the permissions.
        pair.UserPair.OwnPerms = newUniquePerms;
        pair.UserPair.OwnAccess = newAccessPerms;

        if (newlyEnabledPermissions is not PuppetPerms.None)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, newlyEnabledPermissions);

        Logger.LogDebug($"Updated own unique permissions for '{pair.GetNickAliasOrUid()}'", LoggerType.PairDataTransfer);
    }


    public void UpdatePairUpdateOtherAllGlobalPermissions(BulkChangeGlobal dto)
    {
        // update the pairs permissions.
        if (!_allClientPairs.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Found no Kinkster with UID: {dto.User.UID}");

        kinkster.UserPair.Globals = dto.NewPerms;
        Logger.LogDebug($"Updated all globals for '{kinkster.GetNickname() ?? kinkster.UserData.AliasOrUID}'", LoggerType.PairDataTransfer);
    }

    /// <summary>
    ///     Updates a global permission of a client pair user.
    ///     Edit access is checked server-side to prevent abuse, so these should be all accurate.
    /// </summary>>
    public void UpdateGlobalPerm(UserData target, UserData enactor, KeyValuePair<string, object> newPerm)
    {
        if (!_allClientPairs.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Found no Kinkster with UID: {target.UID}");

        var NewPerm = newPerm.Key;
        var ChangedValue = newPerm.Value;

        if (!PropertyChanger.TrySetProperty(kinkster.PairGlobals, NewPerm, ChangedValue, out object? finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{NewPerm}' on {kinkster.GetNickAliasOrUid()} with value '{ChangedValue}'");

        Logger.LogDebug($"Updated other pair global permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);
        RecreateLazy(false);

        // Handle hardcore changes here. (DO THIS LATER)
    }

    /// <summary>
    /// Updates one of the paired users pair permissions they have set for you. (their permission for you)
    /// </summary>>
    public void UpdateOtherPairPermission(SingleChangeUnique dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Found no Kinkster with UID: {dto.User.UID}");

        var NewPerm = dto.NewPerm.Key;
        var ChangedValue = dto.NewPerm.Value;
        var propertyInfo = typeof(PairPerms).GetProperty(NewPerm);

        // Handle Pause Mediator Call.
        if (NewPerm is nameof(PairPerms.IsPaused) && kinkster.PairPerms.IsPaused != (bool)ChangedValue)
            Mediator.Publish(new ClearProfileDataMessage(dto.User));

        // Attempt to change the property.
        if (!PropertyChanger.TrySetProperty(kinkster.PairPerms, NewPerm, ChangedValue, out object? convertedValue) || convertedValue is null)
            throw new InvalidOperationException($"Failed to set property '{NewPerm}' on {kinkster.GetNickAliasOrUid()} with value '{ChangedValue}'");

        
        Logger.LogDebug($"Updated other pair permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);
        RecreateLazy(false);

        // Notify Kinkster Moodle change to Moodles via IPC Provider.
        if (NewPerm is nameof(PairPerms.MoodlePerms) || NewPerm is nameof(PairPerms.MaxMoodleTime))
            if (GetOnlineUserDatas().Contains(kinkster.UserData))
                Mediator.Publish(new MoodlesPermissionsUpdated(kinkster.PlayerNameWithWorld));
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
    ///     Updates one of your unique pair permissions you have set with the paired user.
    /// </summary>>
    public void UpdateSelfPairPermission(SingleChangeUnique dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Found no Kinkster with UID: {dto.User.UID}");

        var NewPerm = dto.NewPerm.Key;
        var ChangedValue = dto.NewPerm.Value;
        var prevPerms = kinkster.OwnPerms.PuppetPerms;
        var puppetChanged = NewPerm == nameof(PairPerms.PuppetPerms);

        // Handle Pause Mediator Call.
        if (NewPerm is nameof(PairPerms.IsPaused) && kinkster.OwnPerms.IsPaused != (bool)ChangedValue)
            Mediator.Publish(new ClearProfileDataMessage(dto.User));

        // Attempt to change the property.
        if (!PropertyChanger.TrySetProperty(kinkster.OwnPerms, NewPerm, ChangedValue, out object? convertedValue) || convertedValue is null)
            throw new InvalidOperationException($"Failed to set property '{NewPerm}' on {kinkster.GetNickAliasOrUid()} with value '{ChangedValue}'");


        Logger.LogDebug($"Updated own pair permission '{NewPerm}' to '{ChangedValue}'", LoggerType.PairDataTransfer);
        RecreateLazy(false);

        // Notify Kinkster Moodle change to Moodles via IPC Provider.
        if ((kinkster.OwnPerms.PuppetPerms & ~prevPerms) != 0)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, (kinkster.OwnPerms.PuppetPerms & ~prevPerms));
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
