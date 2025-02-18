using GagSpeak.PlayerState.Models;
using GagSpeak.Utils;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerData.Storage;

public class RestrictionStorage : List<RestrictionItem>
{
    public bool TryGetRestriction(Guid id, [NotNullWhen(true)] out RestrictionItem? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    public RestrictionItem? ByIdentifier(Guid id)
        => this.FirstOrDefault(x => x.Identifier == id);

    public bool Contains(Guid id)
        => ByIdentifier(id) != null;
}

public class GagRestrictionStorage : SortedList<GagType, GarblerRestriction>
{
    public bool TryGetGag(GagType gag, [NotNullWhen(true)] out GarblerRestriction? item)
        => this.TryGetValue(gag, out item);

    /// <summary> A variant of TryGetGag that only returns the item if it's marked as Enabled. </summary>
    /// <remarks> Useful for disabling Gag Items so we know if the Gag Info is cached or not. </remarks>
    public bool TryGetEnabledGag(GagType gag, [NotNullWhen(true)] out GarblerRestriction? item)
        => TryGetGag(gag, out item) && item.IsEnabled;

    // locate the GarblerRestriction in the sorted list by finding the value of its corresponding key type.
    public GarblerRestriction? ByIdentifier(GagType gag)
        => this.FirstOrDefault(x => x.Key == gag).Value;

    /// <summary> Checks if the GagType is a key within the sorted list. </summary>
    public bool Contains(GagType gag) => ContainsKey(gag);

    /// <summary> The constructor of the storage, which helps initialize the sorted list. </summary>
    public GagRestrictionStorage()
        : base(Enum.GetValues(typeof(GagType))
              .Cast<GagType>()
              .Where(gag => gag != GagType.None)
              .ToDictionary(gag => gag, gag => new GarblerRestriction(gag)))
    { }

    /// <summary> Gets if the respective gag is enabled, if it exists. </summary>
    /// <returns> True if it exists and is enabled, false otherwise. </returns>
    public bool IsEnabled(GagType gag) => ContainsKey(gag) && this[gag].IsEnabled;

    /// <summary> Used by client-side dropdown combos to identify if the gag has any attached data. </summary>
    /// <returns> True if any Glamour, Gags, or Mods are attached (and is enabled), false otherwise. </returns>
    /// <remarks> This accesses the sorted list directly. Calling an unstored item will throw unhandled. </remarks>
    public bool IsActiveWithData(GagType gag)
    {
        if (!ContainsKey(gag))
            return false;

        var restriction = this[gag];
        // we should return true if there is any attached Glamour, moodle, or mods.
        if (restriction.Glamour.GameItem.ItemId != ItemService.NothingItem(restriction.Glamour.Slot).ItemId)
            return true;

        if (restriction.Moodle.Id != Guid.Empty)
            return true;

        if (restriction.Mod != null)
            return true;
        // did not match any, return false.
        return false;
    }
}

public class RestraintStorage : List<RestraintSet>
{
    public bool TryGetRestraint(Guid id, [NotNullWhen(true)] out RestraintSet? set)
    {
        set = this.FirstOrDefault(x => x.Identifier == id);
        return set != null;
    }

    public RestraintSet? ByIdentifier(Guid id)
        => this.FirstOrDefault(x => x.Identifier == id);

    public bool Contains(Guid id)
        => ByIdentifier(id) != null;
}

public class CursedLootStorage : List<CursedItem>
{
    public bool TryGetLoot(Guid id, [NotNullWhen(true)] out CursedItem? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    public CursedItem? ByIdentifier(Guid id)
        => this.FirstOrDefault(x => x.Identifier == id);

    public bool TryRemoveLoot(Guid id)
    {
        var item = ByIdentifier(id);
        if (item is null)
            return false;

        Remove(item);
        return true;
    }

    public IReadOnlyList<CursedItem> ActiveItems => this
        .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
        .OrderBy(x => x.AppliedTime)
        .ToList();

    public IReadOnlyList<CursedItem> ActiveItemsDecending => this
        .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
        .OrderByDescending(x => x.AppliedTime)
        .ToList();

    public IReadOnlyList<CursedItem> InactiveItemsInPool => this
        .Where(x => x.InPool && x.AppliedTime == DateTimeOffset.MinValue)
        .ToList();

    public IReadOnlyList<CursedItem> ItemsInPool => this
        .Where(x => x.InPool)
        .ToList();

    public IReadOnlyList<CursedItem> ItemsNotInPool => this
        .Where(x => !x.InPool)
        .ToList();
}
