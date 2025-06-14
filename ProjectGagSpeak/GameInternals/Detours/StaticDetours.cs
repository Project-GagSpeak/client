using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using System.Runtime.InteropServices;
using GagSpeak.State.Managers;
using GagSpeak.State.Caches;

// We can seperate these into their own classes down the line
// if possible and have an overall manager. Once it needs more control.
// At the moment the chatdetours is the most messy.

// For further control, look more indepth to Lifestream:
// https://github.com/NightmareXIV/Lifestream/blob/main/Lifestream/Tasks/CrossDC/TaskTpAndGoToWard.cs#L132

namespace GagSpeak.GameInternals.Detours;

/// <summary> For Signature Credits and References, see the <see cref="Signatures"/> class. </summary>
/// <remarks> All related Detours that are turned on during startup, and off during shutdown. </remarks>
public unsafe partial class StaticDetours : DisposableMediatorSubscriberBase
{
    private readonly GlobalPermissions _globals;
    private readonly GagRestrictionManager _gags;
    private readonly LootHandler _lootHandler;
    private readonly GlamourHandler _glamourHandler;
    private readonly TraitsCache _traitCache;
    private readonly TriggerHandler _triggerHandler;
    private readonly MufflerService _muffler;
    private readonly OnFrameworkService _frameworkUtils;

    public StaticDetours(
        ILogger<StaticDetours> logger,
        GagspeakMediator mediator,
        GlobalPermissions globals,
        GagRestrictionManager gags,
        GlamourHandler glamourHandler,
        LootHandler lootHandler,
        TraitsCache traitCache,
        TriggerHandler triggerHandler,
        MufflerService muffler,
        OnFrameworkService frameworkUtils,
        ISigScanner ss,
        IGameInteropProvider gip)
        : base(logger, mediator)
    {
        _globals = globals;
        _gags = gags;
        _lootHandler = lootHandler;
        _traitCache = traitCache;
        _glamourHandler = glamourHandler;
        _triggerHandler = triggerHandler;
        _muffler = muffler;
        _frameworkUtils = frameworkUtils;

        Logger.LogInformation("Initializing all StaticDetours!");
        gip.InitializeFromAttributes(this);

        ActionEffectHook = gip.HookFromAddress<ProcessActionEffect>(ss.ScanText(Signatures.ReceiveActionEffect), ActionEffectDetour);
        
        OnExecuteEmoteHook = gip.HookFromAddress<AgentEmote.Delegates.ExecuteEmote>((nint)AgentEmote.MemberFunctionPointers.ExecuteEmote, OnExecuteEmote);
        ProcessEmoteHook = gip.HookFromSignature<OnEmoteFuncDelegate>(Signatures.OnEmote, ProcessEmoteDetour);
        
        UseActionHook = gip.HookFromAddress<ActionManager.Delegates.UseAction>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
        
        FireCallback = Marshal.GetDelegateForFunctionPointer<FireCallbackFuncDelegate>(ss.ScanText(Signatures.Callback));

        ItemInteractedHook = gip.HookFromAddress<TargetSystem.Delegates.InteractWithObject>((nint)TargetSystem.MemberFunctionPointers.InteractWithObject, ItemInteractedDetour);

        GearsetInternalHook = gip.HookFromAddress<RaptureGearsetModule.Delegates.EquipGearsetInternal>((nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearsetInternal, GearsetInternalDetour);

        EnableHooks();
    }

    public void EnableHooks()
    {
        Logger.LogInformation("Enabling all StaticDetours and their hooks.");

        ActionEffectHook?.Enable();
        
        ProcessEmoteHook?.Enable();
        OnExecuteEmoteHook?.Enable();
        
        UseActionHook?.Enable();
        
        FireCallbackHook?.Enable();
        
        ItemInteractedHook?.Enable();
        
        ProcessChatInputHook?.Enable();
        
        GearsetInternalHook?.Enable();

        Logger.LogInformation("Enabled all StaticDetours and their hooks.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Logger.LogInformation("Disabling all StaticDetours and their hooks.");

        ActionEffectHook?.Disable();
        ActionEffectHook?.Dispose();
        
        ProcessEmoteHook?.Disable();
        ProcessEmoteHook?.Dispose();
        OnExecuteEmoteHook?.Disable();
        OnExecuteEmoteHook?.Dispose();
        
        UseActionHook?.Disable();
        UseActionHook?.Dispose();
        
        FireCallbackHook?.Disable();
        FireCallbackHook?.Dispose();
        
        ItemInteractedHook?.Disable();
        ItemInteractedHook?.Dispose();
        
        ProcessChatInputHook?.Disable();
        ProcessChatInputHook?.Dispose();

        GearsetInternalHook?.Disable();
        GearsetInternalHook?.Dispose();

        Logger.LogInformation("Disabled all StaticDetours and their hooks.");
    }
}
