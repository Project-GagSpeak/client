using CkCommons.Classes;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.State.Models;

public interface IRestrictionRef
{
    /// <summary> Used to determine what parts of this RestraintSlot are applied. </summary>
    public RestraintFlags ApplyFlags { get; set; }

    /// <summary> a reference location to a restriction item (thanks c#) </summary>
    /// <remarks> This can be null if no item is set yet. </remarks>
    RestrictionItem Ref { get; set; }

    /// <summary> Custom stains that can be set on-top of the base restriction items stains without changing them. </summary>
    StainIds CustomStains { get; set; }

    public bool IsValid();
}


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
    public RestraintFlags ApplyFlags { get; set; } = RestraintFlags.IsOverlay | RestraintFlags.Glamour;
    public GlamourSlot Glamour { get; set; } = new();
    public EquipSlot EquipSlot => Glamour.Slot;
    public EquipItem EquipItem => Glamour.GameItem;
    public StainIds Stains => Glamour.GameStain;

    public RestraintSlotBasic() => Glamour = new GlamourSlot();

    public RestraintSlotBasic(EquipSlot slot) => Glamour = new GlamourSlot(slot, ItemService.NothingItem(slot));

    public RestraintSlotBasic(RestraintSlotBasic other)
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

public class RestraintSlotAdvanced : IRestraintSlot, IRestrictionRef
{
    public RestraintFlags ApplyFlags { get; set; } = RestraintFlags.Advanced;
    // Creates an item with an empty identifier, indicating it is not a part of anything and holds default values.
    public RestrictionItem Ref { get; set; } = new RestrictionItem() { Identifier = Guid.Empty };
    public StainIds CustomStains { get; set; } = StainIds.None;
    public EquipSlot EquipSlot => Ref.Glamour.Slot;
    public EquipItem EquipItem => Ref.Glamour.GameItem ;
    public StainIds Stains => CustomStains != StainIds.None ? CustomStains : Ref.Glamour.GameStain;
    public RestraintSlotAdvanced() { }
    public RestraintSlotAdvanced(RestraintSlotAdvanced other)
    {
        ApplyFlags = other.ApplyFlags;
        Ref = other.Ref;
        CustomStains = other.CustomStains;
    }

    public bool IsValid() => Ref.Identifier != Guid.Empty;
    public IRestraintSlot Clone() => new RestraintSlotAdvanced(this);
    public JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = RestraintSlotType.Advanced.ToString(),
            ["ApplyFlags"] = (int)ApplyFlags,
            ["RestrictionRef"] = Ref.Identifier.ToString(),
            ["CustomStains"] = CustomStains.ToString(),
        };
    }
}

// This will eventually be able to be a mod customization toggle as well for a layer.
public interface IRestraintLayer
{
    public Guid ID { get; }
    public Arousal Arousal { get; }
    public bool IsValid();
    public IRestraintLayer Clone();
    public JObject Serialize();
}

public class RestrictionLayer : IRestraintLayer, IRestrictionRef
{
    public Guid ID { get; internal set; } = Guid.NewGuid();
    public RestraintFlags ApplyFlags { get; set; } = RestraintFlags.Advanced;
    public RestrictionItem Ref { get; set; } = new RestrictionItem() { Identifier = Guid.Empty };
    public StainIds CustomStains { get; set; } = StainIds.None;
    public EquipSlot EquipSlot => Ref.Glamour.Slot;
    public EquipItem EquipItem => Ref.Glamour.GameItem;
    public StainIds Stains => CustomStains != StainIds.None ? CustomStains : Ref.Glamour.GameStain;
    public Arousal Arousal => Ref.Arousal;

    internal RestrictionLayer()
    { }

    internal RestrictionLayer(RestrictionLayer other)
    {
        ApplyFlags = other.ApplyFlags;
        Ref = other.Ref; // Point to the same reference
        CustomStains = other.CustomStains;
    }

    public bool IsValid() => Ref.Identifier != Guid.Empty;
    public IRestraintLayer Clone() => new RestrictionLayer(this);

    public JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = RestraintLayerType.Restriction.ToString(),
            ["ID"] = ID,
            ["ApplyFlags"] = (int)ApplyFlags,
            ["RestrictionRef"] = $"{Ref?.Identifier ?? Guid.Empty}",
            ["CustomStains"] = CustomStains.ToString(),
            ["Arousal"] = Arousal.ToString(),
        };
    }
}

public class ModPresetLayer : IRestraintLayer, IModPreset
{
    public Guid ID { get; internal set; } = Guid.NewGuid();
    public ModSettingsPreset Mod { get; set; } = new ModSettingsPreset(new ModPresetContainer());
    public Arousal Arousal { get; set; } = Arousal.None;

    internal ModPresetLayer() { }
    internal ModPresetLayer(ModPresetLayer other)
    {
        Mod = other.Mod;
    }

    public bool IsValid() => Mod.HasData;
    public IRestraintLayer Clone() => new ModPresetLayer(this);

    public JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = RestraintLayerType.ModPreset.ToString(),
            ["ID"] = ID,
            ["Arousal"] = Arousal.ToString(),
            ["Mod"] = Mod.SerializeReference()
        };
    }
}


/// <summary>
///     This class is the most complex out of everything in CK.
///     Any methods to get or fetch data from this should be in extension methods.
/// </summary>
/// <remarks></remarks>
public class RestraintSet : IEditableStorageItem<RestraintSet>, IAttributeItem
{
    public Guid Identifier { get; internal set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public bool DoRedraw { get; set; } = false;

    public Dictionary<EquipSlot, IRestraintSlot> RestraintSlots { get; set; } = EquipSlotExtensions.EqdpSlots.ToDictionary(slot => slot, slot => (IRestraintSlot)new RestraintSlotBasic(slot));
    public GlamourBonusSlot Glasses { get; set; } = new GlamourBonusSlot();
    public List<IRestraintLayer> Layers { get; set; } = new List<IRestraintLayer>();

    public MetaDataStruct RestraintMeta { get; set; } = MetaDataStruct.Empty;

    public List<ModSettingsPreset> RestraintMods { get; set; } = new();
    public HashSet<Moodle> RestraintMoodles { get; set; } = new();
    public Traits Traits { get; set; } = Traits.None;
    public Arousal Arousal { get; set; } = Arousal.None;

    public RestraintSet() { }
    public RestraintSet(RestraintSet other, bool keepIdentifier)
    {
        Identifier = keepIdentifier ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public RestraintSet Clone(bool keepId = false) => new RestraintSet(this, keepId);

    /// <summary> Updates all properties, without updating the object itself, to keep references intact. </summary>
    public void ApplyChanges(RestraintSet other)
    {
        // Apply changes from the other RestraintSet to this one
        Label = other.Label;
        Description = other.Description;
        ThumbnailPath = other.ThumbnailPath;
        DoRedraw = other.DoRedraw;

        RestraintSlots = other.RestraintSlots.ToDictionary(x => x.Key, x => x.Value.Clone());
        Glasses = new GlamourBonusSlot(other.Glasses);
        Layers = other.Layers.Select(layer => layer.Clone()).ToList();

        RestraintMeta = other.RestraintMeta;

        RestraintMods = other.RestraintMods.Select(mod => new ModSettingsPreset(mod)).ToList();
        RestraintMoodles = other.RestraintMoodles.Select(m => m is MoodlePreset p ? new MoodlePreset(p) : new Moodle(m)).ToHashSet();

        Traits = other.Traits;
        Arousal = other.Arousal;
    }

    /// <summary> Arranges the associated mods for this restraint set through a helper function. </summary>
    /// <remarks> Prioritizes the base slots, then the restraint layers, then additional mods. </remarks>
    public HashSet<ModSettingsPreset> GetMods()
    {
        // Maybe consider making this a hash-set to begin with and checking by directory path for replacement.
        var associationPresets = new SortedList<string, ModSettingsPreset>();

        // Append all base slot mod attachments.
        foreach (var slot in RestraintSlots.Values.OfType<RestraintSlotAdvanced>().Where(x => x.Ref is not null))
            if (slot.ApplyFlags.HasFlag(RestraintFlags.Mod) && slot.Ref!.Mod.HasData)
                associationPresets[slot.Ref!.Mod.Container.DirectoryPath] = slot.Ref!.Mod;

        // append the additional mods from the layers.
        foreach (var layer in Layers.Where(l => l.IsActive))
        {
            if (layer is RestrictionLayer bindLayer && bindLayer.Ref is not null)
            {
                if (bindLayer.ApplyFlags.HasFlag(RestraintFlags.Mod) && bindLayer.Ref.Mod.HasData)
                    associationPresets[bindLayer.Ref.Mod.Container.DirectoryPath] = bindLayer.Ref.Mod;
            }
            else if (layer is ModPresetLayer modLayer && modLayer.Mod.HasData)
            {
                associationPresets[modLayer.Mod.Container.DirectoryPath] = modLayer.Mod;
            }
        }

        // Convert the dictionary entries back to ModSettingsPreset
        return associationPresets.Values.ToHashSet();
    }

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

    public BlindfoldOverlay? GetBlindfoldOverlay()
    {
        // Try to get from RestraintSlots first
        foreach (var advSlot in RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
            if (advSlot.Ref is BlindfoldRestriction bfr)
                return bfr.Properties;

        // Then try the Layers.
        foreach (var layer in Layers.OfType<RestrictionLayer>())
            if (layer.Ref is BlindfoldRestriction bfr)
                return bfr.Properties;

        // If no blindfold overlay is found, return null.
        return null;
    }

    public HypnoticOverlay? GetHypnoOverlay()
    {
        // Try to get from RestraintSlots first
        foreach (var advSlot in RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
            if (advSlot.Ref is HypnoticRestriction hr)
                return hr.Properties;

        // Then try the Layers.
        foreach (var layer in Layers.OfType<RestrictionLayer>())
            if (layer.Ref is HypnoticRestriction hr)
                return hr.Properties;

        // If no Hypno Effect is found, return null.
        return null;
    }

    #endregion Cache Helpers

    public JObject Serialize()
    {
        return new JObject
        {
            ["Identifier"] = Identifier.ToString(),
            ["Label"] = Label,
            ["Description"] = Description,
            ["ThumbnailPath"] = ThumbnailPath,
            ["DoRedraw"] = DoRedraw,
            ["RestraintSlots"] = new JObject(RestraintSlots.Select(x => new JProperty(x.Key.ToString(), x.Value.Serialize()))),
            ["Glasses"] = Glasses.Serialize(),
            ["RestraintLayers"] = new JArray(Layers.Select(x => x.Serialize())),
            ["HeadgearState"] = HeadgearState.ToString(),
            ["VisorState"] = VisorState.ToString(),
            ["WeaponState"] = WeaponState.ToString(),
            ["RestraintMods"] = new JArray(RestraintMods.Select(x => x.SerializeReference())),
            ["RestraintMoodles"] = new JArray(RestraintMoodles.Select(x => x.Serialize())),
            ["Traits"] = Traits.ToString(),
            ["Stimulation"] = Arousal.ToString(),
        };
    }

    public LightRestraintSet ToLightRestraint()
    {
        var appliedSlots = new List<AppliedSlot>();
        foreach (var kv in RestraintSlots)
        {
            if (kv.Value.ApplyFlags.HasAny(RestraintFlags.Glamour) && kv.Value is RestraintSlotBasic basic)
                appliedSlots.Add(new AppliedSlot((byte)basic.EquipSlot, basic.Glamour.GameItem.Id.Id));
            else if (kv.Value is RestraintSlotAdvanced adv && adv.Ref != null)
                appliedSlots.Add(new AppliedSlot((byte)adv.EquipSlot, adv.EquipItem.ItemId.Id));
        }

        var attributes = new Attributes(RestraintFlags.Advanced, Traits, Arousal);
        return new LightRestraintSet(Identifier, Label, Description, appliedSlots, attributes);
    }
}




