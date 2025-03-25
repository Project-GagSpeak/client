using GagSpeak.PlayerState.Components;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;

namespace GagSpeak.PlayerState.Models;

[Serializable]
public class CursedItem : ICursedItem
{
    public Guid             Identifier     { get; init; }          = Guid.NewGuid();
    public string           Label          { get; internal set; }  = string.Empty;
    public bool             InPool         { get; internal set; }  = false;
    public DateTimeOffset   AppliedTime    { get; internal set; }  = DateTimeOffset.MinValue;
    public DateTimeOffset   ReleaseTime    { get; internal set; }  = DateTimeOffset.MinValue;
    public bool             CanOverride    { get; internal set; }  = false;
    public Precedence       Precedence     { get; internal set; }  = Precedence.Default;
    public IRestriction     RestrictionRef { get; internal set; } // Can refernce a gag or a restriction type.

    public CursedItem() { }

    internal CursedItem(CursedItem other, bool keepIdentifier)
    {
        if (keepIdentifier)
        {
            Identifier = other.Identifier;
        }
        Label = other.Label;
        InPool = other.InPool;
        AppliedTime = other.AppliedTime;
        ReleaseTime = other.ReleaseTime;
        CanOverride = other.CanOverride;
        Precedence = other.Precedence;
        RestrictionRef = other.RestrictionRef;
    }

    // May need to be moved up or something. Not sure though. Look into later.
    public LightCursedItem ToLightData()
    {
        // determine what RestrictionType the IRestriction is by checking the type of the object
        var restrictionType = RestrictionRef switch
        {
            GarblerRestriction _ => RestrictionType.Gag,
            CollarRestriction _ => RestrictionType.Collar,
            BlindfoldRestriction _ => RestrictionType.Blindfold,
            _ => RestrictionType.Normal,
        };

        return new LightCursedItem(Identifier, Label, restrictionType)
        {
            GagType = (RestrictionRef is GarblerRestriction gag) ? gag.GagType : GagType.None,
            RestrictionId = (RestrictionRef is IRestrictionItem restriction) ? restriction.Identifier : Guid.Empty,
        };
    }

    // parameterless constructor for serialization
    public JObject Serialize()
    {
        var jsonObject = new JObject()
        {
            ["Identifier"] = Identifier.ToString(),
            ["Label"] = Label,
            ["InPool"] = InPool,
            ["AppliedTime"] = AppliedTime.UtcDateTime.ToString("o"),
            ["ReleaseTime"] = ReleaseTime.UtcDateTime.ToString("o"),
            ["CanOverride"] = CanOverride,
            ["Precedence"] = Precedence.ToString(),
        };

        if (RestrictionRef is GarblerRestriction gag)
        {
            jsonObject["RestrictionRef"] = gag.GagType.ToString();
        }
        else if (RestrictionRef is IRestrictionItem restriction)
        {
            jsonObject["RestrictionRef"] = restriction.Identifier.ToString();
        }
        return jsonObject;
    }
}
