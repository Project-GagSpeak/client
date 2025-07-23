using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;
using GagSpeak.State.Models;

namespace GagSpeak.PlayerClient;

public class RestraintStorage : List<RestraintSet>, IEditableStorage<RestraintSet>
{
    public bool TryGetRestraint(Guid id, [NotNullWhen(true)] out RestraintSet? set)
    {
        set = this.FirstOrDefault(x => x.Identifier == id);
        return set != null;
    }

    /// <summary> Informs us if the item is in the storage. </summary>
    public bool Contains(Guid id)
        => this.Any(x => x.Identifier == id);

    // Interface Requirements:
    public bool TryApplyChanges(RestraintSet oldItem, RestraintSet changedItem)
    {
        if (changedItem is null)
            return false;
        oldItem.ApplyChanges(changedItem);
        return true;
    }
}

public class RestrictionStorage : List<RestrictionItem>, IEditableStorage<RestrictionItem>
{
    public bool TryGetRestriction(Guid id, [NotNullWhen(true)] out RestrictionItem? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    /// <summary> Informs us if the item is in the storage. </summary>
    public bool Contains(Guid id)
        => this.Any(x => x.Identifier == id);

    // Interface Requirements:
    public bool TryApplyChanges(RestrictionItem oldItem, RestrictionItem changedItem)
    {
        if (changedItem is null)
            return false;
        oldItem.ApplyChanges(changedItem);
        return true;
    }
}

public class GagRestrictionStorage : SortedList<GagType, GarblerRestriction>, IEditableStorage<GarblerRestriction>
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
    public bool Contains(GagType gag)
        => ContainsKey(gag);

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

    
    public IEnumerable<LightGag> ToLightStorage()
        => this.Values.Select(x => x.ToLightItem());

    // Interface Requirements:
    public bool TryApplyChanges(GarblerRestriction oldItem, GarblerRestriction changedItem)
    {
        if (changedItem is null)
            return false;
        oldItem.ApplyChanges(changedItem);
        return true;
    }
}

public class CursedLootStorage : List<CursedItem>, IEditableStorage<CursedItem>
{
    /// <summary> C# Quirk Dev Note here: Modifying any properties from the fetched object WILL update them directly.
    /// <para> Modifying the object itself will not update the actual item in the list, and must be accessed by index. </para>
    /// </summary>
    public bool TryGetLoot(Guid id, [NotNullWhen(true)] out CursedItem? item)
    {
        item = this.FirstOrDefault(x => x.Identifier == id);
        return item != null;
    }

    /// <summary> A mix of FindIndex() and TryGetValue() through the item GUID </summary>
    /// <param name="id"> the RestraintSet GUID to find the index of in storage. </param>
    /// <param name="index"> the index of the item in the list (if found). </param>
    /// <returns> True if the index was found, false if it was not. </returns>
    /// <remarks> This should be used when updating the full object, and not just its properties. </remarks>
    public bool TryFindIndexById(Guid id, out int index)
        => (index = this.FindIndex(x => x.Identifier == id)) != -1;

    /// <summary> Attempts to remove a loot item from the storage. </summary>
    public bool TryRemoveLoot(Guid id)
    {
        if(TryGetLoot(id, out var item))
            return Remove(item);

        return false;
    }

    public List<LightCursedLoot> GetLightStorage() => this
        .Select(x => x.ToLightItem())
        .ToList();

    public IEnumerable<Guid> ActiveIds => this
        .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
        .Select(x => x.Identifier);

    public IReadOnlyList<CursedItem> ActiveItems => this
        .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
        .OrderBy(x => x.AppliedTime)
        .ToList();

    public IReadOnlyList<CursedItem> AllItemsInPoolByActive => this
        .Where(x => x.InPool)
        .OrderByDescending(x => x.AppliedTime)
        .ThenBy(x => x.Label)
        .ToList();

    public IReadOnlyList<CursedItem> InactiveItemsInPool => this
        .Where(x => x.InPool && x.AppliedTime == DateTimeOffset.MinValue)
        .ToList();

    // Interface Requirements:
    public bool TryApplyChanges(CursedItem oldItem, CursedItem changedItem)
    {
        if (changedItem is null)
            return false;
        oldItem.ApplyChanges(changedItem);
        return true;
    }
}
