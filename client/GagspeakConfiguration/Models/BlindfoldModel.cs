
using GagSpeak.Utils;
using GagspeakAPI.Data.Struct;
using Penumbra.GameData.Enums;

namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public class BlindfoldModel
{
    public BlindfoldModel()
    {
        BlindfoldItem = new EquipDrawData(ItemIdVars.NothingItem(EquipSlot.Head)) 
        { 
            Slot = EquipSlot.Head, 
            IsEnabled = false 
        };
    }

    public bool ForceHeadgear { get; set; } = false;
    public bool ForceVisor { get; set; } = false;
    public List<Guid> BlindfoldMoodles { get; set; } = new List<Guid>();
    public EquipDrawData BlindfoldItem { get; set; }

    public AppliedSlot GetAppliedSlot()
    {
        return new AppliedSlot
        {
            Slot = (byte)BlindfoldItem.Slot,
            CustomItemId = BlindfoldItem.GameItem.Id.Id,
            Tooltip = "This Slot is Locked! --SEP--An equipped blindfold is occupying this slot!",
        };
    }

    public JObject Serialize()
    {
        return new JObject
        {
            ["ForceHeadgear"] = ForceHeadgear,
            ["ForceVisor"] = ForceVisor,
            ["BlindfoldMoodles"] = new JArray(BlindfoldMoodles),
            ["BlindfoldItem"] = BlindfoldItem.Serialize()
        };
    }

    public void Deserialize(JObject jsonObject)
    {
        ForceHeadgear = jsonObject["ForceHeadgear"]?.Value<bool>() ?? false;
        ForceVisor = jsonObject["ForceVisor"]?.Value<bool>() ?? false;
        if (jsonObject["BlindfoldMoodles"] is JArray associatedMoodlesArray)
        {
            BlindfoldMoodles = associatedMoodlesArray.Select(moodle => Guid.Parse(moodle.Value<string>() ?? string.Empty)).ToList();
        }
        BlindfoldItem.Deserialize((JObject)jsonObject["BlindfoldItem"]!);
    }
}
