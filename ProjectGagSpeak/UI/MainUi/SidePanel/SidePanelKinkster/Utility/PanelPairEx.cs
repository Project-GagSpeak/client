using CkCommons;using CkCommons.Gui;using Dalamud.Bindings.ImGui;using Dalamud.Interface.Colors;using Dalamud.Interface.Utility;using Dalamud.Interface.Utility.Raii;using GagSpeak.Kinksters;using GagSpeak.Services;using GagSpeak.Utils;using GagSpeak.WebAPI;using GagspeakAPI.Data.Permissions;using System.Collections.Immutable;namespace GagSpeak.Gui.MainWindow;

/// <summary> 
///     Kinkster Permission ID 
/// </summary>
public enum KPID : byte
{
    ChatGarblerActive,
    ChatGarblerLocked,
    GaggedNameplate, // maybe remove but im not sure.

    PermanentLocks,
    OwnerLocks,
    DevotionalLocks,

    GagVisuals,
    ApplyGags,
    LockGags,
    MaxGagTime,
    UnlockGags,
    RemoveGags,

    RestrictionVisuals,
    ApplyRestrictions,
    LockRestrictions,
    MaxRestrictionTime,
    UnlockRestrictions,
    RemoveRestrictions,

    RestraintSetVisuals,
    ApplyRestraintSets,
    ApplyLayers,
    ApplyLayersWhileLocked,
    LockRestraintSets,
    MaxRestraintTime,
    UnlockRestraintSets,
    RemoveLayers,
    RemoveLayersWhileLocked,
    RemoveRestraintSets,

    PuppetPermSit,
    PuppetPermEmote,
    PuppetPermAlias,
    PuppetPermAll,

    ApplyPositive,
    ApplyNegative,
    ApplySpecial,
    ApplyPairsMoodles,
    ApplyOwnMoodles,
    MaxMoodleTime,
    PermanentMoodles,
    RemoveAppliedMoodles,
    RemoveAnyMoodles,

    PatternStarting,
    PatternStopping,
    AlarmToggling,
    TriggerToggling,

    HypnosisMaxTime,
    HypnosisEffect,

    HardcoreModeState,
    PairLockedStates,
    LockedFollowing,
    LockedEmoteState,
    IndoorConfinement,
    Imprisonment,
    GarbleChannelEditing,
    ChatBoxesHidden,
    ChatInputHidden,
    ChatInputBlocked,

    HypnoticImage,

    PiShockShareCode,
    MaxVibrateDuration,
}
public static class PanelPairEx
{
    public record OwnPermRowData(bool IsGlobal, FAI IconYes, FAI IconNo, string AllowedStr, string BlockedStr, string PermLabel, string JoinWord, string PairAllowedTT, string PairBlockedTT);
    public record OtherPermRowData(FAI IconYes, FAI IconNo, string CondTrue, string CondFalse, string Label, bool CondAfterLabel, string suffix = "");
    public record OwnHcRowData(FAI IconT, FAI IconF, string PermLabel, string EnabledPreText, string DisabledText, string ToggleTrueSuffixTT, string ToggleFalseSuffixTT);
    public record OtherHcRowData(FAI IconActive, FAI IconInactive, string PermLabel, string ActionText, string InactiveText, string AllowedTT);

    public static readonly ImmutableDictionary<KPID, OwnPermRowData> OwnRowInfo = ImmutableDictionary<KPID, OwnPermRowData>.Empty
        .Add(KPID.ChatGarblerActive,     new OwnPermRowData(true, FAI.MicrophoneSlash,       FAI.Microphone,    "active",        "inactive",    "Chat Garbler", "is",       string.Empty, string.Empty))
        .Add(KPID.ChatGarblerLocked,     new OwnPermRowData(true, FAI.Key,                   FAI.UnlockAlt,     "locked",        "unlocked",    "Chat Garbler", "is",       string.Empty, string.Empty))
        .Add(KPID.GaggedNameplate,       new OwnPermRowData(true, FAI.IdCard,                FAI.Ban,           "enabled",       "disabled",    "GagPlates", "are",         string.Empty, string.Empty))

        .Add(KPID.PermanentLocks,        new OwnPermRowData(false, FAI.Infinity,              FAI.Ban,           "allow",       "prevent",      "Permanent Locks", "are", "to use padlocks without timers.", "from using padlocks without timers."))
        .Add(KPID.OwnerLocks,            new OwnPermRowData(false, FAI.UserLock,              FAI.Ban,           "allow",       "prevent",      "Owner Locks", "are", "to use owner padlocks.", "from using owner padlocks."))
        .Add(KPID.DevotionalLocks,       new OwnPermRowData(false, FAI.UserLock,              FAI.Ban,           "allow",       "prevent",      "Devotional Locks", "are", "to use devotional padlocks.", "from using devotional padlocks."))

        .Add(KPID.GagVisuals,            new OwnPermRowData(true, FAI.Surprise,              FAI.Ban,           "enabled",       "disabled",        "Gag Visuals", "are", string.Empty, string.Empty))
        .Add(KPID.ApplyGags,             new OwnPermRowData(false, FAI.Mask,                  FAI.Ban,           "allow",       "prevent",      "applying Gags", "are", "to apply gags.", "from applying gags"))
        .Add(KPID.LockGags,              new OwnPermRowData(false, FAI.Lock,                  FAI.Ban,           "allow",       "prevent",      "locking Gags", "are", "to lock gags",                     "from locking gags"))
        .Add(KPID.MaxGagTime,            new OwnPermRowData(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Gag Time", "are", string.Empty,                       string.Empty))
        .Add(KPID.UnlockGags,            new OwnPermRowData(false, FAI.Key,                   FAI.Ban,           "allow",       "prevent",      "unlocking Gags", "are", "to unlock gags",                   "from unlocking gags"))
        .Add(KPID.RemoveGags,            new OwnPermRowData(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "removing Gags", "are", "to remove gags",                   "from removing gags"))

        .Add(KPID.RestrictionVisuals,    new OwnPermRowData(true,  FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",        "Restriction Visuals", "are", "to enable restriction visuals",    "from enabling restriction visuals"))
        .Add(KPID.ApplyRestrictions,     new OwnPermRowData(false, FAI.Handcuffs,             FAI.Ban,           "allow",       "prevent",      "Applying Restrictions", "are", "to apply restrictions",            "from applying restrictions"))
        .Add(KPID.LockRestrictions,      new OwnPermRowData(false, FAI.Lock,                  FAI.Ban,           "allow",       "prevent",      "Locking Restrictions", "are", "to lock restrictions",             "from locking restrictions"))
        .Add(KPID.MaxRestrictionTime,    new OwnPermRowData(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty,      "Max Restriction Time", string.Empty, string.Empty,                       string.Empty))
        .Add(KPID.UnlockRestrictions,    new OwnPermRowData(false, FAI.Key,                   FAI.Ban,           "allow",       "prevent",      "Unlocking Restrictions", "are", "to unlock restrictions",           "from unlocking restrictions"))
        .Add(KPID.RemoveRestrictions,    new OwnPermRowData(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "Removing Restrictions", "are", "to remove restrictions",           "from removing restrictions"))

        .Add(KPID.RestraintSetVisuals,   new OwnPermRowData(true, FAI.Tshirt,                FAI.Ban,           "enabled",       "disabled",    "Restraint Visuals", "are", "to enable restraint visuals",      "from enabling restraint visuals"))
        .Add(KPID.ApplyRestraintSets,    new OwnPermRowData(false, FAI.Handcuffs,             FAI.Ban,           "allow",       "prevent",      "applying restraints", "are", "to apply restraints",              "from applying restraints"))
        .Add(KPID.ApplyLayers,           new OwnPermRowData(false, FAI.LayerGroup,            FAI.Ban,           "allow",       "prevent",      "adding layers", "are", "to apply layers",                  "from applying layers"))
        .Add(KPID.ApplyLayersWhileLocked,new OwnPermRowData(false, FAI.LayerGroup,            FAI.Ban,           "allow",       "prevent",      "adding layers when locked", "are", "to apply layers while locked",   "from applying layers while locked"))
        .Add(KPID.LockRestraintSets,     new OwnPermRowData(false, FAI.Lock,                  FAI.Ban,           "allow",       "prevent",      "locking restraints", "are", "to lock restraints",               "from locking restraints"))
        .Add(KPID.MaxRestraintTime,      new OwnPermRowData(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty, "Max Restraint Time", string.Empty,      string.Empty,                       string.Empty))
        .Add(KPID.UnlockRestraintSets,   new OwnPermRowData(false, FAI.Key,                   FAI.Ban,           "allow",       "prevent",      "unlocking restraints", "are", "to unlock restraints",             "from unlocking restraints"))
        .Add(KPID.RemoveLayers,          new OwnPermRowData(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "layer removal", "are", "to remove layers",                 "from removing layers"))
        .Add(KPID.RemoveLayersWhileLocked,new OwnPermRowData(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",     "layer removal when locked", "are", "to remove layers while locked", "from removing layers while locked"))
        .Add(KPID.RemoveRestraintSets,   new OwnPermRowData(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "removing restraints", "are", "to remove restraints",             "from removing restraints"))

        .Add(KPID.PuppetPermSit,         new OwnPermRowData(false, FAI.Chair,                 FAI.Ban,           "allow",       "prevent",      "sit requests", "are", "to invoke sit requests",           "from invoking sit requests"))
        .Add(KPID.PuppetPermEmote,       new OwnPermRowData(false, FAI.Walking,               FAI.Ban,           "allow",       "prevent",      "emote requests", "are", "to invoke emote requests",         "from invoking emote requests"))
        .Add(KPID.PuppetPermAlias,       new OwnPermRowData(false, FAI.Scroll,                FAI.Ban,           "allow",       "prevent",      "alias requests", "are", "to invoke alias requests",         "from invoking alias requests"))
        .Add(KPID.PuppetPermAll,         new OwnPermRowData(false, FAI.CheckDouble,           FAI.Ban,           "allow",       "prevent",      "all requests", "are", "to invoke all requests",           "from invoking all requests"))

        .Add(KPID.ApplyPositive,         new OwnPermRowData(false, FAI.SmileBeam,             FAI.Ban,           "allow",       "prevent",      "positive Moodles", "are", "to apply positive moodles",        "from applying positive moodles"))
        .Add(KPID.ApplyNegative,         new OwnPermRowData(false, FAI.FrownOpen,             FAI.Ban,           "allow",       "prevent",      "negative Moodles", "are", "to apply negative moodles",        "from applying negative moodles"))
        .Add(KPID.ApplySpecial,          new OwnPermRowData(false, FAI.WandMagicSparkles,     FAI.Ban,           "allow",       "prevent",      "special Moodles", "are", "to apply special moodles",         "from applying special moodles"))
        .Add(KPID.ApplyPairsMoodles,     new OwnPermRowData(false, FAI.PersonArrowUpFromLine, FAI.Ban,           "allow",       "prevent",      "applying your Moodles", "are", "to apply your moodles",            "from applying your moodles"))
        .Add(KPID.ApplyOwnMoodles,       new OwnPermRowData(false, FAI.PersonArrowDownToLine, FAI.Ban,           "allow",       "prevent",      "applying their Moodles", "are", "to apply their moodles",           "from applying their moodles"))
        .Add(KPID.MaxMoodleTime,         new OwnPermRowData(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty, "Max Moodle Time", string.Empty,         string.Empty,                       string.Empty))
        .Add(KPID.PermanentMoodles,      new OwnPermRowData(false, FAI.Infinity,              FAI.Ban,           "allow",       "prevent",      "permanent Moodles", "are", "to apply permanent moodles",       "from applying permanent moodles"))
        .Add(KPID.RemoveAppliedMoodles,  new OwnPermRowData(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "removing applied Moodles", "are", "to remove applied moodles",                "from removing applied moodles"))
        .Add(KPID.RemoveAnyMoodles,      new OwnPermRowData(false, FAI.Eraser,                FAI.Ban,           "allow",       "prevent",      "removing any Moodle", "are", "to remove any moodle",                "from removing any moodle"))

        .Add(KPID.HypnosisMaxTime,       new OwnPermRowData(false, FAI.HourglassHalf,         FAI.None,          string.Empty,    string.Empty, "Max Hypnosis Time", string.Empty, string.Empty,                       string.Empty))
        .Add(KPID.HypnosisEffect,        new OwnPermRowData(false, FAI.CameraRotate,          FAI.Ban,           "allow",       "prevent",      "Hypnotic Effect Sending", "are", "to send hypnotic effects",         "from sending hypnotic effects"))

        .Add(KPID.PatternStarting,       new OwnPermRowData(false, FAI.Play,                  FAI.Ban,           "allow",       "prevent",      "Pattern Starting", "is", "to start patterns",                "from starting patterns"))
        .Add(KPID.PatternStopping,       new OwnPermRowData(false, FAI.Stop,                  FAI.Ban,           "allow",       "prevent",      "Pattern Stopping", "is", "to stop patterns",                 "from stopping patterns"))
        .Add(KPID.AlarmToggling,         new OwnPermRowData(false, FAI.Bell,                  FAI.Ban,           "allow",       "prevent",      "Alarm Toggling", "is", "to toggle alarms",                 "from toggling alarms"))
        .Add(KPID.TriggerToggling,       new OwnPermRowData(false, FAI.FileMedicalAlt,        FAI.Ban,           "allow",       "prevent",      "Trigger Toggling", "is", "to toggle triggers",               "from toggling triggers"))

        .Add(KPID.HardcoreModeState,     new OwnPermRowData(false, FAI.AnchorLock,            FAI.Unlock,        "enabled",     "disabled",     "Hardcore Mode", "is",  string.Empty,                             string.Empty))
        .Add(KPID.GarbleChannelEditing,  new OwnPermRowData(false, FAI.CommentDots,           FAI.Ban,           "allow",       "prevent",      "garble channel editing", "is", "to change your configured garbler channels", "from changing your configured garbler channels."))
        .Add(KPID.HypnoticImage,         new OwnPermRowData(false, FAI.Images,                FAI.Ban,           "allow",       "prevent",      "hypnotic image sending", "is", "to send custom hypnosis BG's", "from sending custom hypnosis BG's"));

    public static readonly ImmutableDictionary<KPID, OtherPermRowData> OtherRowInfo = ImmutableDictionary<KPID, OtherPermRowData>.Empty
        .Add(KPID.ChatGarblerActive,     new OtherPermRowData(FAI.MicrophoneSlash,       FAI.Microphone, "enabled",       "disabled",     "Chat Garbler",              true , "is"))
        .Add(KPID.ChatGarblerLocked,     new OtherPermRowData(FAI.Key,                   FAI.UnlockAlt,  "locked",        "unlocked",     "Chat Garbler",              true , "is"))
        .Add(KPID.GaggedNameplate,       new OtherPermRowData(FAI.IdCard,                FAI.Ban,        "enabled",       "disabled",     "GagPlates",                 true , "are"))

        .Add(KPID.PermanentLocks,        new OtherPermRowData(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "permanent locks",           false))
        .Add(KPID.OwnerLocks,            new OtherPermRowData(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "owner locks",               false))
        .Add(KPID.DevotionalLocks,       new OtherPermRowData(FAI.UserLock,              FAI.Ban,        "allows",        "prevents",     "devotional locks",          false))

        .Add(KPID.GagVisuals,            new OtherPermRowData(FAI.Surprise,              FAI.Ban,        "enabled",       "disabled",     "Gag Visuals",               true , "are"))
        .Add(KPID.ApplyGags,             new OtherPermRowData(FAI.Mask,                  FAI.Ban,        "allows",        "prevents",     "applying Gags",             false))
        .Add(KPID.LockGags,              new OtherPermRowData(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Gags",              false))
        .Add(KPID.MaxGagTime,            new OtherPermRowData(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Gag time",              false))
        .Add(KPID.UnlockGags,            new OtherPermRowData(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Gags",            false))
        .Add(KPID.RemoveGags,            new OtherPermRowData(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Gags",             false))

        .Add(KPID.RestrictionVisuals,    new OtherPermRowData(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restriction visuals",       true , "are"))
        .Add(KPID.ApplyRestrictions,     new OtherPermRowData(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restrictions",     false))
        .Add(KPID.LockRestrictions,      new OtherPermRowData(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restrictions",      false))
        .Add(KPID.MaxRestrictionTime,    new OtherPermRowData(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restriction time",      false))
        .Add(KPID.UnlockRestrictions,    new OtherPermRowData(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restrictions",    false))
        .Add(KPID.RemoveRestrictions,    new OtherPermRowData(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restrictions",     false))

        .Add(KPID.RestraintSetVisuals,   new OtherPermRowData(FAI.Tshirt,                FAI.Ban,        "enabled",       "disabled",     "Restraint visuals",         true , "are"))
        .Add(KPID.ApplyRestraintSets,    new OtherPermRowData(FAI.Handcuffs,             FAI.Ban,        "allows",        "prevents",     "applying Restraints",       false))
        .Add(KPID.ApplyLayers,           new OtherPermRowData(FAI.LayerGroup,            FAI.Ban,        "allows",        "prevents",     "applying Layers",           false))
        .Add(KPID.ApplyLayersWhileLocked,new OtherPermRowData(FAI.LayerGroup,            FAI.Ban,        "allows",        "prevents",     "applying locked Layers",    false))
        .Add(KPID.LockRestraintSets,     new OtherPermRowData(FAI.Lock,                  FAI.Ban,        "allows",        "prevents",     "locking Restraints",        false))
        .Add(KPID.MaxRestraintTime,      new OtherPermRowData(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Restraint time",        false))
        .Add(KPID.UnlockRestraintSets,   new OtherPermRowData(FAI.Key,                   FAI.Ban,        "allows",        "prevents",     "unlocking Restraints",      false))
        .Add(KPID.RemoveLayers,          new OtherPermRowData(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Layers",           false))
        .Add(KPID.RemoveLayersWhileLocked,new OtherPermRowData(FAI.Eraser,               FAI.Ban,        "allows",        "prevents",     "removing locked Layers",    false))
        .Add(KPID.RemoveRestraintSets,   new OtherPermRowData(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing Restraints",       false))

        .Add(KPID.PuppetPermSit,         new OtherPermRowData(FAI.Chair,                 FAI.Ban,        "allows",        "prevents",     "sit Requests",              false))
        .Add(KPID.PuppetPermEmote,       new OtherPermRowData(FAI.Walking,               FAI.Ban,        "allows",        "prevents",     "emote Requests",            false))
        .Add(KPID.PuppetPermAlias,       new OtherPermRowData(FAI.Scroll,                FAI.Ban,        "allows",        "prevents",     "alias Requests",            false))
        .Add(KPID.PuppetPermAll,         new OtherPermRowData(FAI.CheckDouble,           FAI.Ban,        "allows",        "prevents",     "all Requests",              false))

        .Add(KPID.ApplyPositive,         new OtherPermRowData(FAI.SmileBeam,             FAI.Ban,        "allows",        "prevents",     "positive Moodles",          false))
        .Add(KPID.ApplyNegative,         new OtherPermRowData(FAI.FrownOpen,             FAI.Ban,        "allows",        "prevents",     "negative Moodles",          false))
        .Add(KPID.ApplySpecial,          new OtherPermRowData(FAI.WandMagicSparkles,     FAI.Ban,        "allows",        "prevents",     "special Moodles",           false))
        .Add(KPID.ApplyPairsMoodles,     new OtherPermRowData(FAI.PersonArrowUpFromLine, FAI.Ban,        "allows",        "prevents",     "applying your Moodles",     false))
        .Add(KPID.ApplyOwnMoodles,       new OtherPermRowData(FAI.PersonArrowDownToLine, FAI.Ban,        "allows",        "prevents",     "applying their Moodles",    false))
        .Add(KPID.MaxMoodleTime,         new OtherPermRowData(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max Moodle time",           false))
        .Add(KPID.PermanentMoodles,      new OtherPermRowData(FAI.Infinity,              FAI.Ban,        "allows",        "prevents",     "permanent Moodles",         false))
        .Add(KPID.RemoveAppliedMoodles,  new OtherPermRowData(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing applied Moodles",  false))
        .Add(KPID.RemoveAnyMoodles,      new OtherPermRowData(FAI.Eraser,                FAI.Ban,        "allows",        "prevents",     "removing any Moodle",       false))

        .Add(KPID.HypnosisMaxTime,       new OtherPermRowData(FAI.HourglassHalf,         FAI.None,       string.Empty,    string.Empty,   "Max hypnosis time",         false))
        .Add(KPID.HypnosisEffect,        new OtherPermRowData(FAI.CameraRotate,          FAI.Ban,        "allows",        "prevents",     "Hypno effect sending",      false))

        .Add(KPID.PatternStarting,       new OtherPermRowData(FAI.Play,                  FAI.Ban,        "allows",        "prevents",     "Pattern starting",          false))
        .Add(KPID.PatternStopping,       new OtherPermRowData(FAI.Stop,                  FAI.Ban,        "allows",        "prevents",     "Pattern stopping",          false))
        .Add(KPID.AlarmToggling,         new OtherPermRowData(FAI.Bell,                  FAI.Ban,        "allows",        "prevents",     "Alarm toggling",            false))
        .Add(KPID.TriggerToggling,       new OtherPermRowData(FAI.FileMedicalAlt,        FAI.Ban,        "allows",        "prevents",     "Trigger toggling",          false));


    public readonly static ImmutableDictionary<KPID, OwnHcRowData> OwnHcRowInfo = ImmutableDictionary<KPID, OwnHcRowData>.Empty
        .Add(KPID.LockedFollowing, new OwnHcRowData(FAI.Walking, FAI.Ban, "Forced Follow", "Actively following", "Not following anyone", "to make you follow them", "from triggering --COL--Forced Follow--COL-- on you"))
        .Add(KPID.LockedEmoteState, new OwnHcRowData(FAI.PersonArrowDownToLine, FAI.Ban, "Locked Emote State", "In emote lock for", "Not locked in an emote loop", string.Empty, string.Empty)) // Handle this seperately, it has it's own call.
        .Add(KPID.IndoorConfinement, new OwnHcRowData(FAI.HouseLock, FAI.Ban, "Indoor Confinement", "Confined by", "Not confined by anyone", "to confine you indoors --COL--via the nearest housing node--COL----NL--If --COL--Lifestream--COL-- is installed, can be confined to --COL--any address--COL--.", "from confining you indoors"))
        .Add(KPID.Imprisonment, new OwnHcRowData(FAI.Bars, FAI.Ban, "Imprisonment", "Imprisoned by", "Not imprisoned", "to imprison you at a desired location.--SEP----COL--They must be nearby when giving a location besides your current position.", "from imprisoning you at a desired location"))
        .Add(KPID.ChatBoxesHidden, new OwnHcRowData(FAI.CommentSlash, FAI.Ban, "ChatBox Visibility", "Chat box hidden by", "Chatbox is visible", "to hide your Chatbox UI", "from hiding your chatbox"))
        .Add(KPID.ChatInputHidden, new OwnHcRowData(FAI.CommentSlash, FAI.Ban, "ChatInput Visibility", "Chat input hidden by", "ChatInput is visible", "to hide your chat input UI", "from hiding your chat input"))
        .Add(KPID.ChatInputBlocked, new OwnHcRowData(FAI.CommentDots, FAI.Ban, "ChatInput Blocking", "Chat input blocked by", "ChatInput is accessible", "to block your chat input", "from blocking your chat input"));


    public static readonly ImmutableDictionary<KPID, OtherHcRowData> OtherHcRowInfo = ImmutableDictionary<KPID, OtherHcRowData>.Empty
        .Add(KPID.LockedFollowing,    new OtherHcRowData(FAI.Walking,           FAI.Ban, "Forced Follow",         "Actively following",      " is not following anyone.",    "has allowed you to enact forced follow on them."))
        .Add(KPID.LockedEmoteState,   new OtherHcRowData(FAI.PersonArrowDownToLine, FAI.Ban, "Forced Emote Lock", "In emote lock for",       " is not emote locked.",        "has allowed you to lock them in an emote loop.")) // Handle this separately, it has its own call.
        .Add(KPID.IndoorConfinement,  new OtherHcRowData(FAI.HouseLock,         FAI.Ban, "Indoor Confinement",    "Confined by",             " is not confined.",            "has allowed you to confine them indoors."))
        .Add(KPID.Imprisonment,       new OtherHcRowData(FAI.Bars,              FAI.Ban, "Imprisonment",          "Imprisoned by",           " is not imprisoned.",          "has allowed you to imprison them at a desired location.--SEP--They must be nearby when giving a location besides your current position."))
        .Add(KPID.ChatBoxesHidden,    new OtherHcRowData(FAI.CommentSlash,      FAI.Ban, "ChatBox Visibility",    "Chatbox hidden by",       "'s chatbox is visible.",       "has allowed you to hide their chat--NL--Note: This will prevent them from seeing your messages, or anyone else's."))
        .Add(KPID.ChatInputHidden,    new OtherHcRowData(FAI.CommentSlash,      FAI.Ban, "ChatInput Visibility",  "Chat input hidden by",    "'s chat input is visible.",    "has allowed you to hide their chat input.--NL--Note: They will still be able to type, but can't see what they type."))
        .Add(KPID.ChatInputBlocked,   new OtherHcRowData(FAI.CommentDots,       FAI.Ban, "ChatInput Blocking",    "Chat input blocked by",   "'s chat input is accessible.", "has allowed you to block their chat input.--SEP--THEIR SAFEWORD IN THIS CASE IS --COL--CTRL + ALT + BACKSPACE (FUCK GO BACK)--COL--"));


    public static (string name, PermissionType type) ToPermValue(this KPID perm)
        => perm switch
        {
            KPID.ChatGarblerActive     => (nameof(GlobalPerms.ChatGarblerActive),          PermissionType.Global),
            KPID.ChatGarblerLocked     => (nameof(GlobalPerms.ChatGarblerLocked),          PermissionType.Global),

            KPID.PermanentLocks        => (nameof(PairPerms.PermanentLocks),               PermissionType.PairPerm),
            KPID.OwnerLocks            => (nameof(PairPerms.OwnerLocks),                   PermissionType.PairPerm),
            KPID.DevotionalLocks       => (nameof(PairPerms.DevotionalLocks),              PermissionType.PairPerm),

            KPID.GagVisuals            => (nameof(GlobalPerms.GagVisuals),                 PermissionType.Global),
            KPID.ApplyGags             => (nameof(PairPerms.ApplyGags),                    PermissionType.PairPerm),
            KPID.LockGags              => (nameof(PairPerms.LockGags),                     PermissionType.PairPerm),
            KPID.MaxGagTime            => (nameof(PairPerms.MaxGagTime),                   PermissionType.PairPerm),
            KPID.UnlockGags            => (nameof(PairPerms.UnlockGags),                   PermissionType.PairPerm),
            KPID.RemoveGags            => (nameof(PairPerms.RemoveGags),                   PermissionType.PairPerm),

            KPID.RestrictionVisuals    => (nameof(GlobalPerms.RestrictionVisuals),         PermissionType.Global),
            KPID.ApplyRestrictions     => (nameof(PairPerms.ApplyRestrictions),            PermissionType.PairPerm),
            KPID.LockRestrictions      => (nameof(PairPerms.LockRestrictions),             PermissionType.PairPerm),
            KPID.MaxRestrictionTime    => (nameof(PairPerms.MaxRestrictionTime),           PermissionType.PairPerm),
            KPID.UnlockRestrictions    => (nameof(PairPerms.UnlockRestrictions),           PermissionType.PairPerm),
            KPID.RemoveRestrictions    => (nameof(PairPerms.RemoveRestrictions),           PermissionType.PairPerm),

            KPID.RestraintSetVisuals   => (nameof(GlobalPerms.RestraintSetVisuals),        PermissionType.Global),
            KPID.ApplyRestraintSets    => (nameof(PairPerms.ApplyRestraintSets),           PermissionType.PairPerm),
            KPID.ApplyLayers           => (nameof(PairPerms.ApplyLayers),                  PermissionType.PairPerm),
            KPID.ApplyLayersWhileLocked => (nameof(PairPerms.ApplyLayersWhileLocked),      PermissionType.PairPerm),
            KPID.LockRestraintSets     => (nameof(PairPerms.LockRestraintSets),            PermissionType.PairPerm),
            KPID.MaxRestraintTime      => (nameof(PairPerms.MaxRestraintTime),             PermissionType.PairPerm),
            KPID.UnlockRestraintSets   => (nameof(PairPerms.UnlockRestraintSets),          PermissionType.PairPerm),
            KPID.RemoveLayers          => (nameof(PairPerms.RemoveLayers),                 PermissionType.PairPerm),
            KPID.RemoveLayersWhileLocked => (nameof(PairPerms.RemoveLayersWhileLocked),    PermissionType.PairPerm),
            KPID.RemoveRestraintSets   => (nameof(PairPerms.RemoveRestraintSets),          PermissionType.PairPerm),

            KPID.PuppetPermSit         => (nameof(PairPerms.PuppetPerms),                  PermissionType.PairPerm),
            KPID.PuppetPermEmote       => (nameof(PairPerms.PuppetPerms),                  PermissionType.PairPerm),
            KPID.PuppetPermAlias       => (nameof(PairPerms.PuppetPerms),                  PermissionType.PairPerm),
            KPID.PuppetPermAll         => (nameof(PairPerms.PuppetPerms),                  PermissionType.PairPerm),

            KPID.ApplyPositive         => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),
            KPID.ApplyNegative         => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),
            KPID.ApplySpecial          => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),
            KPID.ApplyPairsMoodles     => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),
            KPID.ApplyOwnMoodles       => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),
            KPID.MaxMoodleTime         => (nameof(PairPerms.MaxMoodleTime),                PermissionType.PairPerm),
            KPID.PermanentMoodles      => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),
            KPID.RemoveAppliedMoodles  => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),
            KPID.RemoveAnyMoodles      => (nameof(PairPerms.MoodleAccess),                 PermissionType.PairPerm),

            KPID.PatternStarting       => (nameof(PairPerms.ExecutePatterns),              PermissionType.PairPerm),
            KPID.PatternStopping       => (nameof(PairPerms.StopPatterns),                 PermissionType.PairPerm),
            KPID.AlarmToggling         => (nameof(PairPerms.ToggleAlarms),                 PermissionType.PairPerm),
            KPID.TriggerToggling       => (nameof(PairPerms.ToggleTriggers),               PermissionType.PairPerm),

            KPID.HypnosisMaxTime       => (nameof(PairPerms.MaxHypnosisTime),              PermissionType.PairPerm),
            KPID.HypnosisEffect        => (nameof(PairPerms.HypnoEffectSending),           PermissionType.PairPerm),

            KPID.HardcoreModeState     => (nameof(PairPerms.InHardcore),                   PermissionType.PairPerm),
            KPID.PairLockedStates      => (nameof(PairPerms.PairLockedStates),             PermissionType.PairPerm),
            KPID.LockedFollowing       => (nameof(HardcoreStatus.LockedFollowing),          PermissionType.Global),
            KPID.LockedEmoteState      => (nameof(HardcoreStatus.LockedEmoteState),         PermissionType.Global),
            KPID.IndoorConfinement     => (nameof(HardcoreStatus.IndoorConfinement),        PermissionType.Global),
            KPID.ChatBoxesHidden       => (nameof(HardcoreStatus.ChatBoxesHidden),          PermissionType.Global),
            KPID.ChatInputHidden       => (nameof(HardcoreStatus.ChatInputHidden),          PermissionType.Global),
            KPID.ChatInputBlocked      => (nameof(HardcoreStatus.ChatInputBlocked),         PermissionType.Global),
            
            KPID.GarbleChannelEditing  => (nameof(PairPerms.AllowGarbleChannelEditing),    PermissionType.PairPerm),
            KPID.HypnoticImage         => (nameof(PairPerms.AllowHypnoImageSending),       PermissionType.PairPerm),
            KPID.PiShockShareCode      => (nameof(PairPerms.PiShockShareCode),             PermissionType.PairPerm),
            KPID.MaxVibrateDuration    => (nameof(PairPerms.MaxVibrateDuration),           PermissionType.PairPerm),

            _ => (string.Empty, PermissionType.Global)
        };

        public static string ToPermAccessValue(this KPID perm, bool isAllEmote = false)
        => perm switch
        {
            KPID.ChatGarblerActive     => nameof(PairPermAccess.ChatGarblerActiveAllowed),
            KPID.ChatGarblerLocked     => nameof(PairPermAccess.ChatGarblerLockedAllowed),
            KPID.GaggedNameplate       => nameof(PairPermAccess.GaggedNameplateAllowed), // maybe remove but im not sure.
            
            KPID.PermanentLocks        => nameof(PairPermAccess.PermanentLocksAllowed),
            KPID.OwnerLocks            => nameof(PairPermAccess.OwnerLocksAllowed),
            KPID.DevotionalLocks       => nameof(PairPermAccess.DevotionalLocksAllowed),
            
            KPID.GagVisuals            => nameof(PairPermAccess.GagVisualsAllowed),
            KPID.ApplyGags             => nameof(PairPermAccess.ApplyGagsAllowed),
            KPID.LockGags              => nameof(PairPermAccess.LockGagsAllowed),
            KPID.MaxGagTime            => nameof(PairPermAccess.MaxGagTimeAllowed),
            KPID.UnlockGags            => nameof(PairPermAccess.UnlockGagsAllowed),
            KPID.RemoveGags            => nameof(PairPermAccess.RemoveGagsAllowed),
            
            KPID.RestrictionVisuals    => nameof(PairPermAccess.RestrictionVisualsAllowed),
            KPID.ApplyRestrictions     => nameof(PairPermAccess.ApplyRestrictionsAllowed),
            KPID.LockRestrictions      => nameof(PairPermAccess.LockRestrictionsAllowed),
            KPID.MaxRestrictionTime    => nameof(PairPermAccess.MaxRestrictionTimeAllowed),
            KPID.UnlockRestrictions    => nameof(PairPermAccess.UnlockRestrictionsAllowed),
            KPID.RemoveRestrictions    => nameof(PairPermAccess.RemoveRestrictionsAllowed),
            
            KPID.RestraintSetVisuals   => nameof(PairPermAccess.RestraintSetVisualsAllowed),
            KPID.ApplyRestraintSets    => nameof(PairPermAccess.ApplyRestraintSetsAllowed),
            KPID.ApplyLayers           => nameof(PairPermAccess.ApplyLayersAllowed),
            KPID.ApplyLayersWhileLocked => nameof(PairPermAccess.ApplyLayersWhileLockedAllowed),
            KPID.LockRestraintSets     => nameof(PairPermAccess.LockRestraintSetsAllowed),
            KPID.MaxRestraintTime      => nameof(PairPermAccess.MaxRestraintTimeAllowed),
            KPID.UnlockRestraintSets   => nameof(PairPermAccess.UnlockRestraintSetsAllowed),
            KPID.RemoveLayers          => nameof(PairPermAccess.RemoveLayersAllowed),
            KPID.RemoveLayersWhileLocked => nameof(PairPermAccess.RemoveLayersWhileLockedAllowed),
            KPID.RemoveRestraintSets   => nameof(PairPermAccess.RemoveRestraintSetsAllowed),
            
            KPID.PuppetPermSit         => nameof(PairPermAccess.PuppetPermsAllowed),
            KPID.PuppetPermEmote       => nameof(PairPermAccess.PuppetPermsAllowed),
            KPID.PuppetPermAlias       => nameof(PairPermAccess.PuppetPermsAllowed),
            KPID.PuppetPermAll         => nameof(PairPermAccess.PuppetPermsAllowed),
            
            KPID.ApplyPositive         => nameof(PairPermAccess.MoodleAccessAllowed),
            KPID.ApplyNegative         => nameof(PairPermAccess.MoodleAccessAllowed),
            KPID.ApplySpecial          => nameof(PairPermAccess.MoodleAccessAllowed),
            KPID.ApplyPairsMoodles     => nameof(PairPermAccess.MoodleAccessAllowed),
            KPID.ApplyOwnMoodles       => nameof(PairPermAccess.MoodleAccessAllowed),
            KPID.MaxMoodleTime         => nameof(PairPermAccess.MaxMoodleTimeAllowed),
            KPID.PermanentMoodles      => nameof(PairPermAccess.MoodleAccessAllowed),
            KPID.RemoveAppliedMoodles  => nameof(PairPermAccess.MoodleAccessAllowed),
            KPID.RemoveAnyMoodles      => nameof(PairPermAccess.MoodleAccessAllowed),

            KPID.PatternStarting       => nameof(PairPermAccess.ExecutePatternsAllowed),
            KPID.PatternStopping       => nameof(PairPermAccess.StopPatternsAllowed),
            KPID.AlarmToggling         => nameof(PairPermAccess.ToggleAlarmsAllowed),
            KPID.TriggerToggling       => nameof(PairPermAccess.ToggleTriggersAllowed),

            KPID.HypnosisMaxTime       => nameof(PairPermAccess.HypnosisMaxTimeAllowed),
            KPID.HypnosisEffect        => nameof(PairPermAccess.HypnosisSendingAllowed),

            KPID.LockedFollowing       => nameof(PairPerms.AllowLockedFollowing),
            KPID.LockedEmoteState      => isAllEmote ? nameof(PairPerms.AllowLockedEmoting) : nameof(PairPerms.AllowLockedSitting),
            KPID.IndoorConfinement     => nameof(PairPerms.AllowIndoorConfinement),
            KPID.Imprisonment          => nameof(PairPerms.AllowImprisonment),
            KPID.GarbleChannelEditing  => nameof(PairPerms.AllowGarbleChannelEditing),
            KPID.ChatBoxesHidden       => nameof(PairPerms.AllowHidingChatBoxes),
            KPID.ChatInputHidden       => nameof(PairPerms.AllowHidingChatInput),
            KPID.ChatInputBlocked      => nameof(PairPerms.AllowChatInputBlocking),
            KPID.HypnoticImage         => nameof(PairPerms.AllowHypnoImageSending),
            _ => string.Empty
        };

    public static void HardcoreConfirmationPopup(MainHub hub, Kinkster k, string name)
    {
        if (!ImGui.IsPopupOpen("Confirm Hardcore"))
            return;

        // center the hardcore window.
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f));
        // set the size of the popup.
        var size = new Vector2(600f, 345f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextWindowSize(size);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.WindowRounding, 12f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudGrey2);

        using var pop = ImRaii.Popup("Confirm Hardcore", WFlags.Modal | WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoMove);
        if (!pop)
            return;

        using (ImRaii.Group())
        {
            CkGui.FontTextCentered("CAUTIONARY WARNING", UiFontService.GagspeakTitleFont, ImGuiColors.DalamudRed);

            CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X);

            CkGui.OutlinedFont("In Hardcore Mode:", ImGuiColors.DalamudOrange, CkCol.LChildSplit.Vec4(), 2);

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline("You can no longer change permissions or edit access for");
            CkGui.ColorTextInline(name, GsCol.VibrantPink.Uint());

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.ColorTextInline(name, GsCol.VibrantPink.Uint());
            CkGui.TextInline("can change non-hardcore permissions with edit access.");

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline("You can set which Hardcore Interactions");
            CkGui.ColorTextInline(name, GsCol.VibrantPink.Uint());
            CkGui.TextInline("can use.");
            CkGui.ColorTextInline("(Only you can change this)", ImGuiColors.ParsedGrey);

            CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X - ImGui.GetStyle().WindowPadding.X);
            CkGui.OutlinedFont("Recommendations:", ImGuiColors.DalamudOrange, CkCol.LChildSplit.Vec4(), 2);

            CkGui.IconText(FAI.AngleDoubleRight);
            ImGui.SameLine();
            CkGui.TextWrapped($"Give {name} EditAccess to perms you are OK with them controlling, " +
                "and enable permissions without access as fit for your dynamics limits.");

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.ColorTextInline("Power Control Adjustment", GsCol.VibrantPink.Uint());
            ImGui.SameLine(0, 1);
            CkGui.HoverIconText(FAI.QuestionCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
            CkGui.AttachToolTip($"Provides a 5 second window for you to change permissions and edit access for {name}.");
            CkGui.TextInline($"can modify your dynamic limits while in Hardcore.");

            CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X - ImGui.GetStyle().WindowPadding.X);

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline("Hardcore Safeword:");
            CkGui.ColorTextInline("/safewordhardcore KINKSTERUID", ImGuiColors.ParsedGold);
            CkGui.TextInline("(this has a 10minute CD).");

            CkGui.IconText(FAI.AngleDoubleRight);
            CkGui.TextInline($"If ChatInput is blocked, use:");
            CkGui.ColorTextInline("CTRL + ALT + BACKSPACE", ImGuiColors.ParsedGold);
            CkGui.TextInline("('Fuck, go back')");
        }
        var yesButton = $"Enter Hardcore for {name}";
        var noButton = "Oh my, take me back!";
        var yesSize = ImGuiHelpers.GetButtonSize(yesButton);
        var noSize = ImGuiHelpers.GetButtonSize(noButton);
        var offsetX = (size.X - (yesSize.X + noSize.X + ImGui.GetStyle().ItemSpacing.X) - ImGui.GetStyle().WindowPadding.X * 2) / 2;
        CkGui.SeparatorSpaced();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);
        if (ImGui.Button(yesButton))
        {
            UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(hub, k.UserData, k.OwnPerms, nameof(PairPerms.InHardcore), !k.OwnPerms.InHardcore));
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button(noButton))
            ImGui.CloseCurrentPopup();
    }
}

