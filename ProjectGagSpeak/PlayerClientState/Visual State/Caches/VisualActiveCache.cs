using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Models;
using GagSpeak.Restrictions;
using OtterGui.Classes;
using Penumbra.GameData.Enums;

namespace GagSpeak.PlayerState.Visual;

public interface IVisualCache
{
    Dictionary<EquipSlot, GlamourSlot> Glamour { get; }
    HashSet<ModAssociation> Mods { get; }
    HashSet<Moodle> Moodles { get; }
    Traits Traits { get; }
}

public class VisualRestrictionsCache : IVisualCache
{
    public Dictionary<EquipSlot, GlamourSlot> Glamour { get; private set; }
    public HashSet<ModAssociation> Mods { get; private set; }
    public HashSet<Moodle> Moodles { get; private set; }
    public Traits Traits { get; private set; }

    public VisualRestrictionsCache()
    {
        Glamour = new Dictionary<EquipSlot, GlamourSlot>();
        Mods = new HashSet<ModAssociation>();
        Moodles = new HashSet<Moodle>();
        Traits = 0;
    }

    public void UpdateCache(SortedList<int, RestrictionItem> newRestrictions)
    {
        Glamour = newRestrictions.Values.Select(x => x.Glamour).ToDictionary(x => x.Slot);
        Mods = newRestrictions.Values.Select(x => x.Mod).ToHashSet();
        Moodles = newRestrictions.Values.Select(x => x.Moodle).ToHashSet();
        Traits = newRestrictions.Values.Select(x => x.Traits).Aggregate((x, y) => x | y);
    }
}

public class VisualAdvancedRestrictionsCache : IVisualCache
{
    public Dictionary<EquipSlot, GlamourSlot> Glamour { get; private set; }
    public HashSet<ModAssociation> Mods { get; private set; }
    public HashSet<Moodle> Moodles { get; private set; }
    public OptionalBool Headgear { get; private set; }
    public OptionalBool Visor { get; private set; }
    public OptionalBool Weapon { get; private set; }
    public (Guid Profile, uint Priority) CustomizeProfile { get; private set; }
    public Traits Traits { get; private set; }

    public VisualAdvancedRestrictionsCache()
    {
        ToDefaults();
    }

    private void ToDefaults()
    {
        Glamour = new Dictionary<EquipSlot, GlamourSlot>();
        Mods = new HashSet<ModAssociation>();
        Moodles = new HashSet<Moodle>();
        Headgear = OptionalBool.Null;
        Visor = OptionalBool.Null;
        Weapon = OptionalBool.Null;
        CustomizeProfile = (Guid.Empty, 0);
        Traits = 0;
    }

    public void UpdateCache(SortedList<GagLayer, GarblerRestriction> gagRestrictions)
    {
        Glamour = gagRestrictions.Values.Select(x => x.Glamour).ToDictionary(x => x.Slot);
        Mods = gagRestrictions.Values.Select(x => x.Mod).ToHashSet();
        Headgear = gagRestrictions.Values.Select(x => x.HeadgearState).Aggregate((x, y) => x | y);
        Visor = gagRestrictions.Values.Select(x => x.VisorState).Aggregate((x, y) => x | y);
        Moodles = gagRestrictions.Values.Select(x => x.Moodle).ToHashSet();
        CustomizeProfile = gagRestrictions.Values.Select(x => (x.ProfileGuid, x.ProfilePriority)).LastOrDefault(x => x.ProfileGuid != Guid.Empty);
        Traits = gagRestrictions.Values.Select(x => x.Traits).Aggregate((x, y) => x | y);
    }

    public void UpdateCache(RestraintSet? activeSet)
    {
        // if the active set is null, it was disabled, and we should reset to defaults.
        if (activeSet is null)
        {
            ToDefaults();
            return;
        }

        // Otherwise, update the items.
        Glamour = activeSet.GetGlamour().ToDictionary(x => x.Slot);
        Mods = activeSet.GetMods().ToHashSet();
        var meta = activeSet.GetMetaData();
        Headgear = meta.Headgear;
        Visor = meta.Visor;
        Weapon = meta.Weapon;
        Moodles = activeSet.GetMoodles();
        Traits = activeSet.GetTraits();
    }
}
