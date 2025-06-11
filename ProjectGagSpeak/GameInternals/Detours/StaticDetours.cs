using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.UpdateMonitoring;
using System.Runtime.InteropServices;

// For further control, look more indepth to Lifestream:
// https://github.com/NightmareXIV/Lifestream/blob/main/Lifestream/Tasks/CrossDC/TaskTpAndGoToWard.cs#L132

namespace GagSpeak.GameInternals.Detours;

/// <summary> For Signature Credits and References, see the <see cref="Signatures"/> class. </summary>
/// <remarks> All related Detours that are turned on during startup, and off during shutdown. </remarks>
public unsafe partial class StaticDetours : DisposableMediatorSubscriberBase
{
    private readonly LootHandler _lootHandler;
    private readonly TraitsHandler _traitHandler;
    private readonly TriggerHandler _triggerHandler;
    private readonly OnFrameworkService _frameworkUtils;

    public StaticDetours(
        ILogger<StaticDetours> logger,
        GagspeakMediator mediator,
        LootHandler lootHandler,
        TraitsHandler traitHandler,
        TriggerHandler triggerHandler,
        OnFrameworkService frameworkUtils,
        ISigScanner ss,
        IGameInteropProvider gip)
        : base(logger, mediator)
    {
        _lootHandler = lootHandler;
        _traitHandler = traitHandler;
        _triggerHandler = triggerHandler;
        _frameworkUtils = frameworkUtils;

        Logger.LogInformation("Initializing all StaticDetours!");
        gip.InitializeFromAttributes(this);

        ProcessActionEffectHook = gip.HookFromAddress<ProcessActionEffect>(ss.ScanText(Signatures.ReceiveActionEffect), ActionEffectDetour);
        
        OnExecuteEmoteHook = gip.HookFromAddress<AgentEmote.Delegates.ExecuteEmote>((nint)AgentEmote.MemberFunctionPointers.ExecuteEmote, OnExecuteEmote);
        ProcessEmoteHook = gip.HookFromSignature<OnEmoteFuncDelegate>(Signatures.OnEmote, ProcessEmoteDetour);
        
        UseActionHook = gip.HookFromAddress<ActionManager.Delegates.UseAction>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
        
        FireCallback = Marshal.GetDelegateForFunctionPointer<FireCallbackFuncDelegate>(ss.ScanText(Signatures.Callback));

        ItemInteractedHook = gip.HookFromAddress<TargetSystem.Delegates.InteractWithObject>((nint)TargetSystem.MemberFunctionPointers.InteractWithObject, ItemInteractedDetour);


        EnableHooks();
    }

    public void EnableHooks()
    {
        Logger.LogInformation("Enabling all StaticDetours and their hooks.");

        ProcessActionEffectHook?.Enable();
        ProcessEmoteHook?.Enable();
        OnExecuteEmoteHook?.Enable();
        UseActionHook?.Enable();
        FireCallbackHook?.Enable();
        ItemInteractedHook?.Enable();

        Logger.LogInformation("Enabled all StaticDetours and their hooks.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Logger.LogInformation("Disabling all StaticDetours and their hooks.");

        ProcessActionEffectHook?.Disable();
        ProcessActionEffectHook?.Dispose();
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

        Logger.LogInformation("Disabled all StaticDetours and their hooks.");
    }
}
