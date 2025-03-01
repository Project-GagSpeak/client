using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Components;
using GagSpeak.Services;
using GagSpeak.Utils;
using JetBrains.Annotations;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.PlayerState.Models;

public interface IRestraintSlot
{
    /// <summary> Used to determine what parts of this RestraintSlot are applied. </summary>
    public RestraintFlags ApplyFlags { get; set; }
    /// <summary> Required to have a way to obtain slot info. </summary>
    public EquipSlot EquipSlot { get; }
    /// <summary> Required to have a way to obtain item info. </summary>
    public EquipItem EquipItem { get; }
    /// <summary> Required to have a way to obtain stain info. </summary>
    public StainIds Stains { get; }
    public IRestraintSlot Clone();
    public JObject Serialize();
}

public class RestraintSlotBasic : IRestraintSlot
{
    public RestraintFlags ApplyFlags { get; set; } = RestraintFlags.Basic;
    public GlamourSlot Glamour { get; set; }
    public EquipSlot EquipSlot => Glamour.Slot;
    public EquipItem EquipItem => Glamour.GameItem;
    public StainIds Stains => Glamour.GameStain;

    internal RestraintSlotBasic() => Glamour = new GlamourSlot();
    internal RestraintSlotBasic(RestraintSlotBasic other)
    {
        Glamour = new GlamourSlot(other.Glamour);
    }

    public IRestraintSlot Clone() => new RestraintSlotBasic(this);

    public JObject Serialize()
    {
        return new JObject
        {
            // is overlay is true if the flags contain IsOverlay, false otherwise.
            ["Type"] = RestraintSlotType.Basic.ToString(),
            ["ApplyFlags"] = (int)ApplyFlags,
            ["Glamour"] = Glamour.Serialize(),
        };
    }
}

public class RestraintSlotAdvanced : IRestraintSlot
{
    public RestraintFlags ApplyFlags { get; set; } = RestraintFlags.Advanced;
    public RestrictionItem Ref { get; internal set; }
    public EquipSlot EquipSlot => Ref.Glamour.Slot;
    public EquipItem EquipItem => Ref.Glamour.GameItem;
    public StainIds Stains => Ref.Glamour.GameStain;
    internal RestraintSlotAdvanced() { }
    internal RestraintSlotAdvanced(RestraintSlotAdvanced other)
    {
        ApplyFlags = other.ApplyFlags;
        Ref = other.Ref;
    }

    public IRestraintSlot Clone() => new RestraintSlotAdvanced(this);
    public JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = RestraintSlotType.Advanced.ToString(),
            ["ApplyFlags"] = (int)ApplyFlags,
            ["RestrictionRef"] = Ref.Identifier.ToString(),
        };
    }
}

// This will eventually be able to be a mod customization toggle as well for a layer.
public interface IRestraintLayer : IComparable<IRestraintLayer>
{
    public bool IsActive { get; }
    public int Priority { get; }
    public IRestraintLayer Clone();
    public JObject Serialize();

    int IComparable<IRestraintLayer>.CompareTo(IRestraintLayer? other)
    {
        if (other == null) return 1;
        return Priority.CompareTo(other.Priority);
    }
}

public class RestrictionLayer : IRestraintLayer
{
    public bool IsActive { get; protected set; } = false;
    public int Priority { get; protected set; } = 0;
    public RestraintFlags ApplyFlags { get; set; } = RestraintFlags.Advanced;
    public IRestrictionItem Ref { get; set; }
    public EquipSlot EquipSlot => Ref.Glamour.Slot;
    public EquipItem EquipItem => Ref.Glamour.GameItem;
    public StainIds Stains => Ref.Glamour.GameStain;

    internal RestrictionLayer() { }
    internal RestrictionLayer(RestrictionLayer other)
    {
        IsActive = other.IsActive;
        Priority = other.Priority;
        ApplyFlags = other.ApplyFlags;
        Ref = other.Ref; // Point to the same reference
    }

    public IRestraintLayer Clone() => new RestrictionLayer(this);

    public JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = RestraintLayerType.Restriction.ToString(),
            ["IsActive"] = IsActive,
            ["Priority"] = Priority,
            ["ApplyFlags"] = (int)ApplyFlags,
            ["RestrictionRef"] = Ref.Identifier.ToString(),
        };
    }
}

public class ModPresetLayer : IRestraintLayer
{
    public bool IsActive { get; protected set; } = false;
    public int Priority { get; protected set; } = 0;
    public ModAssociation Ref { get; internal set; }

    internal ModPresetLayer() { }
    internal ModPresetLayer(ModPresetLayer other)
    {
        IsActive = other.IsActive;
        Priority = other.Priority;
        Ref = other.Ref;
    }
    public IRestraintLayer Clone() => new ModPresetLayer(this);

    public JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = RestraintLayerType.ModPreset.ToString(),
            ["IsActive"] = IsActive,
            ["Priority"] = Priority,
            ["ModRef"] = Ref.Serialize(),
        };
    }
}



public class RestraintSet
{
    public Guid Identifier { get; internal set; } = Guid.NewGuid();
    public string Label { get; internal set; } = string.Empty;
    public string Description { get; internal set; } = string.Empty;
    public bool DoRedraw { get; internal set; } = false;

    public Dictionary<EquipSlot, IRestraintSlot> RestraintSlots { get; internal set; }
    public GlamourBonusSlot Glasses { get; internal set; }
    public List<IRestraintLayer> Layers { get; internal set; }

    // Satisfy IMetaToggles
    public OptionalBool HeadgearState { get; internal set; } = OptionalBool.Null;
    public OptionalBool VisorState { get; internal set; } = OptionalBool.Null;
    public OptionalBool WeaponState { get; internal set; } = OptionalBool.Null;

    // Additional Appends
    public List<ModAssociation> RestraintMods { get; internal set; }
    public List<Moodle> RestraintMoodles { get; internal set; }

    internal RestraintSet() { }

    internal RestraintSet(RestraintSet other, bool keepIdentifier)
    {
        // we need to make a perfect clone.
        if (keepIdentifier)
        {
            Identifier = other.Identifier;
        }
        Label = other.Label;
        Description = other.Description;
        DoRedraw = other.DoRedraw;

        RestraintSlots = other.RestraintSlots.ToDictionary(x => x.Key, x => x.Value.Clone());
        Glasses = new GlamourBonusSlot(other.Glasses);
        Layers = other.Layers.Select(layer => layer.Clone()).ToList();

        // Optionally clone other properties as needed
        RestraintMods = other.RestraintMods.Select(mod => new ModAssociation(mod)).ToList();
        RestraintMoodles = other.RestraintMoodles.Select(moodle =>
        {
            return moodle switch
            {
                MoodlePreset preset => new MoodlePreset(preset),
                _ => new Moodle(moodle)
            };
        }).ToList();
    }

    #region Cache Helpers
    /// <summary> Arranges the associated glamour slots for this restraint set through a helper function. </summary>
    /// <returns> A dictionary of glamour slots associated with this restraint set. </returns>
    /// <remarks> Prioritizes the base slots, then the restraint layers. </remarks>
    public IEnumerable<GlamourSlot> GetGlamour()
    {
        var GlamourItems = new Dictionary<EquipSlot, GlamourSlot>();
        foreach (var glamourItem in RestraintSlots.Values.Where(x => x != null))
        {
            if(!glamourItem.ApplyFlags.HasFlag(RestraintFlags.Glamour)) 
                continue;

            if(glamourItem.EquipItem.ItemId == ItemService.NothingItem(glamourItem.EquipSlot).ItemId && glamourItem.ApplyFlags.HasFlag(RestraintFlags.IsOverlay))
                continue;

            if(glamourItem is RestraintSlotBasic basic) 
                GlamourItems[basic.EquipSlot] = basic.Glamour;
            if(glamourItem is RestraintSlotAdvanced advanced)
                GlamourItems[advanced.EquipSlot] = advanced.Ref.Glamour;
        }
        // then handle the layers.
        foreach (var layer in Layers.OfType<RestrictionLayer>())
        {
            if(!layer.ApplyFlags.HasFlag(RestraintFlags.Glamour))
                continue;

            if(layer.EquipItem.ItemId == ItemService.NothingItem(layer.EquipSlot).ItemId && layer.ApplyFlags.HasFlag(RestraintFlags.IsOverlay))
                continue;
            
            GlamourItems[layer.EquipSlot] = layer.Ref.Glamour;
        }
        // Append
        return GlamourItems.Values;
    }

    /// <summary> Arranges the associated mods for this restraint set through a helper function. </summary>
    /// <returns> A dictionary of mods associated with this restraint set. </returns>
    /// <remarks> Prioritizes the base slots, then the restraint layers, then additional mods. </remarks>
    public HashSet<ModAssociation> GetMods()
    {
        var associationPresets = new Dictionary<Mod, string>();
        foreach (var slot in RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
            if (slot.ApplyFlags.HasFlag(RestraintFlags.Mod))
                associationPresets[slot.Ref.Mod.ModInfo] = slot.Ref.Mod.CustomSettings;
        // append the additional mods from the layers.
        foreach (var l in Layers)
        {
            if (l is RestrictionLayer layer)
            {
                if (layer.ApplyFlags.HasFlag(RestraintFlags.Mod))
                    associationPresets[layer.Ref.Mod.ModInfo] = layer.Ref.Mod.CustomSettings;
            }

            if (l is ModPresetLayer modLayer)
            {
                if (modLayer.Ref is not null)
                    associationPresets[modLayer.Ref.ModInfo] = modLayer.Ref.CustomSettings;
            }
        }

        // Convert the dictionary entries back to ModAssociation
        return associationPresets.Select(kvp => new ModAssociation(kvp)).ToHashSet();
    }

    /// <summary> Grabs all 3 MetaData states compiled into a MetaDataStruct to operate with. </summary>
    public MetaDataStruct GetMetaData() => new MetaDataStruct(HeadgearState, VisorState, WeaponState);
    
    /// <summary> Obtains the distinct Moodles collection across all enabled parts of the restraint set. </summary>
    /// <remarks> Prioritizes the base slots, then the restraint layers, then additional mods. </remarks>
    public HashSet<Moodle> GetMoodles()
    {
        return new HashSet<Moodle>(
            RestraintSlots.Values
                .OfType<RestraintSlotAdvanced>()
                .Where(x => x.ApplyFlags.HasFlag(RestraintFlags.Moodle))
                .Select(x => x.Ref.Moodle)
                .Union(Layers.OfType<RestrictionLayer>().Where(x => x.ApplyFlags.HasFlag(RestraintFlags.Moodle)).Select(x => x.Ref.Moodle))
                .Union(RestraintMoodles));
    }

    /// <summary> Grabs all aggregated traits across the base restraint slots and layers if enabled. </summary>
    /// <remarks> Prioritizes RestraintSlots first, layers second. </remarks>
    public Traits GetTraits()
    {
        return RestraintSlots.Values
            .OfType<RestraintSlotAdvanced>()
            .Where(x => x.ApplyFlags.HasFlag(RestraintFlags.Trait))
            .Select(x => x.Ref.Traits)
            .DefaultIfEmpty(Traits.None)
            .Aggregate((x, y) => x | y)
            | Layers.OfType<RestrictionLayer>()
                .Where(x => x.ApplyFlags.HasFlag(RestraintFlags.Trait))
                .Select(x => x.Ref.Traits)
                .DefaultIfEmpty(Traits.None)
                .Aggregate((x, y) => x | y);
    }
    #endregion Cache Helpers

    public JObject Serialize()
    {
        return new JObject
        {
            ["Identifier"] = Identifier.ToString(),
            ["Label"] = Label,
            ["Description"] = Description,
            ["DoRedraw"] = DoRedraw,
            ["RestraintSlots"] = new JObject(RestraintSlots.Select(x => new JProperty(x.Key.ToString(), x.Value.Serialize()))),
            ["Glasses"] = Glasses.Serialize(),
            ["Layers"] = new JArray(Layers.OrderBy(x => x.Priority).Select(x => x.Serialize())),
            ["HeadgearState"] = HeadgearState.ToString(),
            ["VisorState"] = VisorState.ToString(),
            ["WeaponState"] = WeaponState.ToString(),
            ["RestraintMoodles"] = new JArray(RestraintMoodles.Select(x => x.Serialize())),
            ["RestraintMods"] = new JArray(RestraintMods.Select(x => x.Serialize())),
        };
    }
}




