using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Penumbra.GameData.DataContainers;

/// <summary> A set of items restricted to specific races. </summary>
public sealed class RestrictedItemsRace(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : DataSharer<IReadOnlySet<uint>>(pluginInterface, log, "RacialRestrictedItems", gameData.Language, 2, () => CreateItems(log, gameData))
{
    /// <summary> Create the data and also warn for unknown restrictions. </summary>
    private static FrozenSet<uint> CreateItems(Logger log, IDataManager gameData)
    {
        var ret = RaceGenderGroup.Where(c => c is not 0 and not uint.MaxValue).ToHashSet();

        var items      = gameData.GetExcelSheet<Item>();
        var categories = gameData.GetExcelSheet<EquipRaceCategory>(gameData.Language);
        foreach (var item in items.Where(i => i.EquipRestriction > 3))
        {
            if (ret.Contains((uint)item.ModelMain))
                continue;

            log.Information(
                $"{item.RowId:D5} {item.Name} has unknown restriction group {categories.GetRow(item.EquipRestriction).RowId:D2}.");
        }

        return ret.ToFrozenSet();
    }

    /// <summary> Check if the item is restricted to a different race than your characters. </summary>
    internal (bool Replaced, CharacterArmor Armor) Resolve(CharacterArmor armor, Race race, Gender gender)
    {
        var quad = armor.Set.Id | ((uint)armor.Variant.Id << 16);
        if (!Value.Contains(quad))
            return (false, armor);

        var idx   = ((int)race - 1) * 2 + (gender is Gender.Female or Gender.FemaleNpc ? 1 : 0);
        var value = RaceGenderGroup[idx];
        return (value != quad, new CharacterArmor((ushort)value, (byte)(value >> 16), armor.Stains));
    }

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => Value.Count * 16 + 32;

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Value.Count;


    /// <summary>
    /// The racial starter sets are available for all 4 slots each,
    /// but have no associated accessories or hats.
    /// </summary>
    // @Formatter:off
    public static readonly IReadOnlyList<uint> RaceGenderGroup =
    [
        0x020054,
        0x020055,
        0x020056,
        0x020057,
        0x02005C,
        0x02005D,
        0x020058,
        0x020059,
        0x02005A,
        0x02005B,
        0x020101,
        0x020102,
        0x010255,
        0x01033D,
        0x0102E8,
        0x010245,
    ];
    // @Formatter:on
}
