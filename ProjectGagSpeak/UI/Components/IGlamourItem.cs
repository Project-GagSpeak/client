using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.UI.Components;
public interface IGlamourItem
{
    /// <summary>
    /// Determines whether or not to apply the item as direct, or as overlay.
    /// <para> OVERLAY MODE should skip over the item if it is a nothing item. </para>
    /// <para> DIRECT MODE will apply the item regardless of what it is. </para>
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// The slot that the item is intended to be applied to.
    /// </summary>
    EquipSlot Slot { get; set; }

    /// <summary>
    /// The EquipItem / Game Item that is intended to be applied.
    /// </summary>
    EquipItem GameItem { get; set; }

    /// <summary>
    /// The Stain ID(s) to place on this game item.
    /// </summary>
    StainIds GameStain { get; set; }
}
