using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace GagSpeak.GameInternals.Detours;
public partial class StaticDetours
{
    /// <summary>
    ///     Detours Emote Requests sent by the Client Player to perform an emote.
    ///     Performs an early return if the emote is not allowed to be executed.
    /// </summary>
    internal Hook<TargetSystem.Delegates.SetHardTarget> SetHardTargetHook;

    private unsafe bool SetHardTargetDetour(TargetSystem* thisPtr, GameObject* hardTargetObject, bool ignoreTargetModes, bool a4, int a5)
    {
        if (_controlCache.PreventUnfollowing && hardTargetObject is null)
        {
            Logger.LogWarning("Preventing the untarget from occuring!");
            return false;
        }

        // otherwise allow it.
        return SetHardTargetHook.Original(thisPtr, hardTargetObject, ignoreTargetModes, a4, a5);
    }
}
