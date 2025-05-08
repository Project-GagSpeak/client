using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data;
using OtterGui.Classes;
using Penumbra.GameData.Enums;

namespace GagSpeak.PlayerState.Visual;

public interface IVisualCache
{
    Dictionary<EquipSlot, GlamourSlot> Glamour { get; }
    List<ModSettingsPreset> Mods { get; }
    HashSet<Moodle> Moodles { get; }
    Traits Traits { get; }
    Stimulation Stimulation { get; }
}

public class VisualRestrictionsCache : IVisualCache
{
    public Dictionary<EquipSlot, GlamourSlot> Glamour { get; private set; } = new Dictionary<EquipSlot, GlamourSlot>();
    public List<ModSettingsPreset> Mods { get; private set; } = new List<ModSettingsPreset>();
    public HashSet<Moodle> Moodles { get; private set; } = new HashSet<Moodle>();
    public Traits Traits { get; private set; } = Traits.None;
    public Stimulation Stimulation { get; private set; } = Stimulation.None;

    public VisualRestrictionsCache() { }

    public void UpdateCache(RestrictionItem[] newRestrictions)
    {
        // Update them in order, such that the higher layers can replace the lower layers.
        Glamour = newRestrictions.Select(x => x.Glamour).Where(x => x.Slot is not EquipSlot.Nothing).GroupBy(x => x.Slot).ToDictionary(g => g.Key, g => g.Last());
        Mods = newRestrictions.Select(x => x.Mod).ToList();
        Moodles = newRestrictions.Select(x => x.Moodle).ToHashSet();
        Traits = newRestrictions.Select(x => x.Traits).DefaultIfEmpty(Traits.None).Aggregate((x, y) => x | y);
        Stimulation = newRestrictions.Select(x => x.Stimulation).DefaultIfEmpty(Stimulation.None).Aggregate((x, y) => x | y);
    }

    public void UpdateCache(IEnumerable<CursedItem> newCursedItems, bool allowHc)
    {
/*        // Sort the incoming cursed items based on precedence, ensuring override-capable items go first within the same level
        var sortedItems = newCursedItems
            .OrderByDescending(x => x.Precedence) // Higher precedence first
            .ThenByDescending(x => x.CanOverride) // Override-capable items first within the same precedence
            .ToList();

        // Clear the cache
        Glamour.Clear();
        Mods.Clear();
        Moodles.Clear();
        Traits = Traits.None;
        Stimulation = Stimulation.None;

        // Initialize the cache
        foreach (var item in sortedItems)
        {
            var restriction = item.RestrictionRef;
            if (!Glamour.ContainsKey(restriction.Glamour.Slot))
                Glamour[restriction.Glamour.Slot] = restriction.Glamour;


            // add the mod to the list if we should. If a mod preset with the same modName exists, we should replace it.
            if (restriction.Mod.HasData)
            {
                if (Mods.FindIndex(x => x.Container.ModName == restriction.Mod.Container.ModName) is int idx && idx >= 0)
                    Mods[idx] = restriction.Mod; // Replace it.
                else
                    Mods.Add(restriction.Mod); // Add it.
            }

            // Try to add the Moodle.
            Moodles.Add(restriction.Moodle);

            // Aggregate the traits and stimulation.
            if(allowHc)
            {
                Traits |= restriction.Traits;
                Stimulation |= restriction.Stimulation;
            }
        }*/
    }
}

public class VisualAdvancedRestrictionsCache : IVisualCache
{
    public Dictionary<EquipSlot, GlamourSlot> Glamour { get; private set; } = new Dictionary<EquipSlot, GlamourSlot>();
    public List<ModSettingsPreset> Mods { get; private set; } = new List<ModSettingsPreset>();
    public HashSet<Moodle> Moodles { get; private set; } = new HashSet<Moodle>();
    public OptionalBool Headgear { get; private set; } = OptionalBool.Null;
    public OptionalBool Visor { get; private set; } = OptionalBool.Null;
    public OptionalBool Weapon { get; private set; } = OptionalBool.Null;
    public (Guid Profile, uint Priority) CustomizeProfile { get; private set; } = (Guid.Empty, 0);
    public Traits Traits { get; private set; } = Traits.None;
    public Stimulation Stimulation { get; private set; } = Stimulation.None;

    public VisualAdvancedRestrictionsCache()
    { }

    private void ToDefaults()
    {
        Glamour = new Dictionary<EquipSlot, GlamourSlot>();
        Mods = new List<ModSettingsPreset>();
        Moodles = new HashSet<Moodle>();
        Headgear = OptionalBool.Null;
        Visor = OptionalBool.Null;
        Weapon = OptionalBool.Null;
        CustomizeProfile = (Guid.Empty, 0);
        Traits = Traits.None;
        Stimulation = Stimulation.None;
    }

    // TODO: Optimize Logic!!!
    public void UpdateCache(GarblerRestriction[] gagRestrictions)
    {
        Glamour = gagRestrictions.Select(x => x.Glamour).Where(x => x.Slot is not EquipSlot.Nothing).GroupBy(x => x.Slot).ToDictionary(g => g.Key, g => g.Last());
        Mods = gagRestrictions.Select(x => x.Mod).ToList();
        Headgear = gagRestrictions.Select(x => x.HeadgearState).DefaultIfEmpty(OptionalBool.Null).Aggregate((x, y) => x | y);
        Visor = gagRestrictions.Select(x => x.VisorState).DefaultIfEmpty(OptionalBool.Null).Aggregate((x, y) => x | y);
        Moodles = gagRestrictions.Select(x => x.Moodle).ToHashSet();
        CustomizeProfile = gagRestrictions.Select(x => (x.ProfileGuid, x.ProfilePriority)).LastOrDefault(x => x.ProfileGuid != Guid.Empty);
        Traits = gagRestrictions.Select(x => x.Traits).DefaultIfEmpty(Traits.None).Aggregate((x, y) => x | y);
    }

    public void UpdateCache(RestraintSet activeSet)
    {
        // if the active set is null, it was disabled, and we should reset to defaults.
        if (activeSet is null)
        {
            ToDefaults();
            return;
        }

        // Otherwise, update the items.
        Glamour = activeSet.GetGlamour().Where(x => x.Slot is not EquipSlot.Nothing).GroupBy(x => x.Slot).ToDictionary(g => g.Key, g => g.Last());
        Mods = activeSet.GetMods().ToList();
        var meta = activeSet.GetMetaData();
        Headgear = meta.Headgear;
        Visor = meta.Visor;
        Weapon = meta.Weapon;
        Moodles = activeSet.GetMoodles();
        Traits = activeSet.GetTraits();
    }
}
