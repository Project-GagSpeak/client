namespace Penumbra.GameData.Enums;

/// <summary> All available racial scaling parameters. </summary>
public enum RspAttribute : byte
{
    MaleMinSize,
    MaleMaxSize,
    MaleMinTail,
    MaleMaxTail,
    FemaleMinSize,
    FemaleMaxSize,
    FemaleMinTail,
    FemaleMaxTail,
    BustMinX,
    BustMinY,
    BustMinZ,
    BustMaxX,
    BustMaxY,
    BustMaxZ,
    NumAttributes,
}

public static class RspAttributeExtensions
{
    /// <summary> For which gender a certain racial scaling parameter is available. </summary>
    public static Gender ToGender(this RspAttribute attribute)
        => attribute switch
        {
            RspAttribute.MaleMinSize   => Gender.Male,
            RspAttribute.MaleMaxSize   => Gender.Male,
            RspAttribute.MaleMinTail   => Gender.Male,
            RspAttribute.MaleMaxTail   => Gender.Male,
            RspAttribute.FemaleMinSize => Gender.Female,
            RspAttribute.FemaleMaxSize => Gender.Female,
            RspAttribute.FemaleMinTail => Gender.Female,
            RspAttribute.FemaleMaxTail => Gender.Female,
            RspAttribute.BustMinX      => Gender.Female,
            RspAttribute.BustMinY      => Gender.Female,
            RspAttribute.BustMinZ      => Gender.Female,
            RspAttribute.BustMaxX      => Gender.Female,
            RspAttribute.BustMaxY      => Gender.Female,
            RspAttribute.BustMaxZ      => Gender.Female,
            _                          => Gender.Unknown,
        };

    /// <summary> Human-readable names for all racial scaling parameters. </summary>
    public static string ToFullString(this RspAttribute attribute)
        => attribute switch
        {
            RspAttribute.MaleMinSize   => "Male Minimum Size",
            RspAttribute.MaleMaxSize   => "Male Maximum Size",
            RspAttribute.FemaleMinSize => "Female Minimum Size",
            RspAttribute.FemaleMaxSize => "Female Maximum Size",
            RspAttribute.BustMinX      => "Bust Minimum X-Axis",
            RspAttribute.BustMaxX      => "Bust Maximum X-Axis",
            RspAttribute.BustMinY      => "Bust Minimum Y-Axis",
            RspAttribute.BustMaxY      => "Bust Maximum Y-Axis",
            RspAttribute.BustMinZ      => "Bust Minimum Z-Axis",
            RspAttribute.BustMaxZ      => "Bust Maximum Z-Axis",
            RspAttribute.MaleMinTail   => "Male Minimum Tail Length",
            RspAttribute.MaleMaxTail   => "Male Maximum Tail Length",
            RspAttribute.FemaleMinTail => "Female Minimum Tail Length",
            RspAttribute.FemaleMaxTail => "Female Maximum Tail Length",
            _                          => throw new InvalidEnumArgumentException(),
        };
}
