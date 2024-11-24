using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration;
using Serilog.Events;

namespace GagSpeak.Interop;

/// <summary>
/// A provider for Dalamud loggers, where we can construct our customized logger output message string
/// </summary>
[ProviderAlias("Dalamud")]
public sealed class DalamudLoggingProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers =
        new(StringComparer.OrdinalIgnoreCase);                              // the concurrent dictionary of loggers that we have created

    private readonly GagspeakConfigService _gagspeakConfigService;          // the config service for the client 
    private readonly IPluginLog _pluginLog;                                 // the dalamud plugin log interface

    public DalamudLoggingProvider(GagspeakConfigService gagspeakConfigService, IPluginLog pluginLog)
    {
        _gagspeakConfigService = gagspeakConfigService;
        _pluginLog = pluginLog;
        _pluginLog.MinimumLogLevel = LogEventLevel.Verbose;

        // initialize the static logger filter with the stored filter list in the config service
        LoggerFilter.ClearAllowedCategories();
        LoggerFilter.AddAllowedCategories(_gagspeakConfigService.Current.LoggerFilters);
    }

    public ILogger CreateLogger(string categoryName)
    {
        // make the catagory name. Should be 15 characters or less long.
        // begin by spliting categoryName by periods (.), removes any empty entries,
        // then selects the last segment.
        // (This is a common pattern to extract the most specific part of a namespace
        // or class name, which often represents the actual class or component name.)
        string catName = categoryName.Split(".", StringSplitOptions.RemoveEmptyEntries).Last();
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
        return _loggers.GetOrAdd(catName, name => new DalamudLogger(name, _gagspeakConfigService, _pluginLog));
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
