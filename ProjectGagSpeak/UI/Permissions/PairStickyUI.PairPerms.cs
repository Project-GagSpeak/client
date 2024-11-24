using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Enums;
using GagspeakAPI.Dto.Permissions;
using ImGuiNET;
using System.Security;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using GagSpeak.WebAPI;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// </summary>
public partial class PairStickyUI
{
    public void DrawPairPermsForClient()
    {
        /* ----------- GLOBAL SETTINGS ----------- */
        ImGui.TextUnformatted("Global Settings");

        DrawOtherPairSetting("LiveChatGarblerActive", "LiveChatGarblerActiveAllowed", // permission name and permission access name
            StickyPair.PairGlobals.LiveChatGarblerActive ? (PairUID+"'s Chat Garbler is Active") : (PairUID+"'s Chat Garbler is Inactive"),
            StickyPair.PairGlobals.LiveChatGarblerActive ? FontAwesomeIcon.MicrophoneSlash : FontAwesomeIcon.Microphone,
            StickyPair.PairPermAccess.LiveChatGarblerActiveAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LiveChatGarblerActiveAllowed,
            PermissionType.Global, PermissionValueType.YesNo); // permission type and value type

        DrawOtherPairSetting("LiveChatGarblerLocked", "LiveChatGarblerLockedAllowed",
            StickyPair.PairGlobals.LiveChatGarblerLocked ? (PairUID + "'s Chat Garbler is Locked") : (PairUID + "'s Chat Garbler is Unlocked"),
            StickyPair.PairGlobals.LiveChatGarblerLocked ? FontAwesomeIcon.Key : FontAwesomeIcon.UnlockAlt,
            StickyPair.PairPermAccess.LiveChatGarblerLockedAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LiveChatGarblerLockedAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOtherPairSetting("LockToyboxUI", "LockToyboxUIAllowed",
            StickyPair.PairGlobals.LockToyboxUI ? (PairUID + "'s Toybox UI is Restricted") : (PairUID + "'s Toybox UI is Accessible"),
            StickyPair.PairGlobals.LockToyboxUI ? FontAwesomeIcon.Box : FontAwesomeIcon.BoxOpen,
            StickyPair.PairPermAccess.LockToyboxUIAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LockToyboxUIAllowed,
            PermissionType.Global, PermissionValueType.YesNo);
        
        ImGui.Separator();
        /* ----------- GAG PERMISSIONS ----------- */
        ImGui.TextUnformatted("Gag Permissions");


        DrawOtherPairSetting("GagFeatures", "GagFeaturesAllowed",
            StickyPair.PairPerms.GagFeatures ? (PairUID + " enabled Gag Interactions") : (PairUID + " disabled Gag Interactions"),
            StickyPair.PairPerms.GagFeatures ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.GagFeaturesAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.GagFeaturesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("ItemAutoEquip", "ItemAutoEquipAllowed",
            StickyPair.PairGlobals.ItemAutoEquip ? (PairUID + " has Gag Glamours Enabled") : (PairUID + " has Gag Glamours Disabled"),
            StickyPair.PairGlobals.ItemAutoEquip ? FontAwesomeIcon.Surprise : FontAwesomeIcon.MehBlank,
            StickyPair.PairPermAccess.ItemAutoEquipAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.ItemAutoEquipAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOtherPairSetting("MaxLockTime", "MaxLockTimeAllowed",
            StickyPair.PairPerms.MaxLockTime == TimeSpan.Zero ? "Set Max Lock Time" : "Change Max Lock Time",
            FontAwesomeIcon.HourglassHalf,
            $"Max time {PairNickOrAliasOrUID} can lock your gags for.",
            StickyPair.PairPermAccess.MaxLockTimeAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOtherPairSetting("ExtendedLockTimes", "ExtendedLockTimesAllowed",
            StickyPair.PairPerms.ExtendedLockTimes ? (PairUID + " allows Extended Locks") : (PairUID + " prevents Extended Locks"),
            StickyPair.PairPerms.ExtendedLockTimes ? FontAwesomeIcon.Stopwatch : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.ExtendedLockTimesAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.ExtendedLockTimesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("OwnerLocks", "OwnerLocksAllowed",
            StickyPair.PairPerms.OwnerLocks ? (PairUID + " allows Owner Locks") : (PairUID + " prevents Owner Locks"),
            StickyPair.PairPerms.OwnerLocks ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.OwnerLocksAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.OwnerLocksAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("DevotionalLocks", "DevotionalLocksAllowed",
            StickyPair.PairPerms.DevotionalLocks ? (PairUID + " allows Devotional Locks") : (PairUID + " prevents Devotional Locks"),
            StickyPair.PairPerms.DevotionalLocks ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.DevotionalLocksAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.DevotionalLocksAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);


        ImGui.Separator();
        /* ----------- RESTRAINT SET PERMISSIONS ----------- */
        ImGui.TextUnformatted("Restraint Set Permissions");

        // Rewrite all of the below functions but using the format above
        DrawOtherPairSetting("RestraintSetAutoEquip", "RestraintSetAutoEquipAllowed",
            StickyPair.PairGlobals.RestraintSetAutoEquip ? (PairUID + " has Restraint Glamours Enabled") : (PairUID + " has Restraint Glamours Disabled"),
            StickyPair.PairGlobals.RestraintSetAutoEquip ? FontAwesomeIcon.Tshirt : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.RestraintSetAutoEquipAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.RestraintSetAutoEquipAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOtherPairSetting("ApplyRestraintSets", "ApplyRestraintSetsAllowed",
            StickyPair.PairPerms.ApplyRestraintSets ? (PairUID + " allows Applying Restraints") : (PairUID + " prevents Applying Restraints"),
            StickyPair.PairPerms.ApplyRestraintSets ? FontAwesomeIcon.Female : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.ApplyRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.ApplyRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("LockRestraintSets", "LockRestraintSetsAllowed",
            StickyPair.PairPerms.LockRestraintSets ? (PairUID + " allows Locking Restraints") : (PairUID + " prevents Locking Restraints"),
            StickyPair.PairPerms.LockRestraintSets ? FontAwesomeIcon.Lock : FontAwesomeIcon.ShopSlash,
            StickyPair.PairPermAccess.LockRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LockRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("MaxAllowedRestraintTime", "MaxAllowedRestraintTimeAllowed",
            StickyPair.PairPerms.MaxAllowedRestraintTime == TimeSpan.Zero ? "Set Max Lock Time" : "Change Max Lock Time",
            FontAwesomeIcon.HourglassHalf,
            StickyPair.PairPermAccess.MaxAllowedRestraintTimeAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.MaxAllowedRestraintTimeAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOtherPairSetting("UnlockRestraintSets", "UnlockRestraintSetsAllowed",
            StickyPair.PairPerms.UnlockRestraintSets ? (PairUID + " allows Unlocking Restraints") : (PairUID + " prevents Unlocking Restraints"),
            StickyPair.PairPerms.UnlockRestraintSets ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.UnlockRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.UnlockRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("RemoveRestraintSets", "RemoveRestraintSetsAllowed",
            StickyPair.PairPerms.RemoveRestraintSets ? (PairUID + " allows Removing Restraints") : (PairUID + " prevents Removing Restraints"),
            StickyPair.PairPerms.RemoveRestraintSets ? FontAwesomeIcon.Key : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.RemoveRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.RemoveRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- PUPPETEER PERMISSIONS ----------- */
        ImGui.TextUnformatted("Puppeteer Permissions");

        DrawOtherPairSetting("AllowSitRequests", "AllowSitRequestsAllowed",
            StickyPair.PairPerms.AllowSitRequests ? (PairUID + " allows Sit Requests") : (PairUID + " prevents Sit Requests"),
            StickyPair.PairPerms.AllowSitRequests ? FontAwesomeIcon.Chair : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowSitRequestsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowSitRequestsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("AllowMotionRequests", "AllowMotionRequestsAllowed",
            StickyPair.PairPerms.AllowMotionRequests ? (PairUID + " allows Motion Requests") : (PairUID + " prevents Motion Requests"),
            StickyPair.PairPerms.AllowMotionRequests ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowMotionRequestsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowMotionRequestsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("AllowAllRequests", "AllowAllRequestsAllowed",
            StickyPair.PairPerms.AllowAllRequests ? (PairUID + " allows All Requests") : (PairUID + " prevents All Requests"),
            StickyPair.PairPerms.AllowAllRequests ? FontAwesomeIcon.Unlock : FontAwesomeIcon.Lock,
            StickyPair.PairPermAccess.AllowAllRequestsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowAllRequestsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- MOODLES PERMISSIONS ----------- */
        ImGui.TextUnformatted("Moodles Permissions");

        DrawOtherPairSetting("AllowPositiveStatusTypes", "AllowPositiveStatusTypesAllowed",
            StickyPair.PairPerms.AllowPositiveStatusTypes ? (PairUID + " allows Positive Moodles") : (PairUID + " prevents Positive Moodles"),
            StickyPair.PairPerms.AllowPositiveStatusTypes ? FontAwesomeIcon.SmileBeam : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowPositiveStatusTypesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowPositiveStatusTypesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("AllowNegativeStatusTypes", "AllowNegativeStatusTypesAllowed",
            StickyPair.PairPerms.AllowNegativeStatusTypes ? (PairUID + " allows Negative Moodles") : (PairUID + " prevents Negative Moodles"),
            StickyPair.PairPerms.AllowNegativeStatusTypes ? FontAwesomeIcon.FrownOpen : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowNegativeStatusTypesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowNegativeStatusTypesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("AllowSpecialStatusTypes", "AllowSpecialStatusTypesAllowed",
            StickyPair.PairPerms.AllowSpecialStatusTypes ? (PairUID + " allows Special Moodles") : (PairUID + " prevents Special Moodles"),
            StickyPair.PairPerms.AllowSpecialStatusTypes ? FontAwesomeIcon.WandMagicSparkles : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowSpecialStatusTypesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowSpecialStatusTypesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("PairCanApplyOwnMoodlesToYou", "PairCanApplyOwnMoodlesToYouAllowed",
            StickyPair.PairPerms.PairCanApplyOwnMoodlesToYou ? (PairUID + " allows applying your Moodles") : (PairUID + " prevents applying your Moodles"),
            StickyPair.PairPerms.PairCanApplyOwnMoodlesToYou ? FontAwesomeIcon.PersonArrowUpFromLine : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.PairCanApplyOwnMoodlesToYouAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.PairCanApplyOwnMoodlesToYouAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("PairCanApplyYourMoodlesToYou", "PairCanApplyYourMoodlesToYouAllowed",
            StickyPair.PairPerms.PairCanApplyYourMoodlesToYou ? (PairUID + " allows applying their Moodles") : (PairUID + " prevents applying their Moodles"),
            StickyPair.PairPerms.PairCanApplyYourMoodlesToYou ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.PairCanApplyYourMoodlesToYouAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.PairCanApplyYourMoodlesToYouAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("MaxMoodleTime", "MaxMoodleTimeAllowed",
            "Max Moodles Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max Duration a Moodle can be applied for.",
            StickyPair.PairPermAccess.MaxMoodleTimeAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOtherPairSetting("AllowPermanentMoodles", "AllowPermanentMoodlesAllowed",
            StickyPair.PairPerms.AllowPermanentMoodles ? (PairUID + " allows Permanent Moodles") : (PairUID + " prevents Permanent Moodles"),
            StickyPair.PairPerms.AllowPermanentMoodles ? FontAwesomeIcon.Infinity : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowPermanentMoodlesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowPermanentMoodlesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("AllowRemovingMoodles", "AllowRemovingMoodlesAllowed",
            StickyPair.PairPerms.AllowRemovingMoodles ? (PairUID + " allowing Removal of Moodles") : (PairUID + " prevents Removal of Moodles"),
            StickyPair.PairPerms.AllowRemovingMoodles ? FontAwesomeIcon.Eraser : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowRemovingMoodlesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowRemovingMoodlesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- TOYBOX PERMISSIONS ----------- */
        ImGui.TextUnformatted("Toybox Permissions");

        DrawOtherPairSetting("CanToggleToyState", "CanToggleToyStateAllowed",
            StickyPair.PairPerms.CanToggleToyState ? (PairUID + " allows Toy State Changing") : (PairUID + " prevents Toy State Changing"),
            StickyPair.PairPerms.CanToggleToyState ? FontAwesomeIcon.PowerOff : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanToggleToyStateAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanToggleToyStateAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("CanUseVibeRemote", "CanUseVibeRemoteAllowed",
            StickyPair.PairPerms.CanUseVibeRemote ? (PairUID + " allows Vibe Control") : (PairUID + " prevents Vibe Control"),
            StickyPair.PairPerms.CanUseVibeRemote ? FontAwesomeIcon.Mobile : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanUseVibeRemoteAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanUseVibeRemoteAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("CanToggleAlarms", "CanToggleAlarmsAllowed",
            StickyPair.PairPerms.CanToggleAlarms ? (PairUID + " allows Alarm Toggling") : (PairUID + " prevents Alarm Toggling"),
            StickyPair.PairPerms.CanToggleAlarms ? FontAwesomeIcon.Bell : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanToggleAlarmsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanToggleAlarmsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("CanSendAlarms", "CanSendAlarmsAllowed",
            StickyPair.PairPerms.CanSendAlarms ? (PairUID + " allows sending Alarms") : (PairUID + " prevents sending Alarms"),
            StickyPair.PairPerms.CanSendAlarms ? FontAwesomeIcon.FileExport : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanSendAlarmsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanSendAlarmsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("CanExecutePatterns", "CanExecutePatternsAllowed",
            StickyPair.PairPerms.CanExecutePatterns ? (PairUID + " allows Pattern Execution") : (PairUID + " prevents Pattern Execution"),
            StickyPair.PairPerms.CanExecutePatterns ? FontAwesomeIcon.LandMineOn : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanExecutePatternsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanExecutePatternsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("CanStopPatterns", "CanStopPatternsAllowed",
            StickyPair.PairPerms.CanStopPatterns ? (PairUID + " allows stopping Patterns") : (PairUID + " prevents stopping Patterns"),
            StickyPair.PairPerms.CanStopPatterns ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanStopPatternsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanStopPatternsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting("CanToggleTriggers", "CanToggleTriggersAllowed",
            StickyPair.PairPerms.CanToggleTriggers ? (PairUID + " allows Toggling Triggers") : (PairUID + " prevents Toggling Triggers"),
            StickyPair.PairPerms.CanToggleTriggers ? FontAwesomeIcon.FileMedicalAlt : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanToggleTriggersAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanToggleTriggersAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);
    }

    /// <summary>
    /// The primary call for displaying a setting for the client permissions.
    /// <para>
    /// These are formatted slightly different than the ClientPairPerms. Instead of having an interactable checkbox, 
    /// you'll see a colored lock/unlock icon. Red lock indicates they have not given you edit access, and green unlock means they have.
    /// </para>
    /// <para>
    /// Additionally, the condition for modifying the permission is not based on hardcore mode, but instead the edit access permission.
    /// </para>
    /// </summary>
    /// <param name="permissionName"> The name of the unique pair perm in string format. </param>
    /// <param name="permissionAccessName"> The name of the pair perm edit access in string format </param>
    /// <param name="textLabel"> The text to display beside the icon </param>
    /// <param name="icon"> The icon to display to the left of the text. </param>
    /// <param name="canChange"> If the permission (not edit access) can be changed. </param>
    /// <param name="tooltipStr"> the tooltip to display when hovered. </param>
    /// <param name="permissionType"> If the permission is a global perm, unique pair perm, or access permission. </param>
    /// <param name="permissionValueType"> what permission type it is (string, char, timespan, boolean) </param>
    private void DrawOtherPairSetting(string permissionName, string permissionAccessName, string textLabel, FontAwesomeIcon icon,
        string tooltipStr, bool canChange, PermissionType permissionType, PermissionValueType permissionValueType)
    {
        try
        {
            switch (permissionType)
            {
                case PermissionType.Global:
                    DrawOtherPairPermission(permissionType, StickyPair.PairGlobals, textLabel, icon, tooltipStr, canChange,
                        permissionName, permissionValueType);
                    break;
                case PermissionType.UniquePairPerm:
                    DrawOtherPairPermission(permissionType, StickyPair.PairPerms, textLabel, icon, tooltipStr, canChange,
                        permissionName, permissionValueType);
                    break;
                // this case should technically never be called for this particular instance.
                case PermissionType.UniquePairPermEditAccess:
                    DrawOtherPairPermission(permissionType, StickyPair.PairPermAccess, textLabel, icon, tooltipStr, canChange,
                        permissionName, permissionValueType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update permissions :: {ex}");
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
    private void DrawOtherPairPermission(PermissionType permissionType, object permissionSet, string label,
        FontAwesomeIcon icon, string tooltip, bool hasAccess, string permissionName, PermissionValueType type)
    {

        // firstly, if the permission value type is a boolean, then process handling the change as a true/false.
        if (type == PermissionValueType.YesNo)
        {
            // localize the object as a boolean value from its property name.
            bool currValState = (bool)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!;
            // draw the iconTextButton and checkbox beside it. Because we are in control, unless in hardcore, this should never be disabled.
            using (var group = ImRaii.Group())
            {
                // have a special case, where we mark the button as disabled if StickyPair.PairGlobals.LiveChatGarblerLocked is true
                if (_uiShared.IconTextButton(icon, label, IconButtonTextWidth, true, !hasAccess))
                {
                    SetOtherPairPermission(permissionType, permissionName, !currValState);
                }
                UiSharedService.AttachToolTip(tooltip);
                // display the respective lock/unlock icon based on the edit access permission.
                _uiShared.BooleanToColoredIcon(hasAccess, true, FontAwesomeIcon.Unlock, FontAwesomeIcon.Lock);
                // attach tooltip to it.
                UiSharedService.AttachToolTip(!hasAccess
                    ? ("Only " + PairNickOrAliasOrUID + " may update this setting. (They have not given you override access)")
                    : (PairNickOrAliasOrUID + " has allowed you to override their permission state at will."));
            }
        }
        // next, handle it if it is a timespan value.
        if (type == PermissionValueType.TimeSpan)
        {
            // attempt to parse the timespan value to a string.
            string timeSpanString = _uiShared.TimeSpanToString((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!) ?? "0d0h0m0s";

            using (var group = ImRaii.Group())
            {
                var id = label + "##" + permissionName;
                // draw the iconTextButton and checkbox beside it. Because we are in control, unless in hardcore, this should never be disabled.
                if (_uiShared.IconInputText(id, icon, label, "format 0d0h0m0s...", ref timeSpanString, 32, IconButtonTextWidth*.5f, true, !hasAccess)) { }
                // Set the permission once deactivated. If invalid, set to default.
                if (ImGui.IsItemDeactivatedAfterEdit()
                    && timeSpanString != _uiShared.TimeSpanToString((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!))
                {
                    // attempt to parse the string back into a valid timespan.
                    if (_uiShared.TryParseTimeSpan(timeSpanString, out TimeSpan result))
                    {
                        ulong ticks = (ulong)result.Ticks;
                        SetOtherPairPermission(permissionType, permissionName, ticks);
                    }
                    else
                    {
                        // find some way to print this to the chat or something.
                        _logger.LogWarning("You tried to set an invalid timespan format. Please use the format 0d0h0m0s");
                        timeSpanString = "0d0h0m0s";
                    }
                }
                UiSharedService.AttachToolTip(tooltip);
                ImGui.SameLine(IconButtonTextWidth + ImGui.GetStyle().ItemSpacing.X);
                // display the respective lock/unlock icon based on the edit access permission.
                _uiShared.BooleanToColoredIcon(hasAccess, false, FontAwesomeIcon.Unlock, FontAwesomeIcon.Lock);
                // attach tooltip to it.
                UiSharedService.AttachToolTip(!hasAccess
                    ? ("Only " + PairNickOrAliasOrUID + " may update this setting. (They have not given you override access)")
                    : (PairNickOrAliasOrUID + " has allowed you to override their permission state at will."));
            }
        }
    }

    /// <summary>
    /// Send the updated permission we made for ourselves to the server.
    /// </summary>
    /// <param name="permissionType"> If Global, UniquePairPerm, or EditAccessPerm. </param>
    /// <param name="permissionName"> the attribute of the object we are changing</param>
    /// <param name="newValue"> New value to set. </param>
    private void SetOtherPairPermission(PermissionType permissionType, string permissionName, object newValue)
    {
        // Call the update to the server.
        switch (permissionType)
        {
            case PermissionType.Global:
                {
                    _logger.LogTrace($"Updated Other pair's global permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOtherGlobalPerm(new UserGlobalPermChangeDto(StickyPair.UserData,
                        new KeyValuePair<string, object>(permissionName, newValue), MainHub.PlayerUserData));
                }
                break;
            case PermissionType.UniquePairPerm:
                {
                    _logger.LogTrace($"Updated other pair's unique pair permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOtherPairPerm(new UserPairPermChangeDto(StickyPair.UserData,
                        new KeyValuePair<string, object>(permissionName, newValue)));
                }
                break;
        }
    }
}
