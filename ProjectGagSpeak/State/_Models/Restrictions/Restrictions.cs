using CkCommons.Classes;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Struct;
using Penumbra.GameData.Enums;

namespace GagSpeak.State.Models;

/// <summary> Interface used by any item holding a modSettingPreset Reference </summary>
/// <remarks> Nessisary for some interactive draw functions that directly effect these values. </remarks>
public interface IModPreset
{
    /// <summary> Holds a reference to a mod setting preset. This object stores a ref to the mod container it resides in. </summary>
    ModSettingsPreset Mod { get; set; }
}

/// <summary> An Interface for Immersive Attributes that can be attached. </summary>
/// <remarks> Nessisary for some interactive draw functions that directly effect these values. </remarks>
public interface IAttributeItem
{
    Traits Traits { get; set; }
    Arousal Arousal { get; set; }
}

/// <summary> Basic Restriction Item Contract requirements. </summary>
/// <remarks> Also contains ModSettingsPreset, and Traits, and Arousal. </remarks>
public interface IRestriction : IModPreset
{
    /// <summary> Determines the Glamour applied from this restriction item. </summary>
    GlamourSlot Glamour { get; set; }

    /// <summary> Determines the Moodle applied from this restriction item. </summary>
    Moodle Moodle { get; set; }

    /// <summary> If a redraw should be performed after application or removal. </summary>
    bool DoRedraw { get; set; }

    /// <summary> Serializes the restriction item to a JObject. </summary>
    JObject Serialize();
}

/// <summary> Requirements for a restriction Item. </summary>
/// <remarks> Used to keep GagRestrictions and other restrictions with separate identifier sources but shared material. </remarks>
public interface IRestrictionItem : IRestriction, IAttributeItem
{
    Guid Identifier { get; }
    string Label { get; }
}

// Used for Gags. | Ensure C+ Allowance & Meta Allowance. Uses shared functionality but is independent.
public class GarblerRestriction : IEditableStorageItem<GarblerRestriction>, IRestriction, IAttributeItem
{
    public GagType GagType { get; init; }
    public bool IsEnabled { get; set; } = false;
    public GlamourSlot Glamour { get; set; } = new GlamourSlot();
    public ModSettingsPreset Mod { get; set; } = new ModSettingsPreset(new ModPresetContainer());
    public Moodle Moodle { get; set; } = new Moodle();
    public Traits Traits { get; set; } = Traits.None;
    public Arousal Arousal { get; set; } = Arousal.None;
    public TriStateBool HeadgearState { get; set; } = TriStateBool.Null;
    public TriStateBool VisorState { get; set; } = TriStateBool.Null;
    public CustomizeProfile CPlusProfile { get; set; } = CustomizeProfile.Empty;
    public bool DoRedraw { get; set; } = false;

    internal GarblerRestriction(GagType gagType) => GagType = gagType;
    public GarblerRestriction(GarblerRestriction other)
    {
        GagType = other.GagType;
        ApplyChanges(other);
    }

    public GarblerRestriction Clone(bool _ = true) => new GarblerRestriction(this);

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(GarblerRestriction other)
    {
        IsEnabled = other.IsEnabled;
        Glamour = other.Glamour;
        Mod = other.Mod;
        Moodle = other.Moodle;
        Traits = other.Traits;
        Arousal = other.Arousal;
        HeadgearState = other.HeadgearState;
        VisorState = other.VisorState;
        CPlusProfile = other.CPlusProfile;
        DoRedraw = other.DoRedraw;
    }

    public JObject Serialize()
        => new JObject
        {
            ["IsEnabled"] = IsEnabled,
            ["Glamour"] = Glamour.Serialize(),
            ["Mod"] = Mod.SerializeReference(),
            ["Moodle"] = Moodle.Serialize(),
            ["Traits"] = Traits.ToString(),
            ["Arousal"] = Arousal.ToString(),
            ["HeadgearState"] = HeadgearState.ToString(),
            ["VisorState"] = VisorState.ToString(),
            ["ProfileGuid"] = CPlusProfile.ProfileGuid.ToString(),
            ["ProfilePriority"] = CPlusProfile.Priority,
            ["DoRedraw"] = DoRedraw,
        };

    public LightGag ToLightItem()
    {
        var lightProps = new LightItem()
        {
            Slot = Glamour.ToLightSlot(),
            ModName = Mod.ToString(),
            Moodle = new LightMoodle(Moodle.Type, Moodle.Id),
            Traits = Traits,
            Arousal = Arousal
        };
        return new LightGag(GagType, IsEnabled, lightProps, CPlusProfile.ProfileName, DoRedraw);
    }

    public static GarblerRestriction FromToken(JToken? token, GagType gagType, ModSettingPresetManager mp)
    {
        if (token is not JObject json)
            throw new ArgumentException("Invalid JObjectToken!");

        var modAttachment = ModSettingsPreset.FromRefToken(json["Mod"], mp);
        var moodles = GsExtensions.LoadMoodle(json["Moodle"]);
        var profileId = json["ProfileGuid"]?.ToObject<Guid>() ?? throw new ArgumentNullException("ProfileGuid");
        var profilePrio = json["ProfilePriority"]?.ToObject<int>() ?? throw new ArgumentNullException("ProfilePriority");
        return new GarblerRestriction(gagType)
        {
            IsEnabled = json["IsEnabled"]?.ToObject<bool>() ?? false,
            Glamour = ItemSvc.ParseGlamourSlot(json["Glamour"]),
            Mod = modAttachment,
            Moodle = moodles,
            Traits = Enum.TryParse<Traits>(json["Traits"]?.ToObject<string>(), out var traits) ? traits : Traits.None,
            Arousal = Enum.TryParse<Arousal>(json["Arousal"]?.ToObject<string>(), out var stim) ? stim : Arousal.None,
            HeadgearState = TriStateBool.FromJObject(json["HeadgearState"]),
            VisorState = TriStateBool.FromJObject(json["VisorState"]),
            CPlusProfile = new CustomizeProfile(profileId, profilePrio),
            DoRedraw = json["DoRedraw"]?.ToObject<bool>() ?? false,
        };
    }
}

public class RestrictionItem : IEditableStorageItem<RestrictionItem>, IRestrictionItem
{
    public virtual RestrictionType Type { get; } = RestrictionType.Normal;
    public Guid Identifier { get; internal set; } = Guid.NewGuid();
    public bool IsEnabled { get; set; } = true;
    public string Label { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public GlamourSlot Glamour { get; set; } = new GlamourSlot(EquipSlot.Head, ItemSvc.NothingItem(EquipSlot.Head));
    public ModSettingsPreset Mod { get; set; } = new ModSettingsPreset(new ModPresetContainer());
    public TriStateBool HeadgearState { get; set; } = TriStateBool.Null;
    public TriStateBool VisorState { get; set; } = TriStateBool.Null;
    public Moodle Moodle { get; set; } = new Moodle();
    public Traits Traits { get; set; } = Traits.None;
    public Arousal Arousal { get; set; } = Arousal.None;
    public bool DoRedraw { get; set; } = false;
 
    public RestrictionItem()
    { }

    public RestrictionItem(RestrictionItem other, bool keepIdentifier)
    {
        Identifier = keepIdentifier ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public virtual RestrictionItem Clone(bool keepId = false) => new RestrictionItem(this, keepId);

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(RestrictionItem other)
    {
        Label = other.Label;
        ThumbnailPath = other.ThumbnailPath;
        Glamour = other.Glamour;
        Mod = other.Mod;
        Moodle = other.Moodle;
        HeadgearState = other.HeadgearState;
        VisorState = other.VisorState;
        Traits = other.Traits;
        Arousal = other.Arousal;
        DoRedraw = other.DoRedraw;
    }

    public virtual JObject Serialize()
        => new JObject
        {
            ["Type"] = Type.ToString(),
            ["Identifier"] = Identifier.ToString(),
            ["IsEnabled"] = IsEnabled,
            ["Label"] = Label,
            ["ThumbnailPath"] = ThumbnailPath,
            ["Glamour"] = Glamour.Serialize(),
            ["Mod"] = Mod.SerializeReference(),
            ["Moodle"] = Moodle.Serialize(),
            ["HeadgearState"] = HeadgearState.ToString(),
            ["VisorState"] = VisorState.ToString(),
            ["Traits"] = Traits.ToString(),
            ["Arousal"] = Arousal.ToString(),
            ["Redraw"] = DoRedraw,
        };

    public LightRestriction ToLightItem()
    {
        var lightProps = new LightItem()
        {
            Slot = Glamour.ToLightSlot(),
            ModName = Mod.ToString(),
            Moodle = new LightMoodle(Moodle is MoodlePreset ? MoodleType.Preset : MoodleType.Status, Moodle.Id),
            Traits = Traits,
            Arousal = Arousal
        };
        return new LightRestriction(Identifier, IsEnabled, Type, Label, lightProps);
    }

    /// <summary> Constructs the RestrictionItem from a JToken. </summary>
    /// <remarks> This method can throw an exception if tokens are not valid. </remarks>
    public static RestrictionItem FromToken(JToken? token, ModSettingPresetManager mp)
    {
        if (token is not JObject json || json["Moodle"] is not JObject jsonMoodle)
            throw new ArgumentException("Invalid JObjectToken!");

        var id = jsonMoodle["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
        // If the "StatusIds" property exists, treat this as a MoodlePreset
        var moodle = jsonMoodle.TryGetValue("StatusIds", out var statusToken) && statusToken is JArray
            ? new MoodlePreset(id, statusToken.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>()) : new Moodle(id);

        // Construct the item to return.
        return new RestrictionItem()
        {
            Identifier = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            Label = json["Label"]?.ToObject<string>() ?? string.Empty,
            IsEnabled = json["IsEnabled"]?.ToObject<bool>() ?? true,
            ThumbnailPath = json["ThumbnailPath"]?.ToObject<string>() ?? string.Empty,
            Glamour = ItemSvc.ParseGlamourSlot(json["Glamour"]),
            Mod = ModSettingsPreset.FromRefToken(json["Mod"], mp),
            Moodle = moodle,
            HeadgearState = TriStateBool.FromJObject(json["HeadgearState"]),
            VisorState = TriStateBool.FromJObject(json["VisorState"]),
            Traits = Enum.TryParse<Traits>(json["Traits"]?.ToObject<string>(), out var traits) ? traits : Traits.None,
            Arousal = Enum.TryParse<Arousal>(json["Arousal"]?.ToObject<string>(), out var stim) ? stim : Arousal.None,
            DoRedraw = json["DoRedraw"]?.ToObject<bool>() ?? false,
        };
    }
}

public class HypnoticRestriction : RestrictionItem
{
    public override RestrictionType Type { get; } = RestrictionType.Hypnotic;
    public HypnoticOverlay Properties { get; set; } = new();

    public HypnoticRestriction() 
    { }

    public HypnoticRestriction(HypnoticRestriction other, bool keepIdentifier)
        : base(other, keepIdentifier)
    {
        ApplyChanges(other);
    }

    public bool HasValidPath() => !string.IsNullOrEmpty(Properties.OverlayPath)
        && File.Exists(Path.Combine(ConfigFileProvider.ThumbnailDirectory, ImageDataType.Hypnosis.ToString(), Properties.OverlayPath));

    public override HypnoticRestriction Clone(bool keepId = false) 
        => new HypnoticRestriction(this, keepId);

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(HypnoticRestriction other)
    {
        base.ApplyChanges(other);
        Properties = new(other.Properties);
    }

    public override JObject Serialize()
    {
        // serialize the base, and add to it the additional.
        var json = base.Serialize();
        json["Properties"] = JObject.FromObject(Properties);
        return json;
    }

    /// <summary> Constructs the HypnoticItem from a JToken. </summary>
    /// <remarks> This method can throw an exception if tokens are not valid. </remarks>
    public new static HypnoticRestriction FromToken(JToken? token, ModSettingPresetManager mp)
    {
        if (token is not JObject json || json["Moodle"] is not JObject jsonMoodle)
            throw new ArgumentException("Invalid JObjectToken!");

        var id = jsonMoodle["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
        // If the "StatusIds" property exists, treat this as a MoodlePreset
        var moodle = jsonMoodle.TryGetValue("StatusIds", out var statusToken) && statusToken is JArray
            ? new MoodlePreset(id, statusToken.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>()) : new Moodle(id);

        // Construct the item to return.
        return new HypnoticRestriction()
        {
            Identifier = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            Label = json["Label"]?.ToObject<string>() ?? string.Empty,
            IsEnabled = json["IsEnabled"]?.ToObject<bool>() ?? true,
            ThumbnailPath = json["ThumbnailPath"]?.ToObject<string>() ?? string.Empty,
            Glamour = ItemSvc.ParseGlamourSlot(json["Glamour"]),
            Mod = ModSettingsPreset.FromRefToken(json["Mod"], mp),
            Moodle = moodle,
            Traits = Enum.TryParse<Traits>(json["Traits"]?.ToObject<string>(), out var traits) ? traits : Traits.None,
            Arousal = Enum.TryParse<Arousal>(json["Arousal"]?.ToObject<string>(), out var stim) ? stim : Arousal.None,
            DoRedraw = json["DoRedraw"]?.ToObject<bool>() ?? false,
            HeadgearState = TriStateBool.FromJObject(json["HeadgearState"]),
            VisorState = TriStateBool.FromJObject(json["VisorState"]),
            Properties = json["Properties"]?.ToObject<HypnoticOverlay>() ?? new HypnoticOverlay(),
        };
    }
}


public class BlindfoldRestriction : RestrictionItem
{
    public override RestrictionType Type { get; } = RestrictionType.Blindfold;
    public BlindfoldOverlay Properties { get; set; } = new("Blindfold_Light.png");

    public BlindfoldRestriction()
    { }

    public BlindfoldRestriction(BlindfoldRestriction other, bool keepIdentifier)
        : base(other, keepIdentifier)
    {
        Properties = other.Properties;
    }

    public bool HasValidPath() => !string.IsNullOrEmpty(Properties.OverlayPath) 
        && File.Exists(Path.Combine(ConfigFileProvider.ThumbnailDirectory, ImageDataType.Blindfolds.ToString(), Properties.OverlayPath));

    public override BlindfoldRestriction Clone(bool keepId = false)
        => new BlindfoldRestriction(this, keepId);

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(BlindfoldRestriction other)
    {
        base.ApplyChanges(other);
        Properties = other.Properties;
    }


    public override JObject Serialize()
    {
        // serialize the base, and add to it the additional.
        var json = base.Serialize();
        json["Properties"] = JObject.FromObject(Properties);
        return json;
    }

    /// <summary> Constructs the BlindfoldItem from a JToken. </summary>
    /// <remarks> This method can throw an exception if tokens are not valid. </remarks>
    public new static BlindfoldRestriction FromToken(JToken? token, ModSettingPresetManager mp)
    {
        if (token is not JObject json || json["Moodle"] is not JObject jsonMoodle)
            throw new ArgumentException("Invalid JObjectToken!");

        var id = jsonMoodle["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
        // If the "StatusIds" property exists, treat this as a MoodlePreset
        var moodle = jsonMoodle.TryGetValue("StatusIds", out var statusToken) && statusToken is JArray
            ? new MoodlePreset(id, statusToken.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>()) : new Moodle(id);
        
        // Construct the item to return.
        return new BlindfoldRestriction()
        {
            Identifier = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            Label = json["Label"]?.ToObject<string>() ?? string.Empty,
            IsEnabled = json["IsEnabled"]?.ToObject<bool>() ?? true,
            ThumbnailPath = json["ThumbnailPath"]?.ToObject<string>() ?? string.Empty,
            Glamour = ItemSvc.ParseGlamourSlot(json["Glamour"]),
            Mod = ModSettingsPreset.FromRefToken(json["Mod"], mp),
            Moodle = moodle,
            Traits = Enum.TryParse<Traits>(json["Traits"]?.ToObject<string>(), out var traits) ? traits : Traits.None,
            Arousal = Enum.TryParse<Arousal>(json["Arousal"]?.ToObject<string>(), out var stim) ? stim : Arousal.None,
            DoRedraw = json["DoRedraw"]?.ToObject<bool>() ?? false,
            HeadgearState = TriStateBool.FromJObject(json["HeadgearState"]),
            VisorState = TriStateBool.FromJObject(json["VisorState"]),
            Properties = json["Properties"]?.ToObject<BlindfoldOverlay>() ?? new BlindfoldOverlay(),
        };
    }
}


public class CollarRestriction : IEditableStorageItem<CollarRestriction>, IRestriction
{
    public Guid Identifier { get; internal set; } = Guid.NewGuid();
    public string OwnerUID { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string CollarWriting { get; set; } = string.Empty;
    public GlamourSlot Glamour { get; set; } = new GlamourSlot();
    public ModSettingsPreset Mod { get; set; } = new ModSettingsPreset(new ModPresetContainer());
    public Moodle Moodle { get; set; } = new Moodle();
    public bool DoRedraw { get; set; } = false;
    
    public CollarRestriction()
    { }

    public CollarRestriction(CollarRestriction other, bool keepId = false)
    {
        Identifier = keepId ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public CollarRestriction Clone(bool keepId = true) => new CollarRestriction(this, keepId);

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(CollarRestriction other)
    {
        OwnerUID = other.OwnerUID;
        ThumbnailPath = other.ThumbnailPath;
        CollarWriting = other.CollarWriting;
        Glamour = other.Glamour;
        Mod = other.Mod;
        Moodle = other.Moodle;
        DoRedraw = other.DoRedraw;
    }

    public JObject Serialize()
    {
        return new JObject
        {
            ["Identifier"] = Identifier.ToString(),
            ["OwnerUID"] = OwnerUID,
            ["ThumbnailPath"] = ThumbnailPath,
            ["CollarWriting"] = CollarWriting,
            ["Glamour"] = Glamour.Serialize(),
            ["Mod"] = Mod.SerializeReference(),
            ["Moodle"] = Moodle.Serialize(),
            ["DoRedraw"] = DoRedraw,
        };
    }

    /// <summary> Constructs the CollarItem from a JToken. </summary>
    /// <remarks> This method can throw an exception if tokens are not valid. </remarks>
    public static CollarRestriction FromToken(JToken? token, ModSettingPresetManager mp)
    {
        if (token is not JObject json || json["Moodle"] is not JObject jsonMoodle)
            throw new ArgumentException("Invalid JObjectToken!");

        var id = jsonMoodle["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
        // If the "StatusIds" property exists, treat this as a MoodlePreset
        var moodle = jsonMoodle.TryGetValue("StatusIds", out var statusToken) && statusToken is JArray
            ? new MoodlePreset(id, statusToken.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>()) : new Moodle(id);

        var profileId = json["ProfileGuid"]?.ToObject<Guid>() ?? throw new ArgumentNullException("ProfileGuid");
        var profilePrio = json["ProfilePriority"]?.ToObject<int>() ?? throw new ArgumentNullException("ProfilePriority");
        var profileName = json["ProfileName"]?.ToObject<string>() ?? string.Empty;

        // Construct the item to return.
        return new CollarRestriction()
        {
            Identifier = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            OwnerUID = json["OwnerUID"]?.ToObject<string>() ?? string.Empty,
            ThumbnailPath = json["ThumbnailPath"]?.ToObject<string>() ?? string.Empty,
            CollarWriting = json["CollarWriting"]?.ToObject<string>() ?? string.Empty,
            Glamour = ItemSvc.ParseGlamourSlot(json["Glamour"]),
            Mod = ModSettingsPreset.FromRefToken(json["Mod"], mp),
            Moodle = moodle,
            DoRedraw = json["DoRedraw"]?.ToObject<bool>() ?? false,
        };
    }
}
