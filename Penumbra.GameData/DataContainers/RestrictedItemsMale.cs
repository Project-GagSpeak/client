using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Penumbra.GameData.DataContainers;

/// <summary> A set of items restricted to male characters. </summary>
public sealed class RestrictedItemsMale(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : DataSharer<IReadOnlyDictionary<uint, uint>>(pluginInterface, log, "GenderRestrictedItemsMale", gameData.Language, 3,
        () => CreateItems(log, gameData))
{
    /// <summary> Check if the item is restricted to male characters and the character is not male. </summary>
    public (bool Replaced, CharacterArmor Armor) Resolve(CharacterArmor armor, EquipSlot slot, Race race, Gender gender)
    {
        if (gender is Gender.Male or Gender.MaleNpc)
            return (false, armor);

        var needle = armor.Set.Id | ((uint)armor.Variant.Id << 16) | ((uint)slot.ToSlot() << 24);
        if (Value.TryGetValue(needle, out var newValue))
            return (true, new CharacterArmor((ushort)newValue, (byte)(newValue >> 16), armor.Stains));

        return (false, armor);
    }

    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<uint, uint> CreateItems(Logger log, IDataManager gameData)
    {
        var ret   = new Dictionary<uint, uint>(128);
        var items = gameData.GetExcelSheet<Item>();
        foreach (var pair in GenderRestrictedItems.KnownItems)
            GenderRestrictedItems.AddItemMale(ret, pair, items, log);
        GenderRestrictedItems.AddUnknownItems(ret, items, log, 2);
        return ret.ToFrozenDictionary();
    }

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(8, Value.Count);

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Value.Count;
}
