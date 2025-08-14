using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using GagSpeak.GameInternals.Structs;
using InteropGenerator.Runtime.Attributes;
#nullable enable

namespace GagSpeak.GameInternals.Detours;

// Most of this is taken from penumbra as vfxeditor has been falling behind on updates.
public unsafe partial class ResourceDetours
{
    // An invokable function pointer delegate signature to create a static VFX.
    [Signature(Signatures.CreateStaticVfx)]
    private readonly delegate* unmanaged<byte*, byte*, VfxStruct*> CreateStaticVfx;

    // An invokable function pointer delegate signature to run a created static VFX.
    [Signature(Signatures.RunStaticVfx)]
    private readonly delegate* unmanaged<VfxStruct*, float, int, ulong> RunStaticVfx;

    // An invokable function pointer delegate signature to remove a static VFX.
    [Signature(Signatures.RemoveStaticVfx)]
    private readonly delegate* unmanaged<VfxStruct*, nint> RemoveStaticVfx;

    /// <summary>
    ///     Invokable function pointer to create an actorVFX.
    /// </summary>
    private static ActorVfxCreateDelegate ActorVfxCreateFunc = null!;

    /// <summary>
    ///     Invokable function pointer to remove an actorVFX. <para />
    ///     Unfortunately we cannot use the signature attribute here, and must assign it in constructor.
    /// </summary>
    private static ActorVfxRemoveDelegate ActorVfxRemoveFunc = null!;

    // detour that fires whenever an ActorVfx is created.
    private delegate IntPtr ActorVfxCreateDelegate(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7);
    [Signature(Signatures.CreateActorVfx, DetourName = nameof(ActorVfxCreatedDetour))]
    private readonly Hook<ActorVfxCreateDelegate> ActorVfxCreateHook = null!;

    // detour that fires whenever an ActorVfx is removed. (this must be initialized within the constructor)
    private delegate IntPtr ActorVfxRemoveDelegate(IntPtr vfx, char a2);
    private readonly Hook<ActorVfxRemoveDelegate> ActorVfxRemoveHook = null!;

    private IntPtr ActorVfxCreatedDetour(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7)
    {
        var vfx = ActorVfxCreateHook.Original(path, a2, a3, a4, a5, a6, a7);
        //_logger.LogTrace($"New Actor Created: {path} ({vfx:X8})");
        return vfx;
    }

    private IntPtr ActorVfxRemovedDetour(IntPtr vfx, char a2)
    {
        // remove from tracked cache.
        _cache.RemoveTrackedVfx(vfx);
        //_logger.LogTrace($"Removed Actor: ({vfx:X8})");
        return ActorVfxRemoveHook.Original(vfx, a2);
    }

    // add more paramaters as we learn more about this function!
    /// <summary>
    ///     Creates an actor vfx. This will summon Bagagwa if the path is invalid or the vfx is not found.
    /// </summary>
    public static VfxStruct* CreateActorVfx(string path, IntPtr caster, IntPtr target)
        => (VfxStruct*)ActorVfxCreateFunc(path, caster, target, -1, (char)0, 0, (char)0);

    /// <summary>
    ///     Forcefully removes the vfx spesified at the passed in pointer.
    /// </summary>
    /// <param name="vfxPtr"> address of the vfx to remove </param>
    /// <param name="a2"> should be 1 in most cases. </param>
    public static void RemoveActorVfx(IntPtr vfxPtr, char a2)
        => ActorVfxRemoveFunc(vfxPtr, a2);

}
