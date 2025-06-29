using CkCommons;
using CkCommons.Classes;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.State.Managers;
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

    /// <summary> Attempts to load a BasicSlot from the restraint set. </summary>
    /// <param name="token"> The JSON Token for the Slot. </param>
    /// <returns> The loaded BasicSlot. </returns>
    /// <exception cref="Exception"> If the JToken is either not valid or the GlamourSlot fails to parse. </exception>
    /// <remarks> Throws if the JToken is either not valid or the GlamourSlot fails to parse.</remarks>
    public static RestraintSlotBasic FromToken(JToken token, ItemService items)
    {
        if (token is not JObject slotJson)
            throw new Exception("Invalid JSON Token for Slot.");

        return new RestraintSlotBasic()
        {
            ApplyFlags = slotJson["ApplyFlags"]?.ToObject<int>() is int v ? (RestraintFlags)v : RestraintFlags.IsOverlay | RestraintFlags.Glamour,
            Glamour = items.ParseGlamourSlot(slotJson["Glamour"])
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

    /// <summary> Attempts to load a Advanced from the restraint set. </summary>
    /// <param name="slotToken"> The JSON Token for the Slot. </param>
    /// <returns> The loaded Advanced. </returns>
    /// <exception cref="Exception"></exception>
    /// <remarks> If advanced slot fails to load, a default, invalid restriction item will be put in place. </remarks>
    public static RestraintSlotAdvanced FromToken(JToken? token, RestrictionManager restrictions)
    {
        if (token is not JObject slotJson) throw new Exception("Invalid JSON Token for Slot.");

        var applyFlags = slotJson["ApplyFlags"]?.ToObject<int>() is int v ? (RestraintFlags)v : RestraintFlags.Advanced;
        var refId = slotJson["RestrictionRef"]?.ToObject<Guid>() ?? Guid.Empty;
        var stains = GsExtensions.ParseCompactStainIds(slotJson["CustomStains"]);
        // Handle invalid advanced slots.
        if (restrictions.Storage.TryGetRestriction(refId, out var restriction))
            return new RestraintSlotAdvanced() { ApplyFlags = applyFlags, Ref = restriction, CustomStains = stains }; 
        else
        {
            Svc.Logger.Warning("ID Was empty for advanced restriction or restriction was not found, resetting to empty item.");
            return new RestraintSlotAdvanced()
            {
                ApplyFlags = applyFlags,
                Ref = new RestrictionItem() { Identifier = Guid.Empty },
                CustomStains = stains,
            };
            // try and help create a graceful RestraintBasic return maybe?
        }
    }
}

// This will eventually be able to be a mod customization toggle as well for a layer.
public interface IRestraintLayer
{
    public Guid ID { get; }
    public string Label { get; }
    public Arousal Arousal { get; }
    public bool IsValid();
    public IRestraintLayer Clone();
    public JObject Serialize();
}

public class RestrictionLayer : IRestraintLayer, IRestrictionRef
{
    public Guid ID { get; internal set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public RestraintFlags ApplyFlags { get; set; } = RestraintFlags.Advanced;
    public RestrictionItem Ref { get; set; } = new RestrictionItem() { Identifier = Guid.Empty };
    public StainIds CustomStains { get; set; } = StainIds.None;
    public EquipSlot EquipSlot => Ref.Glamour.Slot;
    public EquipItem EquipItem => Ref.Glamour.GameItem;
    public StainIds Stains => CustomStains != StainIds.None ? CustomStains : Ref.Glamour.GameStain;
    public Arousal Arousal => ApplyFlags.HasAny(RestraintFlags.Arousal) && IsValid() ? Ref.Arousal : Arousal.None;

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
            ["Label"] = Label,
            ["ApplyFlags"] = (int)ApplyFlags,
            ["CustomStains"] = CustomStains.ToString(),
            ["RestrictionRef"] = $"{Ref?.Identifier ?? Guid.Empty}",
        };
    }

    public static RestrictionLayer FromToken(JToken? token, RestrictionManager restrictions)
    {
        if (token is not JObject bLayerJson)
            throw new Exception("Invalid JSON Token for Layer.");

        var id = Guid.TryParse(bLayerJson["ID"]?.Value<string>(), out var guid) ? guid : throw new Exception("InvalidGUID");
        var label = bLayerJson["Label"]?.Value<string>() ?? string.Empty;
        var flags = bLayerJson["ApplyFlags"]?.ToObject<int>() is int v ? (RestraintFlags)v : RestraintFlags.Advanced;
        var customStains = GsExtensions.ParseCompactStainIds(bLayerJson["CustomStains"]);

        var refId = Guid.TryParse(bLayerJson["RestrictionRef"]?.Value<string>(), out var rId) ? rId : throw new Exception("Bad Ref GUID");
        // Attempt firstly to get it from the storage.
        if (restrictions.Storage.TryGetRestriction(refId, out var restriction))
        {
            return new RestrictionLayer()
            {
                ID = id,
                Label = label,
                ApplyFlags = flags,
                Ref = restriction,
                CustomStains = customStains,
            };
        }
        // If the id was just empty, it was a blank layer, so we should make that.
        else
        {
            Svc.Logger.Warning("ID Was empty for advanced restriction or restriction was not found, resetting to empty item.");
            return new RestrictionLayer()
            {
                ID = id,
                Label = label,
                ApplyFlags = flags,
                CustomStains = customStains,
            };
        }
    }
}

public class ModPresetLayer : IRestraintLayer, IModPreset
{
    public Guid ID { get; internal set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
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
            ["Label"] = Label,
            ["Arousal"] = Arousal.ToString(),
            ["Mod"] = Mod.SerializeReference()
        };
    }

    public static ModPresetLayer FromToken(JToken? token, ModSettingPresetManager mods)
    {
        if (token is not JObject mLayerJson) throw new Exception("Invalid JSON Token for Layer.");
        
        // get the values from this token.
        var id = Guid.TryParse(mLayerJson["ID"]?.Value<string>(), out var guid) ? guid : throw new Exception("InvalidGUID");
        var label = mLayerJson["Label"]?.Value<string>() ?? string.Empty;
        var arousal = mLayerJson["Arousal"]?.Value<string>() is string aStr ? Enum.Parse<Arousal>(aStr) : Arousal.None;
        // Attempt to load in the mod ref using the presetManager.
        var modItem = ModSettingsPreset.FromRefToken(mLayerJson["Mod"], mods);
        return new ModPresetLayer()
        {
            ID = id,
            Label = label,
            Mod = modItem,
            Arousal = arousal,
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

    public MetaDataStruct MetaStates { get; set; } = MetaDataStruct.Empty;

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

        MetaStates = other.MetaStates;

        RestraintMods = other.RestraintMods.Select(mod => new ModSettingsPreset(mod)).ToList();
        RestraintMoodles = other.RestraintMoodles.Select(m => m is MoodlePreset p ? new MoodlePreset(p) : new Moodle(m)).ToHashSet();

        Traits = other.Traits;
        Arousal = other.Arousal;
    }

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
            ["MetaStates"] = MetaStates.ToJObject(),
            ["BaseMods"] = new JArray(RestraintMods.Select(x => x.SerializeReference())),
            ["BaseMoodles"] = new JArray(RestraintMoodles.Select(x => x.Serialize())),
            ["BaseTraits"] = Traits.ToString(),
            ["BaseArousal"] = Arousal.ToString(),
        };
    }

    public static RestraintSet FromToken(JToken token, ItemService items, ModSettingPresetManager mods, RestrictionManager restrictions)
    {
        // if not a valid token, throw an exception
        if (token is not JObject setJObj || setJObj["RestraintSlots"] is not JObject slotsJObj)
            throw new InvalidDataException("Invalid RestraintSet JObject object.");

        // Attempts to load a restraint set slot item. This can be Basic or Advanced.
        var slotDict = new Dictionary<EquipSlot, IRestraintSlot>();
        foreach (var restraintSlotToken in slotsJObj)
        {
            var equipSlot = (EquipSlot)Enum.Parse(typeof(EquipSlot), restraintSlotToken.Key);
            // Attempt to process the Restraint Slot.
            if (restraintSlotToken.Value is not JObject slotInnerToken)
                throw new Exception("Invalid JSON Token for Slot.");
            // Get the type identifier.
            var typeStr = slotInnerToken["Type"]?.Value<string>() ?? throw new InvalidOperationException("Missing Type information in JSON.");
            if (!Enum.TryParse(typeStr, out RestraintSlotType type))
                throw new InvalidOperationException($"Unknown RestraintSlotType: {typeStr}");
            // Attempt to add the type.
            slotDict.TryAdd(equipSlot, type switch
            {
                RestraintSlotType.Basic => RestraintSlotBasic.FromToken(slotInnerToken, items),
                RestraintSlotType.Advanced => RestraintSlotAdvanced.FromToken(slotInnerToken, restrictions),
                _ => throw new InvalidOperationException($"Unsupported RestraintSlotType: {typeStr}")
            });
        }

        // Attempts to load the Restraint Layers.
        var layers = new List<IRestraintLayer>();
        if (setJObj["RestraintLayers"] is JArray layerArray)
        {
            foreach (var layerToken in layerArray)
            {
                if (layerToken is not JObject json)
                    throw new Exception("Invalid JSON Token for Layer.");

                var typeStr = json["Type"]?.Value<string>() ?? throw new InvalidOperationException("Missing Type information in JSON.");
                if (!Enum.TryParse(typeStr, out RestraintLayerType type))
                    throw new InvalidOperationException($"Unknown RestraintLayerType: {typeStr}");

                layers.Add(type switch
                {
                    RestraintLayerType.Restriction => RestrictionLayer.FromToken(json, restrictions),
                    RestraintLayerType.ModPreset => ModPresetLayer.FromToken(json, mods),
                    _ => throw new InvalidOperationException($"Unknown RestraintLayerType: {type}"),
                });
            }
        }

        // Handle the mod additions for the basic mods.
        var baseMods = new List<ModSettingsPreset>();
        if (setJObj["RestraintMods"] is JArray modArray)
            foreach (var modToken in modArray)
                Generic.Safe(() => baseMods.Add(ModSettingsPreset.FromRefToken(modToken, mods)));

        // Handle the base Moodles.
        var baseMoodles = new HashSet<Moodle>();
        if (setJObj["RestraintMoodles"] is JArray moodleArray)
            foreach (var moodleToken in moodleArray)
                Generic.Safe(() => baseMoodles.Add(GsExtensions.LoadMoodle(moodleToken)));

        // If you made it all the way here without the world absolutely imploding on itself
        // and setting your pc on fire congrats we can now load the restraint set.
        return new RestraintSet
        {
            Identifier = Guid.TryParse(setJObj["Identifier"]?.Value<string>(), out var id) ? id : Guid.NewGuid(),
            Label = setJObj["Label"]?.Value<string>() ?? string.Empty,
            Description = setJObj["Description"]?.Value<string>() ?? string.Empty,
            ThumbnailPath = setJObj["ThumbnailPath"]?.Value<string>() ?? string.Empty,
            DoRedraw = setJObj["DoRedraw"]?.Value<bool>() ?? false,
            RestraintSlots = slotDict,
            Glasses = items.ParseBonusSlot(setJObj["Glasses"]),
            Layers = layers,
            MetaStates = MetaDataStruct.FromJObject(setJObj["MetaStates"]),
            RestraintMods = baseMods,
            RestraintMoodles = baseMoodles,
            Traits = Enum.TryParse<Traits>(setJObj["Traits"]?.ToObject<string>(), out var traits) ? traits : Traits.None,
            Arousal = Enum.TryParse<Arousal>(setJObj["Arousal"]?.ToObject<string>(), out var stim) ? stim : Arousal.None,
        };
    }

    // Need to serialously overhaul a FromJObect method here.
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
