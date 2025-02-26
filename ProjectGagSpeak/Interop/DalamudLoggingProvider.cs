using Dalamud.Plugin.Services;
using GagSpeak.Services.Configs;
using Serilog.Events;

namespace GagSpeak.Interop;

/// <summary>
/// A provider for Dalamud loggers, where we can construct our customized logger output message string
/// </summary>
[ProviderAlias("Dalamud")]
public sealed class DalamudLoggingProvider : ILoggerProvider
{
    // the concurrent dictionary of loggers that we have created
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IPluginLog _pluginLog;

    public DalamudLoggingProvider(IPluginLog pluginLog)
    {
        _pluginLog = pluginLog;
        pluginLog.Information("DalamudLoggingProvider is initialized.");
        _pluginLog.MinimumLogLevel = LogEventLevel.Verbose;
    }

    public ILogger CreateLogger(string categoryName)
    {
        // make the catagory name. Should be 15 characters or less long.
        // begin by spliting categoryName by periods (.), removes any empty entries,
        // then selects the last segment.
        // (This is a common pattern to extract the most specific part of a namespace
        // or class name, which often represents the actual class or component name.)
        var catName = categoryName.Split(".", StringSplitOptions.RemoveEmptyEntries).Last();
        // if the name is longer than 15 characters, take the first 6 characters, the last 6 characters, and add "..."
        if (catName.Length > 19)
        {
            catName = string.Join("", catName.Take(8)) + "..." + string.Join("", catName.TakeLast(8));
        }
        // otherwise replace any leftover empty space with spaces
        else
        {
            catName = string.Join("", Enumerable.Range(0, 19 - catName.Length).Select(_ => " ")) + catName;
        }
        // now that we have the name properly, get/add it to our logger for dalamud
        try
        {
            var newLogger = _loggers.GetOrAdd(catName, name => new DalamudLogger(name, _pluginLog));
            //_pluginLog.Information($"Logger {catName} is created."); // <--- FOR DEBUGGING
            return newLogger;
        }
        catch (Exception e)
        {
            _pluginLog.Error($"Failed to create logger {catName}.");
            _pluginLog.Error(e.ToString());
            throw;
        }
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
