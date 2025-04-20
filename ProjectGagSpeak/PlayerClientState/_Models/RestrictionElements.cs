using GagSpeak.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.PlayerState.Models;

public class Moodle
{
    public Guid Id { get; internal set; } = Guid.Empty;

    public Moodle() { }
    public Moodle(Moodle other) => Id = other.Id;
    public Moodle(Guid id) => Id = id;

    public override bool Equals(object? obj) => obj is Moodle other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();
}

public class MoodlePreset : Moodle
{
    public IEnumerable<Guid> StatusIds { get; internal set; } = Enumerable.Empty<Guid>();

    public MoodlePreset()
        => (Id, StatusIds) = (Guid.Empty, Enumerable.Empty<Guid>());

    public MoodlePreset(MoodlePreset other)
        => (Id, StatusIds) = (other.Id, other.StatusIds);
}

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

public class GlamourSlot
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
}
