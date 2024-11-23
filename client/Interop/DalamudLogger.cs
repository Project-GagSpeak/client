using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration;
using GagspeakAPI.Enums;

namespace GagSpeak.Interop;

/// <summary>
/// An internally sealed glass for the dalamud logger, that helps us
/// correlate Microsofts ILogger with Dalamuds IPluginLog interface.
/// </summary>
internal sealed class DalamudLogger : ILogger
{
    private readonly GagspeakConfigService _gagspeakConfigService;  // gagspeaks config service
    private readonly string _name;                                  // the name of our plugin
    private readonly IPluginLog _pluginLog;                         // the plugin log Dalamud uses

    public DalamudLogger(string name, GagspeakConfigService gagspeakConfigService, IPluginLog pluginLog)
    {
        _name = name;
        _gagspeakConfigService = gagspeakConfigService;
        _pluginLog = pluginLog;
    }

    /// <summary> The disposable beginscope statement that the DalamudLogger uses
    public IDisposable BeginScope<TState>(TState state) => default!;


    /// <summary> Checks if the log level is enabled for the current log level we are inspecting. </summary>
    public bool IsEnabled(LogLevel logLevel)
    {
        return (int)_gagspeakConfigService.Current.LogLevel <= (int)logLevel;
    }

    /// <summary> This is the Log method that is called by the ILogger interface. </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object. (usually the message content)</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // if the log level we are trying to log is not enabled, then do not log it to the IPluginLog and return
        if (!IsEnabled(logLevel)) return;
        // otherwise, we are outputting a message. Lets get the formats stored first
        var noExceptionStr = $"[{_name}] {state}";

        // temp string builder string for exceptions if we have them
        StringBuilder exceptionStr;
        // now apply the message to the appropriate logger
        switch (logLevel)
        {
            case LogLevel.Trace:
                _pluginLog.Verbose(noExceptionStr);
                break;
            case LogLevel.Debug:
                _pluginLog.Debug(noExceptionStr);
                break;
            case LogLevel.Information:
                _pluginLog.Info(noExceptionStr);
                break;
            case LogLevel.Warning:
                // create the format for our exception format
                exceptionStr = ExceptionStringBuilder(state, exception);
                _pluginLog.Warning(exceptionStr.ToString());
                break;
            case LogLevel.Error:
                // create the format for our exception format
                exceptionStr = ExceptionStringBuilder(state, exception);
                _pluginLog.Error(exceptionStr.ToString());
                break;
            case LogLevel.Critical:
                // create the format for our exception format
                exceptionStr = ExceptionStringBuilder(state, exception);
                _pluginLog.Fatal(exceptionStr.ToString());
                break;
            default:
                _pluginLog.Info(noExceptionStr);
                break;
        }
    }
    private StringBuilder ExceptionStringBuilder<TState>(TState state, Exception? exception)
    {
        // create the format for our exception format
        StringBuilder exceptionStr = new StringBuilder();
        exceptionStr.AppendLine($"[{_name}] {state} {exception?.Message}");
        exceptionStr.AppendLine(exception?.StackTrace);


        var innerException = exception?.InnerException;
        while (innerException != null)
        {
            exceptionStr.AppendLine($"InnerException {innerException}: {innerException.Message}");
            exceptionStr.AppendLine(innerException.StackTrace);
            // get the inner exception of the inner exception until it ends
            innerException = innerException.InnerException;
        }

        return exceptionStr;
    }
}

/* REFERENCE COMPARISON
 * 
 * MICROSOFT ILOGGER | Dalamud IPluginLog
 *                   |
 * TRACE             | Verbose
 * DEBUG             | Debug
 * INFO              | Info
 * WARNING           | Warning
 * ERROR             | Error
 * CRITICAL          | Fatal
*/
