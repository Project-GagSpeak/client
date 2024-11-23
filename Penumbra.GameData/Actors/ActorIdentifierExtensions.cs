using Dalamud.Game.ClientState.Objects.Enums;
using Penumbra.GameData.Enums;

namespace Penumbra.GameData.Actors;

/// <summary>
/// Since ActorIdentifiers are lightweight they do not contain a reference to the manager,
/// which is required to improve their naming and comparisons.
/// Thus, this is set by the manager itself statically.
/// </summary>
public static class ActorIdentifierExtensions
{
    /// <summary> Set by the ActorManager constructor, unset by its Dispose. </summary>
    public static ActorManager? Manager;

    /// <summary> Compare two identifiers by data id, which can yield equal names even for different ids. </summary>
    public static bool DataIdEquals(this ActorManager? manager, ActorIdentifier lhs, ActorIdentifier rhs)
    {
        // Different kinds can not ever be equal.
        if (lhs.Kind != rhs.Kind)
            return false;

        // Same kind and data id is always equal.
        if (lhs.DataId == rhs.DataId)
            return true;

        // If the manager is unavailable, we also accept no data-id in either of them.
        if (manager == null)
            return lhs.DataId == uint.MaxValue || rhs.DataId == uint.MaxValue;

        // If the manager is available, obtain the actual names from the data id and compare them for equality.
        return manager.Data.TryGetName(lhs.Kind, lhs.DataId, out var lhsName)
         && manager.Data.TryGetName(rhs.Kind,    rhs.DataId, out var rhsName)
         && lhsName.Equals(rhsName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary> Get the display name for ObjectKinds. </summary>
    public static string ToName(this ObjectKind kind)
        => kind switch
        {
            ObjectKind.None      => "Unknown",
            ObjectKind.BattleNpc => "Battle NPC",
            ObjectKind.EventNpc  => "Event NPC",
            ObjectKind.MountType => "Mount",
            ObjectKind.Companion => "Companion",
            ObjectKind.Ornament  => "Accessory",
            _                    => kind.ToString(),
        };

    /// <summary> Get the display name for IdentifierTypes. </summary>
    public static string ToName(this IdentifierType type)
        => type switch
        {
            IdentifierType.Player    => "Player",
            IdentifierType.Retainer  => "Retainer (Bell)",
            IdentifierType.Owned     => "Owned NPC",
            IdentifierType.Special   => "Special Actor",
            IdentifierType.Npc       => "NPC",
            IdentifierType.UnkObject => "Unknown Object",
            _                        => "Invalid",
        };

    /// <summary> Get the display name for special actors. </summary>
    public static string ToName(this ScreenActor actor)
        => actor switch
        {
            ScreenActor.CharacterScreen => "Character Screen Actor",
            ScreenActor.ExamineScreen   => "Examine Screen Actor",
            ScreenActor.FittingRoom     => "Fitting Room Actor",
            ScreenActor.DyePreview      => "Dye Preview Actor",
            ScreenActor.Portrait        => "Portrait Actor",
            _                           => "Invalid",
        };
}
