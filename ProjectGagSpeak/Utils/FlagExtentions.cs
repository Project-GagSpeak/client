using GagSpeak.PlayerState.Visual;

namespace GagSpeak.Utils;

// We handle this through individual cases because its more efficient 
public static class FlagEx
{
    // we avoid doing generic types here because it actually increases the processing time in the compiler if we convert to ambiguous types.
    public static bool HasAny(this HardcoreState flags, HardcoreState check) => (flags & check) != 0;
    public static bool HasAny(this VisualUpdateFlags flags, VisualUpdateFlags check) => (flags & check) != 0;
    public static bool HasAny(this PuppetPerms flags, PuppetPerms check) => (flags & check) != 0;
    public static bool HasAny(this MoodlePerms flags, MoodlePerms check) => (flags & check) != 0;
    public static bool HasAny(this HypnoAttributes flags, HypnoAttributes check) => (flags & check) != 0;
    public static bool HasAny(this DaysOfWeek flags, DaysOfWeek check) => (flags & check) != 0;



    /// <summary> Strictly to be used for debugging purposes. </summary>
    /// <param name="flags"> The flags to check. </param>
    /// <returns> A string representation of the individual flags that are set. </returns>
    /// <remarks> Throws exception if enum type is not a [Flags] enum. </remarks>
    /// <exception cref="ArgumentException"></exception>
    public static string ToSplitFlagString<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var type = typeof(TEnum);
        if (!type.IsEnum || !Attribute.IsDefined(type, typeof(FlagsAttribute)))
            throw new ArgumentException("ToIndividualFlagsString can only be used with [Flags] enums.");

        var inputValue = Convert.ToUInt64(value); // Handles all enum base types
        if (inputValue == 0)
            return value.ToString(); // Typically "None"

        var individualFlags = Enum.GetValues(type)
            .Cast<Enum>()
            .Where(flag =>
            {
                var flagValue = Convert.ToUInt64(flag);
                return flagValue != 0 && IsPowerOfTwo(flagValue) && (inputValue & flagValue) == flagValue;
            });

        return string.Join(", ", individualFlags.Select(f => f.ToString()));

        static bool IsPowerOfTwo(ulong x) => (x & (x - 1)) == 0;
    }
}
