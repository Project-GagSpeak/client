using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary that maps full item types to lists of all corresponding items. </summary>
public sealed class ItemsByType(IDalamudPluginInterface pi, Logger log, IDataManager dataManager, DictBonusItems bonusItems)
    : DataSharer<IReadOnlyList<IReadOnlyList<PseudoEquipItem>>>(pi, log, "ItemsByType", dataManager.Language, 4,
            () => CreateItems(dataManager, bonusItems)),
        IReadOnlyDictionary<FullEquipType, IReadOnlyList<EquipItem>>
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyList<PseudoEquipItem>[] CreateItems(IDataManager dataManager, DictBonusItems bonusItems)
    {
        var tmp = Enum.GetValues<FullEquipType>().Select(_ => new List<EquipItem>(1024)).ToArray();

        var itemSheet = dataManager.GetExcelSheet<Item>(dataManager.Language)!;
        // Take all items with actual names.
        foreach (var item in itemSheet.Where(i => i.Name.ByteLength > 1))
        {
            var type = item.ToEquipType();

            // For weapons and tools, we need to check primary and secondary models.
            if (type.IsWeapon() || type.IsTool())
            {
                var mh = EquipItem.FromMainhand(item);
                if (item.ModelMain != 0)
                    tmp[(int)type].Add(mh);

                if (item.ModelSub == 0)
                    continue;

                // The game uses a hack for fist weapons where they have a tertiary gauntlet model in the secondary slot,
                // and compute the actual secondary model from the primary.
                if (type is FullEquipType.Fists && item.ModelSub < 0x100000000)
                {
                    tmp[(int)FullEquipType.Hands].Add(new EquipItem(mh.Name + " (Gauntlets)", mh.Id, mh.IconId, (PrimaryId)item.ModelSub, 0,
                        (byte)(item.ModelSub >> 16), FullEquipType.Hands, mh.Flags, mh.Level, mh.JobRestrictions));

                    tmp[(int)FullEquipType.FistsOff].Add(new EquipItem(mh.Name + FullEquipType.FistsOff.OffhandTypeSuffix(), mh.Id,
                        mh.IconId, (PrimaryId)(mh.PrimaryId.Id + 50), mh.SecondaryId, mh.Variant, FullEquipType.FistsOff, mh.Flags, mh.Level,
                        mh.JobRestrictions));
                }
                else
                {
                    tmp[(int)type.ValidOffhand()].Add(EquipItem.FromOffhand(item));
                }
            }
            // Regular gear can just be added.
            else if (type != FullEquipType.Unknown)
            {
                tmp[(int)type].Add(EquipItem.FromArmor(item));
            }
        }

        foreach (var item in bonusItems.Values)
            tmp[(int)item.Type].Add(item);

        var ret = new IReadOnlyList<PseudoEquipItem>[tmp.Length];
        ret[0] = Array.Empty<PseudoEquipItem>();
        // Order all collected items, plug it into the list and shrink to fit to arrays.
        for (var i = 1; i < tmp.Length; ++i)
            ret[i] = tmp[i].OrderBy(item => item.Name).Select(s => (PseudoEquipItem)s).ToArray();

        return ret;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<FullEquipType, IReadOnlyList<EquipItem>>> GetEnumerator()
    {
        for (var i = 1; i < Value.Count; ++i)
            yield return new KeyValuePair<FullEquipType, IReadOnlyList<EquipItem>>((FullEquipType)i, new EquipItemList(Value[i]));
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count - 1;

    /// <inheritdoc/>
    public bool ContainsKey(FullEquipType key)
        => (int)key < Value.Count && key != FullEquipType.Unknown;

    /// <inheritdoc/>
    public bool TryGetValue(FullEquipType key, out IReadOnlyList<EquipItem> value)
    {
        if (ContainsKey(key))
        {
            value = new EquipItemList(Value[(int)key]);
            return true;
        }

        value = [];
        return false;
    }

    /// <inheritdoc/>
    public IReadOnlyList<EquipItem> this[FullEquipType key]
        => TryGetValue(key, out var ret) ? ret : throw new IndexOutOfRangeException();

    /// <inheritdoc/>
    public IEnumerable<FullEquipType> Keys
        => Enum.GetValues<FullEquipType>().Skip(1);

    /// <inheritdoc/>
    public IEnumerable<IReadOnlyList<EquipItem>> Values
        => Value.Skip(1).Select(l => (IReadOnlyList<EquipItem>)new EquipItemList(l));

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => 16 + TotalCount * 32 + Count * 16;

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Value.Sum(row => row.Count);
}
