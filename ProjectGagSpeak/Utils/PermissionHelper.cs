using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using GagspeakAPI.Util;

namespace GagSpeak.Utils;

/// <summary>
///     WARNING: This class can bypass any special permissions that need to happen on value change, 
///     be sure to account for these, or else it will become problematic.
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    ///     Updates a client's own global permission client-side.
    ///     After the client-side change is made, it requests the change serverside.
    ///     If any error occurs from the server-call, the value is reverted to its state before the change.
    /// </summary>
    public static async Task<bool> ChangeOwnGlobal(MainHub hub, GlobalPerms perms, string propertyName, object newValue)
    {
        var type = perms.GetType();
        var property = type.GetProperty(propertyName);
        if (property is null || !property.CanRead || !property.CanWrite)
            return false;

        // Initially, Before sending it off, store the current value.
        var currentValue = property.GetValue(perms);

        try
        {
            // Update it before we send off for validation.
            if (!PropertyChanger.TrySetProperty(perms, propertyName, newValue, out object? finalVal))
                throw new InvalidOperationException($"Failed to set property {propertyName} for self in GlobalPerms with value {newValue}.");

            if (finalVal is null)
                throw new InvalidOperationException($"Property {propertyName} in GlobalPerms, has the finalValue was null, which is not allowed.");

            // Now that it is updated clientside, attempt to make the change on the server, and get the hub responce.
            HubResponse response = await hub.UserChangeOwnGlobalPerm(propertyName, finalVal);

            if (response.ErrorCode is not GagSpeakApiEc.Success)
                throw new InvalidOperationException($"Failed to change {propertyName} to {finalVal} for self. Reason: {response.ErrorCode}");
        }
        catch (InvalidOperationException ex)
        {
            Svc.Logger.Warning(ex.Message + "(Resetting to Previous Value)");
            property.SetValue(perms, currentValue);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Updates a client's own PairPermission for a defined <paramref name="target"/> Kinkster client-side.
    ///     After the client-side change is made, it requests the change serverside.
    ///     If any error occurs from the server-call, the value is reverted to its state before the change.
    /// </summary>
    public static async Task<bool> ChangeOwnUnique(MainHub hub, UserData target, PairPerms perms, string propertyName, object newValue)
    {
        var type = perms.GetType();
        var property = type.GetProperty(propertyName);
        if (property is null || !property.CanRead || !property.CanWrite)
            return false;

        // Initially, Before sending it off, store the current value.
        var currentValue = property.GetValue(perms);

        try
        {
            // Update it before we send off for validation.
            if (!PropertyChanger.TrySetProperty(perms, propertyName, newValue, out object? finalVal))
                throw new InvalidOperationException($"Failed to set property {propertyName} for self in PairPerms with value {newValue}.");

            if (finalVal is null)
                throw new InvalidOperationException($"Property {propertyName} in PairPerms, has the finalValue was null, which is not allowed.");

            // Now that it is updated clientside, attempt to make the change on the server, and get the hub responce.
            HubResponse response = await hub.UserChangeOwnPairPerm(
                new(target, new KeyValuePair<string, object>(propertyName, finalVal), UpdateDir.Own, MainHub.PlayerUserData));

            if (response.ErrorCode is not GagSpeakApiEc.Success)
                throw new InvalidOperationException($"Failed to change {propertyName} to {finalVal} for self. Reason: {response.ErrorCode}");
        }
        catch (InvalidOperationException ex)
        {
            Svc.Logger.Warning(ex.Message + "(Resetting to Previous Value)");
            property.SetValue(perms, currentValue);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Updates a client's own PairPermAccess for a defined <paramref name="target"/> Kinkster client-side.
    ///     After the client-side change is made, it requests the change serverside.
    ///     If any error occurs from the server-call, the value is reverted to its state before the change.
    /// </summary>
    public static async Task<bool> ChangeOwnAccess(MainHub hub, UserData target, PairPermAccess perms, string propertyName, object newValue)
    {
        var type = perms.GetType();
        var property = type.GetProperty(propertyName);
        if (property is null || !property.CanRead || !property.CanWrite)
            return false;

        // Initially, Before sending it off, store the current value.
        var currentValue = property.GetValue(perms);

        try
        {
            // Update it before we send off for validation.
            if (!PropertyChanger.TrySetProperty(perms, propertyName, newValue, out object? finalVal))
                throw new InvalidOperationException($"Failed to set property {propertyName} for self in PairPermAccess with value {newValue}.");

            if (finalVal is null)
                throw new InvalidOperationException($"Property {propertyName} in PairPermAccess, has the finalValue was null, which is not allowed.");

            // Now that it is updated clientside, attempt to make the change on the server, and get the hub responce.
            HubResponse response = await hub.UserChangeOwnPairPermAccess(
                new(target, new KeyValuePair<string, object>(propertyName, finalVal), UpdateDir.Own, MainHub.PlayerUserData));

            if (response.ErrorCode is not GagSpeakApiEc.Success)
                throw new InvalidOperationException($"Failed to change {propertyName} to {finalVal} for self. Reason: {response.ErrorCode}");
        }
        catch (InvalidOperationException ex)
        {
            Svc.Logger.Warning(ex.Message + "(Resetting to Previous Value)");
            property.SetValue(perms, currentValue);
            return false;
        }

        return true;
    }


    /// <summary>
    ///     Changes one of the client's Kinkster pair <paramref name="target"/>'s GlobalPerms, if permissions allow.
    ///     This is initially changed client-side, and then a request for the change is sent to the server.
    ///     If any error occurs from the server-call, the value is reverted to its state before the change.
    /// </summary>
    public static async Task<bool> ChangeOtherGlobal(MainHub hub, UserData target, GlobalPerms perms, string propertyName, object newValue)
    {
        var type = perms.GetType();
        var property = type.GetProperty(propertyName);
        if (property is null || !property.CanRead || !property.CanWrite)
            return false;

        // Initially, Before sending it off, store the current value.
        var currentValue = property.GetValue(perms);
        
        try
        {
            // Update it before we send off for validation.
            if (!PropertyChanger.TrySetProperty(perms, propertyName, newValue, out object? finalVal))
                throw new InvalidOperationException($"Failed to set property {propertyName} for {target.AliasOrUID} in GlobalPerms with value {newValue}.");

            if (finalVal is null)
                throw new InvalidOperationException($"Property {propertyName} in GlobalPerms, has the finalValue was null, which is not allowed.");

            // Now that it is updated clientside, attempt to make the change on the server, and get the hub responce.
            HubResponse response = await hub.UserChangeOtherGlobalPerm(
                new(target, new KeyValuePair<string, object>(propertyName, finalVal), UpdateDir.Other, MainHub.PlayerUserData));

            if (response.ErrorCode is not GagSpeakApiEc.Success)
                throw new InvalidOperationException($"Failed to change {propertyName} to {finalVal} for {target.AliasOrUID}, Reason: {response.ErrorCode}");
        }
        catch (InvalidOperationException ex)
        {
            Svc.Logger.Warning(ex.Message + "(Resetting to Previous Value)");
            property.SetValue(perms, currentValue);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Changes one of the client's Kinkster pair <paramref name="target"/>'s PairPerms, if permissions allow.
    ///     This is initially changed client-side, and then a request for the change is sent to the server.
    ///     If any error occurs from the server-call, the value is reverted to its state before the change.
    /// </summary>
    public static async Task<bool> ChangeOtherUnique(MainHub hub, UserData target, PairPerms perms, string propertyName, object newValue)
    {
        // Initially, Before sending it off, store the current value.
        var type = perms.GetType();
        var property = type.GetProperty(propertyName);
        if (property is null || !property.CanWrite)
            return false;

        // Initially, Before sending it off, store the current value.
        var currentValue = property.GetValue(perms);

        try
        {
            // Update it before we send off for validation.
            if (!PropertyChanger.TrySetProperty(perms, propertyName, newValue, out object? finalVal))
                throw new InvalidOperationException($"Failed to set property {propertyName} for {target.AliasOrUID} in PairPerms with value {newValue}.");

            if (finalVal is null)
                throw new InvalidOperationException($"Property {propertyName} in PairPerms, has the finalValue was null, which is not allowed.");

            // Now that it is updated clientside, attempt to make the change on the server, and get the hub responce.
            HubResponse response = await hub.UserChangeOtherPairPerm(
                new(target, new KeyValuePair<string, object>(propertyName, finalVal), UpdateDir.Other, MainHub.PlayerUserData));

            if (response.ErrorCode is not GagSpeakApiEc.Success)
                throw new InvalidOperationException($"Failed to change {propertyName} to {finalVal} for {target.AliasOrUID}, Reason: {response.ErrorCode}");
        }
        catch (InvalidOperationException ex)
        {
            Svc.Logger.Warning(ex.Message + "(Resetting to Previous Value)");
            property.SetValue(perms, currentValue);
            return false;
        }

        return true;
    }

    /// <summary> Validates if the pair can apply the status to the user. </summary>
    /// <param name="pairPerms"> The permissions of the pair. </param>
    /// <param name="statuses"> The statuses to apply. </param>
    /// <returns> True if the statuses can be applied.
    public static bool CanApplyPairStatus(PairPerms pairPerms, IEnumerable<MoodlesStatusInfo> statuses)
    {
        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.PositiveStatusTypes) && statuses.Any(statuses => statuses.Type == StatusType.Positive))
        {
            Svc.Logger.Warning("Client Attempted to apply status(s) with at least one containing a positive status, but they are not allowed to.");
            return false;
        }
        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.NegativeStatusTypes) && statuses.Any(statuses => statuses.Type == StatusType.Negative))
        {
            Svc.Logger.Warning("Client Attempted to apply status(s) with at least one containing a negative status, but they are not allowed to.");
            return false;
        }
        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.SpecialStatusTypes) && statuses.Any(statuses => statuses.Type == StatusType.Special))
        {
            Svc.Logger.Warning("Client Attempted to apply status(s) with at least one containing a special status, but they are not allowed to.");
            return false;
        }

        if (!pairPerms.MoodlePerms.HasAny(MoodlePerms.PermanentMoodles) && statuses.Any(statuses => statuses.NoExpire))
        {
            Svc.Logger.Warning("Client Attempted to apply status(s) with at least one containing a permanent status, but they are not allowed to.");
            return false;
        }

        // check the max moodle time exceeding
        if (statuses.Any(status => status.NoExpire == false && // if the status is not permanent, and the time its set for is longer than max allowed time.
            new TimeSpan(status.Days, status.Hours, status.Minutes, status.Seconds) > pairPerms.MaxMoodleTime))
        {
            Svc.Logger.Warning("Client Attempted to apply status(s) with at least one containing a time exceeding the max allowed time.");
            return false;
        }
        // return true if reached here.
        return true;
    }
}
