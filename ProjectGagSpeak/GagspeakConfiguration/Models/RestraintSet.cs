using Dalamud.Utility;
using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.Utils;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data.Struct;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
#nullable enable
namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
[Serializable]
public record RestraintSet : IMoodlesAssociable, IPadlockable
{
    public RestraintSet()
    {
        // Initialize DrawData in the constructor
        DrawData = EquipSlotExtensions.EqdpSlots.ToDictionary(
            slot => slot, slot => new EquipDrawData(ItemIdVars.NothingItem(slot)) { Slot = slot, IsEnabled = false });

        // Initialize BonusDrawData in the constructor
        BonusDrawData = BonusExtensions.AllFlags.ToDictionary(
            slot => slot, slot => new BonusDrawData(EquipItem.BonusItemNothing(slot)) { Slot = slot, IsEnabled = false });
    }

    public Guid RestraintId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Restraint Set";
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
    public string EnabledBy { get; set; } = string.Empty;

    public string Padlock { get; set; } = Padlocks.None.ToName();
    public string Password { get; set; } = string.Empty;
    public DateTimeOffset Timer { get; set; } = DateTimeOffset.MinValue;
    public string Assigner { get; set; } = string.Empty;
    public bool ForceHeadgear { get; set; } = false;
    public bool ForceVisor { get; set; } = false;
    public bool ApplyCustomizations { get; set; } = false;
    public Dictionary<EquipSlot, EquipDrawData> DrawData { get; set; } = [];
    public Dictionary<BonusItemFlag, BonusDrawData> BonusDrawData { get; set; } = [];
    public JToken CustomizeObject { get; set; } = new JObject();
    public JToken ParametersObject { get; set; } = new JObject();

    // any Mods to apply with this set, and their respective settings.
    public List<AssociatedMod> AssociatedMods { get; private set; } = new List<AssociatedMod>();

    // the list of Moodles to apply when the set is active, and remove when inactive.
    public List<Guid> AssociatedMoodles { get; set; } = new List<Guid>();
    public Guid AssociatedMoodlePreset { get; set; } = Guid.Empty;

    // Spatial Audio Sound Type to use while this restraint set is active. [WIP]

    /// <summary> 
    /// If a key for a pair exists here, they are allowed to view the set.
    /// If any properties for a key are enabled, they are applied when enabled by that pair.
    /// </summary>
    public Dictionary<string, HardcoreTraits> SetTraits { get; set; } = new Dictionary<string, HardcoreTraits>();
    
    public bool IsLocked() => Padlock != Padlocks.None.ToName();
    public bool HasTimerExpired() => Timer < DateTimeOffset.UtcNow;
    public int EquippedSlotsTotal => DrawData.Count(kvp => kvp.Value.GameItem.ItemId != ItemIdVars.NothingItem(kvp.Key).ItemId);
    public bool HasPropertiesForUser(string uid) => SetTraits.ContainsKey(uid);
    public bool PropertiesEnabledForUser(string uid) => HasPropertiesForUser(uid) && SetTraits[uid].AnyEnabled();
    public LightRestraintData ToLightData()
    {
        return new LightRestraintData()
        {
            Identifier = RestraintId,
            Name = Name,
            Description = Description,
            HardcoreTraits = SetTraits,
            AffectedSlots = DrawData
                .Where(kvp => kvp.Value.IsEnabled && kvp.Value.GameItem.Id != ItemIdVars.NothingItem(kvp.Key).Id)
                .Select(kvp => new AppliedSlot()
                {
                    Slot = (byte)kvp.Key,
                    CustomItemId = kvp.Value.GameItem.Id.Id,
                    Tooltip = "This Slot is Locked! --SEP--An active Restraint set is occupying this slot as part of its set!",
                })
                .ToList()
        };
    }

    public RestraintSet DeepCloneSet()
    {
        // Clone basic properties
        var clonedSet = new RestraintSet()
        {
            // do not clone guid
            Name = this.Name,
            Description = this.Description,
            Enabled = this.Enabled,
            EnabledBy = this.EnabledBy,
            Padlock = this.Padlock,
            Password = this.Password,
            Timer = this.Timer,
            Assigner = this.Assigner,
            ForceHeadgear = this.ForceHeadgear,
            ForceVisor = this.ForceVisor,
            ApplyCustomizations = this.ApplyCustomizations,
            CustomizeObject = this.CustomizeObject,
            ParametersObject = this.ParametersObject,
            AssociatedMods = new List<AssociatedMod>(this.AssociatedMods.Select(mod => mod.DeepClone())),
            AssociatedMoodles = new List<Guid>(this.AssociatedMoodles),
            AssociatedMoodlePreset = this.AssociatedMoodlePreset,
            SetTraits = new Dictionary<string, HardcoreTraits>(this.SetTraits)
        };

        // Deep clone DrawData
        clonedSet.DrawData = new Dictionary<EquipSlot, EquipDrawData>(
            this.DrawData.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.DeepCloneDrawData()));

        // Deep clone BonusDrawData
        clonedSet.BonusDrawData = new Dictionary<BonusItemFlag, BonusDrawData>(
            this.BonusDrawData.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.DeepClone()));

        return clonedSet;
    }


    // parameterless constructor for serialization
    public JObject Serialize()
    {
        var serializer = new JsonSerializer();
        // for the DrawData dictionary.
        JObject drawDataEquipmentObject = new JObject();
        // serialize each item in it
        foreach (var pair in DrawData)
        {
            drawDataEquipmentObject[pair.Key.ToString()] = new JObject()
            {
                ["Slot"] = pair.Value.Slot.ToString(),
                ["IsEnabled"] = pair.Value.IsEnabled,
                ["CustomItemId"] = pair.Value.GameItem.Id.ToString(),
                ["GameStain"] = pair.Value.GameStain.ToString(),
            };
        }

        // for the BonusDrawData dictionary.
        var bonusDrawDataArray = new JArray();
        // serialize each item in it
        foreach (var pair in BonusDrawData)
        {
            bonusDrawDataArray.Add(new JObject()
            {
                ["BonusItemFlag"] = pair.Key.ToString(),
                ["BonusDrawData"] = pair.Value.Serialize()
            });
        }

        // for the AssociatedMods list.
        var associatedModsArray = new JArray();
        // serialize each item in it
        foreach (var mod in AssociatedMods)
        {
            associatedModsArray.Add(mod.Serialize());
        }

        // for the set properties
        var setPropertiesArray = new JArray();
        // serialize each item in it
        var setPropertiesObject = JObject.FromObject(SetTraits);

        // Ensure Customize & Parameters are correctly serialized

        return new JObject()
        {
            ["RestraintId"] = RestraintId.ToString(),
            ["Name"] = Name,
            ["Description"] = Description,
            ["Enabled"] = Enabled,
            ["EnabledBy"] = EnabledBy,
            ["Padlock"] = Padlock,
            ["Password"] = Password,
            ["Timer"] = Timer.UtcDateTime.ToString("o"),
            ["Assigner"] = Assigner,
            ["ForceHeadgear"] = ForceHeadgear,
            ["ForceVisor"] = ForceVisor,
            ["ApplyCustomizations"] = ApplyCustomizations,
            ["CustomizeObject"] = CustomizeObject,
            ["ParametersObject"] = ParametersObject,
            ["DrawData"] = drawDataEquipmentObject,
            ["BonusDrawData"] = bonusDrawDataArray,
            ["AssociatedMods"] = associatedModsArray,
            ["AssociatedMoodles"] = new JArray(AssociatedMoodles),
            ["AssociatedMoodlePresets"] = AssociatedMoodlePreset,
            ["SetTraits"] = setPropertiesObject
        };
    }


    public void Deserialize(JObject jsonObject)
    {
        RestraintId = Guid.TryParse(jsonObject["RestraintId"]?.Value<string>(), out var guid) ? guid : Guid.Empty;
        Name = jsonObject["Name"]?.Value<string>() ?? string.Empty;
        Description = jsonObject["Description"]?.Value<string>() ?? string.Empty;
        Enabled = jsonObject["Enabled"]?.Value<bool>() ?? false;
        EnabledBy = jsonObject["EnabledBy"]?.Value<string>() ?? string.Empty;
        Padlock = jsonObject["Padlock"]?.Value<string>() ?? Padlocks.None.ToName();
        if (Padlock.IsNullOrEmpty()) Padlock = Padlocks.None.ToName(); ; // correct lockType if it is null or empty
        Password = jsonObject["Password"]?.Value<string>() ?? string.Empty;

        var dateTime = jsonObject["Timer"]?.Value<DateTime>() ?? DateTime.MinValue;
        Timer = new DateTimeOffset(dateTime, TimeSpan.Zero); // Zero indicates UTC

        Assigner = jsonObject["Assigner"]?.Value<string>() ?? string.Empty;
        ForceHeadgear = jsonObject["ForceHeadgear"]?.Value<bool>() ?? false;
        ForceVisor = jsonObject["ForceVisor"]?.Value<bool>() ?? false;
        ApplyCustomizations = jsonObject["ApplyCustomizations"]?.Value<bool>() ?? false;
        CustomizeObject = jsonObject["CustomizeObject"] ?? new JObject();
        ParametersObject = jsonObject["ParametersObject"] ?? new JObject();
        try
        {
            var drawDataObject = jsonObject["DrawData"]?.Value<JObject>();
            if (drawDataObject != null)
            {
                foreach (var property in drawDataObject.Properties())
                {
                    var equipmentSlot = (EquipSlot)Enum.Parse(typeof(EquipSlot), property.Name);
                    var itemObject = property.Value.Value<JObject>();
                    if (itemObject != null)
                    {
                        ulong customItemId = itemObject["CustomItemId"]?.Value<ulong>() ?? 4294967164;
                        var gameStainString = itemObject["GameStain"]?.Value<string>() ?? "0,0";
                        var stainParts = gameStainString.Split(',');

                        StainIds gameStain;
                        if (stainParts.Length == 2 && int.TryParse(stainParts[0], out int stain1) && int.TryParse(stainParts[1], out int stain2))
                            gameStain = new StainIds((StainId)stain1, (StainId)stain2);
                        else
                            gameStain = StainIds.None;

                        var drawData = new EquipDrawData(ItemIdVars.NothingItem(equipmentSlot))
                        {
                            Slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), itemObject["Slot"]?.Value<string>() ?? string.Empty),
                            IsEnabled = itemObject["IsEnabled"]?.Value<bool>() ?? false,
                            GameItem = ItemIdVars.Resolve(equipmentSlot, new CustomItemId(customItemId)),
                            GameStain = gameStain
                        };

                        DrawData[equipmentSlot] = drawData;
                    }
                }
            }

            // Deserialize the BonusDrawData
            if (jsonObject["BonusDrawData"] is JObject bonusDrawDataObj)
            {
                foreach (var (slot, bonusDrawData) in BonusDrawData)
                {
                    if (bonusDrawDataObj[slot.ToString()] is JObject slotObj)
                    {
                        bonusDrawData.Deserialize(slotObj);
                        // add it to the bonus draw data
                        BonusDrawData[slot] = bonusDrawData;
                    }
                }
            }

            // Deserialize the AssociatedMods
            if (jsonObject["AssociatedMods"] is JArray associatedModsArray)
                AssociatedMods = associatedModsArray.Select(mod => mod.ToObject<AssociatedMod>()).Where(mod => mod != null).ToList()!;
            else
                AssociatedMods = new List<AssociatedMod>();

            // Deserialize the AssociatedMoodles
            if (jsonObject["AssociatedMoodles"] is JArray associatedMoodlesArray)
                AssociatedMoodles = associatedMoodlesArray.Select(moodle => Guid.Parse(moodle.Value<string>() ?? string.Empty)).ToList();

            // Deserialize the AssociatedMoodlePreset
            var moodlePreset = jsonObject["AssociatedMoodlePresets"];
            if (moodlePreset is JArray)
                AssociatedMoodlePreset = Guid.Empty;
            else if (Guid.TryParse(moodlePreset?.Value<string>(), out var preset))
                AssociatedMoodlePreset = preset;
            else
                AssociatedMoodlePreset = Guid.Empty;

            // It should be JObject, so convert if it is not.
            if (jsonObject["SetTraits"] is JArray dummyJArray)
            {
                SetTraits = new Dictionary<string, HardcoreTraits>();
            }
            if (jsonObject["SetTraits"] is JObject setPropertiesObj)
            {
                SetTraits = setPropertiesObj.ToObject<Dictionary<string, HardcoreTraits>>() ?? new Dictionary<string, HardcoreTraits>();
            }
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogError($"Failed to deserialize RestraintSet {Name} {e}");
            throw new Exception($"Failed to deserialize RestraintSet {Name} {e}");
        }
    }
}
