using GagSpeak.CkCommons.Newtonsoft;
using GagSpeak.PlayerData.Storage;
using GagspeakAPI.Extensions;
using OtterGui.Classes;

namespace GagSpeak.PlayerState.Models;

/// <summary> Interface used by any item holding a modSettingPreset Reference </summary>
public interface IModPreset
{
    /// <summary> Holds a reference to a mod setting preset. This object stores a ref to the mod container it resides in. </summary>
    ModSettingsPreset Mod { get; set; }
}

/// <summary> An Interface Indicating Hardcore Traits can be attached. </summary>
public interface ITraitHolder
{
    Traits Traits { get; set; }
    Stimulation Stimulation { get; set; }
}

/// <summary> An Interface contract for Customize+ Integration. </summary>
public interface ICustomizePlus
{
    /// <summary> Identifies which Customize+ profile should be used. </summary>
    Guid ProfileGuid { get; set; }

    /// <summary> Determines the priority that the application of this profile will have. </summary>
    uint ProfilePriority { get; set; }
}

/// <summary> Basic Restriction Item Contract requirements. </summary>
public interface IRestriction : IModPreset
{
    /// <summary> Determines the Glamour applied from this restriction item. </summary>
    GlamourSlot Glamour { get; set; }

    /// <summary> Determines the Moodle applied from this restriction item. </summary>
    /// <remarks> Can be either singular status or preset. </remarks>
    Moodle Moodle { get; set; }

    /// <summary> Various Hardcore Traits for a restriction item. </summary>
    Traits Traits { get; set; }

    /// <summary> If a redraw should be performed after application or removal. </summary>
    bool DoRedraw { get; set; }

    /// <summary> Serializes the restriction item to a JObject. </summary>
    JObject Serialize();
}

/// <summary> Requirements for a restriction Item. </summary>
/// <remarks> Used to keep GagRestrictions and other restrictions with seperate identifier sources but shared material. </remarks>
public interface IRestrictionItem : IRestriction
{
    Guid Identifier { get; }
    string Label { get; }
}

// Used for Gags. | Ensure C+ Allowance & Meta Allowance. Uses shared functionality but is independent.
public class GarblerRestriction : IRestriction, ICustomizePlus, ITraitHolder, IComparable
{
    public GagType GagType { get; init; }
    public bool IsEnabled { get; set; } = false;
    public GlamourSlot Glamour { get; set; } = new GlamourSlot();
    public ModSettingsPreset Mod { get; set; } = new ModSettingsPreset(new ModPresetContainer());
    public Moodle Moodle { get; set; } = new Moodle();
    public Traits Traits { get; set; } = Traits.None;
    public Stimulation Stimulation { get; set; } = Stimulation.None;
    public OptionalBool HeadgearState { get; set; } = OptionalBool.Null;
    public OptionalBool VisorState { get; set; } = OptionalBool.Null;
    public Guid ProfileGuid { get; set; } = Guid.Empty;
    public uint ProfilePriority { get; set; } = 0;
    public bool DoRedraw { get; set; } = false;

    internal GarblerRestriction(GagType gagType) => GagType = gagType;
    public GarblerRestriction(GarblerRestriction other)
    {
        GagType = other.GagType;
        ApplyChanges(other);
    }

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(GarblerRestriction other)
    {
        IsEnabled = other.IsEnabled;
        Glamour = other.Glamour;
        Mod = other.Mod;
        Moodle = other.Moodle;
        Traits = other.Traits;
        Stimulation = other.Stimulation;
        HeadgearState = other.HeadgearState;
        VisorState = other.VisorState;
        ProfileGuid = other.ProfileGuid;
        ProfilePriority = other.ProfilePriority;
        DoRedraw = other.DoRedraw;
    }

    public int CompareTo(object? obj) // Useful for sorted set stuff.
    {
        if (obj is GarblerRestriction other)
            return string.Compare(GagType.GagName(), other.GagType.GagName());
        return -1;
    }

    public JObject Serialize()
        => new JObject
        {
            ["IsEnabled"] = IsEnabled,
            ["Glamour"] = Glamour.Serialize(),
            ["Mod"] = Mod.SerializeReference(),
            ["Moodle"] = Moodle.Serialize(),
            ["Traits"] = Traits.ToString(),
            ["Stimulation"] = Stimulation.ToString(),
            ["HeadgearState"] = HeadgearState.ToString(),
            ["VisorState"] = VisorState.ToString(),
            ["ProfileGuid"] = ProfileGuid.ToString(),
            ["ProfilePriority"] = ProfilePriority,
            ["DoRedraw"] = DoRedraw,
        };
}

public class RestrictionItem : IRestrictionItem, ITraitHolder
{
    public virtual RestrictionType Type { get; } = RestrictionType.Normal;
    public Guid Identifier { get; internal set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public GlamourSlot Glamour { get; set; } = new GlamourSlot();
    public ModSettingsPreset Mod { get; set; } = new ModSettingsPreset(new ModPresetContainer());
    public Moodle Moodle { get; set; } = new Moodle();
    public Traits Traits { get; set; } = Traits.None;
    public Stimulation Stimulation { get; set; } = Stimulation.None;
    public bool DoRedraw { get; set; } = false;
 
    public RestrictionItem() { }
    public RestrictionItem(RestrictionItem other, bool keepIdentifier)
    {
        Identifier = keepIdentifier ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(RestrictionItem other)
    {
        Label = other.Label;
        ThumbnailPath = other.ThumbnailPath;
        Glamour = other.Glamour;
        Mod = other.Mod;
        Moodle = other.Moodle;
        Traits = other.Traits;
        Stimulation = other.Stimulation;
        DoRedraw = other.DoRedraw;
    }

    public virtual JObject Serialize()
        => new JObject
        {
            ["Type"] = Type.ToString(),
            ["Identifier"] = Identifier.ToString(),
            ["Label"] = Label,
            ["ThumbnailPath"] = ThumbnailPath,
            ["Glamour"] = Glamour.Serialize(),
            ["Mod"] = Mod.SerializeReference(),
            ["Moodle"] = Moodle.Serialize(),
            ["Traits"] = Traits.ToString(),
            ["Stimulation"] = Stimulation.ToString(),
            ["Redraw"] = DoRedraw,
        };
}

public class BlindfoldRestriction : RestrictionItem
{
    public override RestrictionType Type { get; } = RestrictionType.Blindfold;
    public OptionalBool HeadgearState { get; set; } = OptionalBool.Null;
    public OptionalBool VisorState { get; set; } = OptionalBool.Null;
    public BlindfoldType Kind { get; set; } = BlindfoldType.Light;
    public bool ForceFirstPerson { get; set; } = false;
    public string CustomPath { get; set; } = string.Empty;

    public BlindfoldRestriction() { }
    public BlindfoldRestriction(BlindfoldRestriction other, bool keepIdentifier)
        : base(other, keepIdentifier)
    {
        HeadgearState = other.HeadgearState;
        VisorState = other.VisorState;
        CustomPath = other.CustomPath;
    }

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(BlindfoldRestriction other)
    {
        base.ApplyChanges(other);
        HeadgearState = other.HeadgearState;
        VisorState = other.VisorState;
        CustomPath = other.CustomPath;
    }


    public override JObject Serialize()
    {
        // serialize the base, and add to it the additional.
        var json = base.Serialize();
        json["HeadgearState"] = HeadgearState.ToString();
        json["VisorState"] = VisorState.ToString();
        json["Kind"] = Kind.ToString();
        json["ForceFirstPerson"] = ForceFirstPerson;
        json["CustomPath"] = CustomPath;
        return json;
    }
}

public class CollarRestriction : RestrictionItem
{
    public override RestrictionType Type { get; } = RestrictionType.Collar;
    public string OwnerUID { get; set; } = string.Empty;
    public string CollarWriting { get; set; } = string.Empty;

    public CollarRestriction() { }
    public CollarRestriction(CollarRestriction other, bool keepIdentifier)
        : base(other, keepIdentifier)
    {
        OwnerUID = other.OwnerUID;
        CollarWriting = other.CollarWriting;
    }

    /// <summary> Applies updated changes to an edited item, while still maintaining the original references. <summary>
    public void ApplyChanges(CollarRestriction other)
    {
        base.ApplyChanges(other);
        OwnerUID = other.OwnerUID;
        CollarWriting = other.CollarWriting;
    }

    public override JObject Serialize()
    {
        var json = base.Serialize();
        json["OwnerUID"] = OwnerUID;
        json["CollarWriting"] = CollarWriting;
        return json;
    }
}
