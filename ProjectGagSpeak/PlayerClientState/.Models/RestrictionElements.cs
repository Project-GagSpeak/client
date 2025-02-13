using GagSpeak.CkCommons;
using GagSpeak.Interop.Ipc;
using GagSpeak.Utils;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.PlayerState.Models;

public class Moodle
{
    public MoodleType Type { get; internal set; }
    public Guid Id { get; internal set; }

    internal Moodle()
    {
        Type = MoodleType.Status;
        Id = Guid.Empty;
    }

    public Moodle(Moodle other)
    {
        Type = other.Type;
        Id = other.Id;
    }

    public Moodle(MoodleType type, Guid id)
    {
        Type = type;
        Id = id;
    }

    public virtual JObject Serialize()
        => new JObject
        {
            ["Type"] = Type.ToString(),
            ["Id"] = Id.ToString(),
        };

    public virtual void LoadMoodle(JToken? moodle)
    {
        if (moodle is not JObject jsonObject)
            return;

        Type = (MoodleType)Enum.Parse(typeof(MoodleType), jsonObject["Type"]?.Value<string>() ?? string.Empty);
        Id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
    }
}

public class MoodlePreset : Moodle
{
    public IEnumerable<Guid> StatusIds { get; internal set; }

    public MoodlePreset()
    {
        Type = MoodleType.Status;
        Id = Guid.Empty;
        StatusIds = Enumerable.Empty<Guid>();
    }

    public MoodlePreset(MoodlePreset other)
        : base(other.Type, other.Id)
    {
        StatusIds = other.StatusIds;
    }

    public override JObject Serialize()
    {
        var json = base.Serialize();
        json["StatusIds"] = new JArray(StatusIds.Select(x => x.ToString()));
        return json;
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
    {
        ModInfo = new Mod();
        CustomSettings = string.Empty;
    }

    public ModAssociation(ModAssociation other)
    {
        ModInfo = other.ModInfo;
        CustomSettings = other.CustomSettings;
    }

    public ModAssociation(Mod mod, string customSettingsName)
    {
        ModInfo = mod;
        CustomSettings = customSettingsName;
    }

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
    {
        Slot = BonusItemFlag.Glasses;
        GameItem = EquipItem.BonusItemNothing(BonusItemFlag.Glasses);
    }

    public GlamourBonusSlot(GlamourBonusSlot other)
    {
        Slot = other.Slot;
        GameItem = other.GameItem;
    }

    public GlamourBonusSlot(BonusItemFlag slot, EquipItem gameItem)
    {
        Slot = slot;
        GameItem = gameItem;
    }

    public JObject Serialize()
        => new JObject
        {
            ["Slot"] = Slot.ToString(),
            ["CustomItemId"] = GameItem.Id.ToString(),
        };

    public void LoadBonus(JToken? bonusItem)
    {
        if (bonusItem is not JObject jsonObject)
            return;

        Slot = (BonusItemFlag)Enum.Parse(typeof(BonusItemFlag), jsonObject["Slot"]?.Value<string>() ?? string.Empty);
        ushort customItemId = jsonObject["CustomItemId"]?.Value<ushort>() ?? ushort.MaxValue;
        GameItem = ItemIdVars.Resolve(Slot, new BonusItemId(customItemId));
    }
}

public class GlamourSlot
{
    public EquipSlot Slot { get; internal set; }
    public EquipItem GameItem { get; internal set; }
    public StainIds GameStain { get; internal set; } = StainIds.None;

    internal GlamourSlot()
    {
        Slot = EquipSlot.Head;
        GameItem = ItemIdVars.NothingItem(EquipSlot.Head);
    }

    public GlamourSlot(GlamourSlot other)
    {
        Slot = other.Slot;
        GameItem = other.GameItem;
        GameStain = other.GameStain;
    }

    public GlamourSlot(EquipSlot slot, EquipItem gameItem)
    {
        Slot = slot;
        GameItem = gameItem;
    }

    public JObject Serialize()
        => new JObject
        {
            ["Slot"] = Slot.ToString(),
            ["CustomItemId"] = GameItem.Id.ToString(),
            ["Stains"] = GameStain.ToString(),
        };

    public void LoadEquip(JToken? equipItem)
    {
        if (equipItem is not JObject json)
            return;

        Slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), json["Slot"]?.Value<string>() ?? string.Empty);
        ulong customItemId = json["CustomItemId"]?.Value<ulong>() ?? 4294967164;
        GameItem = ItemIdVars.Resolve(Slot, new CustomItemId(customItemId));
        GameStain = JsonHelp.ParseCompactStainIds(json);
    }
}
