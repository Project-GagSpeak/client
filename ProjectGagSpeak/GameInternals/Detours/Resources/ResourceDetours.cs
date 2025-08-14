using CkCommons;
using GagSpeak.PlayerClient;
using GagSpeak.State.Caches;
using System.Runtime.InteropServices;

// these detours are distinctly related to spacial audio functionality and should only be in use while the setting is active.

namespace GagSpeak.GameInternals.Detours;
public unsafe partial class ResourceDetours : IDisposable
{
    private readonly ILogger<ResourceDetours> _logger;
    private readonly GagspeakConfig _config;
    private readonly SpatialAudioCache _cache;

    public ResourceDetours(ILogger<ResourceDetours> logger, GagspeakConfig config, SpatialAudioCache cache)
    {
        _logger = logger;
        _config = config;
        _cache = cache;

        _logger.LogInformation("Initializing all Resource Detours!");
        Svc.Hook.InitializeFromAttributes(this);

        // handle the special detour.
        var removeActorVfxAddrTemp = Svc.SigScanner.ScanText(Signatures.RemoveActorVfx) + 7;
        var removeActorVfxAddress = Marshal.ReadIntPtr(removeActorVfxAddrTemp + Marshal.ReadInt32(removeActorVfxAddrTemp) + 4);
        ActorVfxRemove = Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(removeActorVfxAddress);

        // get the current state of our spatial audio setting,
        // which will determine how we handle the state of the hooks.
        // This is critical as we don't want to track all resource actions while spatial audio is disabled!
        EnableHooks();
    }

    public void EnableHooks()
    {
        _logger.LogInformation("Enabling all ResourceDetour hooks.");
        // resource hooks.
        ReadSqPackHook.SafeEnable();
        GetResourceSyncHook.SafeEnable();
        GetResourceAsyncHook.SafeEnable();
        // sound hooks.
        CheckFileStateHook.SafeEnable();
        SoundOnLoadHook.SafeEnable();
        // vfx hooks
        ActorVfxCreateHook.SafeEnable();
        ActorVfxRemoveHook.SafeEnable();
        _logger.LogInformation("Enabled all ResopurceDetour hooks.");
    }

    public void DisableHooks()
    {
        _logger.LogInformation("Disabling all ResourceDetour hooks.");
        // resource hooks.
        ReadSqPackHook.SafeDisable();
        GetResourceSyncHook.SafeDisable();
        GetResourceAsyncHook.SafeDisable();
        // sound hooks.
        CheckFileStateHook.SafeDisable();
        SoundOnLoadHook.SafeDisable();
        // vfx hooks
        ActorVfxCreateHook.SafeDisable();
        ActorVfxRemoveHook.SafeDisable();
        // clear the custom scd crc.
        _logger.LogInformation("Disabled all ResourceDetour hooks.");
    }

    public void Dispose()
    {
        _logger.LogInformation("Disabling all ResourceDetour hooks.");
        // dispose all hooks.
        ReadSqPackHook.SafeDispose();
        GetResourceSyncHook.SafeDispose();
        GetResourceAsyncHook.SafeDispose();
        
        CheckFileStateHook.SafeDispose();
        SoundOnLoadHook.SafeDispose();
        
        ActorVfxCreateHook.SafeDispose();
        ActorVfxRemoveHook.SafeDispose();

        // clear any func pointers for deallocation.
        ActorVfxCreate = null!;
        ActorVfxRemove = null!;
        _logger.LogInformation("Disabled all ResourceDetour hooks.");
    }
}
