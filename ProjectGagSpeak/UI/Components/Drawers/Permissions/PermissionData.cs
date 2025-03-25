using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using System.Collections.Immutable;

namespace GagSpeak.UI.Components;

// A helper for providing all the preset variables for various gagspeak permissions.
public class PermissionData : IMediatorSubscriber, IDisposable
{
    /// <summary> The String of the current pairs Nick, Alias, or UID. </summary>
    public static string DispName = "";
    public static string PairAccessYesTT = DispName + " allows you to change this permission at will.";
    public static string PairAccessNoTT = DispName + " is preventing you from changing this permission. Only they can update it.";
    public static string ClientAccessYesTT = "Grant " + DispName + " control over this permission, allowing them to change the permission at any time";
    public static string ClientAccessNoTT = "Revoke " + DispName + "'s control over this permission";

    // Store the actual dictionaries as readonly fields to be immutable after initialization
    public readonly IReadOnlyDictionary<SPPID, PermDataPair>   PairPermData   = _pairPermData;
    public readonly IReadOnlyDictionary<SPPID, PermDataClient> ClientPermData = _clientPermData;
    public UserData PairUserData { get; private set; }

    public GagspeakMediator Mediator { get; }
    public PermissionData(GagspeakMediator mediator)
    {
        Mediator = mediator;
        Mediator.Subscribe<StickyPairWindowCreated>(this, (pair) =>
        {
            PairUserData = pair.newPair.UserData;
            DispName = pair.newPair.GetNickAliasOrUid();
        });
    }

    public void Dispose() => Mediator.Unsubscribe<StickyPairWindowCreated>(this);

    /// <summary> An Immutible record for permission settings recreated on each initialization. </summary>
    public record PermDataPair(FAI IconOn, FAI IconOff, string CondTrue, string CondFalse, string Text)
    {
        public string TextPrefix(bool curState) => curState ? DispName + " has their " + Text : DispName;
        public string TextSuffix(bool curState) => curState ? "." : Text + ".";
        public string ToggleTextTT => "You can toggle " + DispName + "'s permission state by clicking it!";
    
        public (FAI icon, string prefix, string suffix, string condText, string tt) TextInfo(bool useAlt)
            => (useAlt ? IconOn : IconOff, TextPrefix(useAlt), TextSuffix(useAlt), useAlt ? CondTrue : CondFalse, ToggleTextTT);
    }

    public record PermDataClient(FAI IconOn, FAI IconOff, string CondTrue, string CondFalse, string Text, string CondTrueTT, string CondFalseTT)
    {
        public string TextPrefix => Text + " ";
        public string TextSuffix => ".";
        public string Tooltip(bool curState) => curState ? TrueToFalseTT : FalseToTrueTT;
        public string HardcoreTooltip(bool curState) => curState ? HcTrueTT : HcFalseTT;

        private string TrueToFalseTT => DispName + " is currently " + CondTrue + CondTrueTT + ".--SEP-- Clicking this will change it to " + CondFalse + ".";
        private string FalseToTrueTT => DispName + " is currently " + CondFalse + CondFalseTT + ".--SEP-- Clicking this will change it to " + CondTrue + ".";
        private string HcTrueTT => Text + " " + CondTrueTT + " " + CondTrue + " for " + DispName + ".--SEP-- You are helpless to disable this!";
        private string HcFalseTT => Text + " " + CondTrueTT + " " + CondFalse + " for " + DispName + ".";
    }

    /// <summary> The Cache of PermissionData for each permission in the Magnifying Glass Window. </summary>
    private static readonly ImmutableDictionary<SPPID, PermDataPair> _pairPermData = ImmutableDictionary<SPPID, PermDataPair>.Empty
        .Add(SPPID.ChatGarblerActive,     new PermDataPair(FAI.MicrophoneSlash,       FAI.Microphone, "enabled",       "disabled",     "Chat Garbler"))
        .Add(SPPID.ChatGarblerLocked,     new PermDataPair(FAI.Key,                   FAI.UnlockAlt,  "locked",        "unlocked",     "Chat Garbler"))
        .Add(SPPID.LockToyboxUI,          new PermDataPair(FAI.Box,                   FAI.BoxOpen,    "locked",        "unlocked",     "Toybox UI"))
        .Add(SPPID.PermanentLocks,        new PermDataPair(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "Permanent Locks"))
        .Add(SPPID.OwnerLocks,            new PermDataPair(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "Owner Locks"))
        .Add(SPPID.DevotionalLocks,       new PermDataPair(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "Devotional Locks"))
        .Add(SPPID.GagVisuals,            new PermDataPair(FAI.Surprise,              FAI.Ban,        "enabled",       "disabled",     "Gag Visuals"))
        .Add(SPPID.ApplyGags,             new PermDataPair(FAI.Mask,                  FAI.Ban,        "allows",        "prevents",     "applying Gags"))
        .Add(SPPID.LockGags,              new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Gags"))
        .Add(SPPID.MaxGagTime,            new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Gag Time"))
        .Add(SPPID.UnlockGags,            new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Gags"))
        .Add(SPPID.RemoveGags,            new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Gags"))
        .Add(SPPID.RestrictionVisuals,    new PermDataPair(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restriction Visuals"))
        .Add(SPPID.ApplyRestrictions,     new PermDataPair(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restrictions"))
        .Add(SPPID.LockRestrictions,      new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restrictions"))
        .Add(SPPID.MaxRestrictionTime,    new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restriction Time"))
        .Add(SPPID.UnlockRestrictions,    new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restrictions"))
        .Add(SPPID.RemoveRestrictions,    new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restrictions"))
        .Add(SPPID.RestraintSetVisuals,   new PermDataPair(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restraint Visuals"))
        .Add(SPPID.ApplyRestraintSets,    new PermDataPair(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restraints"))
        .Add(SPPID.LockRestraintSets,     new PermDataPair(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restraints"))
        .Add(SPPID.MaxRestraintTime,      new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restraint Time"))
        .Add(SPPID.UnlockRestraintSets,   new PermDataPair(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restraints"))
        .Add(SPPID.RemoveRestraintSets,   new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restraints"))
        .Add(SPPID.PuppetPermSit,         new PermDataPair(FAI.Chair,                 FAI.Ban,        "allows",        "prevents",     "Sit Requests"))
        .Add(SPPID.PuppetPermEmote,       new PermDataPair(FAI.Walking,               FAI.Ban,        "allows",        "prevents",     "Emote Requests"))
        .Add(SPPID.PuppetPermAlias,       new PermDataPair(FAI.Scroll,                FAI.Ban,        "allows",        "prevents",     "Alias Requests"))
        .Add(SPPID.PuppetPermAll,         new PermDataPair(FAI.CheckDouble,           FAI.Ban,        "allows",        "prevents",     "All Requests"))
        .Add(SPPID.ApplyPositive,         new PermDataPair(FAI.SmileBeam,             FAI.Ban,        "allows",        "prevents",     "Positive Moodles"))
        .Add(SPPID.ApplyNegative,         new PermDataPair(FAI.FrownOpen,             FAI.Ban,        "allows",        "prevents",     "Negative Moodles"))
        .Add(SPPID.ApplySpecial,          new PermDataPair(FAI.WandMagicSparkles,     FAI.Ban,        "allows",        "prevents",     "Special Moodles"))
        .Add(SPPID.ApplyPairsMoodles,     new PermDataPair(FAI.PersonArrowUpFromLine, FAI.Ban,        "allows",        "prevents",     "applying your Moodles"))
        .Add(SPPID.ApplyOwnMoodles,       new PermDataPair(FAI.PersonArrowDownToLine, FAI.Ban,        "allows",        "prevents",     "applying their Moodles"))
        .Add(SPPID.MaxMoodleTime,         new PermDataPair(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Moodle Time"))
        .Add(SPPID.PermanentMoodles,      new PermDataPair(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "permanent Moodles"))
        .Add(SPPID.RemoveMoodles,         new PermDataPair(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Moodles"))
        .Add(SPPID.ToyControl,            new PermDataPair(FAI.Phone,                 FAI.Ban,        "allows",        "prevents",     "Remote Controlling"))
        .Add(SPPID.PatternStarting,       new PermDataPair(FAI.Play,                  FAI.Ban,        "allows",        "prevents",     "Pattern Starting"))
        .Add(SPPID.PatternStopping,       new PermDataPair(FAI.Stop,                  FAI.Ban,        "allows",        "prevents",     "Pattern Stopping"))
        .Add(SPPID.AlarmToggling,         new PermDataPair(FAI.Bell,                  FAI.Ban,        "allows",        "prevents",     "Alarm Toggling"))
        .Add(SPPID.TriggerToggling,       new PermDataPair(FAI.FileMedicalAlt,        FAI.Ban,        "allows",        "prevents",     "Trigger Toggling"))
        .Add(SPPID.HardcoreModeState,     new PermDataPair(FAI.Bolt,                  FAI.Ban,        "In",            "Not in",       "Hardcore Mode"));

    /// <summary> The Cache of PermissionData for each permission in the Gear Setting Menu. </summary>
    private static readonly ImmutableDictionary<SPPID, PermDataClient> _clientPermData = ImmutableDictionary<SPPID, PermDataClient>.Empty
        .Add(SPPID.ChatGarblerActive,     new PermDataClient(FAI.MicrophoneSlash,       FAI.Microphone,    "active",        "inactive",        "Chat Garbler",             string.Empty,                       string.Empty))
        .Add(SPPID.ChatGarblerLocked,     new PermDataClient(FAI.Key,                   FAI.UnlockAlt,     "locked",        "unlocked",        "Chat Garbler",             string.Empty,                       string.Empty))
        .Add(SPPID.LockToyboxUI,          new PermDataClient(FAI.Box,                   FAI.BoxOpen,       "allowed",       "restricted",      "Toybox UI locking",        "to lock your Toybox UI",           "from locking your ToyboxUI"))
        .Add(SPPID.PermanentLocks,        new PermDataClient(FAI.Infinity,              FAI.Ban,           "allowed",       "restricted",      "Permanent Locks",          "to use permanent locks.",          "from using permanent locks."))
        .Add(SPPID.OwnerLocks,            new PermDataClient(FAI.UserLock,              FAI.Ban,           "allowed",       "restricted",      "Owner Locks",              "to use owner locks",               "from using owner locks"))
        .Add(SPPID.DevotionalLocks,       new PermDataClient(FAI.UserLock,              FAI.Ban,           "allowed",       "restricted",      "Devotional Locks",         "to use devotional locks",          "from using devotional locks"))
        .Add(SPPID.GagVisuals,            new PermDataClient(FAI.Surprise,              FAI.Ban,           "enabled",       "disabled",        "Gag Visuals",              "to enable gag visuals",            "from enabling gag visuals"))
        .Add(SPPID.ApplyGags,             new PermDataClient(FAI.Mask,                  FAI.Ban,           "allowed",       "restricted",      "Applying Gags",            "to apply gags",                    "from applying gags"))
        .Add(SPPID.LockGags,              new PermDataClient(FAI.Lock,                  FAI.Ban,           "allowed",       "restricted",      "Locking Gags",             "to lock gags",                     "from locking gags"))
        .Add(SPPID.MaxGagTime,            new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Gag Time",             string.Empty,                       string.Empty))
        .Add(SPPID.UnlockGags,            new PermDataClient(FAI.Key,                   FAI.Ban,           "allowed",       "restricted",      "Unlocking Gags",           "to unlock gags",                   "from unlocking gags"))
        .Add(SPPID.RemoveGags,            new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "Removing Gags",            "to remove gags",                   "from removing gags"))
        .Add(SPPID.RestrictionVisuals,    new PermDataClient(FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",        "Restriction Visuals",      "to enable restriction visuals",    "from enabling restriction visuals"))
        .Add(SPPID.ApplyRestrictions,     new PermDataClient(FAI.Handcuffs,             FAI.Ban,           "allowed",       "restricted",      "Applying Restrictions",    "to apply restrictions",            "from applying restrictions"))
        .Add(SPPID.LockRestrictions,      new PermDataClient(FAI.Lock,                  FAI.Ban,           "allowed",       "restricted",      "Locking Restrictions",     "to lock restrictions",             "from locking restrictions"))
        .Add(SPPID.MaxRestrictionTime,    new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Restriction Time",     string.Empty,                       string.Empty))
        .Add(SPPID.UnlockRestrictions,    new PermDataClient(FAI.Key,                   FAI.Ban,           "allowed",       "restricted",      "Unlocking Restrictions",   "to unlock restrictions",           "from unlocking restrictions"))
        .Add(SPPID.RemoveRestrictions,    new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "Removing Restrictions",    "to remove restrictions",           "from removing restrictions"))
        .Add(SPPID.RestraintSetVisuals,   new PermDataClient(FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",        "Restraint Visuals",        "to enable restraint visuals",      "from enabling restraint visuals"))
        .Add(SPPID.ApplyRestraintSets,    new PermDataClient(FAI.Handcuffs,             FAI.Ban,           "allowed",       "restricted",      "Applying Restraints",      "to apply restraints",              "from applying restraints"))
        .Add(SPPID.LockRestraintSets,     new PermDataClient(FAI.Lock,                  FAI.Ban,           "allowed",       "restricted",      "Locking Restraints",       "to lock restraints",               "from locking restraints"))
        .Add(SPPID.MaxRestraintTime,      new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Restraint Time",       string.Empty,                       string.Empty))
        .Add(SPPID.UnlockRestraintSets,   new PermDataClient(FAI.Key,                   FAI.Ban,           "allowed",       "restricted",      "Unlocking Restraints",     "to unlock restraints",             "from unlocking restraints"))
        .Add(SPPID.RemoveRestraintSets,   new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "Removing Restraints",      "to remove restraints",             "from removing restraints"))
        .Add(SPPID.PuppetPermSit,         new PermDataClient(FAI.Chair,                 FAI.Ban,           "allowed",       "restricted",      "Sit Requests",             "to invoke sit requests",           "from invoking sit requests"))
        .Add(SPPID.PuppetPermEmote,       new PermDataClient(FAI.Walking,               FAI.Ban,           "allowed",       "restricted",      "Emote Requests",           "to invoke emote requests",         "from invoking emote requests"))
        .Add(SPPID.PuppetPermAlias,       new PermDataClient(FAI.Scroll,                FAI.Ban,           "allowed",       "restricted",      "Alias Requests",           "to invoke alias requests",         "from invoking alias requests"))
        .Add(SPPID.PuppetPermAll,         new PermDataClient(FAI.CheckDouble,           FAI.Ban,           "allowed",       "restricted",      "All Requests",             "to invoke all requests",           "from invoking all requests"))
        .Add(SPPID.ApplyPositive,         new PermDataClient(FAI.SmileBeam,             FAI.Ban,           "allowed",       "restricted",      "Positive Moodles",         "to apply positive moodles",        "from applying positive moodles"))
        .Add(SPPID.ApplyNegative,         new PermDataClient(FAI.FrownOpen,             FAI.Ban,           "allowed",       "restricted",      "Negative Moodles",         "to apply negative moodles",        "from applying negative moodles"))
        .Add(SPPID.ApplySpecial,          new PermDataClient(FAI.WandMagicSparkles,     FAI.Ban,           "allowed",       "restricted",      "Special Moodles",          "to apply special moodles",         "from applying special moodles"))
        .Add(SPPID.ApplyPairsMoodles,     new PermDataClient(FAI.PersonArrowUpFromLine, FAI.Ban,           "allowed",       "restricted",      "applying your Moodles",    "to apply your moodles",            "from applying your moodles"))
        .Add(SPPID.ApplyOwnMoodles,       new PermDataClient(FAI.PersonArrowDownToLine, FAI.Ban,           "allowed",       "restricted",      "applying their Moodles",   "to apply their moodles",           "from applying their moodles"))
        .Add(SPPID.MaxMoodleTime,         new PermDataClient(FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Moodle Time",          string.Empty,                       string.Empty))
        .Add(SPPID.PermanentMoodles,      new PermDataClient(FAI.Infinity,              FAI.Ban,           "allowed",       "restricted",      "permanent Moodles",        "to apply permanent moodles",       "from applying permanent moodles"))
        .Add(SPPID.RemoveMoodles,         new PermDataClient(FAI.Eraser,                FAI.Ban,           "allowed",       "restricted",      "removing Moodles",         "to remove moodles",                "from removing moodles"))
        .Add(SPPID.ToyControl,            new PermDataClient(FAI.Phone,                 FAI.Ban,           "allowed",       "restricted",      "Remote Controlling",       "to control toys",                  "from controlling toys"))
        .Add(SPPID.PatternStarting,       new PermDataClient(FAI.Play,                  FAI.Ban,           "allowed",       "restricted",      "Pattern Starting",         "to start patterns",                "from starting patterns"))
        .Add(SPPID.PatternStopping,       new PermDataClient(FAI.Stop,                  FAI.Ban,           "allowed",       "restricted",      "Pattern Stopping",         "to stop patterns",                 "from stopping patterns"))
        .Add(SPPID.AlarmToggling,         new PermDataClient(FAI.Bell,                  FAI.Ban,           "allowed",       "restricted",      "Alarm Toggling",           "to toggle alarms",                 "from toggling alarms"))
        .Add(SPPID.TriggerToggling,       new PermDataClient(FAI.FileMedicalAlt,        FAI.Ban,           "allowed",       "restricted",      "Trigger Toggling",         "to toggle triggers",               "from toggling triggers"))
        .Add(SPPID.HardcoreModeState,     new PermDataClient(FAI.AnchorLock,            FAI.Unlock,        "enabled",       "disabled",        "Hardcore Mode",            "is",                               string.Empty))
        .Add(SPPID.PairLockedStates,      new PermDataClient(FAI.AnchorLock,            FAI.Unlock,        "Devotional",    "not Devotional",  "Hardcore States",          "are",                              string.Empty))
        .Add(SPPID.ForcedFollow,          new PermDataClient(FAI.Walking,               FAI.Ban,           "active",        "inactive",        "Forced Follow",            "is",                               string.Empty))
        .Add(SPPID.ForcedEmoteState,      new PermDataClient(FAI.PersonArrowDownToLine, FAI.Ban,           "active",        "inactive",        "Forced Emote",             "is",                               string.Empty))
        .Add(SPPID.ForcedStay,            new PermDataClient(FAI.HouseLock,             FAI.Ban,           "active",        "inactive",        "Forced Stay",              "is",                               string.Empty))
        .Add(SPPID.ChatBoxesHidden,       new PermDataClient(FAI.CommentSlash,          FAI.Ban,           "visible",       "hidden",          "Chatboxes",                "are",                              string.Empty))
        .Add(SPPID.ChatInputHidden,       new PermDataClient(FAI.CommentSlash,          FAI.Ban,           "visible",       "hidden",          "Chat Input",               "is",                               string.Empty))
        .Add(SPPID.ChatInputBlocked,      new PermDataClient(FAI.CommentDots,           FAI.Ban,           "blocked",       "allowed",         "Chat Input",               "is",                               string.Empty));
}
