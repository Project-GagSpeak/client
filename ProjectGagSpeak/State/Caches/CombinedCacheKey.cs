namespace GagSpeak.State.Caches;

/// <summary>
///     A struct that combines the key attribute of a Manager, with the layerIdx of an item.
/// </summary>
/// <remarks> Helps with code readability, and optimal sorting of storage caches. </remarks>
public readonly struct CombinedCacheKey : IComparable<CombinedCacheKey>, IEquatable<CombinedCacheKey>
{
    public ManagerPriority Manager { get; }
    public int LayerIndex { get; }

    public CombinedCacheKey(ManagerPriority manager, int itemIdx)
    {
        Manager = manager;
        LayerIndex = itemIdx;
    }

    public override string ToString()
        => $"{Manager}-Layer {LayerIndex}";

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
