using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using System.Security;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// CLIENT PERMS PARTIAL CLASS
/// </summary>
public partial class PairStickyUI
{
    // This is where we both view our current settings for the pair,
    // and the levels of access we have granted them control over.
    //
    // For each row, to the left will be an icon. Displaying the status relative to the state.
    //
    // beside it will be the current StickyPair.UserPair.OwnPairPerms we have set for them.
    // 
    // to the far right will be a interactable checkbox, this will display if we allow
    // this pair to have control over this option or not.
    public void DrawClientPermsForPair()
    {
        /* ----------- GLOBAL SETTINGS ----------- */
        ImGui.TextUnformatted("Global Permissions");

        DrawOwnSetting("LiveChatGarblerActive", "LiveChatGarblerActiveAllowed", // permission name and permission access name
            _playerManager.GlobalPerms!.LiveChatGarblerActive ? "Live Chat Garbler Active" : "Live Chat Garbler Inactive", // label
            _playerManager.GlobalPerms.LiveChatGarblerActive ? FontAwesomeIcon.MicrophoneSlash : FontAwesomeIcon.Microphone, // icon
            _playerManager.GlobalPerms.LiveChatGarblerActive ? "Click to disable Live Chat Garbler (Global)" : "Click to enable Live Chat Garbler (Global)", // tooltip
            OwnPerms.InHardcore || _playerManager.GlobalPerms.LiveChatGarblerLocked, // Disable condition
            PermissionType.Global, PermissionValueType.YesNo); // permission type and value type

        DrawOwnSetting("LiveChatGarblerLocked", "LiveChatGarblerLockedAllowed",
            _playerManager.GlobalPerms.LiveChatGarblerLocked ? "Live Chat Garbler Locked" : "LIve Chat Garbler Unlocked",
            _playerManager.GlobalPerms.LiveChatGarblerLocked ? FontAwesomeIcon.Key : FontAwesomeIcon.UnlockAlt,
            _playerManager.GlobalPerms.LiveChatGarblerLocked ? "Click to unlock Live Chat Garbler (Global)" : "Click to lock Live Chat Garbler (Global)",
            true,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting("LockToyboxUI", "LockToyboxUIAllowed",
            _playerManager.GlobalPerms.LockToyboxUI ? "Toybox Interactions Disallowed" : "Toybox Interactions Allowed",
            _playerManager.GlobalPerms.LockToyboxUI ? FontAwesomeIcon.Box : FontAwesomeIcon.BoxOpen,
            _playerManager.GlobalPerms.LockToyboxUI ? "Click to allow Toybox Interactions (Global)" : "Click to disallow Toybox Interactions (Global)",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- GAG PERMISSIONS ----------- */
        ImGui.TextUnformatted("Gag Permissions");

        DrawOwnSetting("GagFeatures", "GagFeaturesAllowed",
            OwnPerms.GagFeatures ? "Gag Interactions Allowed" : "Gag Interactions Disallowed",
            OwnPerms.GagFeatures ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.GagFeatures ?
                $"Click to disallow {PairNickOrAliasOrUID} to apply, lock or remove gags" : $"Click to allow {PairNickOrAliasOrUID} to apply, lock or remove gags",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("ItemAutoEquip", "ItemAutoEquipAllowed",
            _playerManager.GlobalPerms.ItemAutoEquip ? "Auto-equip Gag Glamours Active" : Auto-equip Gag Glamours Inactive",
            _playerManager.GlobalPerms.ItemAutoEquip ? FontAwesomeIcon.Surprise : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.ItemAutoEquip ? "Click to disable auto-equip for Gag Glamours (Global)" : "Click to enable auto-equip for Gag Glamours (Global)",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting("MaxLockTime", "MaxLockTimeAllowed",
            "Max Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max duration {PairNickOrAliasOrUID} can lock your gags for",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting("ExtendedLockTimes", "ExtendedLockTimesAllowed",
            OwnPerms.ExtendedLockTimes ? "Extended Lock Time Allowed" : "Extended Lock Time Disallowed",
            OwnPerms.ExtendedLockTimes ? FontAwesomeIcon.Stopwatch : FontAwesomeIcon.Ban,
            OwnPerms.ExtendedLockTimes ?
                $"Click to disallow {PairNickOrAliasOrUID} to set locks longer than 1 hour" : $"Click to allow {PairNickOrAliasOrUID} to set locks longer than 1 hour",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("OwnerLocks", "OwnerLocksAllowed",
            OwnPerms.OwnerLocks ? "Owner Padlocks Allowed" : "Owner Padlocks Disallowed",
            OwnPerms.OwnerLocks ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.OwnerLocks ? $"Click to disallow {PairNickOrAliasOrUID} to use Owner Padlocks" : $"Click to allow {PairNickOrAliasOrUID} to use Owner Padlocks.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);


        DrawOwnSetting("DevotionalLocks", "DevotionalLocksAllowed",
            OwnPerms.DevotionalLocks ? "Devotional Padlocks Allowed" : "Devotional Padlocks Disallowed",
            OwnPerms.DevotionalLocks ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.DevotionalLocks ? $"Click to disallow {PairNickOrAliasOrUID} to use Devotional Padlocks" : $"Click to allow {PairNickOrAliasOrUID} to use Devotional Padlocks",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- RESTRAINT SET PERMISSIONS ----------- */
        ImGui.TextUnformatted("Restraint Set Permissions");

        DrawOwnSetting("RestraintSetAutoEquip", "RestraintSetAutoEquipAllowed",
            _playerManager.GlobalPerms.RestraintSetAutoEquip ? "Restraint Set Glamours Active" : "Restraint Set Glamours Inactive",
            _playerManager.GlobalPerms.RestraintSetAutoEquip ? FontAwesomeIcon.ShopLock : FontAwesomeIcon.ShopSlash,
            _playerManager.GlobalPerms.RestraintSetAutoEquip ? "Click to disable Restraint Set Glamours (Global)" : "Click to enable Restraint Set Glamours (Global)",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting("ApplyRestraintSets", "ApplyRestraintSetsAllowed",
            OwnPerms.ApplyRestraintSets ? "Apply Restraint Sets Allowed" : "Apply Restraint Sets Disallowed",
            OwnPerms.ApplyRestraintSets ? FontAwesomeIcon.Tshirt : FontAwesomeIcon.Ban,
            OwnPerms.ApplyRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to apply Restraint Sets" : $"Click to allow {PairNickOrAliasOrUID} to apply Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("LockRestraintSets", "LockRestraintSetsAllowed",
            OwnPerms.LockRestraintSets ? "Resrtaint Sets Locking Allowed" : "Restraint Sets Locking Disallowed",
            OwnPerms.LockRestraintSets ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            OwnPerms.LockRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to lock your Restraint Sets" : $"Click to allow {PairNickOrAliasOrUID} to lock your Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("MaxAllowedRestraintTime", "MaxAllowedRestraintTimeAllowed",
            "Max Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max duration {PairNickOrAliasOrUID} can lock your Restraint Sets for",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting("UnlockRestraintSets", "UnlockRestraintSetsAllowed",
            OwnPerms.UnlockRestraintSets ? "Restraint SetS Unlocking Allowed" : "Restraint Sets Unlocking Disallowed",
            OwnPerms.UnlockRestraintSets ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Ban,
            OwnPerms.UnlockRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to unlock your Restraint Sets" : $"Click to allow {PairNickOrAliasOrUID} to unlock your Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("RemoveRestraintSets", "RemoveRestraintSetsAllowed",
            OwnPerms.RemoveRestraintSets ? "Restraint Sets Removal Allowed" : "Restraint Sets Removal Disallowed",
            OwnPerms.RemoveRestraintSets ? FontAwesomeIcon.Female : FontAwesomeIcon.Ban,
            OwnPerms.RemoveRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to remove your Restraint Sets" : $"Click to allow {PairNickOrAliasOrUID} to remove your Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();

        /* ----------- PUPPETEER PERMISSIONS ----------- */
        ImGui.TextUnformatted("Puppeteer Permissions");

        DrawOwnSetting("AllowSitRequests", "AllowSitRequestsAllowed",
            OwnPerms.AllowSitRequests ? "Sit Requests Allowed" : "Sit Requests Disallowed",
            OwnPerms.AllowSitRequests ? FontAwesomeIcon.Chair : FontAwesomeIcon.Ban,
            OwnPerms.AllowSitRequests ? $"Click to disallow {PairNickOrAliasOrUID} to force " +
                "you to /groundsit, /sit or /changepose" : $"Click to allow {PairNickOrAliasOrUID} to force you to /groundsit, /sit or /changepose (different to Hardcore)",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowMotionRequests", "AllowMotionRequestsAllowed",
            OwnPerms.AllowMotionRequests ? "Motion Requests Allowed" : "Motion Requests Disallowed",
            OwnPerms.AllowMotionRequests ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            OwnPerms.AllowMotionRequests ? $"Click to disallow {PairNickOrAliasOrUID} to force you to use emotes " +
                "and expressions" : $"Click to allow {PairNickOrAliasOrUID} to force you to use emotes and expressions",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowAllRequests", "AllowAllRequestsAllowed",
            OwnPerms.AllowAllRequests ? "All Requests Allowed" : "All Requests Disallowed",
            OwnPerms.AllowAllRequests ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.AllowAllRequests ? $"Click to disallow {PairNickOrAliasOrUID} to force you to use any game or plugin command" : $"Click to allow {PairNickOrAliasOrUID} to force you to use any game or plugin command",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- MOODLES PERMISSIONS ----------- */
        ImGui.TextUnformatted("Moodles Permissions");

        DrawOwnSetting("AllowPositiveStatusTypes", "AllowPositiveStatusTypesAllowed",
           OwnPerms.AllowPositiveStatusTypes ? "Apply Positive Moodles Allowed" : "Apply Positive Moodles Disallowed",
           OwnPerms.AllowPositiveStatusTypes ? FontAwesomeIcon.SmileBeam : FontAwesomeIcon.Ban,
           OwnPerms.AllowPositiveStatusTypes ? $"Click to disallow {PairNickOrAliasOrUID} to apply Moodles with a positive " +
               "status" : $"Click to allow {PairNickOrAliasOrUID} to apply Moodles with a positive status",
           OwnPerms.InHardcore,
           PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowNegativeStatusTypes", "AllowNegativeStatusTypesAllowed",
            OwnPerms.AllowNegativeStatusTypes ? "Apply Negative Moodles Allowed" : "Apply Negative Moodles Disallowed",
            OwnPerms.AllowNegativeStatusTypes ? FontAwesomeIcon.FrownOpen : FontAwesomeIcon.Ban,
            OwnPerms.AllowNegativeStatusTypes ? $"Click to disallow {PairNickOrAliasOrUID} to apply Moodles with a negative " +
                "status" : $"Click to allow {PairNickOrAliasOrUID} to apply Moodles with a negative status",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowSpecialStatusTypes", "AllowSpecialStatusTypesAllowed",
            OwnPerms.AllowSpecialStatusTypes ? "Apply Special Moodles Allowed" : "Apply Special Moodles Disallowed",
            OwnPerms.AllowSpecialStatusTypes ? FontAwesomeIcon.WandMagicSparkles : FontAwesomeIcon.Ban,
            OwnPerms.AllowSpecialStatusTypes ? $"Click to disallow {PairNickOrAliasOrUID} to apply Moodles with a special " +
                "status" : $"Click to allow {PairNickOrAliasOrUID} to apply Moodles with a special status",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("PairCanApplyOwnMoodlesToYou", "PairCanApplyOwnMoodlesToYouAllowed",
            OwnPerms.PairCanApplyOwnMoodlesToYou ? $"Apply Pair's Moodles Allowed" : $"Apply Pair's Moodles Disallowed",
            OwnPerms.PairCanApplyOwnMoodlesToYou ? FontAwesomeIcon.PersonArrowUpFromLine : FontAwesomeIcon.Ban,
            OwnPerms.PairCanApplyOwnMoodlesToYou ? $"Click to disallow {PairNickOrAliasOrUID} to apply their own Moodles onto " +
                "you" : $"Click to disallow {PairNickOrAliasOrUID} to apply their own Moodles onto you",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("PairCanApplyYourMoodlesToYou", "PairCanApplyYourMoodlesToYouAllowed",
            OwnPerms.PairCanApplyYourMoodlesToYou ? $"Apply Own Moodles Allowed" : $"Apply Own Moodles Disallowed",
            OwnPerms.PairCanApplyYourMoodlesToYou ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Ban,
            OwnPerms.PairCanApplyYourMoodlesToYou ? $"Click to disallow {PairNickOrAliasOrUID} to apply your Moodles onto " +
                "you" : $"Click to allow {PairNickOrAliasOrUID} to apply your Moodles onto you",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("MaxMoodleTime", "MaxMoodleTimeAllowed",
            "Max Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max duration {PairNickOrAliasOrUID} can apply Moodles to you for",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting("AllowPermanentMoodles", "AllowPermanentMoodlesAllowed",
            OwnPerms.AllowPermanentMoodles ? "Permanent Moodles Allowed" : "Permanent Moodles Disallowed",
            OwnPerms.AllowPermanentMoodles ? FontAwesomeIcon.Infinity : FontAwesomeIcon.Ban,
            OwnPerms.AllowPermanentMoodles ? $"Click to disallow {PairNickOrAliasOrUID} to apply permanent Moodles onto to you" : $"Click to allow {PairNickOrAliasOrUID} to apply permanent Moodles onto you",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowRemovingMoodles", "AllowRemovingMoodlesAllowed",
            OwnPerms.AllowRemovingMoodles ? "Moodles Removal Allowed" : "Moodles Removal Disallowed",
            OwnPerms.AllowRemovingMoodles ? FontAwesomeIcon.Eraser : FontAwesomeIcon.Ban,
            OwnPerms.AllowRemovingMoodles ? $"Click to disallow {PairNickOrAliasOrUID} to remove your Moodles" : $"Click to allow {PairNickOrAliasOrUID} to remove your Moodles",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- TOYBOX PERMISSIONS ----------- */
        ImGui.TextUnformatted("Toybox Permissions");

        DrawOwnSetting("CanToggleToyState", "CanToggleToyStateAllowed",
            OwnPerms.CanToggleToyState ? "Toggle Vibe Allowed" : "Toggle Vibe Disallowed",
            OwnPerms.CanToggleToyState ? FontAwesomeIcon.PowerOff : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleToyState ? $"Click to disallow {PairNickOrAliasOrUID} to toggle your sex toys" : $"Click to allow {PairNickOrAliasOrUID} to toggle your sex toys",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanUseVibeRemote", "CanUseVibeRemoteAllowed",
            OwnPerms.CanUseVibeRemote ? "Vibe Control Allowed" : "Vibe Control Disallowed",
            OwnPerms.CanUseVibeRemote ? FontAwesomeIcon.Mobile : FontAwesomeIcon.Ban,
            OwnPerms.CanUseVibeRemote ? $"Click to disallow {PairNickOrAliasOrUID} to control your sex toys" : $"Click to allow {PairNickOrAliasOrUID} to control your sex toys",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanToggleAlarms", "CanToggleAlarmsAllowed",
            OwnPerms.CanToggleAlarms ? "Toggle Alarms Allowed" : "Toggle Alarms Disallowed",
            OwnPerms.CanToggleAlarms ? FontAwesomeIcon.Bell : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleAlarms ? $"Click to disallow {PairNickOrAliasOrUID} to toggle your alarms" : $"Click to allow {PairNickOrAliasOrUID} to toggle your alarms",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanSendAlarms", "CanSendAlarmsAllowed",
            OwnPerms.CanSendAlarms ? "Sending Alarams Allowed" : "Sending Alarms Disallowed",
            OwnPerms.CanSendAlarms ? FontAwesomeIcon.FileExport : FontAwesomeIcon.Ban,
            OwnPerms.CanSendAlarms ? $"Click to disallow {PairNickOrAliasOrUID} to send you alarms" : $"Click to allow {PairNickOrAliasOrUID} to send you alarms",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanExecutePatterns", "CanExecutePatternsAllowed",
            OwnPerms.CanExecutePatterns ? "Start Patterns Allowed" : "Start Patterns Disallowed",
            OwnPerms.CanExecutePatterns ? FontAwesomeIcon.LandMineOn : FontAwesomeIcon.Ban,
            OwnPerms.CanExecutePatterns ? $"Click to disallow {PairNickOrAliasOrUID} to start patterns" : $"Click to allow {PairNickOrAliasOrUID} to start patterns",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanStopPatterns", "CanStopPatternsAllowed",
            OwnPerms.CanStopPatterns ? "Stop Patterns Allowed" : "Stop Patterns Disallowed",
            OwnPerms.CanStopPatterns ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.Ban,
            OwnPerms.CanExecutePatterns ? $"Click to disallow {PairNickOrAliasOrUID} to stop patterns" : $"Click to allow {PairNickOrAliasOrUID} to stop patterns",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanToggleTriggers", "CanToggleTriggersAllowed",
            OwnPerms.CanToggleTriggers ? "Toggle Triggers Allowed" : "Toggle Triggers Disallowed",
            OwnPerms.CanToggleTriggers ? FontAwesomeIcon.FileMedicalAlt : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleTriggers ? $"Click to disallow {PairNickOrAliasOrUID} to toggle your triggers" : $"Click to allow {PairNickOrAliasOrUID} to toggle your triggers",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- HARDCORE PERMISSIONS ----------- */
        ImGui.TextUnformatted("Hardcore Permissions");

        DrawOwnSetting("InHardcore", string.Empty,
            OwnPerms.InHardcore ? $"In Hardcore Mode for {PairNickOrAliasOrUID}" : $"Not in Hardcore Mode",
            OwnPerms.InHardcore ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.InHardcore ? $"Disable Hardcore Mode for {PairNickOrAliasOrUID} (Use /safewordhardcore with your safeword to disable)" : $"Enable Hardcore Mode for {PairNickOrAliasOrUID}",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("DevotionalStatesForPair", string.Empty,
            OwnPerms.DevotionalStatesForPair ? $"Toggles are Devotional from {PairNickOrAliasOrUID}" : $"Toggles are not Devotional",
            OwnPerms.DevotionalStatesForPair ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.DevotionalStatesForPair ? $"Click to make toggles from {PairNickOrAliasOrUID} not be Devotional" : $"Click to make toggles from {PairNickOrAliasOrUID} be Devotional",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("ForcedFollow", "AllowForcedFollow",
            _playerManager.GlobalPerms.IsFollowing() ? "Forced to follow" : "Not forced to follow",
            _playerManager.GlobalPerms.IsFollowing() ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsFollowing() ? $"You are being forced to follow {PairNickOrAliasOrUID}" : $"You are not being forced to follow {PairNickOrAliasOrUID}",
            true, PermissionType.Hardcore);

        using (ImRaii.Group())
        {
            var icon = _playerManager.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? FontAwesomeIcon.Ban : (OwnPerms.AllowForcedEmote ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Chair);
            var txt = _playerManager.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? "Not forced to emote or sit" : "Forced to emote or sit";
            var tooltipStr = _playerManager.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? $"You are not being forced to emote or sit by {PairNickOrAliasOrUID}" : $"You are being forced to emote or sit by {PairNickOrAliasOrUID}";
            var specialWidth = IconButtonTextWidth - ImGui.GetFrameHeightWithSpacing();
            _uiShared.IconTextButton(icon, txt, specialWidth, true, true);
            UiSharedService.AttachToolTip(tooltipStr);

            ImGui.SameLine(specialWidth);
            bool refState = OwnPerms.AllowForcedSit;
            if (ImGui.Checkbox("##AllowForcedSit", ref refState))
            {
                if (refState != OwnPerms.AllowForcedSit)
                    SetOwnPermission(PermissionType.UniquePairPerm, nameof(OwnPerms.AllowForcedSit), refState);
            }
            UiSharedService.AttachToolTip("Limit "+PairNickOrAliasOrUID +" to force Ground Sit and Sit");

            ImUtf8.SameLineInner();
            bool refState2 = OwnPerms.AllowForcedEmote;
            if (ImGui.Checkbox("##AllowForcedEmote", ref refState2))
            {
                if (refState2 != OwnPerms.AllowForcedEmote)
                {
                    _logger.LogInformation("Setting is now " + refState2);
                    SetOwnPermission(PermissionType.UniquePairPerm, nameof(OwnPerms.AllowForcedEmote), refState2);
                }
            }
            UiSharedService.AttachToolTip("Allow " + PairNickOrAliasOrUID + " to force you to perform any looped emote");
        }

        DrawOwnSetting("ForcedStay", "AllowForcedToStay",
            _playerManager.GlobalPerms.IsStaying() ? "Forced to stay" : "Not forced to stay",
            _playerManager.GlobalPerms.IsStaying() ? FontAwesomeIcon.HouseLock : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsStaying() ? $"You are being forced to stay by {PairNickOrAliasOrUID}" : $"You are not being forced to stay by {PairNickOrAliasOrUID}",
            true, PermissionType.Hardcore);

        DrawOwnSetting("ForcedBlindfold", "AllowBlindfold",
            _playerManager.GlobalPerms.IsBlindfolded() ? "Blindfolded" : "Not Blindfolded",
            _playerManager.GlobalPerms.IsBlindfolded() ? FontAwesomeIcon.Blind : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsBlindfolded() ? $"You have been blindfolded by {PairNickOrAliasOrUID}" : $"You have not been blindfolded by {PairNickOrAliasOrUID}",
            true, PermissionType.Hardcore);

        DrawOwnSetting("ChatBoxesHidden", "AllowHidingChatBoxes",
            _playerManager.GlobalPerms.IsChatHidden() ? "Chatbox is hidden" : "Chatbox is visible",
            _playerManager.GlobalPerms.IsChatHidden() ? FontAwesomeIcon.CommentSlash : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsChatHidden() ? _playerManager.GlobalPerms.ChatBoxesHidden.HardcorePermUID() + " has hidden your chatbox" : "Your chatbox is visible",
            true, PermissionType.Hardcore);

        DrawOwnSetting("ChatInputHidden", "AllowHidingChatInput",
            _playerManager.GlobalPerms.IsChatInputHidden() ? "Chat Input is Hidden" : "Chat Input is Visible",
            _playerManager.GlobalPerms.IsChatInputHidden() ? FontAwesomeIcon.CommentSlash : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsChatInputHidden() ? _playerManager.GlobalPerms.ChatInputHidden.HardcorePermUID() + " has hidden your chat input" : "Your chat input is visible",
            true, PermissionType.Hardcore);
        DrawOwnSetting("ChatInputBlocked", "AllowChatInputBlocking",
            _playerManager.GlobalPerms.IsChatInputBlocked() ? "Chat Input Unavailable" : "Chat Input Available",
            _playerManager.GlobalPerms.IsChatInputBlocked() ? FontAwesomeIcon.CommentDots : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsChatInputBlocked() ? PairNickOrAliasOrUID + " has blocked your chat input access" : "You are chat input access is not being blocked",
            true, PermissionType.Hardcore);


        string shockCollarPairShareCode = OwnPerms.ShockCollarShareCode ?? string.Empty;
        using (var group = ImRaii.Group())
        {
            float width = IconButtonTextWidth - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Sync, "Refresh") + ImGui.GetFrameHeight();
            if (_uiShared.IconInputText("ShockCollarShareCode" + PairUID, FontAwesomeIcon.ShareAlt, string.Empty, "Unique Share Code",
            ref shockCollarPairShareCode, 40, width, true, false))
            {
                OwnPerms.ShockCollarShareCode = shockCollarPairShareCode;
            }
            // Set the permission once deactivated. If invalid, set to default.
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                SetOwnPermission(PermissionType.UniquePairPerm, "ShockCollarShareCode", shockCollarPairShareCode);
            }
            UiSharedService.AttachToolTip($"Unique Share Code for {PairNickOrAliasOrUID}" + Environment.NewLine
            + "This should be a separate Share Code from your Global Share Code." + Environment.NewLine
            + $"A Unique Share Code can have permissions elevated higher than the Global Share Code that only {PairNickOrAliasOrUID} can use.");
            ImUtf8.SameLineInner();
            if (_uiShared.IconTextButton(FontAwesomeIcon.Sync, "Refresh", null, false, DateTime.UtcNow - LastRefresh < TimeSpan.FromSeconds(15) || !UniqueShockCollarPermsExist()))
            {
                LastRefresh = DateTime.UtcNow;
                // Send Mediator Event to grab updated settings for pair.
                Task.Run(async () =>
                {
                    var newPerms = await _shockProvider.GetPermissionsFromCode(shockCollarPairShareCode);
                    // set the new permissions.
                    OwnPerms.AllowShocks = newPerms.AllowShocks;
                    OwnPerms.AllowVibrations = newPerms.AllowVibrations;
                    OwnPerms.AllowBeeps = newPerms.AllowBeeps;
                    OwnPerms.MaxDuration = newPerms.MaxDuration;
                    OwnPerms.MaxIntensity = newPerms.MaxIntensity;
                    // update the permissions.
                    _ = _apiHubMain.UserPushAllUniquePerms(new(StickyPair.UserData, MainHub.PlayerUserData, OwnPerms, StickyPair.OwnPermAccess, UpdateDir.Other));
                });
            }
        }

        // special case for this.
        float seconds = (float)OwnPerms.MaxVibrateDuration.TotalMilliseconds / 1000;
        using (var group = ImRaii.Group())
        {
            if (_uiShared.IconSliderFloat("##ClientSetMaxVibeDurationForPair" + PairUID, FontAwesomeIcon.Stopwatch, "Max Vibe Duration",
                ref seconds, 0.1f, 15f, IconButtonTextWidth * .65f, true, !UniqueShockCollarPermsExist()))
            {
                OwnPerms.MaxVibrateDuration = TimeSpan.FromSeconds(seconds);
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                TimeSpan timespanValue = TimeSpan.FromSeconds(seconds);
                ulong ticks = (ulong)timespanValue.Ticks;
                SetOwnPermission(PermissionType.UniquePairPerm, "MaxVibrateDuration", ticks);
            }
            UiSharedService.AttachToolTip("Max duration you allow this pair to vibrate your Shock Collar for");
        }
    }

    private DateTime LastRefresh = DateTime.MinValue;

    /// <summary>
    /// The primary call for displaying a setting for the client permissions.
    /// </summary>
    /// <param name="permissionName"> The name of the unique pair perm in string format. </param>
    /// <param name="permissionAccessName"> The name of the pair perm edit access in string format </param>
    /// <param name="textLabel"> The text to display beside the icon </param>
    /// <param name="icon"> The icon to display to the left of the text. </param>
    /// <param name="isLocked"> If the permission (not edit access) can be changed. </param>
    /// <param name="tooltipStr"> the tooltip to display when hovered. </param>
    /// <param name="permissionType"> If the permission is a global perm, unique pair perm, or access permission. </param>
    /// <param name="permissionValueType"> what permission type it is (string, char, timespan, boolean) </param>
    private void DrawOwnSetting(string permissionName, string permissionAccessName, string textLabel, FontAwesomeIcon icon,
        string tooltipStr, bool isLocked, PermissionType permissionType, PermissionValueType permissionValueType = PermissionValueType.YesNo)
    {
        try
        {
            switch (permissionType)
            {
                case PermissionType.Global:
                    if (_playerManager.GlobalPerms is null) return;
                    DrawOwnPermission(permissionType, _playerManager.GlobalPerms, textLabel, icon, tooltipStr, isLocked,
                        permissionName, permissionValueType, permissionAccessName);
                    break;
                case PermissionType.UniquePairPerm:
                    DrawOwnPermission(permissionType, OwnPerms, textLabel, icon, tooltipStr, isLocked,
                        permissionName, permissionValueType, permissionAccessName);
                    break;
                // this case should technically never be called for this particular instance.
                case PermissionType.UniquePairPermEditAccess:
                    DrawOwnPermission(permissionType, StickyPair.OwnPermAccess, textLabel, icon, tooltipStr, isLocked,
                        permissionName, permissionValueType, permissionAccessName);
                    break;
                case PermissionType.Hardcore:
                    DrawHardcorePermission(permissionName, permissionAccessName, textLabel, icon, tooltipStr, isLocked);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update permissions for {MainHub.PlayerUserData.AliasOrUID} :: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Responsible for calling the correct display item based on the permission type and permission object
    /// </summary>
    /// <param name="permissionType"> the type of permission we are displaying </param>
    /// <param name="permissionSet"> the permission object we are displaying </param>
    /// <param name="label"> the text label to display beside the icon </param>
    /// <param name="icon"> the icon to display beside the text </param>
    /// <param name="tooltip"> the tooltip to display when hovered </param>
    /// <param name="permissionName"> the name of the permission we are displaying </param>
    /// <param name="type"> the type of permission value we are displaying </param>
    /// <param name="permissionAccessName"> the name of the permission access we are displaying </param>
    private void DrawOwnPermission(PermissionType permissionType, object permissionSet, string label,
        FontAwesomeIcon icon, string tooltip, bool isLocked, string permissionName, PermissionValueType type,
        string permissionAccessName)
    {

        // firstly, if the permission value type is a boolean, then process handling the change as a true/false.
        if (type == PermissionValueType.YesNo)
        {
            // localize the object as a boolean value from its property name.
            bool currValState = (bool)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!;
            // draw the iconTextButton and checkbox beside it. Because we are in control, unless in hardcore, this should never be disabled.
            using (var group = ImRaii.Group())
            {
                // have a special case, where we mark the button as disabled if _playerManager.GlobalPerms.LiveChatGarblerLocked is true
                if (_uiShared.IconTextButton(icon, label, IconButtonTextWidth, true, isLocked))
                    SetOwnPermission(permissionType, permissionName, !currValState);
                UiSharedService.AttachToolTip(tooltip);

                if (!permissionAccessName.IsNullOrEmpty()) // only display checkbox if we should.
                {
                    ImGui.SameLine(IconButtonTextWidth);
                    bool refState = (bool)StickyPair.OwnPermAccess.GetType().GetProperty(permissionAccessName)?.GetValue(StickyPair.OwnPermAccess)!;
                    if (ImGui.Checkbox("##" + permissionAccessName, ref refState))
                    {
                        // if the new state is not the same as the current state, then we should update the permission access.
                        if (refState != (bool)StickyPair.OwnPermAccess.GetType().GetProperty(permissionAccessName)?.GetValue(StickyPair.OwnPermAccess)!)
                            SetOwnPermission(PermissionType.UniquePairPermEditAccess, permissionAccessName, refState);
                    }
                    UiSharedService.AttachToolTip(refState
                        ? "Revoke " + PairNickOrAliasOrUID + "'s control over this permission"
                        : "Grant " + PairNickOrAliasOrUID + " control over this permission, allowing them to change the permission at any time");
                    
                }
            }
        }
        // next, handle it if it is a timespan value.
        else if (type == PermissionValueType.TimeSpan)
        {
            // attempt to parse the timespan value to a string.
            string timeSpanString = _uiShared.TimeSpanToString((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!) ?? "0d0h0m0s";

            using (var group = ImRaii.Group())
            {
                var id = label + "##" + permissionName;
                // draw the iconTextButton and checkbox beside it. Because we are in control, unless in hardcore, this should never be disabled.
                if (_uiShared.IconInputText(id, icon, label, "0d0h0m0s", ref timeSpanString, 32, IconButtonTextWidth * .55f, true, false)) { }
                // Set the permission once deactivated. If invalid, set to default.
                if (ImGui.IsItemDeactivatedAfterEdit()
                    && timeSpanString != _uiShared.TimeSpanToString((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!))
                {
                    // attempt to parse the string back into a valid timespan.
                    if (_uiShared.TryParseTimeSpan(timeSpanString, out TimeSpan result))
                    {
                        ulong ticks = (ulong)result.Ticks;
                        SetOwnPermission(permissionType, permissionName, ticks);
                    }
                    else
                    {
                        // find some way to print this to the chat or something.
                        _logger.LogWarning("You tried to set an invalid timespan format. Please use the format 0d0h0m0s");
                        timeSpanString = "0d0h0m0s";
                    }
                }
                UiSharedService.AttachToolTip(tooltip);
                if (!permissionAccessName.IsNullOrEmpty()) // only display checkbox if we should.
                {
                    ImGui.SameLine(IconButtonTextWidth);
                    bool refState = (bool)StickyPair.OwnPermAccess.GetType().GetProperty(permissionAccessName)?.GetValue(StickyPair.OwnPermAccess)!;
                    if (ImGui.Checkbox("##" + permissionAccessName, ref refState))
                    {
                        // if the new state is not the same as the current state, then we should update the permission access.
                        if (refState != (bool)StickyPair.OwnPermAccess.GetType().GetProperty(permissionAccessName)?.GetValue(StickyPair.OwnPermAccess)!)
                            SetOwnPermission(PermissionType.UniquePairPermEditAccess, permissionAccessName, refState);
                    }
                    UiSharedService.AttachToolTip(refState
                        ? ("Revoke " + StickyPair.GetNickname() ?? StickyPair.UserData.AliasOrUID + "'s control over this permission.")
                        : ("Grant " + StickyPair.GetNickname() ?? StickyPair.UserData.AliasOrUID) + " control over this permission, allowing them to change the permission at any time");
                }
            }
        }
    }

    /// <summary>
    /// Hardcore Permissions need to be handled seperately, since they are technically string values, but treated like booleans.
    /// </summary>
    private void DrawHardcorePermission(string permissionName, string permissionAccessName, string textLabel, FontAwesomeIcon icon,
        string tooltipStr, bool isLocked)
    {
        try
        {
            // Grab the current value.
            string currValState = (string)(_playerManager.GlobalPerms?.GetType().GetProperty(permissionName)?.GetValue(_playerManager?.GlobalPerms) ?? string.Empty);

            using (ImRaii.Group())
            {
                // Disabled Button
                _uiShared.IconTextButton(icon, textLabel, IconButtonTextWidth, true, true);
                UiSharedService.AttachToolTip(tooltipStr);

                if (!permissionAccessName.IsNullOrEmpty()) // only display checkbox if we should.
                {
                    ImGui.SameLine(IconButtonTextWidth);
                    bool refState = (bool)OwnPerms.GetType().GetProperty(permissionAccessName)?.GetValue(OwnPerms)!;
                    if (ImGui.Checkbox("##" + permissionAccessName, ref refState))
                    {
                        // if the new state is not the same as the current state, then we should update the permission access.
                        if (refState != (bool)OwnPerms.GetType().GetProperty(permissionAccessName)?.GetValue(OwnPerms)!)
                            SetOwnPermission(PermissionType.UniquePairPerm, permissionAccessName, refState);
                    }
                    UiSharedService.AttachToolTip(refState
                        ? "Revoke " + PairNickOrAliasOrUID + "'s control over this permission."
                        : "Grant " + PairNickOrAliasOrUID + " control over this permission, allowing them to change the permission at any time");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed for {MainHub.PlayerUserData.AliasOrUID} on {permissionAccessName} :: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Send the updated permission we made for ourselves to the server.
    /// </summary>
    /// <param name="permissionType"> If Global, UniquePairPerm, or EditAccessPerm. </param>
    /// <param name="permissionName"> the attribute of the object we are changing</param>
    /// <param name="newValue"> New value to set. </param>
    private void SetOwnPermission(PermissionType permissionType, string permissionName, object newValue)
    {
        // Call the update to the server.
        switch (permissionType)
        {
            case PermissionType.Global:
                {
                    _logger.LogTrace($"Updated own global permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(permissionName, newValue), UpdateDir.Own));
                }
                break;
            case PermissionType.UniquePairPerm:
                {
                    _logger.LogTrace($"Updated own pair permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(permissionName, newValue), UpdateDir.Own));
                }
                break;
            // this case should technically never be called for this particular instance.
            case PermissionType.UniquePairPermEditAccess:
                {
                    _logger.LogTrace($"Updated own edit access permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOwnPairPermAccess(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(permissionName, newValue), UpdateDir.Own));
                }
                break;
        }
    }
}
