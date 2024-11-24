using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.UI.Components;
using GagSpeak.Utils;
using GagspeakAPI.Data.Struct;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary> Model for the draw data of a players equipment slot </summary>
/// <param name="gameItem"> the game item we are storing the drawdata of.</param>
[Serializable]
public record GagDrawData : IMoodlesAssociable, IGlamourItem
{
    public bool IsEnabled { get; set; } = true;
    public EquipSlot Slot { get; set; } = EquipSlot.Head;
    public EquipItem GameItem { get; set; }
    public StainIds GameStain { get; set; } = StainIds.None;
    public bool ForceHeadgear { get; set; } = false;
    public bool ForceVisor { get; set; } = false;

    // List of Moodles to apply while Gagged.
    public List<Guid> AssociatedMoodles { get; set; } = new List<Guid>();
    public Guid AssociatedMoodlePreset { get; set; } = Guid.Empty;

    // C+ Preset to force if not Guid.Empty
    public uint CustomizePriority { get; set; } = 0;
    public Guid CustomizeGuid { get; set; } = Guid.Empty;

    // Spatial Audio type to use while gagged. (May not use since will just have one type?)

    public GagDrawData(EquipItem gameItem) => GameItem = gameItem;

    public AppliedSlot ToAppliedSlot()
    {
        return new AppliedSlot()
        {
            Slot = (byte)Slot,
            CustomItemId = GameItem.Id.Id,
            Tooltip = "This Slot is Locked! --SEP--An equipped gag has its glamour occupying this slot!",
        };
    }


    public JObject Serialize()
    {
        return new JObject()
        {
            ["IsEnabled"] = IsEnabled,
            ["ForceHeadgear"] = ForceHeadgear,
            ["ForceVisor"] = ForceVisor,
            ["GagMoodles"] = new JArray(AssociatedMoodles),
            ["GagMoodlePresets"] = AssociatedMoodlePreset,
            ["CustomizePriority"] = CustomizePriority,
            ["CustomizeGuid"] = CustomizeGuid,
            ["Slot"] = Slot.ToString(),
            ["CustomItemId"] = GameItem.Id.ToString(),
            ["GameStain"] = GameStain.ToString(),
        };
    }

    public void Deserialize(JObject jsonObject)
    {
        IsEnabled = jsonObject["IsEnabled"]?.Value<bool>() ?? false;
        ForceHeadgear = jsonObject["ForceHeadgear"]?.Value<bool>() ?? false;
        ForceVisor = jsonObject["ForceVisor"]?.Value<bool>() ?? false;

        // Deserialize the AssociatedMoodles
        if (jsonObject["GagMoodles"] is JArray associatedMoodlesArray)
            AssociatedMoodles = associatedMoodlesArray.Select(moodle => Guid.Parse(moodle.Value<string>() ?? string.Empty)).ToList();

        // Deserialize the AssociatedMoodlePreset TODO: Remove this on full release (the array check)
        var gagMoodlePresetsToken = jsonObject["GagMoodlePresets"];
        if (gagMoodlePresetsToken is JArray)
            AssociatedMoodlePreset = Guid.Empty;
        else if (Guid.TryParse(gagMoodlePresetsToken?.Value<string>(), out var preset))
            AssociatedMoodlePreset = preset;
        else
            AssociatedMoodlePreset = Guid.Empty;

        CustomizePriority = jsonObject["CustomizePriority"]?.Value<uint>() ?? 0;
        CustomizeGuid = Guid.TryParse(jsonObject["CustomizeGuid"]?.Value<string>(), out var guid) ? guid : Guid.Empty;

        Slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), jsonObject["Slot"]?.Value<string>() ?? string.Empty);
        ulong customItemId = jsonObject["CustomItemId"]?.Value<ulong>() ?? 4294967164;
        GameItem = ItemIdVars.Resolve(Slot, new CustomItemId(customItemId));

        // Parse the StainId
        var gameStainString = jsonObject["GameStain"]?.Value<string>() ?? "0,0";
        var stainParts = gameStainString.Split(',');
        if (stainParts.Length == 2 && int.TryParse(stainParts[0], out int stain1) && int.TryParse(stainParts[1], out int stain2))
        {
            GameStain = new StainIds((StainId)stain1, (StainId)stain2);
        }
        else
        {
            GameStain = StainIds.None;
        }
    }
}

