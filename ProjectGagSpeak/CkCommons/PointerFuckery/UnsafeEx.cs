using Dalamud.Game.ClientState.Objects.Types;

namespace GagSpeak.CkCommons.Pointers;

/// <summary>
/// Static class of unsafe extension methods.
/// </summary>
public static class UnsafeEx
{
    /// <summary> Gets the object table index directly from the GameObject* pointer item. </summary>
    /// <param name="gameObject"> The game object to get the index from. </param>
    /// <returns> The object table index or null if the object is null or the address is invalid. </returns>
    public static unsafe int? ObjectTableIndex(this IGameObject? gameObject)
    {
        if (gameObject == null || gameObject.Address == IntPtr.Zero)
        {
            return null;
        }

        return ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)gameObject.Address)->ObjectIndex;
    }
}

