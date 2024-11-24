using GagSpeak.Utils;

namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public class CursedLootStorage
{
    /// <summary> 
    /// The list of created cursed loot items 
    /// </summary>
    public List<CursedItem> CursedItems { get; set; } = new List<CursedItem>();

    /// <summary> 
    /// The Lower Lock limit for the cursed items 
    /// </summary>
    public TimeSpan LockRangeLower { get; set; } = TimeSpan.Zero;

    /// <summary> 
    /// The Upper Lock limit for the cursed items 
    /// </summary>
    public TimeSpan LockRangeUpper { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary> 
    /// The chance of a cursed item being found in an opened dungeon chest.
    /// </summary>
    public int LockChance { get; set; } = 0;

    public CursedLootStorage() { }
}
