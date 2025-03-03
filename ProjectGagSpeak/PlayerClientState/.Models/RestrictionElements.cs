using GagSpeak.Interop.Ipc;
using GagSpeak.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.PlayerState.Models;

public class Moodle
{
    public Guid Id { get; internal set; }

    internal Moodle() => Id = Guid.Empty;
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

    public virtual void LoadMoodle(JToken? moodle)
    {
        if (moodle is not JObject jsonObject)
            return;

        Id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
    }
}

public class MoodlePreset : Moodle
{
    public IEnumerable<Guid> StatusIds { get; internal set; }

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

    public override void LoadMoodle(JToken? moodle)
    {
        if (moodle is not JObject jsonObject)
            return;

        base.LoadMoodle(moodle);
        StatusIds = jsonObject["StatusIds"]?.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>();
    }
}

public class ModAssociation
{
    public Mod ModInfo { get; internal set; }
    public string CustomSettings { get; internal set; }

    internal ModAssociation()
        => (ModInfo, CustomSettings) = (new Mod(), string.Empty);

    public ModAssociation(ModAssociation other)
        => (ModInfo, CustomSettings) = (other.ModInfo, other.CustomSettings);

    public ModAssociation(KeyValuePair<Mod, string> kvp)
        => (ModInfo, CustomSettings) = (kvp.Key, kvp.Value);

    public override bool Equals(object? obj)
        => obj is ModAssociation other && ModInfo.DirectoryName.Equals(other.ModInfo.DirectoryName);

    public override int GetHashCode()
        => ModInfo.DirectoryName.GetHashCode();

    // Not utilitizing the inherit and remove properties, but may not be nessisary
    public JObject Serialize()
    {
        return new JObject
        {
            ["Name"] = ModInfo.Name,
            ["Directory"] = ModInfo.DirectoryName,
            ["CustomSettingsName"] = CustomSettings
        };
    }

    public void LoadMod(JToken? mod)
    {
        if (mod is not JObject jsonObject)
            return;

        ModInfo = new Mod(jsonObject["Name"]?.Value<string>() ?? string.Empty, jsonObject["Directory"]?.Value<string>() ?? string.Empty);
        CustomSettings = jsonObject["CustomSettingsName"]?.Value<string>() ?? string.Empty;
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
    public EquipSlot Slot { get; internal set; }
    public EquipItem GameItem { get; internal set; }
    public StainIds GameStain { get; internal set; } = StainIds.None;

    internal GlamourSlot()
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
