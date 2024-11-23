using GagspeakAPI.Data.Struct;
using GagspeakAPI.Enums;
using MessagePack;

namespace GagspeakAPI.Data.Character;

/// <summary>
/// A Data class containing lightweight storage data of each modules stored information.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class CharaStorageData
{
    /// <summary>
    /// Lightweight GagStorage. Will only contain information for gag types with set glamours.
    /// </summary>
    public Dictionary<GagType, AppliedSlot> GagItems { get; set; } = new Dictionary<GagType, AppliedSlot>();
    
    /// <summary>
    /// Lightweight RestraintList Storage
    /// </summary>
    public List<LightRestraintData> Restraints { get; set; } = new List<LightRestraintData>();

    /// <summary>
    /// Blindfold Affected Item
    /// </summary>
    public AppliedSlot BlindfoldItem { get; set; } = new AppliedSlot();

    /// <summary>
    /// Lightweight CursedItems Storage
    /// </summary>
    public List<LightCursedItem> CursedItems { get; set; } = new List<LightCursedItem>();

    /// <summary>
    /// Lightweight Pattern Storage
    /// </summary>
    public List<LightPattern> Patterns { get; set; } = new List<LightPattern>();

    /// <summary>
    /// Lightweight Alarm Storage
    /// </summary>
    public List<LightAlarm> Alarms { get; set; } = new List<LightAlarm>();

    /// <summary>
    /// Lightweight Trigger Storage.
    /// </summary>
    public List<LightTrigger> Triggers { get; set; } = new List<LightTrigger>();
}
