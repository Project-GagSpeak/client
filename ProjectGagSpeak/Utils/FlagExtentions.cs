using GagSpeak.State;

namespace GagSpeak.Utils;

// We handle this through individual cases because its more efficient 
public static class FlagEx
{
    // we avoid doing generic types here because it actually increases the processing time in the compiler if we convert to ambiguous types.
    public static bool HasAny(this HardcoreSetting flags, HardcoreSetting check) => (flags & check) != 0;
    public static bool HasAny(this PlayerControlSource flags, PlayerControlSource check) => (flags & check) != 0;
    public static bool HasAny(this PuppetPerms flags, PuppetPerms check) => (flags & check) != 0;
    public static bool HasAny(this MoodlePerms flags, MoodlePerms check) => (flags & check) != 0;
    public static bool HasAny(this HypnoAttributes flags, HypnoAttributes check) => (flags & check) != 0;
    public static bool HasAny(this DaysOfWeek flags, DaysOfWeek check) => (flags & check) != 0;
}
