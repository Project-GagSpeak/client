using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using GagspeakAPI.Attributes;

namespace GagSpeak.State.Caches;

/// <summary>
///     A struct that combines the key attribute of a Manager, with the layerIdx of an item.
/// </summary>
/// <remarks> Helps with code readability, and optimal sorting of storage caches. </remarks>
public readonly struct CombinedCacheKey : IComparable<CombinedCacheKey>, IEquatable<CombinedCacheKey>
{
    public ManagerPriority Manager { get; }
    public int LayerIndex { get; }

    // Used only for display and substituion purposes, may keep if useful later.
    // Do not use for any comparisons.
    public string Label { get; }

    // Used for cases where keys should not be removed unless MainHub.UID == EnactorUID.
    public string EnactorUID { get; }

    public CombinedCacheKey(ManagerPriority manager, int itemIdx, string enactor, string label)
    {
        Manager = manager;
        LayerIndex = itemIdx;
        EnactorUID = enactor;
        Label = label;
    }

    public static CombinedCacheKey Empty => new(ManagerPriority.Restraints, -1, string.Empty, string.Empty);

    public override string ToString()
        => $"{Manager}-Layer {LayerIndex} ({Label})";

    // Higher manager priority first, then higher layer index first
    public int CompareTo(CombinedCacheKey other)
    {
        var cmp = other.Manager.CompareTo(Manager); // Descending
        if (cmp != 0) return cmp;
        return other.LayerIndex.CompareTo(LayerIndex); // Descending
    }

    public bool Equals(CombinedCacheKey other)
        => Manager == other.Manager && LayerIndex == other.LayerIndex;

    public override int GetHashCode() => HashCode.Combine(Manager, LayerIndex);
    public override bool Equals(object? obj) => obj is CombinedCacheKey other && Equals(other);
}

// a struct that can have a key representing both cursed loot identifiers and restriction identifiers.
public readonly struct RestrictionKey : IComparable<RestrictionKey>
{
    public Guid Identifier { get; }
    public int Priority { get; }

    public RestrictionKey(Guid id, Precedence precedence, int index)
    {
        Identifier = id;
        Priority = ((int)precedence * 1000) + index;
    }

    // Dictionary equality by LootId only
    public override bool Equals(object? obj) =>
        obj is RestrictionKey rk && rk.Identifier == Identifier;

    public override int GetHashCode() => Identifier.GetHashCode();

    // Sorting by Precedence, then Index, then GUID
    public int CompareTo(RestrictionKey other)
    {
        int cmp = Priority.CompareTo(other.Priority);
        if (cmp != 0) return cmp;
        return Identifier.CompareTo(other.Identifier);
    }

    public static implicit operator RestrictionKey(Guid lootId) =>
        new RestrictionKey(lootId, 0, 0);
}
