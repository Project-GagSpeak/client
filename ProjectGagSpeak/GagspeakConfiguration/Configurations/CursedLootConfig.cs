using GagSpeak.Restrictions;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class CursedLootConfig : IGagspeakConfiguration
{
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;

    public CursedLootStorage Storage { get; set; } = new CursedLootStorage();
    public TimeSpan LockRangeLower { get; set; } = TimeSpan.Zero;
    public TimeSpan LockRangeUpper { get; set; } = TimeSpan.FromMinutes(1);
    public int LockChance { get; set; } = 0;

    public CursedLootConfig() { }
}
