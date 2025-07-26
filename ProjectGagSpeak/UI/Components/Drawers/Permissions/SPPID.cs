using GagspeakAPI.Data.Permissions;

namespace GagSpeak.Gui;

/// <summary> Sticky Pair Permission ID </summary>
public enum SPPID : byte
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
    RemoveMoodles,

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

public static class SPPIDExtensions
{
    public static (string name, PermissionType type) ToPermValue(this SPPID perm)
        => perm switch
        {
            SPPID.ChatGarblerActive     => (nameof(GlobalPerms.ChatGarblerActive),            PermissionType.Global),
            SPPID.ChatGarblerLocked     => (nameof(GlobalPerms.ChatGarblerLocked),            PermissionType.Global),

            SPPID.PermanentLocks        => (nameof(PairPerms.PermanentLocks),                 PermissionType.PairPerm),
            SPPID.OwnerLocks            => (nameof(PairPerms.OwnerLocks),                     PermissionType.PairPerm),
            SPPID.DevotionalLocks       => (nameof(PairPerms.DevotionalLocks),                PermissionType.PairPerm),

            SPPID.GagVisuals            => (nameof(GlobalPerms.GagVisuals),                   PermissionType.Global),
            SPPID.ApplyGags             => (nameof(PairPerms.ApplyGags),                      PermissionType.PairPerm),
            SPPID.LockGags              => (nameof(PairPerms.LockGags),                       PermissionType.PairPerm),
            SPPID.MaxGagTime            => (nameof(PairPerms.MaxGagTime),                     PermissionType.PairPerm),
            SPPID.UnlockGags            => (nameof(PairPerms.UnlockGags),                     PermissionType.PairPerm),
            SPPID.RemoveGags            => (nameof(PairPerms.RemoveGags),                     PermissionType.PairPerm),

            SPPID.RestrictionVisuals    => (nameof(GlobalPerms.RestrictionVisuals),           PermissionType.Global),
            SPPID.ApplyRestrictions     => (nameof(PairPerms.ApplyRestrictions),              PermissionType.PairPerm),
            SPPID.LockRestrictions      => (nameof(PairPerms.LockRestrictions),               PermissionType.PairPerm),
            SPPID.MaxRestrictionTime    => (nameof(PairPerms.MaxRestrictionTime),             PermissionType.PairPerm),
            SPPID.UnlockRestrictions    => (nameof(PairPerms.UnlockRestrictions),             PermissionType.PairPerm),
            SPPID.RemoveRestrictions    => (nameof(PairPerms.RemoveRestrictions),             PermissionType.PairPerm),

            SPPID.RestraintSetVisuals   => (nameof(GlobalPerms.RestraintSetVisuals),          PermissionType.Global),
            SPPID.ApplyRestraintSets    => (nameof(PairPerms.ApplyRestraintSets),             PermissionType.PairPerm),
            SPPID.ApplyLayers           => (nameof(PairPerms.ApplyLayers),                    PermissionType.PairPerm),
            SPPID.ApplyLayersWhileLocked => (nameof(PairPerms.ApplyLayersWhileLocked),        PermissionType.PairPerm),
            SPPID.LockRestraintSets     => (nameof(PairPerms.LockRestraintSets),              PermissionType.PairPerm),
            SPPID.MaxRestraintTime      => (nameof(PairPerms.MaxRestraintTime),               PermissionType.PairPerm),
            SPPID.UnlockRestraintSets   => (nameof(PairPerms.UnlockRestraintSets),            PermissionType.PairPerm),
            SPPID.RemoveLayers          => (nameof(PairPerms.RemoveLayers),                   PermissionType.PairPerm),
            SPPID.RemoveLayersWhileLocked => (nameof(PairPerms.RemoveLayersWhileLocked),      PermissionType.PairPerm),
            SPPID.RemoveRestraintSets   => (nameof(PairPerms.RemoveRestraintSets),            PermissionType.PairPerm),

            SPPID.PuppetPermSit         => (nameof(PairPerms.PuppetPerms),                    PermissionType.PairPerm),
            SPPID.PuppetPermEmote       => (nameof(PairPerms.PuppetPerms),                    PermissionType.PairPerm),
            SPPID.PuppetPermAlias       => (nameof(PairPerms.PuppetPerms),                    PermissionType.PairPerm),
            SPPID.PuppetPermAll         => (nameof(PairPerms.PuppetPerms),                    PermissionType.PairPerm),

            SPPID.ApplyPositive         => (nameof(PairPerms.MoodlePerms),                    PermissionType.PairPerm),
            SPPID.ApplyNegative         => (nameof(PairPerms.MoodlePerms),                    PermissionType.PairPerm),
            SPPID.ApplySpecial          => (nameof(PairPerms.MoodlePerms),                    PermissionType.PairPerm),
            SPPID.ApplyPairsMoodles     => (nameof(PairPerms.MoodlePerms),                    PermissionType.PairPerm),
            SPPID.ApplyOwnMoodles       => (nameof(PairPerms.MoodlePerms),                    PermissionType.PairPerm),
            SPPID.MaxMoodleTime         => (nameof(PairPerms.MaxMoodleTime),                  PermissionType.PairPerm),
            SPPID.PermanentMoodles      => (nameof(PairPerms.MoodlePerms),                    PermissionType.PairPerm),
            SPPID.RemoveMoodles         => (nameof(PairPerms.MoodlePerms),                    PermissionType.PairPerm),

            SPPID.PatternStarting       => (nameof(PairPerms.ExecutePatterns),                PermissionType.PairPerm),
            SPPID.PatternStopping       => (nameof(PairPerms.StopPatterns),                   PermissionType.PairPerm),
            SPPID.AlarmToggling         => (nameof(PairPerms.ToggleAlarms),                   PermissionType.PairPerm),
            SPPID.TriggerToggling       => (nameof(PairPerms.ToggleTriggers),                 PermissionType.PairPerm),

            SPPID.HypnosisMaxTime       => (nameof(PairPerms.MaxHypnosisTime),                PermissionType.PairPerm),
            SPPID.HypnosisEffect        => (nameof(PairPerms.HypnoEffectSending),             PermissionType.PairPerm),

            SPPID.HardcoreModeState     => (nameof(PairPerms.InHardcore),                     PermissionType.PairPerm),
            SPPID.PairLockedStates      => (nameof(PairPerms.PairLockedStates),               PermissionType.PairPerm),
/*          SPPID.LockedFollowing          => (nameof(GlobalPerms.LockedFollowing),                 PermissionType.Global),
            SPPID.LockedEmoteState      => (nameof(GlobalPerms.LockedEmoteState),             PermissionType.Global),
            SPPID.IndoorConfinement            => (nameof(GlobalPerms.IndoorConfinement),                   PermissionType.Global),
            SPPID.ChatBoxesHidden       => (nameof(GlobalPerms.ChatBoxesHidden),              PermissionType.Global),
            SPPID.ChatInputHidden       => (nameof(GlobalPerms.ChatInputHidden),              PermissionType.Global),
            SPPID.ChatInputBlocked      => (nameof(GlobalPerms.ChatInputBlocked),             PermissionType.Global),
            SPPID.GarbleChannelEditing  => (nameof(GlobalPerms.AllowedGarblerChannels),  PermissionType.Global),*/

            SPPID.HypnoticImage         => (nameof(PairPerms.AllowHypnoImageSending),         PermissionType.PairPerm),
            SPPID.PiShockShareCode      => (nameof(PairPerms.PiShockShareCode),               PermissionType.PairPerm),
            SPPID.MaxVibrateDuration    => (nameof(PairPerms.MaxVibrateDuration),             PermissionType.PairPerm),

            _ => (string.Empty, PermissionType.Global)
        };

        public static string ToPermAccessValue(this SPPID perm, bool isAllEmote = false)
        => perm switch
        {
            SPPID.ChatGarblerActive     => nameof(PairPermAccess.ChatGarblerActiveAllowed),
            SPPID.ChatGarblerLocked     => nameof(PairPermAccess.ChatGarblerLockedAllowed),
            SPPID.GaggedNameplate       => nameof(PairPermAccess.GaggedNameplateAllowed), // maybe remove but im not sure.
            
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
            SPPID.ApplyLayers           => nameof(PairPermAccess.ApplyLayersAllowed),
            SPPID.ApplyLayersWhileLocked => nameof(PairPermAccess.ApplyLayersWhileLockedAllowed),
            SPPID.LockRestraintSets     => nameof(PairPermAccess.LockRestraintSetsAllowed),
            SPPID.MaxRestraintTime      => nameof(PairPermAccess.MaxRestraintTimeAllowed),
            SPPID.UnlockRestraintSets   => nameof(PairPermAccess.UnlockRestraintSetsAllowed),
            SPPID.RemoveLayers          => nameof(PairPermAccess.RemoveLayersAllowed),
            SPPID.RemoveLayersWhileLocked => nameof(PairPermAccess.RemoveLayersWhileLockedAllowed),
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

            SPPID.PatternStarting       => nameof(PairPermAccess.ExecutePatternsAllowed),
            SPPID.PatternStopping       => nameof(PairPermAccess.StopPatternsAllowed),
            SPPID.AlarmToggling         => nameof(PairPermAccess.ToggleAlarmsAllowed),
            SPPID.TriggerToggling       => nameof(PairPermAccess.ToggleTriggersAllowed),

            SPPID.HypnosisMaxTime       => nameof(PairPermAccess.HypnosisMaxTimeAllowed),
            SPPID.HypnosisEffect        => nameof(PairPermAccess.HypnosisSendingAllowed),

            SPPID.LockedFollowing       => nameof(PairPerms.AllowLockedFollowing),
            SPPID.LockedEmoteState      => isAllEmote ? nameof(PairPerms.AllowLockedEmoting) : nameof(PairPerms.AllowLockedSitting),
            SPPID.IndoorConfinement     => nameof(PairPerms.AllowIndoorConfinement),
            SPPID.Imprisonment          => nameof(PairPerms.AllowImprisonment),
            SPPID.GarbleChannelEditing  => nameof(PairPerms.AllowGarbleChannelEditing),
            SPPID.ChatBoxesHidden       => nameof(PairPerms.AllowHidingChatBoxes),
            SPPID.ChatInputHidden       => nameof(PairPerms.AllowHidingChatInput),
            SPPID.ChatInputBlocked      => nameof(PairPerms.AllowChatInputBlocking),
            SPPID.HypnoticImage         => nameof(PairPerms.AllowHypnoImageSending),
            _ => string.Empty
        };
}
