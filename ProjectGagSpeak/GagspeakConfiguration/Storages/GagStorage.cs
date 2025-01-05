using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.Utils;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Enums;
using Microsoft.IdentityModel.Tokens;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Diagnostics;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary> Stores EquipDrawData for each GagType </summary>
[Serializable]
public class GagStorage
{
    public Dictionary<GagType, GagDrawData> GagEquipData { get; set; } = new();


    public GagStorage()
    {
        GagEquipData = Enum.GetValues(typeof(GagType)).Cast<GagType>()
            .ToDictionary(gagType => gagType, gagType => new GagDrawData(ItemIdVars.NothingItem(EquipSlot.Head)));
    }

    public Dictionary<GagType, AppliedSlot> GetAppliedSlotGagData()
    {
        return GagEquipData
            .Where(kvp => kvp.Value.IsEnabled && kvp.Value.GameItem.ItemId != ItemIdVars.NothingItem(kvp.Value.Slot).ItemId)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToAppliedSlot());
    }
}
