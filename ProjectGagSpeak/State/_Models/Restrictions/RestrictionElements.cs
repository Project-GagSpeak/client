using GagSpeak.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.State.Models;

public class GlamourBonusSlot
{
    public BonusItemFlag Slot { get; internal set; } = BonusItemFlag.Glasses;
    public EquipItem GameItem { get; internal set; } = EquipItem.BonusItemNothing(BonusItemFlag.Glasses);

    internal GlamourBonusSlot()
        => (Slot, GameItem) = (BonusItemFlag.Glasses, EquipItem.BonusItemNothing(BonusItemFlag.Glasses));

    public GlamourBonusSlot(GlamourBonusSlot other)
        => (Slot, GameItem) = (other.Slot, other.GameItem);

    public GlamourBonusSlot(BonusItemFlag slot, EquipItem gameItem)
        => (Slot, GameItem) = (slot, gameItem);

    public JObject Serialize()
        => new JObject
        {
            ["Slot"] = Slot.ToString(),
            ["CustomItemId"] = GameItem.Id.BonusItem.ToString(),
        };
}

public class GlamourSlot : IEquatable<GlamourSlot>
{
    public EquipSlot Slot { get; set; } = EquipSlot.Nothing;
    public EquipItem GameItem { get; set; } = ItemService.NothingItem(EquipSlot.Nothing);
    public StainIds GameStain { get; set; } = StainIds.None;

    public GlamourSlot()
    { }

    public GlamourSlot(GlamourSlot other)
        => (Slot, GameItem, GameStain) = (other.Slot, other.GameItem, other.GameStain);

    public GlamourSlot(EquipSlot slot, EquipItem gameItem)
        => (Slot, GameItem) = (slot, gameItem);

    public JObject Serialize()
        => new JObject
        {
            ["Slot"] = Slot.ToString(),
            ["CustomItemId"] = GameItem.Id.ToString(),
            ["Stains"] = GameStain.ToString(),
        };

    public bool Equals(GlamourSlot? other)
        => other != null
           && Slot == other.Slot
           && GameItem.Equals(other.GameItem)
           && GameStain.Equals(other.GameStain);

    public override bool Equals(object? obj)
        => obj is GlamourSlot glamour && Equals(glamour);

    public override int GetHashCode()
        => HashCode.Combine(Slot, GameItem, GameStain);

}

