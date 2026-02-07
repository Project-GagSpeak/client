using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

public sealed class IpcCallerSundouleia : IIpcCaller
{
    // Version Checks.
    private readonly ICallGateSubscriber<int> ApiVersion;

    // Event Calls.
    private readonly ICallGateSubscriber<object> Ready;
    private readonly ICallGateSubscriber<object> Disposing;

    private readonly ICallGateSubscriber<nint, object> PairRendered;   // When a kinkster becomes rendered.
    private readonly ICallGateSubscriber<nint, object> PairUnrendered; // When a kinkster is no longer rendered.

    // API Getters
    private ICallGateSubscriber<List<nint>> GetAllRendered;  // Get rendered kinksters (by pointers)

    private readonly ILogger<IpcCallerSundouleia> _logger;
    private readonly GagspeakMediator _mediator;

    private static HashSet<nint> _renderedSundesmos = new();

    public IpcCallerSundouleia(ILogger<IpcCallerSundouleia> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;

        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Sundouleia.GetApiVersion");
        Ready = Svc.PluginInterface.GetIpcSubscriber<object>("Sundouleia.Ready");
        Disposing = Svc.PluginInterface.GetIpcSubscriber<object>("Sundouleia.Disposing");

        PairRendered = Svc.PluginInterface.GetIpcSubscriber<nint, object>("Sundouleia.PairRendered");
        PairUnrendered = Svc.PluginInterface.GetIpcSubscriber<nint, object>("Sundouleia.PairUnrendered");

        GetAllRendered = Svc.PluginInterface.GetIpcSubscriber<List<nint>>("Sundouleia.GetAllRendered");

        Ready.Subscribe(OnSundouleiaReady);
        Disposing.Subscribe(OnSundouleiaDisposing);

        PairRendered.Subscribe(OnKinksterRendered);
        PairUnrendered.Subscribe(OnKinksterUnrendered);

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;
    public static IReadOnlyCollection<nint> Sundesmos => _renderedSundesmos;

    public void Dispose()
    {
        Ready.Unsubscribe(OnSundouleiaReady);
        Disposing.Unsubscribe(OnSundouleiaDisposing);
        PairRendered.Unsubscribe(OnKinksterRendered);
        PairUnrendered.Unsubscribe(OnKinksterUnrendered);
    }

    public void CheckAPI()
    {
        try
        {
            var result = ApiVersion.InvokeFunc() is 1;
            if (!APIAvailable && result)
                OnSundouleiaReady();
            else
                APIAvailable = result;
        }
        catch
        {
            APIAvailable = false;
        }
    }

    private void OnSundouleiaReady()
    {
        APIAvailable = true;
        _renderedSundesmos = GetAllSundesmos().ToHashSet();
        _mediator.Publish(new SundouleiaReady());
    }

    private void OnSundouleiaDisposing()
    {
        _renderedSundesmos.Clear();
        _mediator.Publish(new SundouleiaDisposed());
    }

    // Maybe inform mediator of change?
    private void OnKinksterRendered(nint ptr)
        => _renderedSundesmos.Add(ptr);

    private void OnKinksterUnrendered(nint ptr)
        => _renderedSundesmos.Remove(ptr);

    public List<nint> GetAllSundesmos()
    {
        if (!APIAvailable)
            return new List<nint>();
        // Can be called off the framework thread, it does not madder.
        var result = GetAllRendered.InvokeFunc();
        _logger.LogDebug($"Retrieved {result.Count} kinksters.", LoggerType.IpcGagSpeak);
        // Update the internal list too.
        _renderedSundesmos = result.ToHashSet();
        return result;
    }
}
