using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagspeakAPI.Extensions;
using OtterGui.Classes;

namespace GagSpeak.PlayerState.Models;

public interface ICustomizePlus // Requirements for C+ Integration
{
    /// <summary> Identifies which Customize+ profile should be used. </summary>
    Guid ProfileGuid { get; set; }
    /// <summary> Determines the priority that the application of this profile will have. </summary>
    uint ProfilePriority { get; set; }
}

public interface IRestriction
{
    /// <summary> Determines the Glamour applied from this restriction item. </summary>
    GlamourSlot Glamour { get; }
    /// <summary> Determines the Mod applied from this restriction item. </summary>
    ModAssociation Mod { get; }
    /// <summary> Determines the Moodle applied from this restriction item. </summary>
    /// <remarks> Can be either singular status or preset. </remarks>
    Moodle Moodle { get; }
    /// <summary> Determines the Traits applied from this restriction item. </summary>
    Traits Traits { get; }
    /// <summary> Serializes the restriction item to a JObject. </summary>
    JObject Serialize();
}

public interface IRestrictionItem : IRestriction
{
    Guid Identifier { get; }
    string Label { get; }
}

// Used for Gags. | Ensure C+ Allowance & Meta Allowance. Uses shared functionality but is independent.
public class GarblerRestriction : IRestriction, ICustomizePlus, IComparable
{
    public GagType GagType { get; init; }
    public bool IsEnabled { get; internal set; }
    public GlamourSlot Glamour { get; internal set; }
    public ModAssociation Mod { get; internal set; }
    public Moodle Moodle { get; internal set; }
    public Traits Traits { get; internal set; }
    public OptionalBool HeadgearState { get; internal set; } = OptionalBool.Null;
    public OptionalBool VisorState { get; internal set; } = OptionalBool.Null;
    public Guid ProfileGuid { get; set; }
    public uint ProfilePriority { get; set; }
    public bool DoRedraw { get; set; }
    internal GarblerRestriction(GagType gagType) => GagType = gagType;
    public GarblerRestriction(GarblerRestriction other)
    {
        GagType = other.GagType;
        IsEnabled = other.IsEnabled;
        Glamour = new GlamourSlot(other.Glamour);
        Mod = new ModAssociation(other.Mod);
        Moodle = new Moodle(other.Moodle);
        Traits = other.Traits;
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
            ["Mod"] = Mod.Serialize(),
            ["Moodle"] = Moodle.Serialize(),
            ["Traits"] = Traits.ToString(),
            ["HeadgearState"] = HeadgearState.ToString(),
            ["VisorState"] = VisorState.ToString(),
            ["ProfileGuid"] = ProfileGuid.ToString(),
            ["ProfilePriority"] = ProfilePriority,
            ["DoRedraw"] = DoRedraw,
        };

    public void LoadRestriction(JObject json)
    {
        IsEnabled = json["IsEnabled"]?.ToObject<bool>() ?? false;
        Glamour.LoadEquip(json["Glamour"]);
        Mod.LoadMod(json["Mod"]);
        Moodle.LoadMoodle(json["Moodle"]);
        Traits = (Traits)Enum.Parse(typeof(Traits), json["Traits"]?.Value<string>() ?? string.Empty);
        HeadgearState = JsonHelp.FromJObject(json["HeadgearState"]);
        VisorState = JsonHelp.FromJObject(json["VisorState"]);
        ProfileGuid = json["ProfileGuid"]?.ToObject<Guid>() ?? throw new ArgumentNullException("ProfileGuid");
        ProfilePriority = json["ProfilePriority"]?.ToObject<uint>() ?? throw new ArgumentNullException("ProfilePriority");
        DoRedraw = json["DoRedraw"]?.ToObject<bool>() ?? false;
    }
}

public class RestrictionItem : IRestrictionItem
{
    public virtual RestrictionType Type { get; } = RestrictionType.Normal;
    public Guid Identifier { get; internal set; } = Guid.NewGuid();
    public string Label { get; internal set; } = string.Empty;
    public GlamourSlot Glamour { get; internal set; }
    public ModAssociation Mod { get; internal set; }
    public Moodle Moodle { get; internal set; }
    public Traits Traits { get; internal set; }
 
    public RestrictionItem() { }
    public RestrictionItem(RestrictionItem other, bool keepIdentifier)
    {
        if (keepIdentifier)
        {
            Identifier = other.Identifier;
        }
        Label = other.Label;
        Glamour = new GlamourSlot(other.Glamour);
        Mod = new ModAssociation(other.Mod);
        Moodle = new Moodle(other.Moodle);
        Traits = other.Traits;
    }
    public virtual JObject Serialize()
        => new JObject
        {
            ["Type"] = Type.ToString(),
            ["Identifier"] = Identifier.ToString(),
            ["Label"] = Label,
            ["Glamour"] = Glamour.Serialize(),
            ["Mod"] = Mod.Serialize(),
            ["Moodle"] = Moodle.Serialize(),
            ["Traits"] = Traits.ToString(),
        };

    public virtual void LoadRestriction(JObject json)
    {
        Identifier = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
        Label = json["Label"]?.ToObject<string>() ?? string.Empty;
        Glamour.LoadEquip(json["Glamour"]);
        Mod.LoadMod(json["Mod"]);
        Moodle.LoadMoodle(json["Moodle"]);
        Traits = (Traits)Enum.Parse(typeof(Traits), json["Traits"]?.Value<string>() ?? string.Empty);
    }
}

public class BlindfoldRestriction : RestrictionItem
{
    public override RestrictionType Type { get; } = RestrictionType.Blindfold;
    public OptionalBool HeadgearState { get; internal set; } = OptionalBool.Null;
    public OptionalBool VisorState { get; internal set; } = OptionalBool.Null;
    public string CustomPath { get; set; }
    public bool IsCustom => !CustomPath.IsNullOrWhitespace();

    public BlindfoldRestriction() { }
    public BlindfoldRestriction(BlindfoldRestriction other, bool keepIdentifier)
        : base(other, keepIdentifier)
    {
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
        json["CustomPath"] = CustomPath;
        return json;
    }

    public override void LoadRestriction(JObject json)
    {
        base.LoadRestriction(json);
        HeadgearState = JsonHelp.FromJObject(json["HeadgearState"]);
        VisorState = JsonHelp.FromJObject(json["VisorState"]);
        CustomPath = json["CustomPath"]?.ToObject<string>() ?? string.Empty;
    }
}

public class CollarRestriction : RestrictionItem
{
    public override RestrictionType Type { get; } = RestrictionType.Collar;
    public string OwnerUID { get; set; }
    public string CollarWriting { get; set; }

    public CollarRestriction() { }
    public CollarRestriction(CollarRestriction other, bool keepIdentifier)
        : base(other, keepIdentifier)
    {
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

    public override void LoadRestriction(JObject json)
    {
        base.LoadRestriction(json);
        OwnerUID = json["OwnerUID"]?.ToObject<string>() ?? string.Empty;
        CollarWriting = json["CollarWriting"]?.ToObject<string>() ?? string.Empty;
    }
}
