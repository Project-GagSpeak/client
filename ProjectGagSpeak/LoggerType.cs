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

    // Interop / IPC
    IpcGagSpeak          = 1L << 6,
    IpcMare              = 1L << 7,
    IpcPenumbra          = 1L << 8,
    IpcGlamourer         = 1L << 9,
    IpcCustomize         = 1L << 10,
    IpcMoodles           = 1L << 11,
    IpcLifestream        = 1L << 12,

    // MufflerCore
    GarblerCore          = 1L << 13,
    ChatDetours          = 1L << 14,

    // PlayerClientState
    Listeners            = 1L << 15,
    VisualCache          = 1L << 16,
    Gags                 = 1L << 17,
    Restrictions         = 1L << 18,
    Restraints           = 1L << 19,
    CursedItems          = 1L << 20,
    Puppeteer            = 1L << 21,
    Toys                 = 1L << 22,
    VibeLobbies          = 1L << 23,
    Patterns             = 1L << 24,
    Alarms               = 1L << 25,
    Triggers             = 1L << 26,

    // Kinkster Data (PlayerData)
    PairManagement       = 1L << 27,
    PairInfo             = 1L << 28,
    PairDataTransfer     = 1L << 29,
    PairHandlers         = 1L << 30,
    OnlinePairs          = 1L << 31,
    VisiblePairs         = 1L << 32,
    GameObjects          = 1L << 33,

    // Services
    ActionsNotifier      = 1L << 34,
    Textures             = 1L << 35,
    ContextDtr           = 1L << 36,
    GlobalChat           = 1L << 37,
    Kinkplates           = 1L << 38,
    Mediator             = 1L << 39,
    ShareHub             = 1L << 40,

    // UI
    UI                   = 1L << 41,
    StickyUI             = 1L << 42,
    Combos               = 1L << 43,
    FileSystems          = 1L << 44,

    // Update Monitoring
    ActionEffects        = 1L << 45,
    EmoteMonitor         = 1L << 46,
    SpatialAudio         = 1L << 47,
    Arousal              = 1L << 48,

    // WebAPI (GagspeakHub)
    PiShock              = 1L << 49,
    ApiCore              = 1L << 50,
    Callbacks            = 1L << 51,
    HubFactory           = 1L << 52,
    Health               = 1L << 53,
    JwtTokens            = 1L << 54,

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
