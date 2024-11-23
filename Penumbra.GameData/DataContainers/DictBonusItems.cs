using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary that maps GlassesIds to Glasses. </summary>
public sealed class DictBonusItems(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : DataSharer<IReadOnlyDictionary<ushort, PseudoEquipItem>>(pluginInterface, log, "BonusItems",
        gameData.Language, 2,
        () => CreateGlassesData(gameData)), IReadOnlyDictionary<BonusItemId, EquipItem>
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<ushort, PseudoEquipItem> CreateGlassesData(
        IDataManager dataManager)
    {
        // TODO
        var glassesSheet = dataManager.GetExcelSheet<Glasses>(dataManager.Language);
        return glassesSheet.Where(s => s.Name.ByteLength > 0)
            .ToFrozenDictionary(s => (ushort)s.RowId, FromBonusItem);
    }

    private static PseudoEquipItem FromBonusItem(Glasses bonusItem)
    {
        var name            = bonusItem.Name.ToString();
        var id              = (CustomItemId)new BonusItemId((ushort)bonusItem.RowId);
        var icon            = new IconId((uint)bonusItem.Icon);
        var model           = new PrimaryId((ushort)bonusItem.Unknown_70_7);
        var variant         = new Variant((byte)(bonusItem.Unknown_70_7 >> 16));
        var type            = FullEquipType.Glasses; // TODO slot other than glasses
        var flags           = (ItemFlags)0;
        var level           = (CharacterLevel)1;
        var jobRestrictions = (JobGroupId)0;
        return (PseudoEquipItem)new EquipItem(name, id, icon, model, 0, variant, type, flags, level, jobRestrictions);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<BonusItemId, EquipItem>> GetEnumerator()
        => Value.Select(kvp => new KeyValuePair<BonusItemId, EquipItem>(new BonusItemId(kvp.Key), kvp.Value)).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(BonusItemId key)
        => Value.ContainsKey(key.Id);

    /// <inheritdoc/>
    public bool TryGetValue(BonusItemId key, out EquipItem value)
    {
        if (!Value.TryGetValue(key.Id, out var data))
        {
            value = default;
            return false;
        }

        value = data;
        return true;
    }

    /// <inheritdoc/>
    public EquipItem this[BonusItemId key]
        => TryGetValue(key, out var data) ? data : throw new ArgumentOutOfRangeException(nameof(key));

    /// <inheritdoc/>
    public IEnumerable<BonusItemId> Keys
        => Value.Keys.Select(k => new BonusItemId(k));

    /// <inheritdoc/>
    public IEnumerable<EquipItem> Values
        => Value.Select(kvp => (EquipItem)kvp.Value);

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(32, Count);

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Count;
}
