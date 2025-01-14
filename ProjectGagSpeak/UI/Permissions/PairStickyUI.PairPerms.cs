using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;

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

        DrawOtherPairSetting(nameof(PairGlobals.LiveChatGarblerActive), nameof(StickyPair.PairPermAccess.LiveChatGarblerActiveAllowed),
            StickyPair.PairGlobals.LiveChatGarblerActive ? (PairNickOrAliasOrUID + "'s Chat Garbler is Active") : (PairNickOrAliasOrUID + "'s Chat Garbler is Inactive"),
            StickyPair.PairGlobals.LiveChatGarblerActive ? FontAwesomeIcon.MicrophoneSlash : FontAwesomeIcon.Microphone,
            StickyPair.PairPermAccess.LiveChatGarblerActiveAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LiveChatGarblerActiveAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairGlobals.LiveChatGarblerLocked), nameof(StickyPair.PairPermAccess.LiveChatGarblerLockedAllowed),
            StickyPair.PairGlobals.LiveChatGarblerLocked ? (PairNickOrAliasOrUID + "'s Chat Garbler is Locked") : (PairNickOrAliasOrUID + "'s Chat Garbler is Unlocked"),
            StickyPair.PairGlobals.LiveChatGarblerLocked ? FontAwesomeIcon.Key : FontAwesomeIcon.UnlockAlt,
            StickyPair.PairPermAccess.LiveChatGarblerLockedAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LiveChatGarblerLockedAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairGlobals.LockToyboxUI), nameof(StickyPair.PairPermAccess.LockToyboxUIAllowed),
            StickyPair.PairGlobals.LockToyboxUI ? (PairNickOrAliasOrUID + "'s Toybox UI is Restricted") : (PairNickOrAliasOrUID + "'s Toybox UI is Accessible"),
            StickyPair.PairGlobals.LockToyboxUI ? FontAwesomeIcon.Box : FontAwesomeIcon.BoxOpen,
            StickyPair.PairPermAccess.LockToyboxUIAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LockToyboxUIAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- GAG PERMISSIONS ----------- */
        ImGui.TextUnformatted("Padlock Permissions");

        // draw settings for Permanent Locks, Owner Locks, and Devotional Locks.
        DrawOtherPairSetting(nameof(PairPerms.PermanentLocks), nameof(StickyPair.PairPermAccess.PermanentLocksAllowed),
            StickyPair.PairPerms.PermanentLocks ? (PairNickOrAliasOrUID + " allows Permanent Locks") : (PairNickOrAliasOrUID + " prevents Permanent Locks"),
            StickyPair.PairPerms.PermanentLocks ? FontAwesomeIcon.Infinity : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.PermanentLocksAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.")
                                                            : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.PermanentLocksAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.OwnerLocks), nameof(StickyPair.PairPermAccess.OwnerLocksAllowed),
            StickyPair.PairPerms.OwnerLocks ? (PairNickOrAliasOrUID + " allows Owner Locks") : (PairNickOrAliasOrUID + " prevents Owner Locks"),
            StickyPair.PairPerms.OwnerLocks ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.OwnerLocksAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.")
                                                        : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.OwnerLocksAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.DevotionalLocks), nameof(StickyPair.PairPermAccess.DevotionalLocksAllowed),
            StickyPair.PairPerms.DevotionalLocks ? (PairNickOrAliasOrUID + " allows Devotional Locks") : (PairNickOrAliasOrUID + " prevents Devotional Locks"),
            StickyPair.PairPerms.DevotionalLocks ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.DevotionalLocksAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.")
                                                             : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.DevotionalLocksAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- GAG PERMISSIONS ----------- */
        ImGui.TextUnformatted("Gag Permissions");

        DrawOtherPairSetting(nameof(PairGlobals.ItemAutoEquip), nameof(StickyPair.PairPermAccess.ItemAutoEquipAllowed),
            StickyPair.PairGlobals.ItemAutoEquip ? (PairNickOrAliasOrUID + " has Gag Glamours Enabled") : (PairNickOrAliasOrUID + " has Gag Glamours Disabled"),
            StickyPair.PairGlobals.ItemAutoEquip ? FontAwesomeIcon.Surprise : FontAwesomeIcon.MehBlank,
            StickyPair.PairPermAccess.ItemAutoEquipAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state. [Global]") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.ItemAutoEquipAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        // draw settings for ApplyGags, LockGags, MaxGagTime, UnlockGags, and RemoveGags.
        DrawOtherPairSetting(nameof(PairPerms.ApplyGags), nameof(StickyPair.PairPermAccess.ApplyGagsAllowed),
            StickyPair.PairPerms.ApplyGags ? (PairNickOrAliasOrUID + " allows Applying Gags") : (PairNickOrAliasOrUID + " prevents Applying Gags"),
            StickyPair.PairPerms.ApplyGags ? FontAwesomeIcon.Mask : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.ApplyGagsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.")
                                                      : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.ApplyGagsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.LockGags), nameof(StickyPair.PairPermAccess.LockGagsAllowed),
            StickyPair.PairPerms.LockGags ? (PairNickOrAliasOrUID + " allows Locking Gags") : (PairNickOrAliasOrUID + " prevents Locking Gags"),
            StickyPair.PairPerms.LockGags ? FontAwesomeIcon.Lock : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.LockGagsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.")
                                                     : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LockGagsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.MaxGagTime), nameof(StickyPair.PairPermAccess.MaxGagTimeAllowed),
            StickyPair.PairPerms.MaxGagTime == TimeSpan.Zero ? "Set Max Lock Time" : "Change Max Lock Time",
            FontAwesomeIcon.HourglassHalf,
            $"Max time {PairNickOrAliasOrUID} can lock your gags for.",
            StickyPair.PairPermAccess.MaxGagTimeAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOtherPairSetting(nameof(PairPerms.UnlockGags), nameof(StickyPair.PairPermAccess.UnlockGagsAllowed),
            StickyPair.PairPerms.UnlockGags ? (PairNickOrAliasOrUID + " allows Unlocking Gags") : (PairNickOrAliasOrUID + " prevents Unlocking Gags"),
            StickyPair.PairPerms.UnlockGags ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.UnlockGagsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.")
                                                       : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.UnlockGagsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.RemoveGags), nameof(StickyPair.PairPermAccess.RemoveGagsAllowed),
            StickyPair.PairPerms.RemoveGags ? (PairNickOrAliasOrUID + " allows Removing Gags") : (PairNickOrAliasOrUID + " prevents Removing Gags"),
            StickyPair.PairPerms.RemoveGags ? FontAwesomeIcon.Key : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.RemoveGagsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.")
                                                        : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.RemoveGagsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- RESTRAINT SET PERMISSIONS ----------- */
        ImGui.TextUnformatted("Restraint Set Permissions");

        // draw settings for ApplyRestraintSets, LockRestraintSets, MaxAllowedRestraintTime, UnlockRestraintSets, and RemoveRestraintSets all using nameof() for the first parameters.
        DrawOtherPairSetting(nameof(PairGlobals.RestraintSetAutoEquip), nameof(StickyPair.PairPermAccess.RestraintSetAutoEquipAllowed),
            StickyPair.PairGlobals.RestraintSetAutoEquip ? (PairNickOrAliasOrUID + " has Restraint Glamours Enabled") : (PairNickOrAliasOrUID + " has Restraint Glamours Disabled"),
            StickyPair.PairGlobals.RestraintSetAutoEquip ? FontAwesomeIcon.Tshirt : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.RestraintSetAutoEquipAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.RestraintSetAutoEquipAllowed,
            PermissionType.Global, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.ApplyRestraintSets), nameof(StickyPair.PairPermAccess.ApplyRestraintSetsAllowed),
            StickyPair.PairPerms.ApplyRestraintSets ? (PairNickOrAliasOrUID + " allows Applying Restraints") : (PairNickOrAliasOrUID + " prevents Applying Restraints"),
            StickyPair.PairPerms.ApplyRestraintSets ? FontAwesomeIcon.Female : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.ApplyRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.ApplyRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.LockRestraintSets), nameof(StickyPair.PairPermAccess.LockRestraintSetsAllowed),
            StickyPair.PairPerms.LockRestraintSets ? (PairNickOrAliasOrUID + " allows Locking Restraints") : (PairNickOrAliasOrUID + " prevents Locking Restraints"),
            StickyPair.PairPerms.LockRestraintSets ? FontAwesomeIcon.Lock : FontAwesomeIcon.ShopSlash,
            StickyPair.PairPermAccess.LockRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.LockRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.MaxAllowedRestraintTime), nameof(StickyPair.PairPermAccess.MaxAllowedRestraintTimeAllowed),
            StickyPair.PairPerms.MaxAllowedRestraintTime == TimeSpan.Zero ? "Set Max Lock Time" : "Change Max Lock Time",
            FontAwesomeIcon.HourglassHalf,
            StickyPair.PairPermAccess.MaxAllowedRestraintTimeAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.MaxAllowedRestraintTimeAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOtherPairSetting(nameof(PairPerms.UnlockRestraintSets), nameof(StickyPair.PairPermAccess.UnlockRestraintSetsAllowed),
            StickyPair.PairPerms.UnlockRestraintSets ? (PairNickOrAliasOrUID + " allows Unlocking Restraints") : (PairNickOrAliasOrUID + " prevents Unlocking Restraints"),
            StickyPair.PairPerms.UnlockRestraintSets ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.UnlockRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.UnlockRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.RemoveRestraintSets), nameof(StickyPair.PairPermAccess.RemoveRestraintSetsAllowed),
            StickyPair.PairPerms.RemoveRestraintSets ? (PairNickOrAliasOrUID + " allows Removing Restraints") : (PairNickOrAliasOrUID + " prevents Removing Restraints"),
            StickyPair.PairPerms.RemoveRestraintSets ? FontAwesomeIcon.Key : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.RemoveRestraintSetsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.RemoveRestraintSetsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- PUPPETEER PERMISSIONS ----------- */
        ImGui.TextUnformatted("Puppeteer Permissions");

        DrawOtherPairSetting(nameof(PairPerms.SitRequests), nameof(StickyPair.PairPermAccess.SitRequestsAllowed),
            StickyPair.PairPerms.SitRequests ? (PairNickOrAliasOrUID + " allows Sit Requests") : (PairNickOrAliasOrUID + " prevents Sit Requests"),
            StickyPair.PairPerms.SitRequests ? FontAwesomeIcon.Chair : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.SitRequestsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.SitRequestsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.MotionRequests), nameof(StickyPair.PairPermAccess.MotionRequestsAllowed),
            StickyPair.PairPerms.MotionRequests ? (PairNickOrAliasOrUID + " allows Motion Requests") : (PairNickOrAliasOrUID + " prevents Motion Requests"),
            StickyPair.PairPerms.MotionRequests ? FontAwesomeIcon.Walking : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.MotionRequestsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.MotionRequestsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.AliasRequests), nameof(StickyPair.PairPermAccess.AliasRequestsAllowed),
            StickyPair.PairPerms.AliasRequests ? (PairNickOrAliasOrUID + " allows Alias Requests") : (PairNickOrAliasOrUID + " prevents Alias Requests"),
            StickyPair.PairPerms.AliasRequests ? FontAwesomeIcon.Scroll : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AliasRequestsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AliasRequestsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.AllRequests), nameof(StickyPair.PairPermAccess.AllRequestsAllowed),
            StickyPair.PairPerms.AllRequests ? (PairNickOrAliasOrUID + " allows All Requests") : (PairNickOrAliasOrUID + " prevents All Requests"),
            StickyPair.PairPerms.AllRequests ? FontAwesomeIcon.CheckDouble : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllRequestsAllowed ? ("Press to Toggle" + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllRequestsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- MOODLES PERMISSIONS ----------- */
        ImGui.TextUnformatted("Moodles Permissions");

        DrawOtherPairSetting(nameof(PairPerms.AllowPositiveStatusTypes), nameof(StickyPair.PairPermAccess.AllowPositiveStatusTypesAllowed),
            StickyPair.PairPerms.AllowPositiveStatusTypes ? (PairNickOrAliasOrUID + " allows Positive Moodles") : (PairNickOrAliasOrUID + " prevents Positive Moodles"),
            StickyPair.PairPerms.AllowPositiveStatusTypes ? FontAwesomeIcon.SmileBeam : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowPositiveStatusTypesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowPositiveStatusTypesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.AllowNegativeStatusTypes), nameof(StickyPair.PairPermAccess.AllowNegativeStatusTypesAllowed),
            StickyPair.PairPerms.AllowNegativeStatusTypes ? (PairNickOrAliasOrUID + " allows Negative Moodles") : (PairNickOrAliasOrUID + " prevents Negative Moodles"),
            StickyPair.PairPerms.AllowNegativeStatusTypes ? FontAwesomeIcon.FrownOpen : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowNegativeStatusTypesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowNegativeStatusTypesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.AllowSpecialStatusTypes), nameof(StickyPair.PairPermAccess.AllowSpecialStatusTypesAllowed),
            StickyPair.PairPerms.AllowSpecialStatusTypes ? (PairNickOrAliasOrUID + " allows Special Moodles") : (PairNickOrAliasOrUID + " prevents Special Moodles"),
            StickyPair.PairPerms.AllowSpecialStatusTypes ? FontAwesomeIcon.WandMagicSparkles : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowSpecialStatusTypesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowSpecialStatusTypesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.PairCanApplyOwnMoodlesToYou), nameof(StickyPair.PairPermAccess.PairCanApplyOwnMoodlesToYouAllowed),
            StickyPair.PairPerms.PairCanApplyOwnMoodlesToYou ? (PairNickOrAliasOrUID + " allows applying your Moodles") : (PairNickOrAliasOrUID + " prevents applying your Moodles"),
            StickyPair.PairPerms.PairCanApplyOwnMoodlesToYou ? FontAwesomeIcon.PersonArrowUpFromLine : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.PairCanApplyOwnMoodlesToYouAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.PairCanApplyOwnMoodlesToYouAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.PairCanApplyYourMoodlesToYou), nameof(StickyPair.PairPermAccess.PairCanApplyYourMoodlesToYouAllowed),
            StickyPair.PairPerms.PairCanApplyYourMoodlesToYou ? (PairNickOrAliasOrUID + " allows applying their Moodles") : (PairNickOrAliasOrUID + " prevents applying their Moodles"),
            StickyPair.PairPerms.PairCanApplyYourMoodlesToYou ? FontAwesomeIcon.PersonArrowDownToLine : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.PairCanApplyYourMoodlesToYouAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.PairCanApplyYourMoodlesToYouAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.MaxMoodleTime), nameof(StickyPair.PairPermAccess.MaxMoodleTimeAllowed),
            "Max Moodles Duration",
            FontAwesomeIcon.HourglassHalf,
            $"Max Duration a Moodle can be applied for.",
            StickyPair.PairPermAccess.MaxMoodleTimeAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.TimeSpan);

        DrawOtherPairSetting(nameof(PairPerms.AllowPermanentMoodles), nameof(StickyPair.PairPermAccess.AllowPermanentMoodlesAllowed),
            StickyPair.PairPerms.AllowPermanentMoodles ? (PairNickOrAliasOrUID + " allows Permanent Moodles") : (PairNickOrAliasOrUID + " prevents Permanent Moodles"),
            StickyPair.PairPerms.AllowPermanentMoodles ? FontAwesomeIcon.Infinity : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowPermanentMoodlesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowPermanentMoodlesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(PairPerms.AllowRemovingMoodles), nameof(StickyPair.PairPermAccess.AllowRemovingMoodlesAllowed),
            StickyPair.PairPerms.AllowRemovingMoodles ? (PairNickOrAliasOrUID + " allowing Removal of Moodles") : (PairNickOrAliasOrUID + " prevents Removal of Moodles"),
            StickyPair.PairPerms.AllowRemovingMoodles ? FontAwesomeIcon.Eraser : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.AllowRemovingMoodlesAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.AllowRemovingMoodlesAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        ImGui.Separator();
        /* ----------- TOYBOX PERMISSIONS ----------- */
        ImGui.TextUnformatted("Toybox Permissions");

        DrawOtherPairSetting(nameof(PairPerms.CanToggleToyState), nameof(StickyPair.PairPermAccess.CanToggleToyStateAllowed),
            StickyPair.PairPerms.CanToggleToyState ? (PairNickOrAliasOrUID + " allows Toy State Changing") : (PairNickOrAliasOrUID + " prevents Toy State Changing"),
            StickyPair.PairPerms.CanToggleToyState ? FontAwesomeIcon.PowerOff : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanToggleToyStateAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanToggleToyStateAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(StickyPair.PairPerms.CanUseVibeRemote), nameof(StickyPair.PairPermAccess.CanUseVibeRemoteAllowed),
            StickyPair.PairPerms.CanUseVibeRemote ? (PairNickOrAliasOrUID + " allows Vibe Control") : (PairNickOrAliasOrUID + " prevents Vibe Control"),
            StickyPair.PairPerms.CanUseVibeRemote ? FontAwesomeIcon.Mobile : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanUseVibeRemoteAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanUseVibeRemoteAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(StickyPair.PairPerms.CanToggleAlarms), nameof(StickyPair.PairPermAccess.CanToggleAlarmsAllowed),
            StickyPair.PairPerms.CanToggleAlarms ? (PairNickOrAliasOrUID + " allows Alarm Toggling") : (PairNickOrAliasOrUID + " prevents Alarm Toggling"),
            StickyPair.PairPerms.CanToggleAlarms ? FontAwesomeIcon.Bell : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanToggleAlarmsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanToggleAlarmsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(StickyPair.PairPerms.CanSendAlarms), nameof(StickyPair.PairPermAccess.CanSendAlarmsAllowed),
            StickyPair.PairPerms.CanSendAlarms ? (PairNickOrAliasOrUID + " allows sending Alarms") : (PairNickOrAliasOrUID + " prevents sending Alarms"),
            StickyPair.PairPerms.CanSendAlarms ? FontAwesomeIcon.FileExport : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanSendAlarmsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanSendAlarmsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(StickyPair.PairPerms.CanExecutePatterns), nameof(StickyPair.PairPermAccess.CanExecutePatternsAllowed),
            StickyPair.PairPerms.CanExecutePatterns ? (PairNickOrAliasOrUID + " allows Pattern Execution") : (PairNickOrAliasOrUID + " prevents Pattern Execution"),
            StickyPair.PairPerms.CanExecutePatterns ? FontAwesomeIcon.LandMineOn : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanExecutePatternsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanExecutePatternsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(StickyPair.PairPerms.CanStopPatterns), nameof(StickyPair.PairPermAccess.CanStopPatternsAllowed),
            StickyPair.PairPerms.CanStopPatterns ? (PairNickOrAliasOrUID + " allows stopping Patterns") : (PairNickOrAliasOrUID + " prevents stopping Patterns"),
            StickyPair.PairPerms.CanStopPatterns ? FontAwesomeIcon.StopCircle : FontAwesomeIcon.Ban,
            StickyPair.PairPermAccess.CanStopPatternsAllowed ? ("Press to Toggle " + PairNickOrAliasOrUID + "'s permission state.") : ("You Can't Change " + PairNickOrAliasOrUID + "'s Permission here."),
            StickyPair.PairPermAccess.CanStopPatternsAllowed,
            PermissionType.UniquePairPerm, PermissionValueType.YesNo);

        DrawOtherPairSetting(nameof(StickyPair.PairPerms.CanToggleTriggers), nameof(StickyPair.PairPermAccess.CanToggleTriggersAllowed),
            StickyPair.PairPerms.CanToggleTriggers ? (PairNickOrAliasOrUID + " allows Toggling Triggers") : (PairNickOrAliasOrUID + " prevents Toggling Triggers"),
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
            string timeSpanString = ((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!).ToGsRemainingTime() ?? "0d0h0m0s";

            using (var group = ImRaii.Group())
            {
                var id = label + "##" + permissionName;
                // draw the iconTextButton and checkbox beside it. Because we are in control, unless in hardcore, this should never be disabled.
                if (_uiShared.IconInputText(id, icon, label, "format 0d0h0m0s...", ref timeSpanString, 32, IconButtonTextWidth * .5f, true, !hasAccess)) { }
                // Set the permission once deactivated. If invalid, set to default.
                if (ImGui.IsItemDeactivatedAfterEdit()
                    && timeSpanString != ((TimeSpan)permissionSet.GetType().GetProperty(permissionName)?.GetValue(permissionSet)!).ToGsRemainingTime())
                {
                    // attempt to parse the string back into a valid timespan.
                    if (GsPadlockEx.TryParseTimeSpan(timeSpanString, out TimeSpan result))
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
                    _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(permissionName, newValue), UpdateDir.Other));
                }
                break;
            case PermissionType.UniquePairPerm:
                {
                    _logger.LogTrace($"Updated other pair's unique pair permission: {permissionName} to {newValue}", LoggerType.Permissions);
                    _ = _apiHubMain.UserUpdateOtherPairPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(permissionName, newValue), UpdateDir.Other));
                }
                break;
        }
    }
}
