using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.GameInternals.Structs;
using ActionEffectHandler = FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler;

namespace GagSpeak.GameInternals.Detours;

// Processing Action Effects.
public unsafe partial class StaticDetours
{
    public delegate void ProcessActionEffect(uint sourceId, Character* sourceCharacter, Vector3* pos, ActionEffectHandler.Header* effectHeader, EffectEntry* effectArray, ulong* effectTail);
    internal static Hook<ProcessActionEffect> ProcessActionEffectHook = null!;

    private void ActionEffectDetour(uint sourceID, Character* sourceCharacter, Vector3* pos, ActionEffectHandler.Header* effectHeader, EffectEntry* effectArray, ulong* effectTail)
    {
        try
        {
            Logger.LogTrace($"--- source actor: {sourceCharacter->GameObject.EntityId}, action id {effectHeader->ActionId}, numTargets: {effectHeader->NumTargets} ---", LoggerType.ActionEffects);

            var TargetEffects = new TargetEffect[effectHeader->NumTargets];

            for (var i = 0; i < effectHeader->NumTargets; i++)
                TargetEffects[i] = new TargetEffect(effectTail[i], effectArray + 8 * i);

            var affectedTargets = new List<ActionEffectEntry>();
            foreach (var effect in TargetEffects)
            {
                effect.ForEach(entry =>
                {
                    if(entry.type == 0)
                        return;

                    if (!entry.TryGetActionEffectType(out var actionEffectType))
                    {
                        Logger.LogTrace("EffectType was of type : " + entry.type, LoggerType.ActionEffects);
                        return;
                    }

                    // the effect is valid, so add it to targeted effects 
                    affectedTargets.Add(new ActionEffectEntry(sourceID, effect.TargetID, actionEffectType, effectHeader->ActionId, entry.Damage));
                });
            }

            if (affectedTargets.Count > 0)
                _triggerHandler.OnActionEffectEvent(affectedTargets);
        }
        catch (Exception e)
        {
            Logger.LogError($"An error has occurred in Action Effect hook.\n{e}");
        }

        ProcessActionEffectHook.Original(sourceID, sourceCharacter, pos, effectHeader, effectArray, effectTail);
    }
}
