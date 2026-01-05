using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using GagSpeak.Services;
using static FFXIVClientStructs.FFXIV.Client.Game.ActionManager;

namespace GagSpeak.GameInternals.Detours;
public unsafe partial class StaticDetours
{
    /// <summary>
    ///     Detours every time a request to use an action is made.
    ///     Returning false prevents the action from being executed.
    /// </summary>
    internal Hook<Delegates.UseAction> UseActionHook;


    /// <summary>
    ///     The Time that you used the last action from cooldown group 58.
    /// </summary>
    private DateTime _lastUsedActionTime = DateTime.MinValue;


    /// <summary>
    ///     Determines if the action should be used or not.
    /// </summary>
    /// <returns> True if the action should execute, false if it should not.</returns>
    private unsafe bool UseActionDetour(ActionManager* am, ActionType type, uint acId, ulong targetId, uint extraParam, UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted)
    {
        if (_controlCache.BlockTeleportActions)
        {
            // check if we are trying to hit teleport or return from hotbars /  menus
            if (type is ActionType.GeneralAction && acId is 7 or 8)
                return false;
            // if we somehow managed to start executing it, then stop that too
            if (type is ActionType.Action && acId is 5 or 6 or 11408)
                return false;
        }

        if (_controlCache.BlockActions)
            return false;

        // Return original if not an action we can put arousal delay on.
        if (type is not ActionType.Action || acId <= 7)
            return UseActionHook.Original(am, type, acId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);

        // If it is one we can, ensure we only check if the Arousal GCD Factor is set for it.
        if (ArousalService.GcdDelayFactor > 1)
        {
            // Obtain the recast group.
            var adjustedId = am->GetAdjustedActionId(acId);
            var recastGroup = am->GetRecastGroup((int)type, adjustedId);

            // If the Action has a 2.5s GCD, we apply the scalar to it.
            if (recastGroup is 58)
            {
                // Multiply the base adjustedRecasttime by the GCD delay factor.
                var baseRecast = GetAdjustedRecastTime(type, acId);
                var expectedRecast = TimeSpan.FromMilliseconds((int)(baseRecast * ArousalService.GcdDelayFactor));

                if (DateTime.Now - _lastUsedActionTime < expectedRecast)
                {
                    // Logger.LogDebug($"ACTION COOLDOWN NOT FINISHED - {acId} | {type} | {expectedRecast}");
                    return false; // Do not execute the action
                }
                else
                {
                    // Logger.LogDebug($"ACTION COOLDOWN FINISHED - {acId} | {type} | {expectedRecast}");
                    _lastUsedActionTime = DateTime.Now; // Update the last used time
                }
            }
        }

        return UseActionHook.Original(am, type, acId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);
    }

}
