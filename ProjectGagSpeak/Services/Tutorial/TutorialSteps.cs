namespace GagSpeak.Services.Tutorial;

public enum TutorialType
{
    MainUi,
    Remote,
    Restraints,
    Restrictions,
    Gags,
    Collar,
    CursedLoot,
    Puppeteer,
    Toys,
    VibeLobby,
    Patterns,
    Alarms,
    Triggers,
    ModPresets,
    Achievements,
}

public enum StepsMainUi
{
    InitialWelcome, // welcome message, warn user to follow tutorial for basic overview, and how to access them at any time (the ? buttons)
    ConnectionState, // Connection Button
    AddingKinksters, // How to Add Pairs, Select dropdown button.
    AttachingMessages, // optional message attachment to requests, close menu on next, and move to account page.
    RequestsPage, // Show the requests page, explain how to accept/reject requests.
    Whitelist, // Overview whitelist.
    Homepage, // overview of account page.
    ClientUID, // indicate the client's UID, and how this is what others give you to pair.
    Safewords, // Importance of safeword.
    SettingSafeword, // How to set safeword, highlight edit button.
    ProfileEditing, // on my profile button, open editor on action.
    ProfilePublicity, // what being public -vs- private means.
    SettingTitles, // where titles are shown, how to earn them.
    CustomizingProfile, // How to unlock customizations. (WIP)
    ProfileDescription, // On click to next step, open light kinkplate, and highlight light preview.
    ProfilePreviewLight, // On click, close light preview, and open full preview.
    ProfilePreviewFull, // On click, close full preview, open image editor.
    ProfileEditImage, // On click, close image editor.
    ProfileSaving, // Emphasis on saving changes, and how editing without saving reverts changes.
    PatternHub, // overview of pattern hub.
    PatternSearch, // how searches are filtered.
    PatternResults, // up to 50 results, ext ext, liking and downloading.
    MoodleHub, // overview of moodle hub.
    MoodleSearch, // how searches are filtered.
    MoodleResults, // up to 50 results, ext ext, liking and downloading.
    GlobalChat, // overview of global chat, how it works, log clearing ext.
    UsingGlobalChat, // must be verified to use, how to verify, ext.
    ChatEmotes, // how to include emotes.
    ChatScroll, // how to scroll,
    ChatMessageExamine, // onHover things, move to account page after, for plug.
    SelfPlug, // How to support
}
public enum StepsRemote
{
    ConnectedUser, // what user the remote is for.
    DeviceList, // the user's devices.
    MotorList, // the devices Motors.
    TimeAlive, // how long remote is powered on for.
    LoopButton, // hotkey alternatives, what it does, ext.
    FloatButton, // hotkey alternatives, what it does, ext.
    PowerButton, // what gets recorded what doesnt.
    MotorControl, // where to control selected motors.
    OutputDisplay, // result output from motor control.
}

public enum StepsRestraints
{
    Searching, // where to lookup restraints.
    CreatingFolders, // (on click, make tutorial folder)
    CreatingRestraints, // Plus button, on click, create a new blank one
    RestraintList, // 'created ones stored here', select tutorial one on click.
    SelectedRestraint, // how to preview contents of a restraint set from selection.
    EnteringEditor, // on click, open editor for item.
    EditName,
    EditMeta,
    HardcoreTraits, // these can only be allowed by pairs you set in allowances.
    Arousal, // how arousal works.
    EquipSlots,
    Importing, // on click, import current equipment.
    SlotTypes, // different slot types, on click, swap helmet to advanced.
    Overlay, // what overlay is, and how it works. (move to overlay after)
    OverlayBuffer, // duplicate previous step, so user can see changes, then switch to layers panel
    Layers, // what layers are, how they work.
    AddingLayers, // show where to add. (on click, add 2)
    LayerTypes, // on click, swap first layer to opposite type.
    LayerTypesBuffer, // duplicate previous step, then switch to modsmoodles panel
    ModsMoodles, // overview.
    AddingMods,
    AddingMoodles,
    SwapMoodleTypes, // on click, add a moodle, if any are present.
    MoodlePreview, // moodle preview box.
    CancelingChanges, // what happens if you dont want to save edits.
    SavingChanges, // on click, saving changes and close.
    SelectingThumbnails, // how to change thumbnails for a restraint set. (open browser)
    UpdateContents, // refreshing folder image contents.
    DisplayScale,
    ImportByFile, // how importing by file works.
    ImportByClipboard, // how importing by clipboard works. (close thumbnail window)
    ActiveRestraint, // where the active restraint is displayed.
    SelectingRestraint, // select created restraint on click.
    LockingRestraint, // lock system overview. (lock on click)
    EditingLayers, // how layer editing works, be sure they know they can't edit them if locked by someone else.
    // also probably change some layers or something and hit update)
    UnlockingRestraints, // basic unlock overview.
    RemovingRestraints, // right click image area thing.
}

public enum StepsRestrictions
{
    Searching, // where to lookup restraints.
    CreatingFolders, // (on click, make tutorial folder)
    CreatingRestriction, // Plus button, on click, create a new blank one
    RestrictionTypes, // explain different types.
    // Create Hypnotic Restriction for best tutorial experience.
    RestrictionList, // 'created ones stored here', select tutorial one on click.
    SelectedRestriction, // how to preview contents of a restraint set from selection.
    EnteringEditor, // on click, open editor for item.
    EditName,
    EditMeta,
    ItemGlamour, // Selecting glamour.
    HypnoInfo, // what hypno is, how it works.
    FirstPersonLock, // what this does.
    SelectingImage, // where to edit the attached image (open selector on click)
    UpdateContents,
    DisplayScale, // how to scale the image.
    ImportingByFile, // how importing by file works.
    ImportingByClipboard, // how importing by clipboard works. (close thumbnail window & select default)
    EffectEditing, // how to edit the selected images display effect. (on click, open effect editor).
    EffectConfig, // various configs,
    EffectWords, // display word editing.
    EffectColors, // color controls. (on click, close hypno editor)
    HardcoreTraits, // these can only be allowed by pairs you set in allowances.
    Arousal, // how arousal works.
    AttachedMoodle, // overview.
    SwitchingMoodleType, // how to swap types. (select first from list if any exist).
    SelectedMoodlePreview, // where preview displays.
    AttachedMod, // overview,
    SelectingMod, // select mod first,
    SelectingPreset, // preset second,
    PresetPreview, // to view what options you picked for the preset.
    CancelingChanges, // what happens if you dont want to save edits.
    SavingChanges, // on click, saving changes and close.
    SelectingThumbnails, // how to change thumbnails for a restraint set. (open browser)
    Applying, // where to apply restrictions, or view active ones.
    Selecting, // locate first open one, if all are full, do not apply any. (otherwise, apply tutorial)
    NoFreeSlots, // Shows if no slots are available
    Locking, // lock system overview. (lock on click)
    Unlocking, // how 2 unlocky (if possible, unlock on next)
    Removing, // remove restriction on click if tutorial one is present.
}

public enum StepsGags
{
    Searching, // where to lookup gags.
    CreatingFolders, // optional way to filter gags.
    GagList, // 'created ones stored here', select tutorial one on click. (select ballgag on next)
    SelectedGag, // content preview.
    VisualState, // explain what this does.
    EnteringEditor, // on click, open editor for item.
    EditMeta,
    ItemGlamour, // Selecting glamour.
    HardcoreTraits, // these can only be allowed by pairs you set in allowances.
    Arousal, // how arousal works.
    CPlusPreset, // setting the preset thingy.
    AttachedMoodle, // overview.
    SwitchingMoodleType, // how to swap types. (select first from list if any exist).
    SelectedMoodlePreview, // where preview displays.
    AttachedMod, // overview,
    SelectingMod, // select mod first,
    SelectingPreset, // preset second,
    PresetPreview, // to view what options you picked for the preset.
    CancelingChanges, // what happens if you dont want to save edits.
    SavingChanges, // on click, saving changes and close.
    ActiveGags, // where to apply Gags, or view active ones.
    Selecting, // locate first open one, if all are full, do not apply any. (otherwise, apply tutorial)
    Locking, // lock system overview. (lock on click)
    Unlocking, // how 2 unlocky (if possible, unlock on next)
    Removing, // remove restriction on click if tutorial one is present.
}

public enum StepsCollar
{
    Step1,
    Step2,
    Step3,
    Step4,
    Step5,
    Step6,
}

public enum StepsCursedLoot
{
    Searching, // where to lookup restraints.
    CreatingFolders, // (on click, make tutorial folder)
    CreatingLootItem, // Plus button, on click, create a new blank one
    CursedItemList, // select tutorial one on click.
    RenamingItems, // how to rename the loot items.
    EditingItems, // how to enter cursed item editor (do so on click)
    SelectingType, // (select gag type on next)
    Priority, // what priority does, and how it works.
    Overriding, // how overriding works, and what it does.
    DiscardingChanges, // how to exit editor without saving.
    SavingChanges, // how to save and exit editor. (do so on click)
    CursedLootPool, // where all enabled items are stored.
    AddingItemsToPool, // how to add items to the pool. (do so on click)
    RemovingItemsFromPool, // how to remove items from the pool. (do so on click)
    LowerLockTimer,
    UpperLockTimer,
    RollChance,
}

public enum StepsPuppeteer
{
    Overview, // overview, select alias tab JIC
    AliasPage, // Explain Aliases
    AliasList, // List of aliases
    SearchBar, // Searching, create and select an alias here
    SelectedAlias, // Details of selected alias
    EditingAlias,
    EditAliasName,
    EditAliasActions,
    EditAliasPermissions,
    EditSavingAliases,
    PuppeteersPage,
    PuppeteersPairs,
    PuppeteersPairSettings,
    PuppeteersPairName,
    PuppeteersPairTriggers,
    PuppeteersPairOrders,
    PuppeteersAdvanced,
    MarionettesPage,
    MarionettesPairs,
    MarionettesPairAliases,
    MarionettesPairPermissions,
    MarionettesPairName,
    MarionettesPairTriggerWords,
    MarionettesPairAliasConfig
}

public enum StepsToys
{
    Overview, // overview, simulated -vs- real, ext.
    IntifaceOpener, // what it does when clicked.
    CurrentConnection, // current connection status.
    ThrottleConnection, // where to throttle connection,
    AddingSimulatedToys, // how to add simulated toys.
    DeviceList, // where added and scanned devices are listed.
    SelectingDevice, // how to select a device. (select simulated tutorial made one)
    InteractableState, // what it is, how it impacts toy control.
    NamingDevices, // how to nickname devices.
    DeviceHealth, // battery life.
    DeviceMotors, // To view the attributes of a devices motors.
    ScanForRealToys, // how to scan for toys through intiface.
    DeviceScanner,
}

// Vibe lobbies are still a WIP, but in theory, do function correctly.
public enum StepsVibeLobby
{
    VibeRoomSearch,
    SearchFilters,
    SearchSorting,
    SearchTags,
    RoomResults,
    SelectedLobby,
    LobbyInfo,
    JoiningLobby, // do not force this... that would be rude.
    HostingARoom,
    NamingRoom, // how to name the room we make.
    RoomPublicity, // how passwords dictate room visibility.
    RoomDescription, // what is displayed in invites and search results.
    RoomTags, // how your room might be filtered, on click, host the room, with a private password (randomly generated)
    RoomParticipants, // to view the current people inside of a vibe room.
    PreviewingOthers, // how to view others devices, if you have access.
    RoomChat, // the chat private to the created room.
    RoomEmotes, // how to use emotes in the room chat.
    ChangingPassword, // how to change the password of a room. (in vibe lobby window)
    ChangingHost, // how to change host (in vibe lobby window)
    LeavingRoom, // how to leave room (and what happens if you leave while a host).
    // the above condition has not yet been tested, and may lead to instability,
    // hince further testing is needed.)
    RoomModeration, // hosts can remove other users from a room if they are being roudy. Show how.
}

public enum StepsPatterns
{
    PatternSearch,
    CreatingFolders,
    CreateNewPattern, // on click, create a new blank pattern. (open remote for this)
    // remember that a recorder cannot start if devices are not connected.
    DeviceList, // device controls. (select first if any)
    MotorList, // motors of selected device. (select first if any)
    MotorDot, // indicate this is how you control motor stimulation.
    RecordingTimeAlive, // to know how long pattern is running.
    LoopButton, // what it does, hotkey, ext.
    FloatButton, // what it does, hotkey, ext.
    RecordingButton, // how to start recording (and begin it after this, if able to )
    StoppingRecording, // Stop recording, (on next, stop, opening the saver)
    SavingName,
    SavingDescription,
    SavingLoop,
    DiscardingPattern, // dont allow this
    FinalizingSave, // do this automatically.
    PatternList, // where saved patterns are stored.
    SelectingPattern, // select tutorial pattern on click.
    ModifyingPatterns, // how to change what devices and what motors tracks are for, where applicable.
    ChangingMotorAssociations, // how to modify what device a motor track can be for. (useful for simulated)
    AdjustingStartPoint, // how to adjust the start point of a pattern.
    AdjustingPlaybackDuration, // how to adjust the playback duration of a pattern.
    DiscardingPatternEdits, // how to discard changes made to a pattern.
    SavingPatternEdits, // how to save changes made to a pattern.
    PlayingPatterns, // how to play a pattern.
    PatternInfo, // where to view extra info about patterns.
}

public enum StepsAlarms
{
    AlarmSearch,
    CreatingFolders,
    CreateNewAlarm, // on click, create a new blank alarm. (open remote for this)
    AlarmList, // device controls. (select first if any)
    SelectingAlarm, // select first alarm if any.
    RenamingAlarm, // how to rename an alarm.
    EditingAlarm, // how to enter alarm editor (do so on click)
    AlarmLocalTimeZone, // how alarms adjust to your local time.
    AlarmTime, // how to change when the alarm goes off.
    AlarmPattern, // how to change what pattern is played when the alarm goes off.
    AlarmStartPoint, // custom pattern start point, just for the alarm.
    AlarmDuration, // custom pattern playback duration, just for the alarm.
    AlarmSample, // how to sample the custom pattern playback.
    SettingFrequency, // when in the week it goes off.
    DiscardingChanges, // how to exit editor without saving.
    SavingChanges, // how to save and exit editor. (do so on click)
    ToggleState, // how to throttle alarms.
}

public enum StepsTriggers
{
    TriggerSearch,
    CreatingFolders,
    CreatingTrigger, // on click, create a new blank trigger. (open remote for this)
    TriggerList, // device controls. (select first if any)
    SelectingTrigger, // select first trigger if any.
    RenamingTrigger, // how to rename an trigger.
    EditingTrigger, // how to enter trigger editor (do so on click)
    Priority, // what it does, how to change it.
    Description, // what purpose it serves (let others know more about what it does.)
    TriggerDetection, // what this means. (on click, switch to applied action)
    TriggerAction, // what this means. (on click, move back to detection)
    GagDetection, // what it does, brief overview. (on click, move to restriction detection)
    RestrictionDetection, // what it does, brief overview. (on click, move to restraint detection)
    RestraintDetection, // what it does, brief overview. (on click, move to emote detection)
    EmoteDetection, // what it does, brief overview. (on click, move to social detection)
    SocialDetection, // what it does, brief overview. (on click, move to health% detection)
    HealthDetection, // what it does, brief overview. (on click, move to action detection)
    SpellActDetection, // what it does, brief overview. (on click, move to to applied actions)
    TextOutputAction, // what it does, brief overview. (on click, move to applied actions)
    GagAction, // what it does, brief overview. (on click, move to restriction action)
    RestrictionAction, // what it does, brief overview. (on click, move to restraint action)
    RestraintAction, // what it does, brief overview. (on click, move to moodle action)
    MoodleAction, // what it does, brief overview. (on click, move to shock collar action)
    ShockCollarAction, // what it does, brief overview. (on click, move to sex toy action)
    SexToyAction, // what it does, brief overview.
    DiscardingChanges, // how to exit editor without saving.
    SavingChanges, // how to save and exit editor. (do so on click)
    ToggleState, // how to throttle trigger.
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
