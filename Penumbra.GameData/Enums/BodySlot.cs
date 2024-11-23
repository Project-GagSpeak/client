using System.Collections.Frozen;

namespace Penumbra.GameData.Enums;

/// <summary> Body Slots as used by character model parts. </summary>
public enum BodySlot : byte
{
    Unknown,
    Hair,
    Face,
    Tail,
    Body,
    Ear,
}

public static class BodySlotEnumExtension
{
    /// <summary> The suffix used by game files to identify. </summary>
    public static string ToSuffix(this BodySlot value)
        => value switch
        {
            BodySlot.Ear  => "zear",
            BodySlot.Face => "face",
            BodySlot.Hair => "hair",
            BodySlot.Body => "body",
            BodySlot.Tail => "tail",
            _             => throw new InvalidEnumArgumentException(),
        };

    /// <summary> The abbreviation used by game files to prefix the secondary ID. </summary>
    public static char ToAbbreviation(this BodySlot value)
        => value switch
        {
            BodySlot.Hair => 'h',
            BodySlot.Face => 'f',
            BodySlot.Tail => 't',
            BodySlot.Body => 'b',
            BodySlot.Ear  => 'z',
            _             => throw new InvalidEnumArgumentException(),
        };

    /// <summary> Convert body slot to customization type. </summary>
    public static CustomizationType ToCustomizationType(this BodySlot value)
        => value switch
        {
            BodySlot.Hair => CustomizationType.Hair,
            BodySlot.Face => CustomizationType.Face,
            BodySlot.Tail => CustomizationType.Tail,
            BodySlot.Body => CustomizationType.Body,
            BodySlot.Ear  => CustomizationType.Ear,
            _             => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        };
}

public static partial class Names
{
    /// <summary> A dictionary converting path suffices into BodySlot. </summary>
    public static readonly IReadOnlyDictionary<string, BodySlot> StringToBodySlot =
        Enum.GetValues<BodySlot>().Skip(1).ToFrozenDictionary(e => e.ToSuffix(), e => e);
}
