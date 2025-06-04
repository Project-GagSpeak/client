using GagspeakAPI.Data.Permissions;

namespace GagSpeak.CkCommons.Gui.Permissions;

/// <summary> Sticky Pair Permission ID </summary>
public enum SPPID : byte
{
    ChatGarblerActive,
    ChatGarblerLocked,
    LockToyboxUI,

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
    LockRestraintSets,
    MaxRestraintTime,
    UnlockRestraintSets,
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
    RemoveMoodles,

    ToyControl,
    PatternStarting,
    PatternStopping,
    AlarmToggling,
    TriggerToggling,

    HardcoreModeState,
    PairLockedStates,
    ForcedFollow,
    ForcedEmoteState,
    ForcedStay,
    ChatBoxesHidden,
    ChatInputHidden,
    ChatInputBlocked,
    GarbleChannelEditing,

    PiShockShareCode,
    MaxVibrateDuration,
}

public static class SPPIDExtensions
{
    public static (string name, PermissionType type) ToPermValue(this SPPID perm)
        => perm switch
        {
            SPPID.ChatGarblerActive     => (nameof(GlobalPerms.ChatGarblerActive),            PermissionType.Global),
            SPPID.ChatGarblerLocked     => (nameof(GlobalPerms.ChatGarblerLocked),            PermissionType.Global),
            SPPID.LockToyboxUI          => (nameof(GlobalPerms.LockToyboxUI),                 PermissionType.Global),

            SPPID.PermanentLocks        => (nameof(PairPerms.PermanentLocks),                 PermissionType.UniquePairPerm),
            SPPID.OwnerLocks            => (nameof(PairPerms.OwnerLocks),                     PermissionType.UniquePairPerm),
            SPPID.DevotionalLocks       => (nameof(PairPerms.DevotionalLocks),                PermissionType.UniquePairPerm),

            SPPID.GagVisuals            => (nameof(GlobalPerms.GagVisuals),                   PermissionType.Global),
            SPPID.ApplyGags             => (nameof(PairPerms.ApplyGags),                      PermissionType.UniquePairPerm),
            SPPID.LockGags              => (nameof(PairPerms.LockGags),                       PermissionType.UniquePairPerm),
            SPPID.MaxGagTime            => (nameof(PairPerms.MaxGagTime),                     PermissionType.UniquePairPerm),
            SPPID.UnlockGags            => (nameof(PairPerms.UnlockGags),                     PermissionType.UniquePairPerm),
            SPPID.RemoveGags            => (nameof(PairPerms.RemoveGags),                     PermissionType.UniquePairPerm),

            SPPID.RestrictionVisuals    => (nameof(GlobalPerms.RestrictionVisuals),           PermissionType.Global),
            SPPID.ApplyRestrictions     => (nameof(PairPerms.ApplyRestrictions),              PermissionType.UniquePairPerm),
            SPPID.LockRestrictions      => (nameof(PairPerms.LockRestrictions),               PermissionType.UniquePairPerm),
            SPPID.MaxRestrictionTime    => (nameof(PairPerms.MaxRestrictionTime),             PermissionType.UniquePairPerm),
            SPPID.UnlockRestrictions    => (nameof(PairPerms.UnlockRestrictions),             PermissionType.UniquePairPerm),
            SPPID.RemoveRestrictions    => (nameof(PairPerms.RemoveRestrictions),             PermissionType.UniquePairPerm),

            SPPID.RestraintSetVisuals   => (nameof(GlobalPerms.RestraintSetVisuals),          PermissionType.Global),
            SPPID.ApplyRestraintSets    => (nameof(PairPerms.ApplyRestraintSets),             PermissionType.UniquePairPerm),
            SPPID.LockRestraintSets     => (nameof(PairPerms.LockRestraintSets),              PermissionType.UniquePairPerm),
            SPPID.MaxRestraintTime      => (nameof(PairPerms.MaxRestraintTime),               PermissionType.UniquePairPerm),
            SPPID.UnlockRestraintSets   => (nameof(PairPerms.UnlockRestraintSets),            PermissionType.UniquePairPerm),
            SPPID.RemoveRestraintSets   => (nameof(PairPerms.RemoveRestraintSets),            PermissionType.UniquePairPerm),

            SPPID.PuppetPermSit         => (nameof(PairPerms.PuppetPerms),                    PermissionType.UniquePairPerm),
            SPPID.PuppetPermEmote       => (nameof(PairPerms.PuppetPerms),                    PermissionType.UniquePairPerm),
            SPPID.PuppetPermAlias       => (nameof(PairPerms.PuppetPerms),                    PermissionType.UniquePairPerm),
            SPPID.PuppetPermAll         => (nameof(PairPerms.PuppetPerms),                    PermissionType.UniquePairPerm),

            SPPID.ApplyPositive         => (nameof(PairPerms.MoodlePerms),                    PermissionType.UniquePairPerm),
            SPPID.ApplyNegative         => (nameof(PairPerms.MoodlePerms),                    PermissionType.UniquePairPerm),
            SPPID.ApplySpecial          => (nameof(PairPerms.MoodlePerms),                    PermissionType.UniquePairPerm),
            SPPID.ApplyPairsMoodles     => (nameof(PairPerms.MoodlePerms),                    PermissionType.UniquePairPerm),
            SPPID.ApplyOwnMoodles       => (nameof(PairPerms.MoodlePerms),                    PermissionType.UniquePairPerm),
            SPPID.MaxMoodleTime         => (nameof(PairPerms.MaxMoodleTime),                  PermissionType.UniquePairPerm),
            SPPID.RemoveMoodles         => (nameof(PairPerms.MoodlePerms),                    PermissionType.UniquePairPerm),

            SPPID.ToyControl            => (nameof(PairPerms.RemoteControlAccess),            PermissionType.UniquePairPerm),
            SPPID.PatternStarting       => (nameof(PairPerms.ExecutePatterns),                PermissionType.UniquePairPerm),
            SPPID.PatternStopping       => (nameof(PairPerms.StopPatterns),                   PermissionType.UniquePairPerm),
            SPPID.AlarmToggling         => (nameof(PairPerms.ToggleAlarms),                   PermissionType.UniquePairPerm),
            SPPID.TriggerToggling       => (nameof(PairPerms.ToggleTriggers),                 PermissionType.UniquePairPerm),

            SPPID.HardcoreModeState     => (nameof(PairPerms.InHardcore),                     PermissionType.UniquePairPerm),
            SPPID.PairLockedStates      => (nameof(PairPerms.PairLockedStates),               PermissionType.UniquePairPerm),
/*          SPPID.ForcedFollow          => (nameof(GlobalPerms.ForcedFollow),                 PermissionType.Global),
            SPPID.ForcedEmoteState      => (nameof(GlobalPerms.ForcedEmoteState),             PermissionType.Global),
            SPPID.ForcedStay            => (nameof(GlobalPerms.ForcedStay),                   PermissionType.Global),
            SPPID.ChatBoxesHidden       => (nameof(GlobalPerms.ChatBoxesHidden),              PermissionType.Global),
            SPPID.ChatInputHidden       => (nameof(GlobalPerms.ChatInputHidden),              PermissionType.Global),
            SPPID.ChatInputBlocked      => (nameof(GlobalPerms.ChatInputBlocked),             PermissionType.Global),
            SPPID.GarbleChannelEditing  => (nameof(GlobalPerms.ChatGarblerChannelsBitfield),  PermissionType.Global),*/
            SPPID.PiShockShareCode      => (nameof(PairPerms.PiShockShareCode),               PermissionType.UniquePairPerm),
            SPPID.MaxVibrateDuration    => (nameof(PairPerms.MaxVibrateDuration),             PermissionType.UniquePairPerm),

            _ => (string.Empty, PermissionType.Global)
        };

        public static string ToPermAccessValue(this SPPID perm, bool isAllEmote = false)
        => perm switch
        {
            SPPID.ChatGarblerActive     => nameof(PairPermAccess.ChatGarblerActiveAllowed),
            SPPID.ChatGarblerLocked     => nameof(PairPermAccess.ChatGarblerLockedAllowed),
            SPPID.LockToyboxUI          => nameof(PairPermAccess.LockToyboxUIAllowed),
            SPPID.PermanentLocks        => nameof(PairPermAccess.PermanentLocksAllowed),
            SPPID.OwnerLocks            => nameof(PairPermAccess.OwnerLocksAllowed),
            SPPID.DevotionalLocks       => nameof(PairPermAccess.DevotionalLocksAllowed),
            SPPID.GagVisuals            => nameof(PairPermAccess.GagVisualsAllowed),
            SPPID.ApplyGags             => nameof(PairPermAccess.ApplyGagsAllowed),
            SPPID.LockGags              => nameof(PairPermAccess.LockGagsAllowed),
            SPPID.MaxGagTime            => nameof(PairPermAccess.MaxGagTimeAllowed),
            SPPID.UnlockGags            => nameof(PairPermAccess.UnlockGagsAllowed),
            SPPID.RemoveGags            => nameof(PairPermAccess.RemoveGagsAllowed),
            SPPID.RestrictionVisuals    => nameof(PairPermAccess.RestrictionVisualsAllowed),
            SPPID.ApplyRestrictions     => nameof(PairPermAccess.ApplyRestrictionsAllowed),
            SPPID.LockRestrictions      => nameof(PairPermAccess.LockRestrictionsAllowed),
            SPPID.MaxRestrictionTime    => nameof(PairPermAccess.MaxRestrictionTimeAllowed),
            SPPID.UnlockRestrictions    => nameof(PairPermAccess.UnlockRestrictionsAllowed),
            SPPID.RemoveRestrictions    => nameof(PairPermAccess.RemoveRestrictionsAllowed),
            SPPID.RestraintSetVisuals   => nameof(PairPermAccess.RestraintSetVisualsAllowed),
            SPPID.ApplyRestraintSets    => nameof(PairPermAccess.ApplyRestraintSetsAllowed),
            SPPID.LockRestraintSets     => nameof(PairPermAccess.LockRestraintSetsAllowed),
            SPPID.MaxRestraintTime      => nameof(PairPermAccess.MaxRestraintTimeAllowed),
            SPPID.UnlockRestraintSets   => nameof(PairPermAccess.UnlockRestraintSetsAllowed),
            SPPID.RemoveRestraintSets   => nameof(PairPermAccess.RemoveRestraintSetsAllowed),
            SPPID.PuppetPermSit         => nameof(PairPermAccess.PuppetPermsAllowed),
            SPPID.PuppetPermEmote       => nameof(PairPermAccess.PuppetPermsAllowed),
            SPPID.PuppetPermAlias       => nameof(PairPermAccess.PuppetPermsAllowed),
            SPPID.PuppetPermAll         => nameof(PairPermAccess.PuppetPermsAllowed),
            SPPID.ApplyPositive         => nameof(PairPermAccess.MoodlePermsAllowed),
            SPPID.ApplyNegative         => nameof(PairPermAccess.MoodlePermsAllowed),
            SPPID.ApplySpecial          => nameof(PairPermAccess.MoodlePermsAllowed),
            SPPID.ApplyPairsMoodles     => nameof(PairPermAccess.MoodlePermsAllowed),
            SPPID.ApplyOwnMoodles       => nameof(PairPermAccess.MoodlePermsAllowed),
            SPPID.MaxMoodleTime         => nameof(PairPermAccess.MaxMoodleTimeAllowed),
            SPPID.PermanentMoodles      => nameof(PairPermAccess.MoodlePermsAllowed),
            SPPID.RemoveMoodles         => nameof(PairPermAccess.MoodlePermsAllowed),
            SPPID.ToyControl            => nameof(PairPermAccess.RemoteControlAccessAllowed),
            SPPID.PatternStarting       => nameof(PairPermAccess.ExecutePatternsAllowed),
            SPPID.PatternStopping       => nameof(PairPermAccess.StopPatternsAllowed),
            SPPID.AlarmToggling         => nameof(PairPermAccess.ToggleAlarmsAllowed),
            SPPID.TriggerToggling       => nameof(PairPermAccess.ToggleTriggersAllowed),

            SPPID.ForcedFollow          => nameof(PairPerms.AllowForcedFollow),
            SPPID.ForcedEmoteState      => isAllEmote ? nameof(PairPerms.AllowForcedEmote) : nameof(PairPerms.AllowForcedEmote),
            SPPID.ForcedStay            => nameof(PairPerms.AllowForcedStay),
            SPPID.ChatBoxesHidden       => nameof(PairPerms.AllowHidingChatBoxes),
            SPPID.ChatInputHidden       => nameof(PairPerms.AllowHidingChatInput),
            SPPID.ChatInputBlocked      => nameof(PairPerms.AllowChatInputBlocking),
            SPPID.GarbleChannelEditing  => nameof(PairPerms.AllowGarbleChannelEditing),
            _ => string.Empty
        };
}
