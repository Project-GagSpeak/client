using CkCommons;
using Dalamud.Bindings.ImGui;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;

namespace GagSpeak.State.Managers;
public unsafe class VfxSpawnManager : DisposableMediatorSubscriberBase
{
    private readonly SpatialAudioCache _cache;
    public VfxSpawnManager(ILogger<VfxSpawnManager> logger, GagspeakMediator mediator, SpatialAudioCache cache)
        : base(logger, mediator)
    {
        _cache = cache;
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => OnTick());
    }

    public void DrawVfxSpawnOptions(string path, bool loop, int labelID = 1)
    {
        if (ImGui.Button($"Spawn on Self##{labelID}SpawnSelf{path}")) 
            OnSelf(path, loop);
        if (ImGui.Button($"Spawn on Target##{labelID}SpawnTarget{path}")) 
            OnTarget(path, loop);
        if (ImGui.Button("Remove VFX"))
            _cache.ClearCachedVfxs();
    }

    public void OnSelf(string path, bool canLoop)
    {
        if (!PlayerData.Available)
            return;

        try
        {
            // attmept to fetch the replacement file from the input path, if we cannot, throw bagagwa
            if (path.EndsWith(".avfx") && !SpatialAudioCache.CustomAvfxPaths.TryGetValue(path, out var newVfxPath))
                throw new Bagagwa("Failed to find replacement path for VFX");
            else if (path.EndsWith(".scd") && !SpatialAudioCache.CustomScdPaths.TryGetValue(path, out var newScdPath))
                throw new Bagagwa("Failed to find replacement path for VFX");
            else if (!path.EndsWith(".avfx") && !path.EndsWith(".scd"))
                throw new Bagagwa("Invalid Path");

            // construct the actorVfx from the given objects and paths.
            var created = ResourceDetours.CreateActorVfx(path, PlayerData.Address, PlayerData.Address);
            _cache.AddTrackedVfx(created, path, SpawnType.Self, canLoop);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Summoned bagagwa while spawning a VFX: {ex}");
        }
    }

    public void OnTarget(string path, bool canLoop)
    {
        var targetObject = Svc.Targets.Target;
        if (targetObject is null) 
            return;

        try
        {
            // attmept to fetch the replacement file from the input path, if we cannot, throw bagagwa
            if (path.EndsWith(".avfx") && !SpatialAudioCache.CustomAvfxPaths.TryGetValue(path, out var newVfxPath))
                throw new Bagagwa("Failed to find replacement path for VFX");
            else if (path.EndsWith(".scd") && !SpatialAudioCache.CustomScdPaths.TryGetValue(path, out var newScdPath))
                throw new Bagagwa("Failed to find replacement path for VFX");
            else if (!path.EndsWith(".avfx") && !path.EndsWith(".scd"))
                throw new Bagagwa("Invalid Path");

            // construct the actorVfx from the given objects and paths.
            var created = ResourceDetours.CreateActorVfx(path, targetObject.Address, targetObject.Address);
            // If the Vfx was created, add it to the cache.
            _cache.AddTrackedVfx(created, path, SpawnType.Target, canLoop);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Summoned bagagwa while spawning a VFX on target: {ex}");
        }
    }

    // Action to perform on each tick
    public void OnTick()
    {
        // Check to see if any stored Vfx's Looped
        var justLooped = new List<VfxLoopItem>();
        foreach (var loop in _cache.VfxsToLoop)
        {
            // If the time since the Vfx was removed is less than 0.1 seconds, skip it
            if ((DateTime.Now - loop.RemovedTime).TotalSeconds < 0.1f) 
                continue;

            // Add it to the loop,
            justLooped.Add(loop);

            // And spawn it again
            if (loop.Item.Type is SpawnType.Self) 
                OnSelf(loop.Item.Path, true);
            else if (loop.Item.Type is SpawnType.Target) 
                OnTarget(loop.Item.Path, true);
        }

        // Remove the Vfx's that just looped
        _cache.RemoveLoopedVfxs(justLooped);
    }
}
