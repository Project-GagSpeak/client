namespace Penumbra.GameData.Enums;

/// <summary> The different customization options as flags. </summary>
[Flags]
public enum CustomizeFlag : ulong
{
    Invalid           = 0,
    Race              = 1ul << CustomizeIndex.Race,
    Gender            = 1ul << CustomizeIndex.Gender,
    BodyType          = 1ul << CustomizeIndex.BodyType,
    Height            = 1ul << CustomizeIndex.Height,
    Clan              = 1ul << CustomizeIndex.Clan,
    Face              = 1ul << CustomizeIndex.Face,
    Hairstyle         = 1ul << CustomizeIndex.Hairstyle,
    Highlights        = 1ul << CustomizeIndex.Highlights,
    SkinColor         = 1ul << CustomizeIndex.SkinColor,
    EyeColorRight     = 1ul << CustomizeIndex.EyeColorRight,
    HairColor         = 1ul << CustomizeIndex.HairColor,
    HighlightsColor   = 1ul << CustomizeIndex.HighlightsColor,
    FacialFeature1    = 1ul << CustomizeIndex.FacialFeature1,
    FacialFeature2    = 1ul << CustomizeIndex.FacialFeature2,
    FacialFeature3    = 1ul << CustomizeIndex.FacialFeature3,
    FacialFeature4    = 1ul << CustomizeIndex.FacialFeature4,
    FacialFeature5    = 1ul << CustomizeIndex.FacialFeature5,
    FacialFeature6    = 1ul << CustomizeIndex.FacialFeature6,
    FacialFeature7    = 1ul << CustomizeIndex.FacialFeature7,
    LegacyTattoo      = 1ul << CustomizeIndex.LegacyTattoo,
    TattooColor       = 1ul << CustomizeIndex.TattooColor,
    Eyebrows          = 1ul << CustomizeIndex.Eyebrows,
    EyeColorLeft      = 1ul << CustomizeIndex.EyeColorLeft,
    EyeShape          = 1ul << CustomizeIndex.EyeShape,
    SmallIris         = 1ul << CustomizeIndex.SmallIris,
    Nose              = 1ul << CustomizeIndex.Nose,
    Jaw               = 1ul << CustomizeIndex.Jaw,
    Mouth             = 1ul << CustomizeIndex.Mouth,
    Lipstick          = 1ul << CustomizeIndex.Lipstick,
    LipColor          = 1ul << CustomizeIndex.LipColor,
    MuscleMass        = 1ul << CustomizeIndex.MuscleMass,
    TailShape         = 1ul << CustomizeIndex.TailShape,
    BustSize          = 1ul << CustomizeIndex.BustSize,
    FacePaint         = 1ul << CustomizeIndex.FacePaint,
    FacePaintReversed = 1ul << CustomizeIndex.FacePaintReversed,
    FacePaintColor    = 1ul << CustomizeIndex.FacePaintColor,
}

public static class CustomizeFlagExtensions
{
    /// <summary> The bitmask of all flags set. </summary>
    public const CustomizeFlag All         = (CustomizeFlag)(((ulong)CustomizeFlag.FacePaintColor << 1) - 1ul);

    /// <summary> The bitmask of all flags we care about set. </summary>
    public const CustomizeFlag AllRelevant = All & ~CustomizeFlag.Race;

    /// <summary> Changing any of these options requires a redraw of the character. </summary>
    /// <remarks> Face does not technically require a redraw, but crashes when looking at things without redrawing. </remarks>
    public const CustomizeFlag RedrawRequired =
        CustomizeFlag.Race | CustomizeFlag.Clan | CustomizeFlag.Gender | CustomizeFlag.Face | CustomizeFlag.BodyType;

    /// <summary> Return if a given set of flags requires a redraw. </summary>
    public static bool RequiresRedraw(this CustomizeFlag flags)
        => (flags & RedrawRequired) != 0;

    /// <summary> Convert CustomizeFlag to CustomizeIndex. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static CustomizeIndex ToIndex(this CustomizeFlag flag)
        => flag switch
        {
            CustomizeFlag.Race              => CustomizeIndex.Race,
            CustomizeFlag.Gender            => CustomizeIndex.Gender,
            CustomizeFlag.BodyType          => CustomizeIndex.BodyType,
            CustomizeFlag.Height            => CustomizeIndex.Height,
            CustomizeFlag.Clan              => CustomizeIndex.Clan,
            CustomizeFlag.Face              => CustomizeIndex.Face,
            CustomizeFlag.Hairstyle         => CustomizeIndex.Hairstyle,
            CustomizeFlag.Highlights        => CustomizeIndex.Highlights,
            CustomizeFlag.SkinColor         => CustomizeIndex.SkinColor,
            CustomizeFlag.EyeColorRight     => CustomizeIndex.EyeColorRight,
            CustomizeFlag.HairColor         => CustomizeIndex.HairColor,
            CustomizeFlag.HighlightsColor   => CustomizeIndex.HighlightsColor,
            CustomizeFlag.FacialFeature1    => CustomizeIndex.FacialFeature1,
            CustomizeFlag.FacialFeature2    => CustomizeIndex.FacialFeature2,
            CustomizeFlag.FacialFeature3    => CustomizeIndex.FacialFeature3,
            CustomizeFlag.FacialFeature4    => CustomizeIndex.FacialFeature4,
            CustomizeFlag.FacialFeature5    => CustomizeIndex.FacialFeature5,
            CustomizeFlag.FacialFeature6    => CustomizeIndex.FacialFeature6,
            CustomizeFlag.FacialFeature7    => CustomizeIndex.FacialFeature7,
            CustomizeFlag.LegacyTattoo      => CustomizeIndex.LegacyTattoo,
            CustomizeFlag.TattooColor       => CustomizeIndex.TattooColor,
            CustomizeFlag.Eyebrows          => CustomizeIndex.Eyebrows,
            CustomizeFlag.EyeColorLeft      => CustomizeIndex.EyeColorLeft,
            CustomizeFlag.EyeShape          => CustomizeIndex.EyeShape,
            CustomizeFlag.SmallIris         => CustomizeIndex.SmallIris,
            CustomizeFlag.Nose              => CustomizeIndex.Nose,
            CustomizeFlag.Jaw               => CustomizeIndex.Jaw,
            CustomizeFlag.Mouth             => CustomizeIndex.Mouth,
            CustomizeFlag.Lipstick          => CustomizeIndex.Lipstick,
            CustomizeFlag.LipColor          => CustomizeIndex.LipColor,
            CustomizeFlag.MuscleMass        => CustomizeIndex.MuscleMass,
            CustomizeFlag.TailShape         => CustomizeIndex.TailShape,
            CustomizeFlag.BustSize          => CustomizeIndex.BustSize,
            CustomizeFlag.FacePaint         => CustomizeIndex.FacePaint,
            CustomizeFlag.FacePaintReversed => CustomizeIndex.FacePaintReversed,
            CustomizeFlag.FacePaintColor    => CustomizeIndex.FacePaintColor,
            _                               => (CustomizeIndex)byte.MaxValue,
        };

    /// <summary> Convert CustomizeIndex to CustomizeFlag. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static CustomizeFlag ToFlag(this CustomizeIndex index)
        => (CustomizeFlag)(1ul << (int)index);
}
