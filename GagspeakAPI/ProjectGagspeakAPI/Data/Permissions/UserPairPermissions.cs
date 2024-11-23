using GagspeakAPI.Extensions;
using MessagePack;
using System.ComponentModel.DataAnnotations;

namespace GagspeakAPI.Data.Permissions;

[MessagePackObject(keyAsPropertyName: true)]
public record UserPairPermissions
{
    // unique permissions stored here:
    public bool IsPaused { get; set; } = false;  // if the pair is paused, a unique unmodifiable permission by other pairs.
    public bool GagFeatures { get; set; } = false; // if this pair can apply / lock (/ unlock / remove ??) gag features on you.
    public bool OwnerLocks { get; set; } = false; // if the pair can use OwnerPadlocks & Timer variants. Only the others with this permission can remove them.
    public bool DevotionalLocks { get; set; } = false; // if the pair can use Devotional Padlocks & Timer variants. Only the assigner of these locks can remove them.
    public bool ExtendedLockTimes { get; set; } = false;  // if user allowed extended lock times for this paired user
    public TimeSpan MaxLockTime { get; set; } = TimeSpan.Zero;    // the max lock time for this paired user
    public bool InHardcore { get; set; } = false;         // if the user is in hardcore mode with this paired user

    // unique permissions for the wardrobe
    public bool ApplyRestraintSets { get; set; } = false; // if the client pair can apply your restraint sets.
    public bool LockRestraintSets { get; set; } = false;  // if the client pair can lock your restraint sets
    public TimeSpan MaxAllowedRestraintTime { get; set; } = TimeSpan.Zero; // the max time the client pair can lock your restraint sets
    public bool UnlockRestraintSets { get; set; } = false; // if the client pair can unlock your restraint sets
    public bool RemoveRestraintSets { get; set; } = false; // if the client pair can remove your restraint sets.

    // unique permissions for the puppeteer
    public string TriggerPhrase { get; set; } = "";    // the end char that is the right enclosing bracket character for commands.
    public char StartChar { get; set; } = '(';          // the start char that is the left enclosing bracket character for commands.
    public char EndChar { get; set; } = ')';            // the end char that is the right enclosing bracket character for commands.
    public bool AllowSitRequests { get; set; } = false;   // if the client pair can request to sit on you.
    public bool AllowMotionRequests { get; set; } = false; // if the client pair can request to move you.
    public bool AllowAllRequests { get; set; } = false;   // if the client pair can request to do anything.

    // unique Moodles permissions
    public bool AllowPositiveStatusTypes { get; set; } = false; // if the client pair can give you positive moodles
    public bool AllowNegativeStatusTypes { get; set; } = false; // if the client pair can give you negative moodles
    public bool AllowSpecialStatusTypes { get; set; } = false;  // if the client pair can give you neutral moodles
    public bool PairCanApplyOwnMoodlesToYou { get; set; } = false; // if the client pair can apply their moodles to you
    public bool PairCanApplyYourMoodlesToYou { get; set; } = false; // if the client pair can apply your moodles
    public TimeSpan MaxMoodleTime { get; set; } = TimeSpan.Zero; // the max time the client pair can apply moodles to you
    public bool AllowPermanentMoodles { get; set; } = false; // if the client pair can apply permanent moodles to you
    public bool AllowRemovingMoodles { get; set; } = false; // if the client pair can remove moodles from you


    // unique permissions for the toybox
    public bool CanToggleToyState { get; set; } = false;   // if the client pair can turn your toy on and off.
    public bool CanUseVibeRemote { get; set; } = false; // if the client pair can use the real-time vibe remote on your toy.
    public bool CanExecutePatterns { get; set; } = false; // if the client pair can use patterns on your toy.
    public bool CanStopPatterns { get; set; } = false; // if the client pair can stop patterns on your toy.
    public bool CanToggleAlarms { get; set; } = false; // if the client pair can toggle alarms on your toy.
    public bool CanSendAlarms { get; set; } = false; // if the client pair can send alarms to your toy.
    public bool CanToggleTriggers { get; set; } = false; // if the client pair can use triggers on your toy.

    // unique hardcore permissions. (only allow the ALLOW permissions to be set by the user).
    public bool DevotionalStatesForPair { get; set; } = false; // Treats any State toggled by this pair like a Devotional Padlock.
    public bool AllowForcedFollow { get; set; } = false;
    public bool AllowForcedSit { get; set; } = false;
    public bool AllowForcedEmote { get; set; } = false;
    public bool AllowForcedToStay { get; set; } = false;
    public bool AllowBlindfold { get; set; } = false;
    public bool AllowHidingChatBoxes { get; set; } = false;
    public bool AllowHidingChatInput { get; set; } = false;
    public bool AllowChatInputBlocking { get; set; } = false;

    public string ShockCollarShareCode { get; set; } = ""; // the share Code for the shock collar unique to this user.
    public bool AllowShocks { get; set; } = false; // If we allow shocks from this pair.
    public bool AllowVibrations { get; set; } = false; // If we allow vibrations from this pair.
    public bool AllowBeeps { get; set; } = false; // If we allow beeps from this pair.
    public int MaxIntensity { get; set; } = -1; // the max intensity of the shock, vibration, or beep.
    public int MaxDuration { get; set; } = -1; // the max duration of the shock, vibration, or beep.
    public TimeSpan MaxVibrateDuration { get; set; } = TimeSpan.Zero; // separate value since vibrations have diff limits.

    // member helper for PiShock functions.
    public bool HasValidShareCode() => !ShockCollarShareCode.NullOrEmpty() && MaxDuration > 0;
    public TimeSpan GetTimespanFromDuration()
    {
        if (MaxDuration > 15 && MaxDuration < 100)
        {
            return TimeSpan.Zero; // invalid range.
        }
        else if (MaxDuration >= 100 && MaxDuration <= 15000)
        {
            return TimeSpan.FromMilliseconds(MaxDuration); // convert to milliseconds
        }
        else
        {
            return TimeSpan.FromSeconds(MaxDuration); // convert to seconds
        }
    }
}
