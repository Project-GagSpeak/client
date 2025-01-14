namespace GagSpeak.Services.Tutorial;

public enum TutorialType
{
    MainUi,
    Remote,
    Gags,
    GagStorage,
    Restraints,
    CursedLoot,
    Toybox,
    Patterns,
    Triggers,
    Alarms,
    Achievements,
}

public enum StepsMainUi
{
    ConnectionState, // Connection Link Button.
    Homepage, // Purpose of Homepage.
    ToWhitelistPage, // Direct to Whitelist.
    Whitelist, // It's Purpose.
    AddingKinksters, // How to Add Pairs.
    ToAccountPage, // Inform User how to share their UID by sending them here after.
    UserProfilePicture, // Self Explanitory
    UserIdentification, // What the UID is, and how it is used.
    SafewordPartOne, // How to set the safeword
    SafewordPartTwo, // Importance of safeword.
    ProfileEditing, // Show User where to go to edit their profile.
    AccessingSettings, // What the Settings Entails.
    SelfPlug, // How to support
    ToGlobalChat, // How to access Global Chat.
    GlobalChat, // What it is.
    ToPatternHub, // How to access Pattern Hub.
    PatternHub, // What it is.
    PatternHubSearch,
    PatternHubUpdate,
    PatternHubFilterType,
    PatternHubResultOrder,

}
public enum StepsRemote
{
    PowerButton,
    FloatButton,
    LoopButton,
    TimerButton,
    ControllableCircle,
    OutputDisplay,
    DeviceList,
}

public enum StepsActiveGags
{
    LayersInfo,
    EquippingGags,
    SelectingPadlocks,
    PadlockTypes, // Types of padlocks.
    LockingPadlocks, // Lock password Padlock.
    UnlockingPadlocks, // unlock the padlock.
    RemovingGags, // Removing Gag
}

public enum StepsGagStorage
{
    GagStorageSelection, // picking a gag to customize
    GagGlamours, // How to change the look of a gag.
    Adjustements, // How to adjust the gag.
    Moodles, // skip if no moodles available.
    Audio,
    SavingCustomizations, // Mention changes are not saved until save is pressed.
}

public enum StepsRestraints
{
    AddingNewRestraint,
    InfoTab,
    ToAppearanceTab,
    SettingGear,
    Metadata,
    ImportingGear,
    ImportingCustomizations,
    ApplyingCustomizations,
    ClearingCustomizations,
    ToModsTab,
    SelectingMod,
    AddingMod,
    ModOptions,
    ToMoodlesTab,
    MoodlesStatuses,
    MoodlesPresets,
    AppendedMoodles,
    ToSoundsTab,
    Sounds,
    ToHardcoreTraitsTab,
    SelectingPair,
    GrantingTraits,
    AddingNewSet,
    RestraintSetList,
    TogglingSets,
    LockingSets,
}

public enum StepsCursedLoot
{
    CreatingCursedItems,
    NamingCursedItems,
    SettingCursedItemType,
    AddingCursedItem,
    CursedItemList,
    TheEnabledPool,
    AddingToEnabledPool,
    RemovingFromEnabledPool,
    LowerLockTimer,
    UpperLockTimer,
    RollChance,
}
public enum StepsToybox
{
    IntifaceConnection,
    OpeningIntiface,
    SelectingVibratorType, // Make it select simulated after this.
    AudioTypeSelection,
    PlaybackAudioDevice, // after this is done switch to action mode
    DeviceScanner,
}

public enum StepsPatterns
{
    CreatingNewPatterns,
    RecordedDuration,
    FloatButton,
    LoopButton,
    DraggableCircle,
    RecordingButton, // let us start the recording
    StoppingRecording, // let us stop the recording.
    SavingPatternName,
    SavingPatternDescription,
    SavingPatternLoop,
    DiscardingPattern,
    FinalizingSave,
    ModifyingPatterns,
    EditDisplayInfo,
    EditLoopToggle,
    EditStartPoint,
    EditPlaybackDuration,
    ApplyChanges,
}

public enum StepsTriggers
{
    CreatingTriggers,
    InfoTab,
    ToTriggerActions,
    ActionExecutionType,
    SelectingTriggerType,
    ToChatText,
    ToActionTrigger,
    ToHealthTrigger,
    ToRestraintTrigger,
    ToGagTrigger,
    ToSocialTrigger,
    ToEmoteTrigger,
    SavingTriggers,
    TriggerList,
    TogglingTriggers,
}

public enum StepsAlarms
{
    CreatingAlarms,
    SettingAlarmName,
    AlarmLocalTimeZone,
    SettingAlarmTime,
    SettingAlarmPattern,
    SettingAlarmStartPoint,
    SettingAlarmDuration,
    SettingFrequency,
    SavingAlarms,
    AlarmList,
    TogglingAlarms,
}

public enum StepsAchievements
{
    OverallProgress,
    ResettingAchievements,
    SectionList,
    Titles,
    ProgressDisplay,
    RewardPreview,
}
