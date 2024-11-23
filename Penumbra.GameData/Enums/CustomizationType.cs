using System.Collections.Frozen;

namespace Penumbra.GameData.Enums;

/// <summary> Human Customizations. </summary>
public enum CustomizationType : byte
{
    Unknown,
    Body,
    Tail,
    Face,
    Iris,
    Accessory,
    Hair,
    Ear,
    DecalFace,
    DecalEquip,
    Skin,
    Etc,
}

public static class CustomizationTypeEnumExtension
{
    /// <summary> Convert a customization type to the suffix used by the game. </summary>
    public static string ToSuffix(this CustomizationType value)
        => value switch
        {
            CustomizationType.Body      => "top",
            CustomizationType.Face      => "fac",
            CustomizationType.Iris      => "iri",
            CustomizationType.Accessory => "acc",
            CustomizationType.Hair      => "hir",
            CustomizationType.Tail      => "til",
            CustomizationType.Ear       => "zer",
            CustomizationType.Etc       => "etc",
            _                           => throw new InvalidEnumArgumentException(),
        };
}

public static partial class Names
{
    /// <summary> A dictionary converting path suffices into CustomizationType. </summary>
    public static readonly IReadOnlyDictionary<string, CustomizationType> SuffixToCustomizationType = FrozenDictionary.ToFrozenDictionary(
    [
        new KeyValuePair<string, CustomizationType>(CustomizationType.Body.ToSuffix(),      CustomizationType.Body),
        new KeyValuePair<string, CustomizationType>(CustomizationType.Face.ToSuffix(),      CustomizationType.Face),
        new KeyValuePair<string, CustomizationType>(CustomizationType.Iris.ToSuffix(),      CustomizationType.Iris),
        new KeyValuePair<string, CustomizationType>(CustomizationType.Accessory.ToSuffix(), CustomizationType.Accessory),
        new KeyValuePair<string, CustomizationType>(CustomizationType.Hair.ToSuffix(),      CustomizationType.Hair),
        new KeyValuePair<string, CustomizationType>(CustomizationType.Tail.ToSuffix(),      CustomizationType.Tail),
        new KeyValuePair<string, CustomizationType>(CustomizationType.Ear.ToSuffix(),       CustomizationType.Ear),
        new KeyValuePair<string, CustomizationType>(CustomizationType.Etc.ToSuffix(),       CustomizationType.Etc),
    ]);
}
