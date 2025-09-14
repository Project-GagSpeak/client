using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Network;

namespace GagSpeak.State.Models;

public abstract class CursedItem : IEditableStorageItem<CursedItem>
{
    public abstract CursedLootKind Type { get; }

    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool InPool { get; set; } = false; // the "enabled" state.
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset AppliedTime { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset ReleaseTime { get; set; } = DateTimeOffset.MinValue;
    public Precedence Precedence { get; set; } = Precedence.Default; // the priority system.
    public bool ApplyTraits { get; set; } = true; // For Hardcore Traits.

    public abstract string RefLabel { get; }

    public CursedItem()
    { }

    public CursedItem(CursedItem other, bool keepId)
    {
        Identifier = keepId ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public abstract CursedItem Clone(bool keepId = false);

    public virtual void ApplyChanges(CursedItem other)
    {
        Label = other.Label;
        InPool = other.InPool;
        AppliedTime = other.AppliedTime;
        ReleaseTime = other.ReleaseTime;
        Precedence = other.Precedence;
    }

    // May need to be moved up or something. Not sure though. Look into later.
    public virtual LightCursedLoot ToLightItem()
        => new LightCursedLoot(Identifier, Label, Precedence, CursedLootType.None);

    public virtual AppliedItem ToAppliedItem()
        => new AppliedItem(ReleaseTime, CursedLootType.None);

    public bool IsActive()
        => AppliedTime != DateTimeOffset.MinValue;

    public abstract JObject Serialize();

    public override bool Equals(object? obj)
        => base.Equals(obj);
    public override int GetHashCode()
        => Identifier.GetHashCode();

    public static bool operator ==(CursedItem? left, CursedItem? right)
    {
        if (left is null || right is null)
            return false;
        return left.Identifier.Equals(right.Identifier);
    }

    public static bool operator !=(CursedItem? left, CursedItem? right)
        => !(left == right);

}

[Serializable]
public class CursedGagItem : CursedItem
{
    public override CursedLootKind Type => CursedLootKind.Gag;
    public GarblerRestriction RefItem { get; set; }

    public override string RefLabel => RefItem?.GagType.ToString() ?? "UNK TYPE";

    public CursedGagItem()
    { }

    public CursedGagItem(CursedItem other, bool keepId)
        : base(other, keepId)
    { }

    public CursedGagItem(CursedGagItem other, bool keepId)
        : base(other, keepId)
    {
        RefItem = other.RefItem;
    }

    public override CursedGagItem Clone(bool keepId)
        => new CursedGagItem(this, keepId);

    public override void ApplyChanges(CursedItem other)
    {
        base.ApplyChanges(other);
        if (other is not CursedGagItem cgl)
            return;

        RefItem = cgl.RefItem;
    }
    
    public override LightCursedLoot ToLightItem()
        => new LightCursedLoot(Identifier, Label, Precedence, CursedLootType.Gag, null, RefItem.GagType);
    public override AppliedItem ToAppliedItem()
        => new AppliedItem(ReleaseTime, CursedLootType.Gag, null, RefItem.GagType);

    public override JObject Serialize()
        => new JObject()
        {
            ["Type"] = Type.ToString(),
            ["Identifier"] = Identifier.ToString(),
            ["InPool"] = InPool,
            ["Label"] = Label,
            ["AppliedTime"] = AppliedTime.UtcDateTime.ToString("o"),
            ["ReleaseTime"] = ReleaseTime.UtcDateTime.ToString("o"),
            ["Precedence"] = Precedence.ToString(),
            ["GagRef"] = RefItem.GagType.ToString()
        };
}

[Serializable]
public class CursedRestrictionItem : CursedItem
{
    public override CursedLootKind Type => CursedLootKind.Restriction;
    public RestrictionItem RefItem { get; set; }

    public override string RefLabel => RefItem?.Label.ToString() ?? "UNK";
    public CursedRestrictionItem()
    { }

    public CursedRestrictionItem(CursedItem other, bool keepId)
        : base(other, keepId)
    { }

    public CursedRestrictionItem(CursedRestrictionItem other, bool keepId)
        : base(other, keepId)
    {
        RefItem = other.RefItem;
    }

    public override CursedRestrictionItem Clone(bool keepId)
        => new CursedRestrictionItem(this, keepId);

    public override void ApplyChanges(CursedItem other)
    {
        base.ApplyChanges(other);
        if (other is not CursedRestrictionItem crl)
            return;

        RefItem = crl.RefItem;
    }
    
    public override LightCursedLoot ToLightItem()
        => new LightCursedLoot(Identifier, Label, Precedence, CursedLootType.Restriction, RefItem.Identifier);
    public override AppliedItem ToAppliedItem()
        => new AppliedItem(ReleaseTime, CursedLootType.Restriction, RefItem.Identifier);

    public override JObject Serialize()
        => new JObject()
        {
            ["Type"] = Type.ToString(),
            ["Identifier"] = Identifier.ToString(),
            ["InPool"] = InPool,
            ["Label"] = Label,
            ["AppliedTime"] = AppliedTime.UtcDateTime.ToString("o"),
            ["ReleaseTime"] = ReleaseTime.UtcDateTime.ToString("o"),
            ["Precedence"] = Precedence.ToString(),
            ["RestrictionRef"] = RefItem.Identifier.ToString()
        };
}
