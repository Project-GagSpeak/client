namespace GagSpeak;

[Flags]
public enum LoggerType : long
{
    None                = 0L,
    // Achievements
    Achievements        = 1L << 0,
    AchievementEvents   = 1L << 1,
    AchievementInfo     = 1L << 2,

    // Hardcore
    HardcoreActions     = 1L << 3,
    HardcoreMovement    = 1L << 4,
    HardcorePrompt      = 1L << 5,
    HardcoreTasks       = 1L << 6,

    // Interop / IPC
    IpcGagSpeak         = 1L << 7,
    IpcPenumbra         = 1L << 8,
    IpcGlamourer        = 1L << 9,
    IpcCustomize        = 1L << 10,
    IpcMoodles          = 1L << 11,
    IpcHeels            = 1L << 12,
    IpcLifeStream       = 1L << 13,
    IpcHonorific        = 1L << 14,
    IpcPetNames         = 1L << 15,
    IpcIntiface         = 1L << 16,

    // MufflerCore
    GarblerCore         = 1L << 17,
    ChatDetours         = 1L << 18,

    // PlayerClientState
    Listeners           = 1L << 19,
    VisualCache         = 1L << 20,
    Gags                = 1L << 21,
    Restrictions        = 1L << 22,
    Restraints          = 1L << 23,
    Collars             = 1L << 24,
    CursedItems         = 1L << 25,
    Puppeteer           = 1L << 26,
    Toys                = 1L << 27,
    VibeLobbies         = 1L << 28,
    Patterns            = 1L << 29,
    Alarms              = 1L << 30,
    Triggers            = 1L << 31,

    // Kinkster Data (PlayerData)
    PairManagement      = 1L << 32,
    PairInfo            = 1L << 33,
    PairDataTransfer    = 1L << 34,
    PairHandlers        = 1L << 35,
    KinksterCache       = 1L << 36,
    OnlinePairs         = 1L << 37,
    VisiblePairs        = 1L << 38,
    GameObjects         = 1L << 39,

    // Services
    AutoUnlocks         = 1L << 40,
    ActionsNotifier     = 1L << 41,
    Textures            = 1L << 42,
    ContextDtr          = 1L << 43,
    GlobalChat          = 1L << 44,
    KinkPlates          = 1L << 45,
    Mediator            = 1L << 46,
    ShareHub            = 1L << 47,

    // UI
    UI                  = 1L << 48,
    StickyUI            = 1L << 49,
    Combos              = 1L << 50,
    FileSystems         = 1L << 51,

    // Update Monitoring
    ActionEffects       = 1L << 52,
    EmoteMonitor        = 1L << 53,
    SpatialAudio        = 1L << 54,
    Arousal             = 1L << 55,

    // WebAPI (GagspeakHub)
    PiShock             = 1L << 56,
    ApiCore             = 1L << 57,
    Callbacks           = 1L << 58,
    HubFactory          = 1L << 59,
    Health              = 1L << 60,
    JwtTokens           = 1L << 61,

    // All Recommended types.
    Recommended =
            Achievements |
            IpcGagSpeak | IpcPenumbra | IpcGlamourer | IpcCustomize | IpcMoodles | IpcHeels | IpcLifeStream | IpcHonorific | IpcPetNames | IpcIntiface |
            VisualCache | Gags | Restrictions | Restraints | Collars | CursedItems |
            Puppeteer |
            Toys | Patterns | Alarms | Triggers |
            PairManagement | PairInfo | PairDataTransfer | OnlinePairs | VisiblePairs |
            ActionsNotifier | ContextDtr |
            UI |
            PiShock | ApiCore | Callbacks | HubFactory,
}
