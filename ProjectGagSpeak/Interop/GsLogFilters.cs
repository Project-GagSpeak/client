using System.Runtime.CompilerServices;

namespace GagSpeak;

public static class GsLogFilters
{
    public static readonly LogFilter[] Recommended =
    [
        LogFilter.Achievements,
        //
        LogFilter.IpcGagSpeak, LogFilter.IpcSundouleia, LogFilter.IpcPenumbra,
        LogFilter.IpcGlamourer, LogFilter.IpcCustomize, LogFilter.IpcLoci,
        LogFilter.IpcHeels, LogFilter.IpcHonorific,
        //
        LogFilter.ContextMenus, LogFilter.DtrBar, LogFilter.Requests,
        //
        LogFilter.VisualCache, LogFilter.Gags, LogFilter.Restrictions, LogFilter.Restraints,
        LogFilter.Collars, LogFilter.CursedItems, LogFilter.Puppeteer, LogFilter.Toys,
        LogFilter.Patterns, LogFilter.Alarms, LogFilter.Triggers,
        //
        LogFilter.PairManagement, LogFilter.PairHandlers, LogFilter.DataTransfers,
        //
        LogFilter.ActorVisibility, LogFilter.OnlineUsers,
        //
        LogFilter.UITasks, LogFilter.HubFactory, LogFilter.MainHub, LogFilter.Callbacks
    ];

    // One-Time static initialization to determine the maximum value of the LogFilter enum.
    private static readonly byte _maxValue = (byte)(Enum.GetValues<LogFilter>().Cast<byte>().Max() + 1);

    // Array for quick lookup of enabled logs.
    private static readonly bool[] _enabledLogs = InitRecommended();

    private static bool[] InitRecommended()
    {
        var initialArray = new bool[_maxValue];
        foreach (var type in Recommended)
            if (type is not LogFilter.None)
                initialArray[(int)type] = true;
        return initialArray;
    }

    /// <summary>
    ///   Updates the active logs to filter.
    /// </summary>
    public static void UpdateFilters(IEnumerable<LogFilter> activeTypes)
    {
        // Reset the enabled logs array to false for all log types.
        Array.Clear(_enabledLogs, 0, _enabledLogs.Length);
        // Then fill in the trues.
        foreach (var type in activeTypes)
            if (type is not LogFilter.None)
                _enabledLogs[(int)type] = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldLog(LogFilter c1)
    {
        return (c1 is LogFilter.None) || _enabledLogs[(int)c1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldLog(LogFilter c1, LogFilter c2)
    {
        return ShouldLog(c1) || ShouldLog(c2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldLog(LogFilter c1, LogFilter c2, LogFilter c3)
    {
        return ShouldLog(c1) || ShouldLog(c2) || ShouldLog(c3);
    }

    #region Logger Extensions
    public static void LogTrace(this ILogger logger, string? message, LogFilter t1 = LogFilter.None)
    {
        if (ShouldLog(t1))
            logger.Log(LogLevel.Trace, message);
    }

    public static void LogTrace(this ILogger logger, string? message, LogFilter t1, LogFilter t2)
    {
        if (ShouldLog(t1, t2))
            logger.Log(LogLevel.Trace, message);
    }

    public static void LogTrace(this ILogger logger, string? message, LogFilter t1, LogFilter t2, LogFilter t3)
    {
        if (ShouldLog(t1, t2, t3))
            logger.Log(LogLevel.Trace, message);
    }

    public static void LogDebug(this ILogger logger, string? message, LogFilter t1 = LogFilter.None)
    {
        if (ShouldLog(t1))
            logger.Log(LogLevel.Debug, message);
    }

    public static void LogDebug(this ILogger logger, string? message, LogFilter t1, LogFilter t2)
    {
        if (ShouldLog(t1, t2))
            logger.Log(LogLevel.Debug, message);
    }

    public static void LogDebug(this ILogger logger, string? message, LogFilter t1, LogFilter t2, LogFilter t3)
    {
        if (ShouldLog(t1, t2, t3))
            logger.Log(LogLevel.Debug, message);
    }

    public static void LogInformation(this ILogger logger, string? message, LogFilter t1 = LogFilter.None)
    {
        if (ShouldLog(t1))
            logger.Log(LogLevel.Information, message);
    }

    public static void LogInformation(this ILogger logger, string? message, LogFilter t1, LogFilter t2)
    {
        if (ShouldLog(t1, t2))
            logger.Log(LogLevel.Information, message);
    }

    public static void LogInformation(this ILogger logger, string? message, LogFilter t1, LogFilter t2, LogFilter t3)
    {
        if (ShouldLog(t1, t2, t3))
            logger.Log(LogLevel.Information, message);
    }

    public static void LogWarning(this ILogger logger, string? message, LogFilter type = LogFilter.None)
    {
        if (type is 0 || ShouldLog(type))
            logger.Log(LogLevel.Warning, message);
    }

    public static void LogError(this ILogger logger, string? message, LogFilter type = LogFilter.None)
    {
        if (type is 0 || ShouldLog(type))
            logger.Log(LogLevel.Error, message);
    }

    #endregion
}

// Categories of logging.
public enum LogFilter : byte
{
    None = 0,

    // Achievements
    Achievements,
    AchievementEvents,
    AchievementInfo,

    // Interop / IPC
    IpcGagSpeak,
    IpcSundouleia,
    IpcPenumbra,
    IpcGlamourer,
    IpcCustomize,
    IpcLoci,
    IpcHeels,
    IpcLifeStream,
    IpcHonorific,
    IpcIntiface,

    // Game Data
    ContextMenus,
    Nameplates,
    DtrBar,

    // Chat Data
    ChatHooks,
    ChatDetours,
    GlobalChat,
    DMChatlogs,

    // Client Data
    Requests,
    GarblerCore,

    // Client Player State
    Listeners,
    VisualCache,
    Gags,
    Restrictions,
    Restraints,
    Collars,
    CursedItems,
    Puppeteer,
    Toys,
    VibeLobbies,
    Patterns,
    Alarms,
    Triggers,

    // Hardcore
    HardcoreActions,
    HardcoreMovement,
    HardcorePrompt,
    HardcoreTasks,

    // Actor Management
    ActorWatcher,
    ActorVisibility,
    OnlineUsers,

    // Kinkster Data
    PairManagement,
    DataTransfers,
    PairHandlers,
    KinksterCache,
    PairExcessive,

    // Update Monitoring
    ActionEffects,
    EmoteMonitor,
    Arousal,
    SpatialAudio,

    // Services
    AutoUnlocks,
    Mediator,
    UITasks,
    StickyUI,
    EventNotifier,
    Textures,
    KinkPlates,
    KinkPlateEditor,
    DrawSystems,
    ShareHub,

    // WebAPI
    HubFactory,
    MainHub,
    JwtTokens,
    Health,
    Callbacks,
    PiShock,
}
