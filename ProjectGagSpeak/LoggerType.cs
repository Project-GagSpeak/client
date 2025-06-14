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

    // MufflerCore
    GarblerCore          = 1L << 12,
    ChatDetours          = 1L << 13,

    // PlayerClientState
    Listeners            = 1L << 14,
    VisualCache          = 1L << 15,
    Gags                 = 1L << 16,
    Restrictions         = 1L << 17,
    Restraints           = 1L << 18,
    CursedItems          = 1L << 19,
    Puppeteer            = 1L << 20,
    Toys                 = 1L << 21,
    VibeLobbies          = 1L << 22,
    Patterns             = 1L << 23,
    Alarms               = 1L << 24,
    Triggers             = 1L << 25,

    // Kinkster Data (PlayerData)
    PairManagement       = 1L << 26,
    PairInfo             = 1L << 27,
    PairDataTransfer     = 1L << 28,
    PairHandlers         = 1L << 29,
    OnlinePairs          = 1L << 30,
    VisiblePairs         = 1L << 31,
    GameObjects          = 1L << 32,

    // Services
    ActionsNotifier      = 1L << 33,
    Textures             = 1L << 34,
    ContextDtr           = 1L << 35,
    GlobalChat           = 1L << 36,
    Kinkplates           = 1L << 37,
    Mediator             = 1L << 38,
    ShareHub             = 1L << 39,

    // UI
    UI                   = 1L << 40,
    StickyUI             = 1L << 41,
    Combos               = 1L << 42,
    FileSystems          = 1L << 43,

    // Update Monitoring
    ActionEffects        = 1L << 44,
    EmoteMonitor         = 1L << 45,
    SpatialAudio         = 1L << 46,
    Arousal              = 1L << 47,

    // WebAPI (GagspeakHub)
    PiShock              = 1L << 48,
    ApiCore              = 1L << 49,
    Callbacks            = 1L << 50,
    HubFactory           = 1L << 51,
    Health               = 1L << 52,
    JwtTokens            = 1L << 53,

    // All Recommended types.
    Recommended =
        Achievements |
        IpcGagSpeak | IpcCustomize | IpcGlamourer | IpcMare | IpcMoodles |
        VisualCache | Gags | Restrictions | Restraints | CursedItems |
        Puppeteer |
        Toys | Patterns | Alarms | Triggers |
        PairManagement | PairInfo | PairDataTransfer | OnlinePairs | VisiblePairs |
        ActionsNotifier | ContextDtr |
        UI |
        PiShock | ApiCore | Callbacks | HubFactory,
}
