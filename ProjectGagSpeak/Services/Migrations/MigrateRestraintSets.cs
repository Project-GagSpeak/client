using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Utils;
using GagspeakAPI.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Services;

// handles the global chat and pattern discovery social features.
public class MigrateRestraintSets
{
    private readonly ILogger<MigrateRestraintSets> _logger;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly string _oldRestraintSetsDirectory;
    public MigrateRestraintSets(ILogger<MigrateRestraintSets> logger,
        ClientConfigurationManager clientConfigs, string configDirectory)
    {
        _logger = logger;
        _clientConfigs = clientConfigs;
        _oldRestraintSetsDirectory = Path.Combine(configDirectory, "..", "GagSpeak", "RestraintSets.json");
    }


    public bool OldRestraintSetsLoaded { get; private set; } = false;
    public OldRestraintSetStorage OldRestraintSets { get; private set; } = new OldRestraintSetStorage();
    public int SelectedRestraintSetIdx = 0;
    public OldRestraintSet TmpOldRestraintSet => OldRestraintSets.RestraintSets[SelectedRestraintSetIdx];

    public void LoadOldRestraintSets()
    {
        var oldRestraintSetsFetched = new OldRestraintSetStorage();

        if (!File.Exists(_oldRestraintSetsDirectory))
        {
            _logger.LogWarning($"Old pattern file not found at {_oldRestraintSetsDirectory}");
            OldRestraintSets = oldRestraintSetsFetched;
            return;
        }

        try
        {
            var text = File.ReadAllText(_oldRestraintSetsDirectory);
            var jsonObject = JObject.Parse(text);
            var restraintSetsArray = jsonObject["RestraintSets"]?.Value<JArray>();

            if (restraintSetsArray != null)
            {
                foreach (var item in restraintSetsArray)
                {
                    var restraintSet = new OldRestraintSet();
                    var itemValue = item.Value<JObject>();
                    if (itemValue != null)
                    {
                        restraintSet.Deserialize(itemValue);
                        oldRestraintSetsFetched.RestraintSets.Add(restraintSet);
                    }
                    else
                    {
                        _logger.LogError($"Array contains an invalid entry (it is null), skipping!");
                    }
                }
            }
            OldRestraintSetsLoaded = true;
            OldRestraintSets = oldRestraintSetsFetched;
        }
        catch (Exception ex)
        {
            OldRestraintSetsLoaded = false;
            OldRestraintSets = new OldRestraintSetStorage();
            _logger.LogError($"Error loading old restraint sets: {ex}");
        }
    }

    // append a new function that takes a old pattern at a spesified index, and constructs a new patternData object from it.
    public void AppendOldRestraintSetToStorage(int index)
    {
        // fetch the pattern from the old pattern storage at the indx.
        OldRestraintSet oldSet = OldRestraintSets.RestraintSets[index];
        // trim the old description to be a max of 150 characters.
        if (oldSet.Description.Length > 150)
        {
            oldSet.Description = oldSet.Description.Substring(0, 150);
        }
        // remove any \n from the description, or any \ characters.
        oldSet.Description = oldSet.Description.Replace("\n", "").Replace("\\", "");

        // construct a new RestraintSet object from the old one.
        RestraintSet newSet = new RestraintSet()
        {
            RestraintId = Guid.NewGuid(),
            Name = oldSet.Name,
            Description = oldSet.Description,
            Enabled = oldSet.Enabled,
            EnabledBy = oldSet.WasEnabledBy,
            Padlock = Padlocks.None.ToName(),
            Password = string.Empty,
            Timer = DateTimeOffset.MinValue,
            Assigner = string.Empty,
            DrawData = oldSet.DrawData
                .ToDictionary(kvp => kvp.Key, kvp => new EquipDrawData(kvp.Value.GameItem)
            {
                IsEnabled = kvp.Value.IsEnabled,
                Slot = kvp.Value.Slot,
                GameItem = kvp.Value.GameItem,
                GameStain = kvp.Value.GameStain
            })
        };

        // add the new RestraintSet object to the RestraintSetStorage.
        _clientConfigs.AddNewRestraintSet(newSet);
    }

    public void AppendAllOldRestraintSetToStorage()
    {
        var newRestraintSets = new List<RestraintSet>();
        foreach (var oldSet in OldRestraintSets.RestraintSets)
        {
            // trim the old description to be a max of 150 characters.
            if (oldSet.Description.Length > 150)
            {
                oldSet.Description = oldSet.Description.Substring(0, 150);
            }
            // remove any \n from the description, or any \ characters.
            oldSet.Description = oldSet.Description.Replace("\n", "").Replace("\\", "");

            // construct a new RestraintSet object from the old one.
            RestraintSet newSet = new RestraintSet()
            {
                RestraintId = Guid.NewGuid(),
                Name = oldSet.Name,
                Description = oldSet.Description,
                Enabled = oldSet.Enabled,
                EnabledBy = oldSet.WasEnabledBy,
                Padlock = Padlocks.None.ToName(),
                Password = string.Empty,
                Timer = DateTimeOffset.MinValue,
                Assigner = string.Empty,
                DrawData = oldSet.DrawData
                    .ToDictionary(kvp => kvp.Key, kvp => new EquipDrawData(kvp.Value.GameItem)
                    {
                        IsEnabled = kvp.Value.IsEnabled,
                        Slot = kvp.Value.Slot,
                        GameItem = kvp.Value.GameItem,
                        GameStain = kvp.Value.GameStain
                    })
            };

            // add the new RestraintSet object to the RestraintSetStorage.
            newRestraintSets.Add(newSet);
        }

        _clientConfigs.AddNewRestraintSets(newRestraintSets);
    }
}

// For storing the imported old data.
public class OldRestraintSetStorage
{
    public List<OldRestraintSet> RestraintSets = []; // stores the restraint sets
}

public class OldRestraintSet
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
    public bool Locked { get; set; }
    public string WasEnabledBy { get; set; }
    public string WasLockedBy { get; set; }
    public DateTimeOffset LockedTimer { get; set; }
    public Dictionary<EquipSlot, OldEquipDrawData> DrawData { get; set; } = new();

    public void Deserialize(JObject jsonObject)
    {
        Name = jsonObject["Name"]?.Value<string>() ?? string.Empty;
        Description = jsonObject["Description"]?.Value<string>() ?? string.Empty;
        Enabled = false;
        Locked = false;
        WasEnabledBy = string.Empty;
        WasLockedBy = string.Empty;
        LockedTimer = DateTimeOffset.Now;
        DrawData.Clear();
        var drawDataArray = jsonObject["DrawData"]?.Value<JArray>();
        if (drawDataArray != null)
        {
            foreach (var item in drawDataArray)
            {
                var itemObject = item.Value<JObject>();
                if (itemObject != null)
                {
                    var equipmentSlot = (EquipSlot)Enum.Parse(typeof(EquipSlot), itemObject["EquipmentSlot"]?.Value<string>() ?? string.Empty);
                    var drawData = new OldEquipDrawData(ItemIdVars.NothingItem(equipmentSlot));
                    drawData.Deserialize(itemObject["DrawData"]?.Value<JObject>() ?? new JObject());
                    DrawData.Add(equipmentSlot, drawData);
                }
            }
        }
    }
}

public class OldEquipDrawData
{
    public bool IsEnabled { get; set; }
    public string WasEquippedBy { get; set; }
    public bool Locked { get; set; }
    public int ActiveSlotListIdx { get; set; }
    public EquipSlot Slot { get; set; }
    public EquipItem GameItem { get; set; }
    public StainId GameStain { get; set; }

    public OldEquipDrawData(EquipItem item)
    {
        GameItem = item;
    }

    public void Deserialize(JObject jsonObject)
    {
        IsEnabled = false;
        WasEquippedBy = string.Empty;
        Locked = false;
        ActiveSlotListIdx = jsonObject["ActiveSlotListIdx"]?.Value<int>() ?? 0;
        Slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), jsonObject["Slot"]?.Value<string>() ?? string.Empty);
        ulong customItemId = jsonObject["GameItem"]!["Id"]?.Value<ulong>() ?? 4294967164;
        GameItem = ItemIdVars.Resolve(Slot, new CustomItemId(customItemId));
        // Parse the StainId
        if (byte.TryParse(jsonObject["GameStain"]?.Value<string>(), out var stainIdByte))
        {
            GameStain = new StainId(stainIdByte);
        }
        else
        {
            GameStain = new StainId(0);
        }
    }
}
