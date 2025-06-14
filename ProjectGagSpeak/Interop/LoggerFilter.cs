using GagSpeak.PlayerClient;

namespace GagSpeak;

public static class LoggerFilter
{
    public static LoggerType FilteredLogTypes => MainConfig.LoggerFilters;

    /// <summary> Perform a bitwise check for validation, extremely fast. </summary>
    public static bool ShouldLog(LoggerType category)
        => (FilteredLogTypes & category) != 0;

    public static void LogTrace(this ILogger logger, string? message, LoggerType type = LoggerType.None)
    {
        if (type is 0 || ShouldLog(type))
            logger.Log(LogLevel.Trace, message);
    }

    public static void LogDebug(this ILogger logger, string? message, LoggerType type = LoggerType.None)
    {
        if (type is 0 || ShouldLog(type))
            logger.Log(LogLevel.Debug, message);
        
    }

    public static void LogInformation(this ILogger logger, string? message, LoggerType type = LoggerType.None)
    {
        if (type is 0 || ShouldLog(type))
            logger.Log(LogLevel.Information, message);
    }

    public static void LogWarning(this ILogger logger, string? message, LoggerType type = LoggerType.None)
    {
        if (type is 0 || ShouldLog(type))
            logger.Log(LogLevel.Warning, message);
    }

    public static void LogError(this ILogger logger, string? message, LoggerType type = LoggerType.None)
    {
        if (type is 0 || ShouldLog(type))
            logger.Log(LogLevel.Error, message);
    }
}
