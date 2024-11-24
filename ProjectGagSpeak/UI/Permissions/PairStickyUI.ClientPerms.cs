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
        ImGui.TextUnformatted("Global Settings");

        DrawOwnSetting("LiveChatGarblerActive", "LiveChatGarblerActiveAllowed", // permission name and permission access name
            _playerManager.GlobalPerms!.LiveChatGarblerActive ? "Live Chat Garbler Active" : "Live Chat Garbler Inactive", // label
            _playerManager.GlobalPerms.LiveChatGarblerActive ? FontAwesomeIcon.MicrophoneSlash : FontAwesomeIcon.Microphone, // icon
            _playerManager.GlobalPerms.LiveChatGarblerActive ? "Disable the Live Chat Garbler. [Global]" : "Enable the Live Chat Garbler. [Global]", // tooltip
            OwnPerms.InHardcore || _playerManager.GlobalPerms.LiveChatGarblerLocked, // Disable condition
            PermissionType.Global, PermissionValueType.YesNo); // permission type and value type

        DrawOwnSetting("LiveChatGarblerLocked", "LiveChatGarblerLockedAllowed",
            _playerManager.GlobalPerms.LiveChatGarblerLocked ? "Chat Garbler Locked" : "Chat Garbler Unlocked",
            _playerManager.GlobalPerms.LiveChatGarblerLocked ? FontAwesomeIcon.Key : FontAwesomeIcon.UnlockAlt,
            _playerManager.GlobalPerms.LiveChatGarblerLocked ? "Chat Garbler has been locked! [Global]" : "Chat Garbler is not currently locked. [Global]",
            true,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting("LockToyboxUI", "LockToyboxUIAllowed",
            _playerManager.GlobalPerms.LockToyboxUI ? "Toybox Interactions Locked" : "Toybox Interactions Available",
            _playerManager.GlobalPerms.LockToyboxUI ? FontAwesomeIcon.Box : FontAwesomeIcon.BoxOpen,
            _playerManager.GlobalPerms.LockToyboxUI ? "Remove active Toybox feature restrictions [Global]" : "Restrict your toybox features. [Global]",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- GAG PERMISSIONS ----------- */
        ImGui.TextUnformatted("Gag Permissions");

        DrawOwnSetting("GagFeatures", "GagFeaturesAllowed",
            OwnPerms.GagFeatures ? "Allowing Gag Interactions" : "Preventing Gag Interactions",
            OwnPerms.GagFeatures ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.GagFeatures ?
                $"Prevent {PairNickOrAliasOrUID} from Applying, Locking, and Removing Gags" : $"Allow {PairNickOrAliasOrUID} to Apply, Lock, and Remove Gags.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("ItemAutoEquip", "ItemAutoEquipAllowed",
            _playerManager.GlobalPerms.ItemAutoEquip ? "Auto-Equip Gag Glamour's" : "No Gag Glamour Auto-Equip",
            _playerManager.GlobalPerms.ItemAutoEquip ? FontAwesomeIcon.Surprise : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.ItemAutoEquip ? "Disable Auto-Equip for Gag Glamour's. [Global]" : "Enable Auto-Equip for Gag Glamour's. [Global]",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting("MaxLockTime", "MaxLockTimeAllowed",
            "Max Time",
            FontAwesomeIcon.HourglassHalf,
            $"Max time {PairNickOrAliasOrUID} can lock your gags for.",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting("ExtendedLockTimes", "ExtendedLockTimesAllowed",
            OwnPerms.ExtendedLockTimes ? "Allowing Extended Lock Times" : "Preventing Extended Lock Times",
            OwnPerms.ExtendedLockTimes ? FontAwesomeIcon.Stopwatch : FontAwesomeIcon.Ban,
            OwnPerms.ExtendedLockTimes ?
                $"Prevent {PairNickOrAliasOrUID} from setting locks longer than 1 hour." : $"Allow {PairNickOrAliasOrUID} to set locks longer than 1 hour.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("OwnerLocks", "OwnerLocksAllowed",
            OwnPerms.OwnerLocks ? "Allowing Owner Padlocks" : "Preventing Owner Padlocks",
            OwnPerms.OwnerLocks ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.OwnerLocks ? $"Prevent {PairNickOrAliasOrUID} from using Owner Padlocks." : $"Allow {PairNickOrAliasOrUID} to use Owner Padlocks.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);


        DrawOwnSetting("DevotionalLocks", "DevotionalLocksAllowed",
            OwnPerms.DevotionalLocks ? "Allowing Devotional Padlocks" : "Preventing Devotional Padlocks",
            OwnPerms.DevotionalLocks ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.DevotionalLocks ? $"Prevent {PairNickOrAliasOrUID} from using Devotion Padlocks." : $"Allow {PairNickOrAliasOrUID} to use Devotion Padlocks.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- RESTRAINT SET PERMISSIONS ----------- */
        ImGui.TextUnformatted("Restraint Set Permissions");

        DrawOwnSetting("RestraintSetAutoEquip", "RestraintSetAutoEquipAllowed",
            _playerManager.GlobalPerms.RestraintSetAutoEquip ? "Restraint Set Glamour's active" : "Restraint Set Glamour's inactive",
            _playerManager.GlobalPerms.RestraintSetAutoEquip ? FontAwesomeIcon.ShopLock : FontAwesomeIcon.ShopSlash,
            _playerManager.GlobalPerms.RestraintSetAutoEquip ? "Disable Restraint Set Glamour's. [Global]" : "Enable Restraint Set Glamour's. [Global]",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting("ApplyRestraintSets", "ApplyRestraintSetsAllowed",
            OwnPerms.ApplyRestraintSets ? "Apply Restraint Sets Allowed" : "Preventing Restraint Set Application",
            OwnPerms.ApplyRestraintSets ? FontAwesomeIcon.Tshirt : FontAwesomeIcon.Ban,
            OwnPerms.ApplyRestraintSets ? $"Prevent {PairNickOrAliasOrUID} from applying restraint sets." : $"Allow {PairNickOrAliasOrUID} to apply your restraint sets.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("LockRestraintSets", "LockRestraintSetsAllowed",
            OwnPerms.LockRestraintSets ? "Allowing Restraint Set Locking" : "Preventing Restraint Set Locking",
            OwnPerms.LockRestraintSets ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            OwnPerms.LockRestraintSets ? $"Prevent {PairNickOrAliasOrUID} from locking your restraint sets." : $"Allow {PairNickOrAliasOrUID} to lock your restraint sets.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("MaxAllowedRestraintTime", "MaxAllowedRestraintTimeAllowed",
            "Max Time",
            FontAwesomeIcon.HourglassHalf,
            $"Max time {PairNickOrAliasOrUID} can lock your restraint sets for.",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting("UnlockRestraintSets", "UnlockRestraintSetsAllowed",
            OwnPerms.UnlockRestraintSets ? "Allowing Restraint Set Unlocking" : "Preventing Restraint Set Unlocking",
            OwnPerms.UnlockRestraintSets ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Ban,
            OwnPerms.UnlockRestraintSets ? $"Prevent {PairNickOrAliasOrUID} from unlocking your restraint sets." : $"Allow {PairNickOrAliasOrUID} to unlock your restraint sets.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("RemoveRestraintSets", "RemoveRestraintSetsAllowed",
            OwnPerms.RemoveRestraintSets ? "Allowing Restraint Set Removal" : "Preventing Restraint Set Removal",
            OwnPerms.RemoveRestraintSets ? FontAwesomeIcon.Female : FontAwesomeIcon.Ban,
            OwnPerms.RemoveRestraintSets ? $"Prevent {PairNickOrAliasOrUID} from removing your restraint sets." : $"Allow {PairNickOrAliasOrUID} to remove your restraint sets.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();

        /* ----------- PUPPETEER PERMISSIONS ----------- */
        ImGui.TextUnformatted("Puppeteer Permissions");

        DrawOwnSetting("AllowSitRequests", "AllowSitRequestsAllowed",
            OwnPerms.AllowSitRequests ? "Allowing Sit Requests" : "Preventing Sit Requests",
            OwnPerms.AllowSitRequests ? FontAwesomeIcon.Chair : FontAwesomeIcon.Ban,
            OwnPerms.AllowSitRequests ? $"Prevent {PairNickOrAliasOrUID} from forcing " +
                "you to /sit (different from hardcore)" : $"Let {PairNickOrAliasOrUID} forcing you to /sit or /groundsit.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowMotionRequests", "AllowMotionRequestsAllowed",
            OwnPerms.AllowMotionRequests ? "Allowing Motion Requests" : "Preventing Motion Requests",
            OwnPerms.AllowMotionRequests ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            OwnPerms.AllowMotionRequests ? $"Prevent {PairNickOrAliasOrUID} from forcing you to do expressions " +
                "and emotes." : $"Let {PairNickOrAliasOrUID} force you to do expressions and emotes.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowAllRequests", "AllowAllRequestsAllowed",
            OwnPerms.AllowAllRequests ? "Allowing All Requests" : "Preventing All Requests",
            OwnPerms.AllowAllRequests ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.AllowAllRequests ? $"Prevent {PairNickOrAliasOrUID} from forcing you to do anything." : $"Let {PairNickOrAliasOrUID} force you to do anything.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- MOODLES PERMISSIONS ----------- */
        ImGui.TextUnformatted("Moodles Permissions");

        DrawOwnSetting("AllowPositiveStatusTypes", "AllowPositiveStatusTypesAllowed",
           OwnPerms.AllowPositiveStatusTypes ? "Allow Applying Positive Moodles" : "Positive Moodles Disallowed",
           OwnPerms.AllowPositiveStatusTypes ? FontAwesomeIcon.SmileBeam : FontAwesomeIcon.Ban,
           OwnPerms.AllowPositiveStatusTypes ? $"Prevent {PairNickOrAliasOrUID} from applying moodles with a positive " +
               "status." : $"Allow {PairNickOrAliasOrUID} to apply moodles with a positive status.",
           OwnPerms.InHardcore,
           PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowNegativeStatusTypes", "AllowNegativeStatusTypesAllowed",
            OwnPerms.AllowNegativeStatusTypes ? "Allow Applying Negative Moodles" : "Negative Moodles Disallowed",
            OwnPerms.AllowNegativeStatusTypes ? FontAwesomeIcon.FrownOpen : FontAwesomeIcon.Ban,
            OwnPerms.AllowNegativeStatusTypes ? $"Prevent {PairNickOrAliasOrUID} from applying moodles with a negative " +
                "status." : $"Allow {PairNickOrAliasOrUID} to apply moodles with a negative status.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowSpecialStatusTypes", "AllowSpecialStatusTypesAllowed",
            OwnPerms.AllowSpecialStatusTypes ? "Allow Applying Special Moodles" : "Special Moodles Disallowed",
            OwnPerms.AllowSpecialStatusTypes ? FontAwesomeIcon.WandMagicSparkles : FontAwesomeIcon.Ban,
            OwnPerms.AllowSpecialStatusTypes ? $"Prevent {PairNickOrAliasOrUID} from applying moodles with a special " +
                "status." : $"Allow {PairNickOrAliasOrUID} to apply moodles with a special status.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("PairCanApplyOwnMoodlesToYou", "PairCanApplyOwnMoodlesToYouAllowed",
            OwnPerms.PairCanApplyOwnMoodlesToYou ? $"Allow Application of Pair's Moodles." : $"Prevent Application of Pair's Moodles.",
            OwnPerms.PairCanApplyOwnMoodlesToYou ? FontAwesomeIcon.PersonArrowUpFromLine : FontAwesomeIcon.Ban,
            OwnPerms.PairCanApplyOwnMoodlesToYou ? $"Allow {PairNickOrAliasOrUID} to apply their own Moodles onto " +
                "you." : $"Prevent {PairNickOrAliasOrUID} from applying their own Moodles onto you.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("PairCanApplyYourMoodlesToYou", "PairCanApplyYourMoodlesToYouAllowed",
            OwnPerms.PairCanApplyYourMoodlesToYou ? $"Allow Application of Your Moodles." : $"Prevent Application of Your Moodles.",
            OwnPerms.PairCanApplyYourMoodlesToYou ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Ban,
            OwnPerms.PairCanApplyYourMoodlesToYou ? $"Allow {PairNickOrAliasOrUID} to apply your Moodles onto " +
                "you." : $"Prevent {PairNickOrAliasOrUID} from applying your Moodles onto you.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("MaxMoodleTime", "MaxMoodleTimeAllowed",
            "Max Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max time {PairNickOrAliasOrUID} can apply moodles to you for.",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting("AllowPermanentMoodles", "AllowPermanentMoodlesAllowed",
            OwnPerms.AllowPermanentMoodles ? "Allow Permanent Moodles" : "Prevent Permanent Moodles",
            OwnPerms.AllowPermanentMoodles ? FontAwesomeIcon.Infinity : FontAwesomeIcon.Ban,
            OwnPerms.AllowPermanentMoodles ? $"Prevent {PairNickOrAliasOrUID} from applying permanent moodles to you." : $"Allow {PairNickOrAliasOrUID} to apply permanent moodles to you.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("AllowRemovingMoodles", "AllowRemovingMoodlesAllowed",
            OwnPerms.AllowRemovingMoodles ? "Allow Removing Moodles" : "Prevent Removing Moodles",
            OwnPerms.AllowRemovingMoodles ? FontAwesomeIcon.Eraser : FontAwesomeIcon.Ban,
            OwnPerms.AllowRemovingMoodles ? $"Prevent {PairNickOrAliasOrUID} from removing your moodles." : $"Allow {PairNickOrAliasOrUID} to remove your moodles.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- TOYBOX PERMISSIONS ----------- */
        ImGui.TextUnformatted("Toybox Permissions");

        DrawOwnSetting("CanToggleToyState", "CanToggleToyStateAllowed",
            OwnPerms.CanToggleToyState ? "Allow Toggling Vibes" : "Preventing Toggling Vibes",
            OwnPerms.CanToggleToyState ? FontAwesomeIcon.PowerOff : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleToyState ? $"Prevent {PairNickOrAliasOrUID} from toggling your toys." : $"Allow {PairNickOrAliasOrUID} to toggle your toys.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanUseVibeRemote", "CanUseVibeRemoteAllowed",
            OwnPerms.CanUseVibeRemote ? "Allow Vibe Control" : "Prevent Vibe Control",
            OwnPerms.CanUseVibeRemote ? FontAwesomeIcon.Mobile : FontAwesomeIcon.Ban,
            OwnPerms.CanUseVibeRemote ? $"Prevent {PairNickOrAliasOrUID} from controlling your Vibe." : $"Allow {PairNickOrAliasOrUID} to control your Vibe.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanToggleAlarms", "CanToggleAlarmsAllowed",
            OwnPerms.CanToggleAlarms ? "Allow Toggling Alarms" : "Prevent Toggling Alarms",
            OwnPerms.CanToggleAlarms ? FontAwesomeIcon.Bell : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleAlarms ? $"Prevent {PairNickOrAliasOrUID} from toggling your alarms." : $"Allow {PairNickOrAliasOrUID} to toggle your alarms.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanSendAlarms", "CanSendAlarmsAllowed",
            OwnPerms.CanSendAlarms ? "Allow Sending Alarms" : "Prevent Sending Alarms",
            OwnPerms.CanSendAlarms ? FontAwesomeIcon.FileExport : FontAwesomeIcon.Ban,
            OwnPerms.CanSendAlarms ? $"Prevent {PairNickOrAliasOrUID} from sending alarms." : $"Allow {PairNickOrAliasOrUID} to send alarms.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanExecutePatterns", "CanExecutePatternsAllowed",
            OwnPerms.CanExecutePatterns ? "Allow Pattern Execution" : "Prevent Pattern Execution",
            OwnPerms.CanExecutePatterns ? FontAwesomeIcon.LandMineOn : FontAwesomeIcon.Ban,
            OwnPerms.CanExecutePatterns ? $"Prevent {PairNickOrAliasOrUID} from executing patterns." : $"Allow {PairNickOrAliasOrUID} to execute patterns.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanStopPatterns", "CanStopPatternsAllowed",
            OwnPerms.CanStopPatterns ? "Allow Stopping Patterns" : "Prevent Stopping Patterns",
            OwnPerms.CanStopPatterns ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.Ban,
            OwnPerms.CanExecutePatterns ? $"Prevent {PairNickOrAliasOrUID} from stopping patterns." : $"Allow {PairNickOrAliasOrUID} to stop patterns.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("CanToggleTriggers", "CanToggleTriggersAllowed",
            OwnPerms.CanToggleTriggers ? "Allow Toggling Triggers" : "Prevent Toggling Triggers",
            OwnPerms.CanToggleTriggers ? FontAwesomeIcon.FileMedicalAlt : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleTriggers ? $"Prevent {PairNickOrAliasOrUID} from toggling your triggers." : $"Allow {PairNickOrAliasOrUID} to toggle your triggers.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- HARDCORE PERMISSIONS ----------- */
        ImGui.TextUnformatted("Hardcore Permissions");

        DrawOwnSetting("InHardcore", string.Empty,
            OwnPerms.InHardcore ? $"In Hardcore Mode for {PairNickOrAliasOrUID}" : $"Not in Hardcore Mode.",
            OwnPerms.InHardcore ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.InHardcore ? $"Disable Hardcore Mode for {PairNickOrAliasOrUID}." : $"Enable Hardcore Mode for {PairNickOrAliasOrUID}.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("DevotionalStatesForPair", string.Empty,
            OwnPerms.DevotionalStatesForPair ? $"Toggles are Devotional from {PairNickOrAliasOrUID}" : $"Toggles are Normal Toggles.",
            OwnPerms.DevotionalStatesForPair ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.DevotionalStatesForPair ? $"Make toggles from {PairNickOrAliasOrUID} be no longer treated as Devotional." : $"Make Toggles done by {PairNickOrAliasOrUID} be treated as devotional.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting("ForcedFollow", "AllowForcedFollow",
            _playerManager.GlobalPerms.IsFollowing() ? "Currently Forced to Follow" : "Not Forced to Follow",
            _playerManager.GlobalPerms.IsFollowing() ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsFollowing() ? $"You are currently being forced to follow {PairNickOrAliasOrUID}" : $"{PairNickOrAliasOrUID} is not currently forcing you to follow them.",
            true, PermissionType.Hardcore);

        using (ImRaii.Group())
        {
            var icon = _playerManager.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? FontAwesomeIcon.Ban : (OwnPerms.AllowForcedEmote ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Chair);
            var txt = _playerManager.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? "Not Forced to Sit/Emote" : "Currently Forced to Sit/Emote";
            var tooltipStr = _playerManager.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? $"{PairNickOrAliasOrUID} is not currently forcing you to sit or emote." : $"You are currently being forced to sit or emote by {PairNickOrAliasOrUID}";
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
            UiSharedService.AttachToolTip("Limit "+PairNickOrAliasOrUID +" to only Force Sit or GroundSit Action for you when checked.");

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
            UiSharedService.AttachToolTip("Allow " + PairNickOrAliasOrUID + " to force you to perform any looped emote.");
        }

        DrawOwnSetting("ForcedStay", "AllowForcedToStay",
            _playerManager.GlobalPerms.IsStaying() ? "Currently Forced to Stay" : "Not Forced to Stay",
            _playerManager.GlobalPerms.IsStaying() ? FontAwesomeIcon.HouseLock : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsStaying() ? $"You are currently being forced to stay by {PairNickOrAliasOrUID}" : $"{PairNickOrAliasOrUID} is not currently forcing you to stay.",
            true, PermissionType.Hardcore);

        DrawOwnSetting("ForcedBlindfold", "AllowBlindfold",
            _playerManager.GlobalPerms.IsBlindfolded() ? "Currently Blindfolded" : "Not Blindfolded",
            _playerManager.GlobalPerms.IsBlindfolded() ? FontAwesomeIcon.Blind : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsBlindfolded() ? $"You are currently blindfolded by {PairNickOrAliasOrUID}" : $"{PairNickOrAliasOrUID} is not currently blindfolding you.",
            true, PermissionType.Hardcore);

        DrawOwnSetting("ChatBoxesHidden", "AllowHidingChatBoxes",
            _playerManager.GlobalPerms.IsChatHidden() ? "Chatbox is Hidden" : "Chatbox is Visible",
            _playerManager.GlobalPerms.IsChatHidden() ? FontAwesomeIcon.CommentSlash : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsChatHidden() ? _playerManager.GlobalPerms.ChatBoxesHidden.HardcorePermUID() + " has hidden your Chatbox!" : "Nobody is hiding your Chat.",
            true, PermissionType.Hardcore);

        DrawOwnSetting("ChatInputHidden", "AllowHidingChatInput",
            _playerManager.GlobalPerms.IsChatInputHidden() ? "Chat Input is Hidden" : "Chat Input is Visible",
            _playerManager.GlobalPerms.IsChatInputHidden() ? FontAwesomeIcon.CommentSlash : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsChatInputHidden() ? _playerManager.GlobalPerms.ChatInputHidden.HardcorePermUID() + " has hidden your Chatbox Input!" : "Nobody is hiding your Chat Input.",
            true, PermissionType.Hardcore);
        DrawOwnSetting("ChatInputBlocked", "AllowChatInputBlocking",
            _playerManager.GlobalPerms.IsChatInputBlocked() ? "Chat Input Blocked" : "Chat Input Available",
            _playerManager.GlobalPerms.IsChatInputBlocked() ? FontAwesomeIcon.CommentDots : FontAwesomeIcon.Ban,
            _playerManager.GlobalPerms.IsChatInputBlocked() ? PairNickOrAliasOrUID + " has currently blocked access to your Chat Input" : "Nobody is blocking your Chat Input Access",
            true, PermissionType.Hardcore);


        string shockCollarPairShareCode = OwnPerms.ShockCollarShareCode ?? string.Empty;
        using (var group = ImRaii.Group())
        {
            float width = IconButtonTextWidth - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Sync, "Refresh") + ImGui.GetFrameHeight();
            if (_uiShared.IconInputText("ShockCollarShareCode" + PairUID, FontAwesomeIcon.ShareAlt, string.Empty, "Unique Share Code...",
            ref shockCollarPairShareCode, 40, width, true, false))
            {
                OwnPerms.ShockCollarShareCode = shockCollarPairShareCode;
            }
            // Set the permission once deactivated. If invalid, set to default.
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                SetOwnPermission(PermissionType.UniquePairPerm, "ShockCollarShareCode", shockCollarPairShareCode);
            }
            UiSharedService.AttachToolTip($"Unique Share Code for {PairNickOrAliasOrUID}." + Environment.NewLine
            + "This should be a Separate Share Code from your Global Code." + Environment.NewLine
            + $"Unique Share Codes can have elevated settings higher than the Global Code, that only {PairNickOrAliasOrUID} can use.");
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
                    _ = _apiHubMain.UserPushAllUniquePerms(new(StickyPair.UserData, OwnPerms, StickyPair.OwnPermAccess));
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
            UiSharedService.AttachToolTip("Sets the Max Duration you allow this pair to vibrate your Shock Collar for.");
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
                        ? "Revoke " + PairNickOrAliasOrUID + "'s control over this permission."
                        : "Grant " + PairNickOrAliasOrUID + " control over this permission, allowing them to change what you've set for them at will.");
                    
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
                if (_uiShared.IconInputText(id, icon, label, "format 0d0h0m0s...", ref timeSpanString, 32, IconButtonTextWidth * .55f, true, false)) { }
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
                        : ("Grant " + StickyPair.GetNickname() ?? StickyPair.UserData.AliasOrUID) + " control over this permission, allowing them to change " +
                           "what you've set for them at will.");
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
                        : "Grant " + PairNickOrAliasOrUID + " control over this permission, allowing them to change what you've set for them at will.");
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
                    _ = _apiHubMain.UserUpdateOwnGlobalPerm(new UserGlobalPermChangeDto(MainHub.PlayerUserData,
                        new KeyValuePair<string, object>(permissionName, newValue), MainHub.PlayerUserData));
                }
                break;
            case PermissionType.UniquePairPerm:
                {
                    _logger.LogTrace($"Updated own pair permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOwnPairPerm(new UserPairPermChangeDto(StickyPair.UserData,
                        new KeyValuePair<string, object>(permissionName, newValue)));
                }
                break;
            // this case should technically never be called for this particular instance.
            case PermissionType.UniquePairPermEditAccess:
                {
                    _logger.LogTrace($"Updated own edit access permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOwnPairPermAccess(new UserPairAccessChangeDto(StickyPair.UserData,
                        new KeyValuePair<string, object>(permissionName, newValue)));
                }
                break;
        }
    }
}
