using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Penumbra.Api.Helpers;

internal class PluginLogHelper
{
    [PluginService]
    private static IPluginLog? _log { get; set; }

    private PluginLogHelper(IDalamudPluginInterface pi)
        => pi.Inject(this);

    public static void WriteError(IDalamudPluginInterface pi, string errorMessage)
        => GetLog(pi).Error(errorMessage);

    public static IPluginLog GetLog(IDalamudPluginInterface pi)
    {
        if (_log != null)
            return _log;

        _ = new PluginLogHelper(pi);
        return _log!;
    }
}
