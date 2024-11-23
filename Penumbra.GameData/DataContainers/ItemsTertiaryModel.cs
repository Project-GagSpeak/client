using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary mapping ItemIds to all primary model items. Currently only used by fist weapon gauntlets for fist weapons in range 1601-1650. This requires secondary models to be finished. </summary>
public sealed class ItemsTertiaryModel(
    IDalamudPluginInterface pi,
    Logger log,
    IDataManager gameData,
    ItemsByType items,
    ItemsSecondaryModel itemsSecondaries)
    : ItemDictionary(pi, log, "ItemDictTertiary", gameData.Language, 4, () => CreateGauntlets(items, itemsSecondaries),
        itemsSecondaries.Awaiter)
{
    /// <summary> Create data by taking only the tertiary models for all items. </summary>
    private static IReadOnlyDictionary<ulong, PseudoEquipItem> CreateGauntlets(ItemsByType items,
        ItemsSecondaryModel itemsSecondaries)
    {
        var gauntlets = items.Value[(int)FullEquipType.Hands]
            .Where(g => itemsSecondaries.Value
                .ContainsKey(g.Item2))
            .ToDictionary(g => g.Item2, g => g);
        return gauntlets.ToFrozenDictionary();
    }
}
