using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Enums;

namespace GagSpeak.Interop.Ipc;

public sealed class IpcCallerMare : IIpcCaller
{
    // Mare has no API Version attribute, so just pray i guess.
    private readonly ICallGateSubscriber<List<nint>>? _handledGameAddresses;

    private readonly ILogger<IpcCallerMare> _logger;
    private readonly OnFrameworkService _frameworkUtil;
    private readonly GagspeakMediator _gagspeakMediator;
    private readonly IDalamudPluginInterface _pi;
    public IpcCallerMare(ILogger<IpcCallerMare> logger, IDalamudPluginInterface pi,
        OnFrameworkService frameworkUtil, GagspeakMediator gagspeakMediator)
    {
        _logger = logger;
        _frameworkUtil = frameworkUtil;
        _gagspeakMediator = gagspeakMediator;
        _pi = pi;

        _handledGameAddresses = pi.GetIpcSubscriber<List<nint>>("MareSynchronos.GetHandledAddresses");

        CheckAPI(); // check to see if we have a valid API
    }

    public void Dispose()
    {
        // Nothing to dispose of.
    }

    public static bool APIAvailable { get; private set; } = false;
    public void CheckAPI()
    {
        var marePlugin = _pi.InstalledPlugins.FirstOrDefault(p => string.Equals(p.InternalName, "mareSynchronos", StringComparison.OrdinalIgnoreCase));
        if (marePlugin == null)
        {
            APIAvailable = false;
            return;
        }
        // mare is installed, so see if it is on.
        APIAvailable = marePlugin.IsLoaded ? true : false;
        return;
    }

    /// <summary> Gets currently handled players from mare. </summary>
    public async Task<List<nint>?> GetHandledMarePlayers()
    {
        if (!APIAvailable) return null; // return if the API isnt available

        try // otherwise, try and return an awaited task that gets the moodles info for a provided GUID
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _handledGameAddresses!.InvokeFunc());
        }
        catch (Exception e)
        {
            // log it if we failed.
            _logger.LogWarning("Could not Get Moodles Info"+e, LoggerType.IpcMare);
            return null;
        }
    }
}
