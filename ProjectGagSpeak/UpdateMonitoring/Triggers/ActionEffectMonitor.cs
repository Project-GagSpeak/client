using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.GagspeakConfiguration;
using GagspeakAPI.Enums;
using Microsoft.Extensions.Logging;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler;
using ActionEffectHandler = FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler;

// References for Knowledge
// https://github.com/NightmareXIV/ECommons/blob/master/ECommons/Hooks/ActionEffect.cs

namespace GagSpeak.UpdateMonitoring.Triggers;
public unsafe class ActionEffectMonitor : IDisposable
{
    private readonly ILogger<ActionEffectMonitor> _logger;
    private readonly GagspeakConfigService _mainConfig;

    private static class Signatures
    {
        public const string ReceiveActionEffect = "40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48";
    }

    public delegate void ProcessActionEffect(uint sourceId, Character* sourceCharacter, Vector3* pos, ActionEffectHandler.Header* effectHeader, EffectEntry* effectArray, ulong* effectTail);
    internal static Hook<ProcessActionEffect> ProcessActionEffectHook = null!;

    public static event Action<List<ActionEffectEntry>> _actionEffectEntryEvent;
    public static event Action<List<ActionEffectEntry>> ActionEffectEntryEvent
    {
        add => _actionEffectEntryEvent += value;
        remove => _actionEffectEntryEvent -= value;
    }

    public ActionEffectMonitor(ILogger<ActionEffectMonitor> logger, GagspeakConfigService mainConfig, 
        ISigScanner sigScanner, IGameInteropProvider interopProvider)
    {
        _logger = logger;
        _logger.LogInformation("Starting ActionEffect Monitor", LoggerType.ActionEffects);
        _mainConfig = mainConfig;
        interopProvider.InitializeFromAttributes(this);

        var actionEffectReceivedAddr = sigScanner.ScanText(Signatures.ReceiveActionEffect);
        ProcessActionEffectHook = interopProvider.HookFromAddress<ProcessActionEffect>(actionEffectReceivedAddr, ProcessActionEffectDetour);
        _logger.LogInformation("Starting ActionEffect Monitor", LoggerType.ActionEffects);
        EnableHook();
        _logger.LogInformation("Started ActionEffect Monitor", LoggerType.ActionEffects);
    }

    public void EnableHook()
    {
        if (ProcessActionEffectHook.IsEnabled) return;
        _logger.LogInformation("Enabling ActionEffect Monitor", LoggerType.ActionEffects);
        ProcessActionEffectHook.Enable();
    }

    public void DisableHook()
    {
        if (!ProcessActionEffectHook.IsEnabled) return;
        _logger.LogInformation("Disabling ActionEffect Monitor", LoggerType.ActionEffects);
        ProcessActionEffectHook.Disable();
    }

    public void Dispose()
    {
        _logger.LogInformation("Stopping ActionEffect Monitor", LoggerType.ActionEffects);
        try
        {
            if (ProcessActionEffectHook.IsEnabled) DisableHook();
            ProcessActionEffectHook.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error disposing of ResourceLoader");
        }
        _logger.LogInformation("Stopped ActionEffect Monitor", LoggerType.ActionEffects);
    }

    private void ProcessActionEffectDetour(uint sourceID, Character* sourceCharacter, Vector3* pos, ActionEffectHandler.Header* effectHeader, EffectEntry* effectArray, ulong* effectTail)
    {
        try
        {
            _logger.LogDebug($"--- source actor: {sourceCharacter->GameObject.EntityId}, action id {effectHeader->ActionId}, numTargets: {effectHeader->NumTargets} ---", LoggerType.ActionEffects);

            var TargetEffects = new TargetEffect[effectHeader->NumTargets];
            for (var i = 0; i < effectHeader->NumTargets; i++)
            {
                TargetEffects[i] = new TargetEffect(effectTail[i], effectArray + 8 * i);
            }

            var affectedTargets = new List<ActionEffectEntry>();
            foreach (var effect in TargetEffects)
            {
                effect.ForEach(entry =>
                {
                    if(entry.type == 0) return; // ignore blank entries.
                    if (!entry.TryGetActionEffectType(out var actionEffectType))
                    {
                        _logger.LogDebug("EffectType was of type : " + entry.type, LoggerType.ActionEffects);
                        return;
                    }
                    // the effect is valid, so add it to targeted effects 
                    affectedTargets.Add(new ActionEffectEntry(sourceID, effect.TargetID, actionEffectType, effectHeader->ActionId, entry.Damage));
                });
            }

            if (affectedTargets.Count > 0) _actionEffectEntryEvent?.Invoke(affectedTargets);
        }
        catch (Exception e)
        {
            _logger.LogError($"An error has occurred in Action Effect hook.\n{e}");
        }

        ProcessActionEffectHook.Original(sourceID, sourceCharacter, pos, effectHeader, effectArray, effectTail);
    }
}

public struct ActionEffectEntry
{
    public uint SourceID { get; }
    public ulong TargetID { get; }
    public LimitedActionEffectType Type { get; } // make this use a byte over ActionEffectType, so we fetch the type even if its not one we care about, and only accept it if it is one we care about.
    public uint ActionID { get; }
    public uint Damage { get; }

    public ActionEffectEntry(uint sourceID, ulong targetID, LimitedActionEffectType type, uint actionID, uint damage)
    {
        SourceID = sourceID;
        TargetID = targetID;
        Type = type;
        ActionID = actionID;
        Damage = damage;
    }
}

public unsafe struct TargetEffect
{
    private readonly EffectEntry* _effects;

    public ulong TargetID { get; }

    public TargetEffect(ulong targetId, EffectEntry* effects)
    {
        TargetID = targetId;
        _effects = effects;
    }

    public EffectEntry this[int index]
    {
        get
        {
            if (index < 0 || index > 7) return default;
            return _effects[index];
        }
    }

    public void ForEach(Action<EffectEntry> act)
    {
        if (act == null) return;
        for (var i = 0; i < 8; i++)
        {
            var e = this[i];
            act(e);
        }
    }
}

public struct EffectEntry
{
    public byte type;
    public byte param0;
    public byte param1;
    public byte param2;
    public byte mult;
    public byte flags;
    public ushort value;

    public byte AttackType => (byte)(param1 & 0xF);

    public uint Damage => mult == 0 ? value : value + ((uint)ushort.MaxValue + 1) * mult;

    public bool TryGetActionEffectType(out LimitedActionEffectType effectType)
    {
        if (Enum.IsDefined(typeof(LimitedActionEffectType), type))
        {
            effectType = (LimitedActionEffectType)type;
            return true;
        }
        effectType = default;
        return false;
    }

    public override string ToString()
    {
        return
            $"Type: {type}, p0: {param0:D3}, p1: {param1:D3}, p2: {param2:D3} 0x{param2:X2} '{Convert.ToString(param2, 2).PadLeft(8, '0')}', " +
            $"mult: {mult:D3}, flags: {flags:D3} | {Convert.ToString(flags, 2).PadLeft(8, '0')}, value: {value:D6} ATTACK TYPE: {AttackType}";
    }
}
