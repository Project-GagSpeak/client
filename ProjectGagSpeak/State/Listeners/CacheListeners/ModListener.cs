using Dalamud.Plugin;
using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using Penumbra.Api.IpcSubscribers;

namespace GagSpeak.State.Listeners;
public class ModListener : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerPenumbra _ipc;
    private readonly ModCache _cache;
    private readonly ModHandler _handler;
    private readonly ModSettingPresetManager _manager;

    public ModListener(ILogger<ModListener> logger, GagspeakMediator mediator,
        IpcCallerPenumbra ipc, ModCache cache, ModHandler handler, ModSettingPresetManager manager) 
        : base(logger, mediator)
    {
        _ipc = ipc;
        _cache = cache;
        _handler = handler;
        _manager = manager;

        _ipc.OnModMoved = ModMoved.Subscriber(Svc.PluginInterface, OnModInfoChanged);
        _ipc.OnModAdded = ModAdded.Subscriber(Svc.PluginInterface, OnModAdded);
        _ipc.OnModDeleted = ModDeleted.Subscriber(Svc.PluginInterface, OnModDeleted);

        // if penumbra api is connected, immediately run a OnPenumbraInitialized after our load.
        if (IpcCallerPenumbra.APIAvailable)
            OnPenumbraInitialized();

        Mediator.Subscribe<PenumbraInitializedMessage>(this, (msg) => OnPenumbraInitialized());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ipc.OnModMoved?.Dispose();
        _ipc.OnModAdded?.Dispose();
        _ipc.OnModDeleted?.Dispose();
    }

    private void OnPenumbraInitialized()
    {
        Logger.LogInformation("Penumbra initialized. Retrieving Mod Info.");
        _manager.PenumbraInitialized(_ipc.GetModInfo());
    }

    /// <summary> Fired whenever a MOD DIRECTORY (not mod name) is moved or renamed in penumbra. We should get a full recalculation if this occurs. </summary>
    private void OnModInfoChanged(string oldPath, string newPath)
    {
        // TODO: (Handle how this affects other dependent sources, (Should not be an issue for us but we will see).
    }

    private void OnModAdded(string addedDirectory)
    {
        // TODO: Get the mod name for the directory, and its data necessary to construct a ModInfo object.
    }

    private void OnModDeleted(string deletedDirectory)
    {
        // TODO: Handle logic that updates anything using this directory to be removed.
    }
}
