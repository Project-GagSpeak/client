using GagSpeak.Interop.Ipc;
using GagSpeak.Localization;
using GagSpeak.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.VfxContainer;

namespace GagSpeak.PlayerState.Models;

public class Moodle
{
    public Guid Id { get; internal set; } = Guid.Empty;

    public Moodle() { }
    public Moodle(Moodle other) => Id = other.Id;
    public Moodle(Guid id) => Id = id;

    public override bool Equals(object? obj) => obj is Moodle other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();

    public virtual JObject Serialize()
        => new JObject
        {
            ["Type"] = MoodleType.Status.ToString(),
            ["Id"] = Id.ToString(),
        };

    public static Moodle StatusFromJToken(JToken? moodle)
    {
        if (moodle is not JObject jsonObject)
            throw new Exception("Invalid Moodle data!");

        return new Moodle
        {
            Id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
        };
    }
}

public class MoodlePreset : Moodle
{
    public IEnumerable<Guid> StatusIds { get; internal set; } = Enumerable.Empty<Guid>();

    public MoodlePreset()
        => (Id, StatusIds) = (Guid.Empty, Enumerable.Empty<Guid>());

    public MoodlePreset(MoodlePreset other)
        => (Id, StatusIds) = (other.Id, other.StatusIds);

    public override JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = MoodleType.Preset.ToString(),
            ["Id"] = Id.ToString(),
            ["StatusIds"] = new JArray(StatusIds.Select(x => x.ToString())),
        };
    }

    public static MoodlePreset PresetFromJToken(JToken? moodle)
    {
        if (moodle is not JObject jsonObject)
            throw new Exception("Invalid MoodlePreset data!");

        return new MoodlePreset
        {
            Id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            StatusIds = jsonObject["StatusIds"]?.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>(),
        };
    }
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
    public EquipSlot Slot { get; set; } = EquipSlot.Head;
    public EquipItem GameItem { get; set; } = ItemService.NothingItem(EquipSlot.Head);
    public StainIds GameStain { get; set; } = StainIds.None;

    public GlamourSlot()
        => (Slot, GameItem, GameStain) = (EquipSlot.Head, ItemService.NothingItem(EquipSlot.Head), StainIds.None);

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
