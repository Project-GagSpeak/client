namespace Glamourer.Api.Enums;

/// <summary> Application flags that can be used in different situations. </summary>
[Flags]
public enum ApplyFlag : ulong
{
    /// <summary> Apply the selected manipulation only once, without forcing the state into automation. </summary>
    Once = 0x01,

    /// <summary> Apply the selected manipulation on the equipment (might be more or less supported). </summary>
    Equipment = 0x02,

    /// <summary> Apply the selected manipulation on the customizations (might be more or less supported). </summary>
    Customization = 0x04,

    /// <summary> Lock the state with the given key after applying the selected manipulation </summary>
    Lock = 0x08,
}

/// <summary> Extensions for apply flags. </summary>
public static class ApplyFlagEx
{
    /// <summary> The default application flags for design-based manipulations. </summary>
    public const ApplyFlag DesignDefault = ApplyFlag.Once | ApplyFlag.Equipment | ApplyFlag.Customization;

    /// <summary> The default application flags for state-based manipulations. </summary>
    public const ApplyFlag StateDefault = ApplyFlag.Equipment | ApplyFlag.Customization | ApplyFlag.Lock;

    /// <summary> The default application flags for reverse manipulations. </summary>
    public const ApplyFlag RevertDefault = ApplyFlag.Equipment | ApplyFlag.Customization;
}
