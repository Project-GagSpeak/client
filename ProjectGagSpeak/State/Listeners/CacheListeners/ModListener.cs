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
    private readonly ModPresetManager _manager;

    public ModListener(ILogger<ModListener> logger, GagspeakMediator mediator,
        IpcCallerPenumbra ipc, ModCache cache, ModHandler handler, ModPresetManager manager) 
        : base(logger, mediator)
    {
        _ipc = ipc;
        _cache = cache;
        _handler = handler;
        _manager = manager;

        _ipc.OnModMoved = ModMoved.Subscriber(Svc.PluginInterface, OnModDirPathChanged);
        _ipc.OnModAdded = ModAdded.Subscriber(Svc.PluginInterface, OnModAdded);
        _ipc.OnModDeleted = ModDeleted.Subscriber(Svc.PluginInterface, OnModDeleted);

        // if penumbra api is connected, immediately run a OnPenumbraInitialized after our load.
        if (IpcCallerPenumbra.APIAvailable)
            OnPenumbraInitialized();

        Mediator.Subscribe<PenumbraInitialized>(this, (msg) => OnPenumbraInitialized());
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
        _manager.PenumbraInitialized(_ipc.GetModListInfo());
    }

    /// <summary> Fired whenever a MOD DIRECTORY (not mod name) is moved or renamed in penumbra. We should get a full recalculation if this occurs. </summary>
    private void OnModDirPathChanged(string oldPath, string newPath)
    {
        Logger.LogInformation($"Mod moved from [{oldPath}] to [{newPath}].");
        // Get the information about the new mod regardless of what happened to it.
        var res = _ipc.GetModInfo(newPath);
        _manager.OnModDirChanged(oldPath, res.Info, res.CurrentSettings);
    }

    private void OnModAdded(string addedDirectory)
    {
        Logger.LogInformation($"Mod added at [{addedDirectory}].");
        // TODO: Get the mod name for the directory, and its data necessary to construct a ModInfo object.
        var res = _ipc.GetModInfo(addedDirectory);
        _manager.OnModAdded(res.Info, res.CurrentSettings);

    }

    private void OnModDeleted(string deletedDirectory)
    {
        Logger.LogInformation($"Mod deleted at [{deletedDirectory}].");
        _manager.OnModRemoved(deletedDirectory);
    }
}
