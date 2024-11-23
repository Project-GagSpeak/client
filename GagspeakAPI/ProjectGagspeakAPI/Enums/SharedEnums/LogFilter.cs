namespace GagspeakAPI.Enums;

public enum LoggerType
{
    None,
    // Foundation
    Achievements,
    AchievementEvents,
    AchievementInfo,

    // interop
    IpcGagSpeak,
    IpcCustomize,
    IpcGlamourer,
    IpcMare,
    IpcMoodles,
    IpcPenumbra,

    // State Managers
    AppearanceState,
    ToyboxState,
    Mediator,
    GarblerCore,

    // Update Monitors
    ToyboxAlarms,
    ActionsNotifier,
    KinkPlateMonitor,
    EmoteMonitor,
    ChatDetours,
    ActionEffects,
    SpatialAudioLogger,

    // hardcore
    HardcoreActions,
    HardcoreMovement,
    HardcorePrompt,

    // PlayerData & Modules.
    ClientPlayerData,
    GagHandling,
    PadlockHandling,
    Restraints,
    Puppeteer,
    CursedLoot,
    ToyboxDevices,
    ToyboxPatterns,
    ToyboxTriggers,
    VibeControl,

    // Pair Data
    PairManagement,
    PairInfo,
    PairDataTransfer,
    PairHandlers,
    OnlinePairs,
    VisiblePairs,
    PrivateRooms,
    GameObjects,

    // services
    Cosmetics,
    Textures,
    GlobalChat,
    ContextDtr,
    PatternHub,
    Safeword,

    // UI
    UiCore,
    UserPairDrawer,
    Permissions,
    Simulation,

    // WebAPI
    PiShock,
    ApiCore,
    Callbacks,
    HubFactory,
    Health,
    JwtTokens,
}
