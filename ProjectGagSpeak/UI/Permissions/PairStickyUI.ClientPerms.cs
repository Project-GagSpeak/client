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

        DrawOwnSetting(nameof(_clientData.GlobalPerms.LiveChatGarblerActive), nameof(StickyPair.OwnPermAccess.LiveChatGarblerActiveAllowed),
            _clientData.GlobalPerms!.LiveChatGarblerActive ? "Live Chat Garbler Active" : "Live Chat Garbler Inactive", // label
            _clientData.GlobalPerms.LiveChatGarblerActive ? FontAwesomeIcon.MicrophoneSlash : FontAwesomeIcon.Microphone, // icon
            _clientData.GlobalPerms.LiveChatGarblerActive ? "Click to disable Live Chat Garbler (Global)" : "Click to enable Live Chat Garbler (Global)", // tooltip
            OwnPerms.InHardcore || _clientData.GlobalPerms.LiveChatGarblerLocked, // Disable condition
            PermissionType.Global, PermissionValueType.YesNo); // permission type and value type

        DrawOwnSetting(nameof(_clientData.GlobalPerms.LiveChatGarblerLocked), nameof(StickyPair.OwnPermAccess.LiveChatGarblerLockedAllowed),
            _clientData.GlobalPerms.LiveChatGarblerLocked ? "Live Chat Garbler Locked" : "LIve Chat Garbler Unlocked",
            _clientData.GlobalPerms.LiveChatGarblerLocked ? FontAwesomeIcon.Key : FontAwesomeIcon.UnlockAlt,
            _clientData.GlobalPerms.LiveChatGarblerLocked ? "Click to unlock Live Chat Garbler (Global)" : "Click to lock Live Chat Garbler (Global)",
            true,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(_clientData.GlobalPerms.LockToyboxUI), nameof(StickyPair.OwnPermAccess.LockToyboxUIAllowed),
            _clientData.GlobalPerms.LockToyboxUI ? "Toybox Interactions Restricted" : "Toybox Interactions Allowed",
            _clientData.GlobalPerms.LockToyboxUI ? FontAwesomeIcon.Box : FontAwesomeIcon.BoxOpen,
            _clientData.GlobalPerms.LockToyboxUI ? "Click to allow Toybox Interactions (Global)" : "Click to disallow Toybox Interactions (Global)",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        ImGui.Separator();

        /* ----------- LOCK PERMISSIONS ----------- */
        ImGui.TextUnformatted("Padlock Permissions");

        DrawOwnSetting(nameof(OwnPerms.PermanentLocks), nameof(StickyPair.OwnPermAccess.PermanentLocksAllowed),
            OwnPerms.PermanentLocks ? "Permanent Locks Allowed" : "Permanent Locks Restricted",
            OwnPerms.PermanentLocks ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.PermanentLocks ? $"Click to revoke {PairNickOrAliasOrUID} from applying Padlocks without timers." 
                                    : $"Click to allow {PairNickOrAliasOrUID} to apply Padlocks without timers.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.OwnerLocks), nameof(StickyPair.OwnPermAccess.OwnerLocksAllowed),
            OwnPerms.OwnerLocks ? "Owner Padlocks Allowed" : "Owner Padlocks Restricted",
            OwnPerms.OwnerLocks ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.OwnerLocks ? $"Click to revoke {PairNickOrAliasOrUID} from applying Owner Padlocks." 
                                : $"Click to allow {PairNickOrAliasOrUID} to apply Owner Padlocks.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.DevotionalLocks), nameof(StickyPair.OwnPermAccess.DevotionalLocksAllowed),
            OwnPerms.DevotionalLocks ? "Devotional Padlocks Allowed" : "Devotional Padlocks Restricted",
            OwnPerms.DevotionalLocks ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.DevotionalLocks ? $"Click to revoke {PairNickOrAliasOrUID} from applying Devotional Padlocks." 
                                    : $"Click to allow {PairNickOrAliasOrUID} to apply Devotional Padlocks.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);
        
        ImGui.Separator();
        /* ----------- GAG PERMISSIONS ----------- */
        ImGui.TextUnformatted("Gag Permissions");

        DrawOwnSetting(nameof(_clientData.GlobalPerms.ItemAutoEquip), nameof(StickyPair.OwnPermAccess.ItemAutoEquipAllowed),
            _clientData.GlobalPerms.ItemAutoEquip ? "Auto-equip Gag Glamours Active" : "Auto-equip Gag Glamours Inactive",
            _clientData.GlobalPerms.ItemAutoEquip ? FontAwesomeIcon.Surprise : FontAwesomeIcon.Ban,
            _clientData.GlobalPerms.ItemAutoEquip ? "Click to disable Gag Glamours (Global)" 
                                                     : "Click to enable Gag Glamours (Global)",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.ApplyGags), nameof(StickyPair.OwnPermAccess.ApplyGagsAllowed),
            OwnPerms.ApplyGags ? "Applying Gags Allowed" : "Applying Gags Restricted",
            OwnPerms.ApplyGags ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            OwnPerms.ApplyGags ? $"Click to revoke {PairNickOrAliasOrUID}'s access to apply Gags to you."
                               : $"Click to allow {PairNickOrAliasOrUID} to apply Gags to you.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.LockGags), nameof(StickyPair.OwnPermAccess.LockGagsAllowed),
            OwnPerms.LockGags ? "Locking Gags Allowed" : "Locking Gags Restricted",
            OwnPerms.LockGags ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            OwnPerms.LockGags ? $"Click to revoke {PairNickOrAliasOrUID}'s access to lock Gags on you."
                              : $"Click to allow {PairNickOrAliasOrUID} to lock Gags on you.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.MaxGagTime), nameof(StickyPair.OwnPermAccess.MaxGagTimeAllowed),
            "Max Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max duration {PairNickOrAliasOrUID} can lock your gags for",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting(nameof(OwnPerms.UnlockGags), nameof(StickyPair.OwnPermAccess.UnlockGagsAllowed),
            OwnPerms.UnlockGags ? "Unlocking Gags Allowed" : "Unlocking Gags Restricted",
            OwnPerms.UnlockGags ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Ban,
            OwnPerms.UnlockGags ? $"Click to revoke {PairNickOrAliasOrUID}'s access to unlock Gags from you."
                               : $"Click to allow {PairNickOrAliasOrUID} to unlock Gags from you.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.RemoveGags), nameof(StickyPair.OwnPermAccess.RemoveGagsAllowed),
            OwnPerms.RemoveGags ? "Removing Gags Allowed" : "Removing Gags Restricted",
            OwnPerms.RemoveGags ? FontAwesomeIcon.TimesCircle : FontAwesomeIcon.Ban,
            OwnPerms.RemoveGags ? $"Click to revoke {PairNickOrAliasOrUID}'s access to remove Gags from you."
                                : $"Click to allow {PairNickOrAliasOrUID} to remove Gags from you.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- RESTRAINT SET PERMISSIONS ----------- */
        ImGui.TextUnformatted("Restraint Set Permissions");

        DrawOwnSetting(nameof(_clientData.GlobalPerms.RestraintSetAutoEquip), nameof(StickyPair.OwnPermAccess.RestraintSetAutoEquipAllowed),
            _clientData.GlobalPerms.RestraintSetAutoEquip ? "Restraint Set Glamours Active" : "Restraint Set Glamours Inactive",
            _clientData.GlobalPerms.RestraintSetAutoEquip ? FontAwesomeIcon.ShopLock : FontAwesomeIcon.ShopSlash,
            _clientData.GlobalPerms.RestraintSetAutoEquip ? "Click to disable Restraint Set Glamours (Global)" 
                                                             : "Click to enable Restraint Set Glamours (Global)",
            OwnPerms.InHardcore,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.ApplyRestraintSets), nameof(StickyPair.OwnPermAccess.ApplyRestraintSetsAllowed),
            OwnPerms.ApplyRestraintSets ? "Apply Restraint Sets Allowed" : "Apply Restraint Sets Restricted",
            OwnPerms.ApplyRestraintSets ? FontAwesomeIcon.Tshirt : FontAwesomeIcon.Ban,
            OwnPerms.ApplyRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to apply Restraint Sets" 
                                        : $"Click to allow {PairNickOrAliasOrUID} to apply Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.LockRestraintSets), nameof(StickyPair.OwnPermAccess.LockRestraintSetsAllowed),
            OwnPerms.LockRestraintSets ? "Restraint Sets Locking Allowed" : "Restraint Sets Locking Restricted",
            OwnPerms.LockRestraintSets ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            OwnPerms.LockRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to lock your Restraint Sets" 
                                       : $"Click to allow {PairNickOrAliasOrUID} to lock your Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.MaxAllowedRestraintTime), nameof(StickyPair.OwnPermAccess.MaxAllowedRestraintTimeAllowed),
            "Max Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max duration {PairNickOrAliasOrUID} can lock your Restraint Sets for",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting(nameof(OwnPerms.UnlockRestraintSets), nameof(StickyPair.OwnPermAccess.UnlockRestraintSetsAllowed),
            OwnPerms.UnlockRestraintSets ? "Restraint SetS Unlocking Allowed" : "Restraint Sets Unlocking Restricted",
            OwnPerms.UnlockRestraintSets ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Ban,
            OwnPerms.UnlockRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to unlock your Restraint Sets" 
                                         : $"Click to allow {PairNickOrAliasOrUID} to unlock your Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.RemoveRestraintSets), nameof(StickyPair.OwnPermAccess.RemoveRestraintSetsAllowed),
            OwnPerms.RemoveRestraintSets ? "Restraint Sets Removal Allowed" : "Restraint Sets Removal Restricted",
            OwnPerms.RemoveRestraintSets ? FontAwesomeIcon.Female : FontAwesomeIcon.Ban,
            OwnPerms.RemoveRestraintSets ? $"Click to disallow {PairNickOrAliasOrUID} to remove your Restraint Sets" 
                                         : $"Click to allow {PairNickOrAliasOrUID} to remove your Restraint Sets",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- PUPPETEER PERMISSIONS ----------- */
        ImGui.TextUnformatted("Puppeteer Permissions");

        // reformat using the nameof
        DrawOwnSetting(nameof(OwnPerms.SitRequests), nameof(StickyPair.OwnPermAccess.SitRequestsAllowed),
            OwnPerms.SitRequests ? "Sit Requests Allowed" : "Sit Requests Restricted",
            OwnPerms.SitRequests ? FontAwesomeIcon.Chair : FontAwesomeIcon.Ban,
            OwnPerms.SitRequests ? $"Click to revoke {PairNickOrAliasOrUID}'s access to make you /groundsit, /sit or /changepose" 
                                 : $"Click to allow {PairNickOrAliasOrUID} to make you to /groundsit, /sit or /changepose (different to Hardcore)",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.MotionRequests), nameof(StickyPair.OwnPermAccess.MotionRequestsAllowed),
            OwnPerms.MotionRequests ? "Motion Requests Allowed" : "Motion Requests Restricted",
            OwnPerms.MotionRequests ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            OwnPerms.MotionRequests ? $"Click to revoke {PairNickOrAliasOrUID}'s access to make you execute emotes and expressions." +
                "and expressions" : $"Click to allow {PairNickOrAliasOrUID} to make you execute any emotes or expression.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.AliasRequests), nameof(StickyPair.OwnPermAccess.AliasRequestsAllowed),
            OwnPerms.AliasRequests ? "Alias Requests Allowed" : "Alias Requests Restricted",
            OwnPerms.AliasRequests ? FontAwesomeIcon.Scroll : FontAwesomeIcon.Ban,
            OwnPerms.AliasRequests ? $"Click to revoke {PairNickOrAliasOrUID}'s access to use any Alias you have for them." 
                                   : $"Click to allow {PairNickOrAliasOrUID} to use any Alias you have configured for them that is enabled.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.AllRequests), nameof(StickyPair.OwnPermAccess.AllRequestsAllowed),
            OwnPerms.AllRequests ? "All Requests Allowed" : "All Requests Restricted",
            OwnPerms.AllRequests ? FontAwesomeIcon.CheckDouble : FontAwesomeIcon.Ban,
            OwnPerms.AllRequests ? $"Click to revoke {PairNickOrAliasOrUID}'s access to make you execute any command." 
                                 : $"Click to allow {PairNickOrAliasOrUID} to make you execute any command.",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- MOODLES PERMISSIONS ----------- */
        ImGui.TextUnformatted("Moodles Permissions");

        // reformat to use Nameof() for property names.
        DrawOwnSetting(nameof(OwnPerms.AllowPositiveStatusTypes), nameof(StickyPair.OwnPermAccess.AllowPositiveStatusTypesAllowed),
           OwnPerms.AllowPositiveStatusTypes ? "Apply Positive Moodles Allowed" : "Apply Positive Moodles Restricted",
           OwnPerms.AllowPositiveStatusTypes ? FontAwesomeIcon.SmileBeam : FontAwesomeIcon.Ban,
           OwnPerms.AllowPositiveStatusTypes ? $"Click to disallow {PairNickOrAliasOrUID} to apply Moodles with a positive status" 
                                             : $"Click to allow {PairNickOrAliasOrUID} to apply Moodles with a positive status",
           OwnPerms.InHardcore,
           PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.AllowNegativeStatusTypes), nameof(StickyPair.OwnPermAccess.AllowNegativeStatusTypesAllowed),
            OwnPerms.AllowNegativeStatusTypes ? "Apply Negative Moodles Allowed" : "Apply Negative Moodles Restricted",
            OwnPerms.AllowNegativeStatusTypes ? FontAwesomeIcon.FrownOpen : FontAwesomeIcon.Ban,
            OwnPerms.AllowNegativeStatusTypes ? $"Click to disallow {PairNickOrAliasOrUID} to apply Moodles with a negative status" 
                                              : $"Click to allow {PairNickOrAliasOrUID} to apply Moodles with a negative status",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.AllowSpecialStatusTypes), nameof(StickyPair.OwnPermAccess.AllowSpecialStatusTypesAllowed),
            OwnPerms.AllowSpecialStatusTypes ? "Apply Special Moodles Allowed" : "Apply Special Moodles Restricted",
            OwnPerms.AllowSpecialStatusTypes ? FontAwesomeIcon.WandMagicSparkles : FontAwesomeIcon.Ban,
            OwnPerms.AllowSpecialStatusTypes ? $"Click to disallow {PairNickOrAliasOrUID} to apply Moodles with a special status" 
                                             : $"Click to allow {PairNickOrAliasOrUID} to apply Moodles with a special status",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.PairCanApplyOwnMoodlesToYou), nameof(StickyPair.OwnPermAccess.PairCanApplyOwnMoodlesToYouAllowed),
            OwnPerms.PairCanApplyOwnMoodlesToYou ? $"Apply Pair's Moodles Allowed" : $"Apply Pair's Moodles Restricted",
            OwnPerms.PairCanApplyOwnMoodlesToYou ? FontAwesomeIcon.PersonArrowUpFromLine : FontAwesomeIcon.Ban,
            OwnPerms.PairCanApplyOwnMoodlesToYou ? $"Click to disallow {PairNickOrAliasOrUID} to apply their own Moodles onto you" 
                                                 : $"Click to disallow {PairNickOrAliasOrUID} to apply their own Moodles onto you",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.PairCanApplyYourMoodlesToYou), nameof(StickyPair.OwnPermAccess.PairCanApplyYourMoodlesToYouAllowed),
            OwnPerms.PairCanApplyYourMoodlesToYou ? $"Apply Own Moodles Allowed" : $"Apply Own Moodles Restricted",
            OwnPerms.PairCanApplyYourMoodlesToYou ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Ban,
            OwnPerms.PairCanApplyYourMoodlesToYou ? $"Click to disallow {PairNickOrAliasOrUID} to apply your Moodles onto you" 
                                                  : $"Click to allow {PairNickOrAliasOrUID} to apply your Moodles onto you",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.MaxMoodleTime), nameof(StickyPair.OwnPermAccess.MaxMoodleTimeAllowed),
            "Max Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max duration {PairNickOrAliasOrUID} can apply Moodles to you for",
            true,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOwnSetting(nameof(OwnPerms.AllowPermanentMoodles), nameof(StickyPair.OwnPermAccess.AllowPermanentMoodlesAllowed),
            OwnPerms.AllowPermanentMoodles ? "Permanent Moodles Allowed" : "Permanent Moodles Restricted",
            OwnPerms.AllowPermanentMoodles ? FontAwesomeIcon.Infinity : FontAwesomeIcon.Ban,
            OwnPerms.AllowPermanentMoodles ? $"Click to disallow {PairNickOrAliasOrUID} to apply permanent Moodles onto to you" 
                                           : $"Click to allow {PairNickOrAliasOrUID} to apply permanent Moodles onto you",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.AllowRemovingMoodles), nameof(StickyPair.OwnPermAccess.AllowRemovingMoodlesAllowed),
            OwnPerms.AllowRemovingMoodles ? "Moodles Removal Allowed" : "Moodles Removal Restricted",
            OwnPerms.AllowRemovingMoodles ? FontAwesomeIcon.Eraser : FontAwesomeIcon.Ban,
            OwnPerms.AllowRemovingMoodles ? $"Click to disallow {PairNickOrAliasOrUID} to remove your Moodles" 
                                          : $"Click to allow {PairNickOrAliasOrUID} to remove your Moodles",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- TOYBOX PERMISSIONS ----------- */
        ImGui.TextUnformatted("Toybox Permissions");

        DrawOwnSetting(nameof(OwnPerms.CanToggleToyState), nameof(StickyPair.OwnPermAccess.CanToggleToyStateAllowed),
            OwnPerms.CanToggleToyState ? "Toggle Vibe Allowed" : "Toggle Vibe Restricted",
            OwnPerms.CanToggleToyState ? FontAwesomeIcon.PowerOff : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleToyState ? $"Click to disallow {PairNickOrAliasOrUID} to toggle your sex toys" : $"Click to allow {PairNickOrAliasOrUID} to toggle your sex toys",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.CanUseVibeRemote), nameof(StickyPair.OwnPermAccess.CanUseVibeRemoteAllowed),
            OwnPerms.CanUseVibeRemote ? "Vibe Control Allowed" : "Vibe Control Restricted",
            OwnPerms.CanUseVibeRemote ? FontAwesomeIcon.Mobile : FontAwesomeIcon.Ban,
            OwnPerms.CanUseVibeRemote ? $"Click to disallow {PairNickOrAliasOrUID} to control your sex toys" : $"Click to allow {PairNickOrAliasOrUID} to control your sex toys",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.CanToggleAlarms), nameof(StickyPair.OwnPermAccess.CanToggleAlarmsAllowed),
            OwnPerms.CanToggleAlarms ? "Toggle Alarms Allowed" : "Toggle Alarms Restricted",
            OwnPerms.CanToggleAlarms ? FontAwesomeIcon.Bell : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleAlarms ? $"Click to disallow {PairNickOrAliasOrUID} to toggle your alarms" : $"Click to allow {PairNickOrAliasOrUID} to toggle your alarms",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.CanSendAlarms), nameof(StickyPair.OwnPermAccess.CanSendAlarmsAllowed),
            OwnPerms.CanSendAlarms ? "Sending Alarms Allowed" : "Sending Alarms Restricted",
            OwnPerms.CanSendAlarms ? FontAwesomeIcon.FileExport : FontAwesomeIcon.Ban,
            OwnPerms.CanSendAlarms ? $"Click to disallow {PairNickOrAliasOrUID} to send you alarms" : $"Click to allow {PairNickOrAliasOrUID} to send you alarms",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.CanExecutePatterns), nameof(StickyPair.OwnPermAccess.CanExecutePatternsAllowed),
            OwnPerms.CanExecutePatterns ? "Start Patterns Allowed" : "Start Patterns Restricted",
            OwnPerms.CanExecutePatterns ? FontAwesomeIcon.LandMineOn : FontAwesomeIcon.Ban,
            OwnPerms.CanExecutePatterns ? $"Click to disallow {PairNickOrAliasOrUID} to start patterns" : $"Click to allow {PairNickOrAliasOrUID} to start patterns",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.CanStopPatterns), nameof(StickyPair.OwnPermAccess.CanStopPatternsAllowed),
            OwnPerms.CanStopPatterns ? "Stop Patterns Allowed" : "Stop Patterns Restricted",
            OwnPerms.CanStopPatterns ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.Ban,
            OwnPerms.CanExecutePatterns ? $"Click to disallow {PairNickOrAliasOrUID} to stop patterns" : $"Click to allow {PairNickOrAliasOrUID} to stop patterns",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.CanToggleTriggers), nameof(StickyPair.OwnPermAccess.CanToggleTriggersAllowed),
            OwnPerms.CanToggleTriggers ? "Toggle Triggers Allowed" : "Toggle Triggers Restricted",
            OwnPerms.CanToggleTriggers ? FontAwesomeIcon.FileMedicalAlt : FontAwesomeIcon.Ban,
            OwnPerms.CanToggleTriggers ? $"Click to disallow {PairNickOrAliasOrUID} to toggle your triggers" : $"Click to allow {PairNickOrAliasOrUID} to toggle your triggers",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- HARDCORE PERMISSIONS ----------- */
        ImGui.TextUnformatted("Hardcore Permissions");

        DrawOwnSetting(nameof(OwnPerms.InHardcore), string.Empty,
            OwnPerms.InHardcore ? $"In Hardcore Mode for {PairNickOrAliasOrUID}" : $"Not in Hardcore Mode",
            OwnPerms.InHardcore ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.InHardcore ? $"Disable Hardcore Mode for {PairNickOrAliasOrUID} (Use /safewordhardcore with your safeword to disable)" : $"Enable Hardcore Mode for {PairNickOrAliasOrUID}",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(OwnPerms.DevotionalStatesForPair), string.Empty,
            OwnPerms.DevotionalStatesForPair ? $"Toggles are Devotional from {PairNickOrAliasOrUID}" : $"Toggles are not Devotional",
            OwnPerms.DevotionalStatesForPair ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
            OwnPerms.DevotionalStatesForPair ? $"Click to make toggles from {PairNickOrAliasOrUID} not be Devotional" : $"Click to make toggles from {PairNickOrAliasOrUID} be Devotional",
            OwnPerms.InHardcore,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOwnSetting(nameof(_clientData.GlobalPerms.ForcedFollow), nameof(OwnPerms.AllowForcedFollow),
            _clientData.GlobalPerms.IsFollowing() ? "Forced to follow" : "Not forced to follow",
            _clientData.GlobalPerms.IsFollowing() ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            _clientData.GlobalPerms.IsFollowing() ? $"You are being forced to follow {PairNickOrAliasOrUID}" : $"You are not being forced to follow {PairNickOrAliasOrUID}",
            true, PermissionType.Hardcore);

        using (ImRaii.Group())
        {
            var icon = _clientData.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? FontAwesomeIcon.Ban : (OwnPerms.AllowForcedEmote ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Chair);
            var txt = _clientData.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? "Not forced to emote or sit" : "Forced to emote or sit";
            var tooltipStr = _clientData.GlobalPerms.ForcedEmoteState.NullOrEmpty() ? $"You are not being forced to emote or sit by {PairNickOrAliasOrUID}" : $"You are being forced to emote or sit by {PairNickOrAliasOrUID}";
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

        DrawOwnSetting(nameof(_clientData.GlobalPerms.ForcedStay), nameof(OwnPerms.AllowForcedToStay),
            _clientData.GlobalPerms.IsStaying() ? "Forced to stay" : "Not forced to stay",
            _clientData.GlobalPerms.IsStaying() ? FontAwesomeIcon.HouseLock : FontAwesomeIcon.Ban,
            _clientData.GlobalPerms.IsStaying() ? $"You are being forced to stay by {PairNickOrAliasOrUID}" : $"You are not being forced to stay by {PairNickOrAliasOrUID}",
            true, PermissionType.Hardcore);

        DrawOwnSetting(nameof(_clientData.GlobalPerms.ForcedBlindfold), nameof(OwnPerms.AllowBlindfold),
            _clientData.GlobalPerms.IsBlindfolded() ? "Blindfolded" : "Not Blindfolded",
            _clientData.GlobalPerms.IsBlindfolded() ? FontAwesomeIcon.Blind : FontAwesomeIcon.Ban,
            _clientData.GlobalPerms.IsBlindfolded() ? $"You have been blindfolded by {PairNickOrAliasOrUID}" : $"You have not been blindfolded by {PairNickOrAliasOrUID}",
            true, PermissionType.Hardcore);

        DrawOwnSetting(nameof(_clientData.GlobalPerms.ChatBoxesHidden), nameof(OwnPerms.AllowHidingChatBoxes),
            _clientData.GlobalPerms.IsChatHidden() ? "Chatbox is hidden" : "Chatbox is visible",
            _clientData.GlobalPerms.IsChatHidden() ? FontAwesomeIcon.CommentSlash : FontAwesomeIcon.Ban,
            _clientData.GlobalPerms.IsChatHidden() ? _clientData.GlobalPerms.ChatBoxesHidden.HardcorePermUID() + " has hidden your chatbox" : "Your chatbox is visible",
            true, PermissionType.Hardcore);

        DrawOwnSetting(nameof(_clientData.GlobalPerms.ChatInputHidden), nameof(OwnPerms.AllowHidingChatInput),
            _clientData.GlobalPerms.IsChatInputHidden() ? "Chat Input is Hidden" : "Chat Input is Visible",
            _clientData.GlobalPerms.IsChatInputHidden() ? FontAwesomeIcon.CommentSlash : FontAwesomeIcon.Ban,
            _clientData.GlobalPerms.IsChatInputHidden() ? _clientData.GlobalPerms.ChatInputHidden.HardcorePermUID() + " has hidden your chat input" : "Your chat input is visible",
            true, PermissionType.Hardcore);

        DrawOwnSetting(nameof(_clientData.GlobalPerms.ChatInputBlocked), nameof(OwnPerms.AllowChatInputBlocking),
            _clientData.GlobalPerms.IsChatInputBlocked() ? "Chat Input Unavailable" : "Chat Input Available",
            _clientData.GlobalPerms.IsChatInputBlocked() ? FontAwesomeIcon.CommentDots : FontAwesomeIcon.Ban,
            _clientData.GlobalPerms.IsChatInputBlocked() ? PairNickOrAliasOrUID + " has blocked your chat input access" : "You are chat input access is not being blocked",
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
                    if (_clientData.GlobalPerms is null) return;
                    DrawOwnPermission(permissionType, _clientData.GlobalPerms, textLabel, icon, tooltipStr, isLocked,
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
                // have a special case, where we mark the button as disabled if _clientData.GlobalPerms.LiveChatGarblerLocked is true
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
            string timeSpanString = ((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!).ToGsRemainingTime() ?? "0d0h0m0s";

            using (var group = ImRaii.Group())
            {
                var id = label + "##" + permissionName;
                // draw the iconTextButton and checkbox beside it. Because we are in control, unless in hardcore, this should never be disabled.
                if (_uiShared.IconInputText(id, icon, label, "0d0h0m0s", ref timeSpanString, 32, IconButtonTextWidth * .55f, true, false)) { }
                // Set the permission once deactivated. If invalid, set to default.
                if (ImGui.IsItemDeactivatedAfterEdit()
                    && timeSpanString != ((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!).ToGsRemainingTime())
                {
                    // attempt to parse the string back into a valid timespan.
                    if (GsPadlockEx.TryParseTimeSpan(timeSpanString, out TimeSpan result))
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
            string currValState = (string)(_clientData.GlobalPerms?.GetType().GetProperty(permissionName)?.GetValue(_clientData?.GlobalPerms) ?? string.Empty);

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
