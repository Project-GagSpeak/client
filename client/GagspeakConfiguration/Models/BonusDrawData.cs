using GagSpeak.Utils;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary> Model for the draw data of a players equipment slot </summary>
[Serializable]
public record BonusDrawData
{
    public bool IsEnabled = false; // determines if it will be applied during event handling.
    public string EquippedBy = string.Empty; // remove if no use
    public bool Locked = false; // remove if no use
    public BonusItemFlag Slot = BonusItemFlag.Glasses;
    public EquipItem GameItem;
    // no stains for now.
    public BonusDrawData(EquipItem gameItem) => GameItem = gameItem;

    // In EquipDrawData
    public JObject Serialize()
    {
        // Include gameItemObj and gameStainObj in the serialized object
        return new JObject()
        {
            ["IsEnabled"] = IsEnabled,
            ["EquippedBy"] = EquippedBy,
            ["Locked"] = Locked,
            ["Slot"] = Slot.ToString(),
            ["CustomItemId"] = GameItem.Id.ToString(),
        };
    }

    public void Deserialize(JObject jsonObject)
    {
        IsEnabled = jsonObject["IsEnabled"]?.Value<bool>() ?? false;
        EquippedBy = jsonObject["EquippedBy"]?.Value<string>() ?? string.Empty;
        Locked = jsonObject["Locked"]?.Value<bool>() ?? false;
        Slot = (BonusItemFlag)Enum.Parse(typeof(BonusItemFlag), jsonObject["Slot"]?.Value<string>() ?? string.Empty);
        ushort customItemId = jsonObject["CustomItemId"]?.Value<ushort>() ?? ushort.MaxValue;
        GameItem = ItemIdVars.Resolve(Slot, new BonusItemId(customItemId));
    }
}
