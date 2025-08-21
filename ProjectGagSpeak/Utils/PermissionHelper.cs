using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Util;
using OtterGui;

namespace GagSpeak.Utils;

/// <summary>
///     WARNING: This class can bypass any special permissions that need to happen on value change, 
///     be sure to account for these, or else it will become problematic.
///     
///     This classes primary purpose is for the UI to display updated values before recieving the callback, and processing the callback after it gets it
///     to handle any achievement tracking or handlers.
///     
///     Either find a way to handle the callbacks automatically based on their changed state, or setup callbacks to never callback to the caller 
///     that made the change and process internally. Either way, do this AFTER the update, as it mostly saves on server cost for interactions.
/// </summary>
public static class PermissionHelper
{
    public static PairPerms WithSafewordApplied(this PairPerms perms)
        => perms with
        {
            InHardcore = false,
            DevotionalLocks = false,
            AllowGarbleChannelEditing = false,
            AllowLockedFollowing = false,
            AllowLockedSitting = false,
            AllowLockedEmoting = false,
            AllowIndoorConfinement = false,
            AllowImprisonment = false,
            AllowHidingChatBoxes = false,
            AllowHidingChatInput = false,
            AllowChatInputBlocking = false,
            AllowHypnoImageSending = false
        };

    /// <summary>
    ///     Updates a client's own global permission client-side.
    ///     After the client-side change is made, it requests the change serverside.
    ///     If any error occurs from the server-call, the value is reverted to its state before the change.
    /// </summary>
    public static async Task<bool> ChangeOwnGlobal(MainHub hub, GlobalPerms ownGlobals, string propertyName, object newValue)
    {
        if (ClientData.Globals is not { } perms)
        {
            Svc.Logger.Error("OwnGlobals.Permissions is null, cannot change own global permissions.");
            return false;
        }

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

            // Now that it is updated client-side, attempt to make the change on the server, and get the hub response.
            HubResponse response = await hub.ChangeOwnGlobalPerm(propertyName, newValue);

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
                new(target, new KeyValuePair<string, object>(propertyName, newValue), UpdateDir.Own, MainHub.PlayerUserData));

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
                new(target, new KeyValuePair<string, object>(propertyName, newValue), UpdateDir.Own, MainHub.PlayerUserData));

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

            // Now that it is updated client-side, attempt to make the change on the server, and get the hub responce.
            HubResponse response = await hub.ChangeOtherGlobalPerm(target, propertyName, finalVal);

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

    public static void DrawHardcoreState(HardcoreState? hardcoreState)
    {
        if (hardcoreState is not { } hc)
        {
            CkGui.CenterColorTextAligned("Hardcore State is NULL!", ImGuiColors.DalamudRed);
            return;
        }

        // Display Hardcore State.
        using (var t = ImRaii.Table("HardcoreStateTable", 6, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t)
                return;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("State");
            ImGui.TableSetupColumn("Enactor");
            ImGui.TableSetupColumn("Time Left");
            ImGui.TableSetupColumn("Devo");
            ImGui.TableSetupColumn("Information");
            ImGui.TableHeadersRow();

            // Follow:
            var followOn = hc.IsEnabled(HcAttribute.Follow);
            ImGuiUtil.DrawFrameColumn("Follow");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(followOn, false);
            ImGuiUtil.DrawFrameColumn(hc.LockedFollowing.Split('|')[0]);
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(hc.IsDevotional(HcAttribute.Follow), false);
            ImGui.TableNextRow();

            // Emote:
            var emoteOn = hc.IsEnabled(HcAttribute.EmoteState);
            ImGuiUtil.DrawFrameColumn("Emote");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(emoteOn, false);
            ImGuiUtil.DrawFrameColumn(hc.LockedEmoteState.Split('|')[0]);
            ImGui.TableNextColumn();
            CkGui.ColorText(hc.EmoteExpireTime.ToGsRemainingTimeFancy(), ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(hc.IsDevotional(HcAttribute.EmoteState), false);
            ImGui.TableNextColumn();
            ImGui.Text("Emote ID:");
            CkGui.ColorTextInline($"{hc.EmoteId}", ImGuiColors.TankBlue);
            CkGui.TextInline("Cycle Pose:");
            CkGui.ColorTextInline($"{hc.EmoteCyclePose}", ImGuiColors.TankBlue);
            ImGui.TableNextRow();

            // Indoor Confinement:
            var confinementOn = hc.IsEnabled(HcAttribute.Confinement);
            ImGuiUtil.DrawFrameColumn("Confinement");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(confinementOn, false);
            ImGuiUtil.DrawFrameColumn(hc.IndoorConfinement.Split('|')[0]);
            ImGui.TableNextColumn();
            CkGui.ColorText(hc.ConfinementTimer.ToGsRemainingTimeFancy(), ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(hc.IsDevotional(HcAttribute.Confinement), false);
            ImGui.TableNextColumn();
            ImGui.Text("World:");
            CkGui.ColorTextInline($"{hc.ConfinedWorld}", ImGuiColors.TankBlue);
            CkGui.TextInline("| City:");
            CkGui.ColorTextInline($"{hc.ConfinedCity}", ImGuiColors.TankBlue);
            CkGui.TextInline("| Ward:");
            CkGui.ColorTextInline($"{hc.ConfinedWard}", ImGuiColors.TankBlue);
            CkGui.TextInline("| Place ID:");
            CkGui.ColorTextInline($"{hc.ConfinedPlaceId}", ImGuiColors.TankBlue);
            CkGui.TextInline("| In Apartment:");
            CkGui.BooleanToColoredIcon(hc.ConfinedInApartment);
            CkGui.TextInline("| In Subdivision:");
            CkGui.BooleanToColoredIcon(hc.ConfinedInSubdivision);
            ImGui.TableNextRow();

            // Imprisonment:
            var imprisonmentOn = hc.IsEnabled(HcAttribute.Imprisonment);
            ImGuiUtil.DrawFrameColumn("Imprisonment");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(imprisonmentOn, false);
            ImGuiUtil.DrawFrameColumn(hc.Imprisonment.Split('|')[0]);
            ImGui.TableNextColumn();
            CkGui.ColorText(hc.ImprisonmentTimer.ToGsRemainingTimeFancy(), ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(hc.IsDevotional(HcAttribute.Imprisonment), false);
            ImGui.TableNextColumn();
            ImGui.Text("Territory:");
            CkGui.ColorTextInline($"{hc.ImprisonedTerritory}", ImGuiColors.TankBlue);
            CkGui.TextInline("| Position:");
            CkGui.ColorTextInline($"{(Vector3)hc.ImprisonedPos:F1}", ImGuiColors.TankBlue);
            CkGui.TextInline(" | Radius:");
            CkGui.ColorTextInline($"{hc.ImprisonedRadius}", ImGuiColors.TankBlue);
            ImGui.TableNextRow();

            // Chat Boxes Hidden:
            var chatBoxesOn = hc.IsEnabled(HcAttribute.HiddenChatBox);
            ImGuiUtil.DrawFrameColumn("NoChatBox");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(chatBoxesOn, false);
            ImGuiUtil.DrawFrameColumn(hc.ChatBoxesHidden.Split('|')[0]);
            ImGui.TableNextColumn();
            CkGui.ColorText(hc.ChatBoxesHiddenTimer.ToGsRemainingTimeFancy(), ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(hc.IsDevotional(HcAttribute.HiddenChatBox), false);
            ImGui.TableNextRow();

            // Chat Input Hidden:
            var chatInputHiddenOn = hc.IsEnabled(HcAttribute.HiddenChatInput);
            ImGuiUtil.DrawFrameColumn("NoChatInput");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(chatInputHiddenOn, false);
            ImGuiUtil.DrawFrameColumn(hc.ChatInputHidden.Split('|')[0]);
            ImGui.TableNextColumn();
            CkGui.ColorText(hc.ChatInputHiddenTimer.ToGsRemainingTimeFancy(), ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(hc.IsDevotional(HcAttribute.HiddenChatInput), false);
            ImGui.TableNextRow();

            // Chat Input Blocked:
            var chatInputBlockedOn = hc.IsEnabled(HcAttribute.BlockedChatInput);
            ImGuiUtil.DrawFrameColumn("BlockedChatInput");
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(chatInputBlockedOn, false);
            ImGuiUtil.DrawFrameColumn(hc.ChatInputBlocked.Split('|')[0]);
            ImGui.TableNextColumn();
            CkGui.ColorText(hc.ChatInputBlockedTimer.ToGsRemainingTimeFancy(), ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            CkGui.BooleanToColoredIcon(hc.IsDevotional(HcAttribute.BlockedChatInput), false);
            ImGui.TableNextRow();
        }
    }
}
