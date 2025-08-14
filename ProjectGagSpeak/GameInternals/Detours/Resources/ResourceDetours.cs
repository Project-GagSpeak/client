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

        // do not call this.
        EnableHooks();
    }

    public void EnableHooks()
    {
        _logger.LogInformation("Enabling all ResourceDetour hooks.");
        // enable all hooks here.
        _logger.LogInformation("Enabled all ResopurceDetour hooks.");
    }

    public void DisableHooks()
    {
        _logger.LogInformation("Disabling all ResourceDetour hooks.");
        // disable all hooks here.
        _logger.LogInformation("Disabled all ResourceDetour hooks.");
    }

    public void Dispose()
    {
        _logger.LogInformation("Disabling all ResourceDetour hooks.");
        DisableHooks();
        // dispose of all hooks.

        // clear any func pointers for deallocation.

        _logger.LogInformation("Disabled all ResourceDetour hooks.");
    }
}
