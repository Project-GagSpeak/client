using Dalamud.Plugin.Ipc;

namespace GagSpeak.Interop;

public sealed class IpcCallerMare : IIpcCaller
{
    // Mare has no API Version attribute, so just pray i guess.
    private readonly ICallGateSubscriber<List<nint>> _handledGameAddresses;

    private readonly ILogger<IpcCallerMare> _logger;
    public IpcCallerMare(ILogger<IpcCallerMare> logger)
    {
        _logger = logger;
        _handledGameAddresses = Svc.PluginInterface.GetIpcSubscriber<List<nint>>("MareSynchronos.GetHandledAddresses");
        CheckAPI(); // check to see if we have a valid API
    }

    public void Dispose()
    {
        // Nothing to dispose of.
    }

    public static bool APIAvailable { get; private set; } = false;
    public void CheckAPI()
    {
        var marePlugin = Svc.PluginInterface.InstalledPlugins
            .FirstOrDefault(p => string.Equals(p.InternalName, "mareSynchronos", StringComparison.OrdinalIgnoreCase));
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
    public List<nint> GetHandledMarePlayers()
    {
        if (!APIAvailable) return new List<nint>();

        try
        {
            return _handledGameAddresses.InvokeFunc();
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Info: " + e, LoggerType.IpcMare);
            return new List<nint>();
        }
    }
}
