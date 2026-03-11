using CkCommons;
using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Interop;

public class IpcProvider : DisposableMediatorSubscriberBase, IHostedService
{
    private const int GagSpeakApiVersion = 2;
    // State
    private ICallGateProvider<int>    ApiVersion;
    private ICallGateProvider<object> Ready;
    private ICallGateProvider<object> Disposing;
    // Events
    private ICallGateProvider<nint, object> KinksterRendered;   // When a kinkster becomes rendered.
    private ICallGateProvider<nint, object> KinksterUnrendered; // When a kinkster is no longer rendered.
    // Getters
    private ICallGateProvider<List<nint>> GetRendered; // Get rendered kinksters pointers.

    private readonly HashSet<nint> _handledKinksters = [];

    public IpcProvider(ILogger<IpcProvider> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        // Should subscribe to characterActorCreated or rendered / unrendered events.
        Mediator.Subscribe<KinksterRendered>(this, _ =>
        {
            if (_handledKinksters.Add(_.Handler.Address))
                Generic.Safe(() => KinksterRendered?.SendMessage(_.Handler.Address));
        });
        Mediator.Subscribe<KinksterUnrendered>(this, _ =>
        {
            if (_handledKinksters.Remove(_.Address))
                Generic.Safe(() => KinksterUnrendered?.SendMessage(_.Address));
        });
    }

    public Task StartAsync(CancellationToken cts)
    {
        Logger.LogInformation("Starting IpcProvider");
        ApiVersion = Svc.PluginInterface.GetIpcProvider<int>("GagSpeak.GetApiVersion");
        // Events
        Ready = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Ready");
        Disposing = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Disposing");
        KinksterRendered = Svc.PluginInterface.GetIpcProvider<nint, object>("GagSpeak.PairRendered");
        KinksterUnrendered = Svc.PluginInterface.GetIpcProvider<nint, object>("GagSpeak.PairUnrendered");
        // Getters
        GetRendered = Svc.PluginInterface.GetIpcProvider<List<nint>>("GagSpeak.GetAllRendered");
        // =====================================
        // ---- FUNC & ACTION REGISTRATIONS ----
        // =====================================
        // By Registering a func, or action, we declare that this IPC Provider when called returns a value.
        // This distguishes it from being invokable by us, versus invokable by other plugins.
        ApiVersion.RegisterFunc(() => GagSpeakApiVersion);
        GetRendered.RegisterFunc(() => _handledKinksters.ToList());
        Logger.LogInformation("Started IpcProvider");
        
        Generic.Safe(() => Ready?.SendMessage());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cts)
    {
        Logger.LogDebug("Stopping IpcProvider");
        Disposing?.SendMessage();

        ApiVersion?.UnregisterFunc();
        GetRendered?.UnregisterFunc();

        Mediator.UnsubscribeAll(this);
        return Task.CompletedTask;
    }
}

