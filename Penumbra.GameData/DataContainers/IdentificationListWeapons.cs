using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A list to efficiently identify weapons. This requires ItemsByType to be finished. </summary>
public sealed class IdentificationListWeapons(IDalamudPluginInterface pi, Logger log, IDataManager gameData, ItemsByType data)
    : KeyList<PseudoEquipItem>(pi, log, "WeaponIdentification", gameData.Language, 9, () => CreateWeaponList(data), ToKey, ValidKey, ValueKeySelector,
        data.Awaiter)
{
    /// <inheritdoc cref="Between(PrimaryId, SecondaryId, Variant)"/>
    public IEnumerable<EquipItem> Between(PrimaryId modelId)
        => Between(ToKey(modelId, 0, 0), ToKey(modelId, 0xFFFF, 0xFF)).Select(e => (EquipItem)e);

    /// <summary> Find all items affected by the given set of input data. </summary>
    /// <param name="modelId"> The primary ID of the weapon. </param>
    /// <param name="type"> The secondary ID of the weapon. </param>
    /// <param name="variant"> The variant. If 0, check all variants. </param>
    /// <returns> A list of all affected EquipItems. </returns>
    public IEnumerable<EquipItem> Between(PrimaryId modelId, SecondaryId type, Variant variant = default)
    {
        if (type == 0)
            return Between(ToKey(modelId, 0, 0), ToKey(modelId, 0xFFFF, 0xFF)).Select(e => (EquipItem)e);
        if (variant == 0)
            return Between(ToKey(modelId, type, 0), ToKey(modelId, type, 0xFF)).Select(e => (EquipItem)e);

        return Between(ToKey(modelId, type, variant), ToKey(modelId, type, variant)).Select(e => (EquipItem)e);
    }

    /// <inheritdoc cref="IdentificationListEquipment.ToKey(PrimaryId, EquipSlot, Variant)"/>
    public static ulong ToKey(PrimaryId modelId, SecondaryId type, Variant variant)
        => ((ulong)modelId.Id << 32) | ((ulong)type.Id << 16) | variant.Id;

    /// <inheritdoc cref="IdentificationListEquipment.ToKey(EquipItem)"/>
    public static ulong ToKey(EquipItem i)
        => ToKey(i.PrimaryId, i.SecondaryId, i.Variant);

    /// <inheritdoc cref="IdentificationListEquipment.ToKey(PseudoEquipItem)"/>
    private static ulong ToKey(PseudoEquipItem data)
        => ToKey((EquipItem)data);

    /// <inheritdoc cref="IdentificationListEquipment.ValidKey"/>
    private static bool ValidKey(ulong key)
        => key != 0;

    /// <inheritdoc cref="IdentificationListEquipment.ValueKeySelector"/>
    private static int ValueKeySelector(PseudoEquipItem data)
        => (int)data.Item2;

    /// <summary> Obtain a key list of all weapons. </summary>
    private static IEnumerable<PseudoEquipItem> CreateWeaponList(ItemsByType data)
        => data.Where(kvp => !kvp.Key.IsEquipment() && !kvp.Key.IsAccessory())
            .SelectMany(kvp => kvp.Value)
            .Select(i => (PseudoEquipItem)i);

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => 24 + Value.Count * 40;
}
