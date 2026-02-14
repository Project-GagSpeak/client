using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Kinksters;

public class RestrictionBase
{
    public EquipSlot Slot { get; private set; }
    public EquipItem GlamItem { get; private set; }
    public StainIds GlamDyes { get; private set; }
    public string ModName { get; private set; }
    public LightMoodle Moodle { get; private set; }
    public Traits Traits { get; private set; }
    public Arousal Arousal { get; private set; }

    public RestrictionBase(LightItem item) => UpdateFrom(item);

    public void UpdateFrom(LightItem item)
    {
        Slot = (EquipSlot)item.Slot.Slot;
        GlamItem = ItemSvc.Resolve(Slot, new(item.Slot.CItemId));
        GlamDyes = new(item.Slot.Dye1, item.Slot.Dye2);
        ModName = item.ModName;
        Moodle = item.Moodle;
        Traits = item.Traits;
        Arousal = item.Arousal;
    }

    public RestrictionBase CloneWith(StainIds newDyes)
    {
        var clone = (RestrictionBase)MemberwiseClone();
        clone.GlamDyes = newDyes;
        return clone;
    }
}

public class KinksterGag : RestrictionBase
{
    public GagType Gag { get; private set; }
    public bool IsEnabled { get; private set; }
    public string CPlusName { get; private set; }
    public bool Redraw { get; private set; }

    public KinksterGag(LightGag apiItem)
        : base(apiItem.Properties)
    {
        Gag = apiItem.Gag;
        IsEnabled = apiItem.Enabled;
        CPlusName = apiItem.CPlusName;
        Redraw = apiItem.Redraw;
    }

    public void UpdateFrom(LightGag apiItem)
    {
        base.UpdateFrom(apiItem.Properties);
        IsEnabled = apiItem.Enabled;
        CPlusName = apiItem.CPlusName;
        Redraw = apiItem.Redraw;
    }
}

public class KinksterRestriction : RestrictionBase
{
    public Guid Id { get; private set; } = Guid.Empty;
    public bool IsEnabled { get; private set; }
    public string Label { get; private set; }

    public KinksterRestriction(LightRestriction apiItem)
        : base(apiItem.Properties)
    {
        Id = apiItem.Id;
        IsEnabled = apiItem.Enabled;
        Label = apiItem.Label;
    }

    public void UpdateFrom(LightRestriction apiItem)
    {
        base.UpdateFrom(apiItem.Properties);
        IsEnabled = apiItem.Enabled;
        Label = apiItem.Label;
    }
}

// Restraint Slots. Keep these records as we never change them internally,
// unless we are updating them entirely, as they are a subset of restraint sets.
public abstract record KinksterSlotBase(RestraintFlags Flags);
public record KinksterSlotBasic(EquipSlot Slot, EquipItem GlamItem, StainIds GlamDyes, RestraintFlags Flags) : KinksterSlotBase(Flags);
public record KinksterSlotAdv(StainIds CustomDyes, RestraintFlags Flags) : KinksterSlotBase(Flags)
{
    public RestrictionBase? ItemRef { get; init; } = null;
    public RestrictionBase? ItemWithCustomDyes => ItemRef is null ? null : ItemRef.CloneWith(CustomDyes);
}

// Restraint Layers
public abstract record KinksterLayerBase(Guid Id, string Label, Arousal Arousal);
public record KinksterLayerRestriction(Guid Id, string Label, Arousal Arousal, StainIds CustomDyes, RestraintFlags Flags) : KinksterLayerBase(Id, Label, Arousal)
{
    public RestrictionBase? ItemRef { get; init; } = null;
    public RestrictionBase? ItemWithCustomDyes => ItemRef is null ? null : ItemRef.CloneWith(CustomDyes);
}
public record KinksterLayerMod(Guid Id, string Label, Arousal Arousal, string ModName) : KinksterLayerBase(Id, Label, Arousal);

// Restraint Item.
public class KinksterRestraint
{
    public Guid Id { get; private set; } = Guid.Empty;
    public bool IsEnabled { get; set; } = false;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<EquipSlot, KinksterSlotBase> SlotData { get; set; } = new();
    public List<KinksterLayerBase> Layers { get; set; } = new();
    public List<string> Mods { get; set; } = new();
    public List<LightMoodle> Moodles { get; set; } = new();
    public Traits BaseTraits { get; set; } = new();
    public Arousal Arousal { get; set; } = new();
    public bool Redraws { get; set; } = false;

    public KinksterRestraint()
    { }

    public void UpdateFrom(LightRestraint apiItem, Dictionary<Guid, KinksterRestriction> refItems)
    {
        Id = apiItem.Id;
        IsEnabled = apiItem.Enabled;
        Label = apiItem.Label;
        Description = apiItem.Desc;
        // Refresh the slots.
        SlotData.Clear();
        foreach (var (key, slot) in apiItem.BasicSlots)
        {
            SlotData.TryAdd((EquipSlot)key, new KinksterSlotBasic((EquipSlot)slot.Slot, 
                ItemSvc.Resolve((EquipSlot)key, slot.Item.CItemId), new(slot.Item.Dye1, slot.Item.Dye2), slot.Flags));
        }

        foreach (var (key, slot) in apiItem.AdvancedSlots)
        {
            var advSlot = new KinksterSlotAdv(new(slot.Dye1, slot.Dye2), slot.Flags) { ItemRef = refItems.GetValueOrDefault(slot.RestrictionId) };
            SlotData.TryAdd((EquipSlot)key, advSlot);
        }

        // Refresh the layers.
        Layers.Clear();
        var orderedLayers = new List<(int Index, KinksterLayerBase Layer)>();
        foreach (var layer in apiItem.RestrictionLayers)
        {
            var customDyes = new StainIds(layer.Dye1, layer.Dye2);
            var restrictionLayer = new KinksterLayerRestriction(layer.Id, layer.Label, layer.Arousal, new(layer.Dye1, layer.Dye2), layer.Flags);
            orderedLayers.Add((layer.LayerIdx, restrictionLayer));
        }

        foreach (var layer in apiItem.ModLayers)
        {
            var modLayer = new KinksterLayerMod(layer.Id, layer.Label, layer.Arousal, layer.ModName);
            orderedLayers.Add((layer.LayerIdx, modLayer));
        }

        // Sort by LayerIdx and extract just the layers
        Layers = orderedLayers.OrderBy(x => x.Index).Select(x => x.Layer).ToList();
        // clone the remaining properties.
        Mods = apiItem.Mods;
        Moodles = apiItem.Moodles;
        BaseTraits = apiItem.BaseTraits;
        Arousal = apiItem.Arousal;
        Redraws = apiItem.Redraws;
    }
}

public class KinksterCollar
{
    public string Label { get; private set; }
    public EquipSlot Slot { get; private set; }
    public EquipItem GlamItem { get; private set; }
    public string ModName { get; private set; }

    public KinksterCollar(LightCollar lightItem)
        => UpdateFrom(lightItem);

    public static readonly KinksterCollar Empty = new(new(string.Empty, new LightSlot(), string.Empty));

    public void UpdateFrom(LightCollar item)
    {
        Label = item.Label;
        Slot = (EquipSlot)item.Glamour.Slot;
        GlamItem = ItemSvc.Resolve(Slot, new(item.Glamour.CItemId));
        ModName = item.ModName;
    }
}

/// <summary> Represents a cursed item that can be applied to a Kinkster. </summary>
public class KinksterCursedLoot
{
    public Guid Id { get; private set; } = Guid.Empty;
    public string Label { get; private set; } = string.Empty;
    public Precedence Precedence { get; private set; } = Precedence.VeryLow;

    public KinksterCursedLoot(Guid id, string label, Precedence precedence, RestrictionBase? ItemRef)
    {
        Id = id;
        Label = label;
        Precedence = precedence;
        ItemReference = ItemRef;
    }

    public RestrictionBase? ItemReference { get; private set; } = null;

    public void UpdateFrom(LightCursedLoot item, RestrictionBase? newRef)
    {
        Id = item.Id;
        Label = item.Label;
        Precedence = item.Precedence;
        ItemReference = newRef;
    }
}

public class KinksterPattern
{
    public Guid Id { get; private set; } = Guid.Empty;
    public string Label { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TimeSpan Duration { get; private set; } = TimeSpan.Zero;
    public bool Loops { get; private set; } = false;
    public ToyBrandName Device1 { get; private set; } = ToyBrandName.Unknown;
    public ToyBrandName Device2 { get; private set; } = ToyBrandName.Unknown;
    public ToyMotor Motors { get; private set; } = ToyMotor.Unknown;

    public KinksterPattern(LightPattern apiItem)
        => UpdateFrom(apiItem);

    public void UpdateFrom(LightPattern apiItem)
    {
        Id = apiItem.Id;
        Label = apiItem.Label;
        Description = apiItem.Desc;
        Duration = apiItem.Duration;
        Loops = apiItem.Loops;
        Device1 = apiItem.Device1;
        Device2 = apiItem.Device2;
        Motors = apiItem.Motors;
    }
}

/// <summary> Pretty much required to initialize within the kinkster cache via lookups. </summary>
public class KinksterAlarm
{
    public Guid Id { get; private set; } = Guid.Empty;
    public string Label { get; private set; } = string.Empty;
    public DateTimeOffset SetTimeUTC { get; private set; } = DateTimeOffset.MinValue;
    public DaysOfWeek SetDays { get; private set; } = DaysOfWeek.None;
    public KinksterPattern? PatternRef { get; private set; } = null;
    public KinksterAlarm(LightAlarm apiItem, KinksterPattern? patternRef)
        => UpdateFrom(apiItem, patternRef);

    public void UpdateFrom(LightAlarm apiItem, KinksterPattern? patternRef)
    {
        Id = apiItem.Id;
        Label = apiItem.Label;
        SetTimeUTC = apiItem.SetTimeUTC;
        SetDays = apiItem.SetDays;
        PatternRef = patternRef;
    }
}

public record KinksterTrigger(Guid Id, string Label, string Description, int Priority, TriggerKind Kind, InvokableActionType ActionType);

/// <summary>
///     Contains lightweight storage of a Kinkster's storages for the client. <para />
///     The only data stored here is what helps assist for KinkPlates and tooltip hovering.
/// </summary>
/// <remarks> Down the line can make some storages only retain enabled items to allow toggling. </remarks>
public class KinksterCache
{
    public Dictionary<GagType, KinksterGag> Gags { get; private set; } = new();
    public Dictionary<Guid, KinksterRestriction> Restrictions { get; private set; } = new();
    public Dictionary<Guid, KinksterRestraint> Restraints { get; private set; } = new();
    public KinksterCollar Collar { get; private set; } = KinksterCollar.Empty;
    public Dictionary<Guid, KinksterCursedLoot> CursedItems { get; private set; } = new();
    public Dictionary<Guid, KinksterPattern> Patterns { get; private set; } = new();
    // Could maybe cache aliases here but maybe not since they store the same format as normal aliases...
    public Dictionary<Guid, KinksterAlarm> Alarms { get; private set; } = new();
    public Dictionary<Guid, KinksterTrigger> Triggers { get; private set; } = new();
    public Dictionary<GSModule, List<string>> Allowances { get; private set; } = new();

    // Do nothing.
    public KinksterCache()
    { }

    // Init Data.
    public KinksterCache(CharaLightStorageData networkData)
    {
        Svc.Logger.Information($"Initializing Kinkster Cache with {networkData.GagItems.Count()} Gags, {networkData.Restrictions.Count()} Restrictions, " +
            $"{networkData.Restraints.Count()} Restraints, {networkData.CursedItems.Count()} Cursed Items, " +
            $"{networkData.Patterns.Count()} Patterns, {networkData.Alarms.Count()} Alarms, and {networkData.Triggers.Count()} Triggers.");
        // convert to local data.
        foreach (var lightGag in networkData.GagItems)
            Gags.TryAdd(lightGag.Gag, new KinksterGag(lightGag));

        foreach (var lightRestriction in networkData.Restrictions)
            Restrictions.TryAdd(lightRestriction.Id, new KinksterRestriction(lightRestriction));

        foreach (var lightRestraint in networkData.Restraints)
        {
            // create a restraint, update it with the light data, then append it to the dictionary.
            var restraint = new KinksterRestraint();
            restraint.UpdateFrom(lightRestraint, Restrictions);
            Restraints.TryAdd(lightRestraint.Id, restraint);
        }

        // cursed items.
        foreach (var lightItem in networkData.CursedItems)
        {
            // attempt to get the reference from the loot id ref.
            RestrictionBase? itemRef = lightItem.Type switch
            {
                CursedLootType.Restriction => Restrictions.GetValueOrDefault(lightItem.RefId!.Value),
                CursedLootType.Gag => Gags.GetValueOrDefault(lightItem.Gag!.Value),
                _ => null
            };
            CursedItems.TryAdd(lightItem.Id, new KinksterCursedLoot(lightItem.Id, lightItem.Label, lightItem.Precedence, itemRef));
        }

        // patterns.
        foreach (var lightPattern in networkData.Patterns)
        {
            var pattern = new KinksterPattern(lightPattern);
            Patterns.TryAdd(lightPattern.Id, pattern);
        }

        // alarms.
        foreach (var lightAlarm in networkData.Alarms)
        {
            var patternRef = lightAlarm.PatternId != Guid.Empty ? Patterns.GetValueOrDefault(lightAlarm.PatternId) : null;
            var alarm = new KinksterAlarm(lightAlarm, patternRef);
            Alarms.TryAdd(alarm.Id, alarm);
        }

        // triggers.
        foreach (var lt in networkData.Triggers)
            Triggers.TryAdd(lt.Id, new KinksterTrigger(lt.Id, lt.Label, lt.Desc, lt.Priority, lt.Kind, lt.ActionType));
    }

    // Injections and updaters for the various modules.
    public void UpdateAllowances(GSModule module, List<string> allowances)
        => Allowances[module] = allowances;

    public void UpdateGagItem(GagType gagType, LightGag? apiGag)
    {
        if (apiGag is null)
        {
            Gags.Remove(gagType);
            return;
        }

        if (Gags.TryGetValue(gagType, out var existingGag))
            existingGag.UpdateFrom(apiGag);
        else
            Gags[gagType] = new KinksterGag(apiGag);
    }

    public void UpdateRestrictionItem(Guid id, LightRestriction? apiItem)
    {
        if (apiItem is null)
        {
            Restrictions.Remove(id);
            return;
        }

        if (Restrictions.TryGetValue(apiItem.Id, out var existingRestriction))
            existingRestriction.UpdateFrom(apiItem);
        else
            Restrictions[apiItem.Id] = new KinksterRestriction(apiItem);
    }

    public void UpdateRestraintItem(Guid id, LightRestraint? apiItem)
    {
        if (apiItem is null)
        {
            Restraints.Remove(id);
            return;
        }
        if (Restraints.TryGetValue(id, out var existingRestraint))
            existingRestraint.UpdateFrom(apiItem, Restrictions);
        else
        {
            var newRestraint = new KinksterRestraint();
            newRestraint.UpdateFrom(apiItem, Restrictions);
            Restraints[id] = newRestraint;
        }
    }

    public void UpdateCollarItem(LightCollar? apiItem)
    {
        if (apiItem is null)
            Collar = KinksterCollar.Empty;
        else
            Collar.UpdateFrom(apiItem);
    }

    public void UpdateLootItem(Guid id, LightCursedLoot? apiItem)
    {
        if (apiItem is null)
        {
            CursedItems.Remove(id);
            return;
        }

        // attempt to get the reference from the loot id ref.
        RestrictionBase? itemRef = apiItem.Type switch
        {
            CursedLootType.Restriction => Restrictions.GetValueOrDefault(apiItem.RefId!.Value),
            CursedLootType.Gag => Gags.GetValueOrDefault(apiItem.Gag!.Value),
            _ => null
        };

        if (CursedItems.TryGetValue(id, out var existingCursedItem))
            existingCursedItem.UpdateFrom(apiItem, itemRef);
        else
            CursedItems[id] = new KinksterCursedLoot(apiItem.Id, apiItem.Label, apiItem.Precedence, itemRef);
    }

    public void UpdatePatternItem(Guid id, LightPattern? apiItem)
    {
        if (apiItem is null)
        {
            Patterns.Remove(id);
            return;
        }
        if (Patterns.TryGetValue(id, out var existingPattern))
            existingPattern.UpdateFrom(apiItem);
        else
            Patterns[id] = new KinksterPattern(apiItem);
    }

    public void UpdateAlarmItem(Guid id, LightAlarm? apiItem)
    {
        if (apiItem is null)
        {
            Alarms.Remove(id);
            return;
        }

        // attempt to get the reference from the loot id ref.
        var patternRef = apiItem.PatternId != Guid.Empty ? Patterns.GetValueOrDefault(apiItem.PatternId) : null;

        if (Alarms.TryGetValue(id, out var currentAlarm))
            currentAlarm.UpdateFrom(apiItem, patternRef);
        else
            Alarms.TryAdd(id, new KinksterAlarm(apiItem, patternRef));
    }

    public void UpdateTriggerItem(Guid id, LightTrigger? apiItem)
    {
        if (apiItem is null)
        {
            Triggers.Remove(id);
            return;
        }

        if (Triggers.TryGetValue(id, out var existingRestraint))
            Triggers[id] = new KinksterTrigger(apiItem.Id, apiItem.Label, apiItem.Desc, apiItem.Priority, apiItem.Kind, apiItem.ActionType);
        else
            Triggers.TryAdd(apiItem.Id, new KinksterTrigger(apiItem.Id, apiItem.Label, apiItem.Desc, apiItem.Priority, apiItem.Kind, apiItem.ActionType));
    }
}
