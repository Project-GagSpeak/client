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
using CkCommons;

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
    private readonly ClientData _clientData;
    private readonly PlayerControlCache _controlCache;
    private readonly GagRestrictionManager _gags;
    private readonly LootHandler _lootHandler;
    private readonly GlamourHandler _glamourHandler;
    private readonly TriggerHandler _triggerHandler;
    private readonly MufflerService _muffler;
    private readonly OnFrameworkService _frameworkUtils;

    private static MoveOverrides _moveOverrides = null!;
    public static MoveOverrides MoveOverrides
    {
        get
        {
            _moveOverrides ??= new MoveOverrides();
            return _moveOverrides;
        }
    }

    public StaticDetours(ILogger<StaticDetours> logger, GagspeakMediator mediator,
        ClientData clientData, PlayerControlCache controlCache, GagRestrictionManager gags, 
        GlamourHandler glamour, LootHandler loot, TriggerHandler trigger, MufflerService muffler, 
        OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _clientData = clientData;
        _controlCache = controlCache;
        _gags = gags;
        _lootHandler = loot;
        _glamourHandler = glamour;
        _triggerHandler = trigger;
        _muffler = muffler;
        _frameworkUtils = frameworkUtils;

        Logger.LogInformation("Initializing all StaticDetours!");
        Svc.Hook.InitializeFromAttributes(this);

        ActionEffectHook = Svc.Hook.HookFromAddress<ProcessActionEffect>(Svc.SigScanner.ScanText(Signatures.ReceiveActionEffect), ActionEffectDetour);
        OnExecuteEmoteHook = Svc.Hook.HookFromAddress<AgentEmote.Delegates.ExecuteEmote>((nint)AgentEmote.MemberFunctionPointers.ExecuteEmote, OnExecuteEmote);
        ProcessEmoteHook = Svc.Hook.HookFromSignature<OnEmoteFuncDelegate>(Signatures.OnEmote, ProcessEmoteDetour);
        UseActionHook = Svc.Hook.HookFromAddress<ActionManager.Delegates.UseAction>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
        FireCallbackFunc = Marshal.GetDelegateForFunctionPointer<AtkUnitBase_FireCallbackDelegate>(Svc.SigScanner.ScanText(Signatures.Callback));
        ItemInteractedHook = Svc.Hook.HookFromAddress<TargetSystem.Delegates.InteractWithObject>((nint)TargetSystem.MemberFunctionPointers.InteractWithObject, ItemInteractedDetour);
        GearsetInternalHook = Svc.Hook.HookFromAddress<RaptureGearsetModule.Delegates.EquipGearsetInternal>((nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearsetInternal, GearsetInternalDetour);
        SetHardTargetHook = Svc.Hook.HookFromAddress<TargetSystem.Delegates.SetHardTarget>((nint)TargetSystem.MemberFunctionPointers.SetHardTarget, SetHardTargetDetour);

        EnableHooks();
    }

    // Only trigger if we absolutely need to, as detouring a function that has a callback on EVERYTHING is a bit excessive. If we dont NEED it, dont USE IT.
    public void EnableCallbackHook()
    {
        if (FireCallbackHook.IsEnabled)
            return;

        Logger.LogInformation("Enabling FireCallbackDetour hook.");
        FireCallbackHook.Enable();
    }

    public void DisableCallbackHook()
    {
        if (!FireCallbackHook.IsEnabled)
            return;
        Logger.LogInformation("Disabling FireCallbackDetour hook.");
        FireCallbackHook.Disable();
    }

    public void EnableHooks()
    {
        Logger.LogInformation("Enabling all StaticDetours and their hooks.");

        ActionEffectHook.SafeEnable();
        ProcessEmoteHook.SafeEnable();
        OnExecuteEmoteHook.SafeEnable();
        UseActionHook.SafeEnable();
        ItemInteractedHook.SafeEnable();
        ProcessChatInputHook.SafeEnable();
        ApplyGlamourPlateHook.SafeEnable();
        GearsetInternalHook.SafeEnable();
        SetHardTargetHook.SafeEnable();

        Logger.LogInformation("Enabled all StaticDetours and their hooks.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Logger.LogInformation("Disabling all StaticDetours and their hooks.");

        _moveOverrides?.Dispose();

        ActionEffectHook.SafeDispose();
        ProcessEmoteHook.SafeDispose();
        OnExecuteEmoteHook.SafeDispose();
        UseActionHook.SafeDispose();
        ItemInteractedHook.SafeDispose();
        ProcessChatInputHook.SafeDispose();
        ApplyGlamourPlateHook.SafeDispose();
        GearsetInternalHook.SafeDispose();
        SetHardTargetHook.SafeDispose();

        // Only dispose if enabled.
        if (FireCallbackHook?.IsEnabled ?? false)
            FireCallbackHook?.Disable();

        FireCallbackHook?.Dispose();

        // clear the func pointer for deallocation.
        FireCallbackFunc = null;
        Logger.LogInformation("Disabled all StaticDetours and their hooks.");
    }
}
