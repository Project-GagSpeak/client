using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draw all collected items. </summary>
public class ItemDataDrawer(ItemData _data) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Item Manager";

    /// <inheritdoc/>
    public bool Disabled
        => !_data.Finished;

    private string _itemFilter = string.Empty;

    /// <inheritdoc/>
    public void Draw()
    {
        DebugUtility.DrawNameTable($"All Items (Main, {_data.Primary.Count})###AllItemsMain", ref _itemFilter, true,
            _data.AllItems(true).Select(p => ((ulong)p.Item1.Id,
                    $"{p.Item2.Name} ({(p.Item2.SecondaryId == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        DebugUtility.DrawNameTable($"All Items (Off), {_data.Secondary.Count})###AllItemsOff", ref _itemFilter, true,
            _data.AllItems(false).Select(p => ((ulong)p.Item1.Id,
                    $"{p.Item2.Name} ({(p.Item2.SecondaryId == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        foreach (var type in Enum.GetValues<FullEquipType>().Skip(1))
        {
            DebugUtility.DrawNameTable(type.ToName(), ref _itemFilter, true,
                _data.ByType[type]
                    .Select(p => ((ulong)p.ItemId.Id, $"{p.Name} ({(p.SecondaryId.Id == 0 ? p.Armor().ToString() : p.Weapon().ToString())})")));
        }
    }
}
