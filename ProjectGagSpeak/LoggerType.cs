namespace GagSpeak;

[Flags]
public enum LoggerType : ulong
{
    None                 = 0,

    // Achievements
    Achievements         = 1L << 0,
    AchievementEvents    = 1L << 1,
    AchievementInfo      = 1L << 2,

    // Hardcore
    HardcoreActions      = 1L << 3,
    HardcoreMovement     = 1L << 4,
    HardcorePrompt       = 1L << 5,
    HardcoreTasks        = 1L << 6,

    // Interop / IPC
    IpcGagSpeak          = 1L << 7,
    IpcMare              = 1L << 8,
    IpcPenumbra          = 1L << 9,
    IpcGlamourer         = 1L << 10,
    IpcCustomize         = 1L << 11,
    IpcMoodles           = 1L << 12,
    IpcLifestream        = 1L << 13,

    // MufflerCore
    GarblerCore          = 1L << 14,
    ChatDetours          = 1L << 15,

    // PlayerClientState
    Listeners            = 1L << 16,
    VisualCache          = 1L << 17,
    Gags                 = 1L << 18,
    Restrictions         = 1L << 19,
    Restraints           = 1L << 20,
    CursedItems          = 1L << 21,
    Puppeteer            = 1L << 22,
    Toys                 = 1L << 23,
    VibeLobbies          = 1L << 24,
    Patterns             = 1L << 25,
    Alarms               = 1L << 26,
    Triggers             = 1L << 27,

    // Kinkster Data (PlayerData)
    PairManagement       = 1L << 28,
    PairInfo             = 1L << 29,
    PairDataTransfer     = 1L << 30,
    PairHandlers         = 1L << 31,
    OnlinePairs          = 1L << 32,
    VisiblePairs         = 1L << 33,
    GameObjects          = 1L << 34,

    // Services
    ActionsNotifier      = 1L << 35,
    Textures             = 1L << 36,
    ContextDtr           = 1L << 37,
    GlobalChat           = 1L << 38,
    Kinkplates           = 1L << 39,
    Mediator             = 1L << 40,
    ShareHub             = 1L << 41,

    // UI
    UI                   = 1L << 42,
    StickyUI             = 1L << 43,
    Combos               = 1L << 44,
    FileSystems          = 1L << 45,

    // Update Monitoring
    ActionEffects        = 1L << 46,
    EmoteMonitor         = 1L << 47,
    SpatialAudio         = 1L << 48,
    Arousal              = 1L << 49,

    // WebAPI (GagspeakHub)
    PiShock              = 1L << 50,
    ApiCore              = 1L << 51,
    Callbacks            = 1L << 52,
    HubFactory           = 1L << 53,
    Health               = 1L << 54,
    JwtTokens            = 1L << 55,

    // All Recommended types.
    Recommended =
        Achievements |
        IpcGagSpeak | IpcCustomize | IpcGlamourer | IpcMare | IpcMoodles | IpcPenumbra | IpcLifestream |
        VisualCache | Gags | Restrictions | Restraints | CursedItems |
        Puppeteer |
        Toys | Patterns | Alarms | Triggers |
        PairManagement | PairInfo | PairDataTransfer | OnlinePairs | VisiblePairs |
        ActionsNotifier | ContextDtr |
        UI |
        PiShock | ApiCore | Callbacks | HubFactory,
}
