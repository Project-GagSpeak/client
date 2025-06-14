using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

[Serializable]
public class CursedItem : IEditableStorageItem<CursedItem>, ICursedItem
{
    public Guid             Identifier     { get; init; }          = Guid.NewGuid();
    public string           Label          { get; internal set; }  = string.Empty;
    public bool             InPool         { get; internal set; }  = false;
    public DateTimeOffset   AppliedTime    { get; internal set; }  = DateTimeOffset.MinValue;
    public DateTimeOffset   ReleaseTime    { get; internal set; }  = DateTimeOffset.MinValue;
    public bool             CanOverride    { get; internal set; }  = false;
    public Precedence       Precedence     { get; internal set; }  = Precedence.Default;
    public IRestriction     RestrictionRef { get; internal set; } // Can reference a gag or a restriction type.

    public CursedItem()
    { }

    public CursedItem(CursedItem other, bool keepIdentifier)
    {
        Identifier = keepIdentifier ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public CursedItem Clone(bool keepId = false) => new CursedItem(this, keepId);

    public void ApplyChanges(CursedItem changedItem)
    {
        Label = changedItem.Label;
        InPool = changedItem.InPool;
        AppliedTime = changedItem.AppliedTime;
        ReleaseTime = changedItem.ReleaseTime;
        CanOverride = changedItem.CanOverride;
        Precedence = changedItem.Precedence;
        RestrictionRef = changedItem.RestrictionRef;
    }

    // May need to be moved up or something. Not sure though. Look into later.
    public LightCursedItem ToLightItem()
    {
        var gagItem = (RestrictionRef is GarblerRestriction gag) ? gag.GagType : GagType.None;
        var refId = (RestrictionRef is IRestrictionItem restriction) ? restriction.Identifier : Guid.Empty;
        return new LightCursedItem(Identifier, Label, gagItem, refId, ReleaseTime);
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
