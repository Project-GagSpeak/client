using Dalamud.Game;
using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring.SpatialAudio.Managers;
using System.Runtime.InteropServices;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Loaders;

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
    private readonly GagspeakConfigService _mainConfig;
    private readonly AvfxManager _avfxManager;
    private readonly ScdManager _scdManager;
    private readonly IDataManager _dataManager;

    public static readonly Dictionary<string, string> CustomPathBackups = []; // Map of lowercase custom game paths to local paths
    public bool HooksEnabled = false;

    // https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/Object/GameObject.cs
    public const int GameResourceOffset = 0x38;

    private static class Signatures
    {
        public const string ReadFileSig = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 63 42";
        public const string ReadSqpackSig = "40 56 41 56 48 83 EC ?? 0F BE 02 ";
        public const string GetResourceSyncSig = "E8 ?? ?? ?? ?? 48 8B D8 8B C7 ";
        public const string GetResourceAsyncSig = "E8 ?? ?? ?? 00 48 8B D8 EB ?? F0 FF 83 ?? ?? 00 00";

        internal const string StaticVfxCreateSig = "E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08";
        internal const string StaticVfxRunSig = "E8 ?? ?? ?? ?? 8B 4B 7C 85 C9";
        internal const string StaticVfxRemoveSig = "40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9";

        internal const string ActorVfxCreateSig = "40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8";
        internal const string ActorVfxRemoveSig = "0F 11 48 10 48 8D 05"; // the weird one

        internal const string CallTriggerSig = "E8 ?? ?? ?? ?? 0F B7 43 56";

        internal const string PlaySoundSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? FE C2"; // FunctionPointer to play sound to system.
        internal const string InitSoundSig = "E8 ?? ?? ?? ?? 8B 5D 77"; // Signature to sound initialization
    }

    public ResourceLoader(ILogger<ResourceLoader> logger,
        GagspeakMediator mediator, GagspeakConfigService mainConfig, 
        AvfxManager avfxManager, ScdManager scdManager, 
        IDataManager dataManager, ISigScanner sigScanner, 
        IGameInteropProvider interopProvider)
    {
        _logger = logger;
        _mediator = mediator;
        _mainConfig = mainConfig;
        _avfxManager = avfxManager;
        _scdManager = scdManager;
        _dataManager = dataManager;

        // init the attributes
        interopProvider.InitializeFromAttributes(this);


        // declare the addresses
        var staticVfxCreateAddress = sigScanner.ScanText(Signatures.StaticVfxCreateSig);
        var staticVfxRemoveAddress = sigScanner.ScanText(Signatures.StaticVfxRemoveSig);
        var actorVfxCreateAddress = sigScanner.ScanText(Signatures.ActorVfxCreateSig);
        var actorVfxRemoveAddresTemp = sigScanner.ScanText(Signatures.ActorVfxRemoveSig) + 7;
        var actorVfxRemoveAddress = Marshal.ReadIntPtr(actorVfxRemoveAddresTemp + Marshal.ReadInt32(actorVfxRemoveAddresTemp) + 4);

        ReadSqpackHook = interopProvider.HookFromSignature<ReadSqpackPrototype>(Signatures.ReadSqpackSig, ReadSqpackDetour);
        GetResourceSyncHook = interopProvider.HookFromSignature<GetResourceSyncPrototype>(Signatures.GetResourceSyncSig, GetResourceSyncDetour);
        GetResourceAsyncHook = interopProvider.HookFromSignature<GetResourceAsyncPrototype>(Signatures.GetResourceAsyncSig, GetResourceAsyncDetour);
        ReadFile = Marshal.GetDelegateForFunctionPointer<ReadFilePrototype>(sigScanner.ScanText(Signatures.ReadFileSig));


        // declare the hooks.
        ActorVfxCreate = Marshal.GetDelegateForFunctionPointer<ActorVfxCreateDelegate>(actorVfxCreateAddress);
        ActorVfxRemove = Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(actorVfxRemoveAddress);
        ActorVfxCreateHook = interopProvider.HookFromAddress<ActorVfxCreateDelegate>(actorVfxCreateAddress, ActorVfxNewDetour);
        ActorVfxRemoveHook = interopProvider.HookFromAddress<ActorVfxRemoveDelegate>(actorVfxRemoveAddress, ActorVfxRemoveDetour);
        VfxUseTriggerHook = interopProvider.HookFromSignature<VfxUseTriggerDelete>(Signatures.CallTriggerSig, VfxUseTriggerDetour);

        PlaySoundPath = Marshal.GetDelegateForFunctionPointer<PlaySoundDelegate>(sigScanner.ScanText(Signatures.PlaySoundSig));
        InitSoundHook = interopProvider.HookFromSignature<InitSoundPrototype>(Signatures.InitSoundSig, InitSoundDetour);

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
