using Dalamud.Game;
using Dalamud.Plugin.Services;
using GagSpeak.Services.Mediator;
using System.Runtime.InteropServices;
using GagSpeak.PlayerClient;
using GagSpeak.GameInternals;

namespace GagSpeak.UpdateMonitoring.SpatialAudio;

// References for Knowledge
// 
// FFXIVClientStruct Sound Manager for handling sound effects and music
// https://github.com/aers/FFXIVClientStructs/blob/f42f0b960f0c956e62344daf161a2196123f0426/FFXIVClientStructs/FFXIV/Client/Sound/SoundManager.cs
//
// Penumbra's approach to intercepting and modifying incoming loaded sounds (requires replacement)
// https://github.com/xivdev/Penumbra/blob/0d1ed6a926ccb593bffa95d78a96b48bd222ecf7/Penumbra/Interop/Hooks/Animation/LoadCharacterSound.cs#L11
//
// Ocalot's Way to play sounds stored within VFX containers when applied on targets: (one we will use)
// https://github.com/0ceal0t/Dalamud-VFXEditor/blob/10c8420d064343f5f6bd902485cbaf28f7524e0d/VFXEditor/Interop

public unsafe partial class ResourceLoader : IDisposable
{
    private readonly ILogger<ResourceLoader> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _mainConfig;
    private readonly AvfxManager _avfxManager;
    private readonly ScdManager _scdManager;

    public static readonly Dictionary<string, string> CustomPathBackups = []; // Map of lowercase custom game paths to local paths
    public bool HooksEnabled = false;

    public const int GameResourceOffset = 0x38;

    public ResourceLoader(ILogger<ResourceLoader> logger, GagspeakMediator mediator,
        MainConfig mainConfig, AvfxManager avfxManager, ScdManager scdManager)
    {
        _logger = logger;
        _mediator = mediator;
        _mainConfig = mainConfig;
        _avfxManager = avfxManager;
        _scdManager = scdManager;
        
        Svc.Hook.InitializeFromAttributes(this);

        // declare the addresses
        var staticVfxCreateAddress = Svc.SigScanner.ScanText(Signatures.StaticVfxCreateSig);
        var staticVfxRemoveAddress = Svc.SigScanner.ScanText(Signatures.StaticVfxRemoveSig);
        var actorVfxCreateAddress = Svc.SigScanner.ScanText(Signatures.ActorVfxCreateSig);
        var actorVfxRemoveAddresTemp = Svc.SigScanner.ScanText(Signatures.ActorVfxRemoveSig) + 7;
        var actorVfxRemoveAddress = Marshal.ReadIntPtr(actorVfxRemoveAddresTemp + Marshal.ReadInt32(actorVfxRemoveAddresTemp) + 4);

        ReadSqpackHook = Svc.Hook.HookFromSignature<ReadSqpackPrototype>(Signatures.ReadSqpackSig, ReadSqpackDetour);
        GetResourceSyncHook = Svc.Hook.HookFromSignature<GetResourceSyncPrototype>(Signatures.GetResourceSyncSig, GetResourceSyncDetour);
        GetResourceAsyncHook = Svc.Hook.HookFromSignature<GetResourceAsyncPrototype>(Signatures.GetResourceAsyncSig, GetResourceAsyncDetour);
        ReadFile = Marshal.GetDelegateForFunctionPointer<ReadFilePrototype>(Svc.SigScanner.ScanText(Signatures.ReadFileSig));


        // declare the hooks.
        ActorVfxCreate = Marshal.GetDelegateForFunctionPointer<ActorVfxCreateDelegate>(actorVfxCreateAddress);
        ActorVfxRemove = Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(actorVfxRemoveAddress);
        ActorVfxCreateHook = Svc.Hook.HookFromAddress<ActorVfxCreateDelegate>(actorVfxCreateAddress, ActorVfxNewDetour);
        ActorVfxRemoveHook = Svc.Hook.HookFromAddress<ActorVfxRemoveDelegate>(actorVfxRemoveAddress, ActorVfxRemoveDetour);
        VfxUseTriggerHook = Svc.Hook.HookFromSignature<VfxUseTriggerDelete>(Signatures.CallTriggerSig, VfxUseTriggerDetour);

        PlaySoundPath = Marshal.GetDelegateForFunctionPointer<PlaySoundDelegate>(Svc.SigScanner.ScanText(Signatures.PlaySoundSig));
        InitSoundHook = Svc.Hook.HookFromSignature<InitSoundPrototype>(Signatures.InitSoundSig, InitSoundDetour);

        logger.LogInformation("Resource Loader Hooks Initialized");

        EnableVfxHooks();

        logger.LogInformation("Resource Loader Hooks Enabled");
    }


    // Hook enablers
    public void EnableVfxHooks()
    {
        if (HooksEnabled) return;

        ReadSqpackHook.Enable();
        GetResourceSyncHook.Enable();
        GetResourceAsyncHook.Enable();

        ActorVfxCreateHook.Enable();
        ActorVfxRemoveHook.Enable();
        VfxUseTriggerHook.Enable();

        InitSoundHook.Enable();

        HooksEnabled = true;
    }

    // Hook disablers
    public void DisableVfxHooks()
    {
        if (!HooksEnabled) return;

        ReadSqpackHook.Disable();
        GetResourceSyncHook.Disable();
        GetResourceAsyncHook.Disable();

        ActorVfxCreateHook.Disable();
        ActorVfxRemoveHook.Disable();
        VfxUseTriggerHook.Disable();

        InitSoundHook.Disable();

        HooksEnabled = false;
    }


    public void Dispose()
    {

        _logger.LogDebug($"Disposing of VfxResourceLoader");

        try
        {
            if (HooksEnabled) DisableVfxHooks(); // disable the hooks

            // dispose of the hooks
            ReadSqpackHook?.Dispose();
            GetResourceSyncHook?.Dispose();
            GetResourceAsyncHook?.Dispose();

            ActorVfxCreateHook?.Dispose();
            ActorVfxRemoveHook?.Dispose();
            VfxUseTriggerHook?.Dispose();

            InitSoundHook?.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error disposing of ResourceLoader");
        }
    }
}
