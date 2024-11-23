using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;

namespace GagSpeak.GagspeakConfiguration;

public class GagStorageConfigService : ConfigurationServiceBase<GagStorageConfig>
{
    public const string ConfigName = "gag-storage.json";
    public const bool PerCharacterConfig = true;
    public GagStorageConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

    // apply an override for migrations off the baseconfigservice
    protected override JObject MigrateConfig(JObject oldConfigJson, int readVersion)
    {
        JObject newConfigJson;

        // no migration needed
        newConfigJson = oldConfigJson;
        return newConfigJson;
    }

    // Safely update data for new format.
    private JObject MigrateFromV0toV1(JObject oldConfigJson)
    {
        // create a new JObject to store the new config
        JObject newConfigJson = new();
        // set the version to 1
        newConfigJson["Version"] = 1;

        return oldConfigJson;
    }

    protected override GagStorageConfig DeserializeConfig(JObject configJson)
    {
        GagStorageConfig config = new GagStorageConfig();

        // Assuming GagStorage has a default constructor
        config.GagStorage = new GagStorage();

        JObject gagEquipDataObject = configJson["GagStorage"]!["GagEquipData"] as JObject ?? new JObject();
        if (gagEquipDataObject == null) return config;

        foreach (var gagData in gagEquipDataObject)
        {
            // Try to parse GagType directly from the key
            if (gagData.Key.IsValidGagName() && gagData.Value is JObject itemObject)
            {
                var gagType = Enum.GetValues(typeof(GagType))
                    .Cast<GagType>()
                    .FirstOrDefault(gt => gt.GagName() == gagData.Key);


                string? slotString = itemObject["Slot"]?.Value<string>() ?? "Head";
                EquipSlot slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), slotString);
                var gagDrawData = new GagDrawData(ItemIdVars.NothingItem(slot));
                gagDrawData.Deserialize(itemObject);
                if(config.GagStorage.GagEquipData.ContainsKey(gagType))
                {
                    config.GagStorage.GagEquipData[gagType] = gagDrawData;
                }
                else
                {
                    config.GagStorage.GagEquipData.Add(gagType, gagDrawData);
                }
            }
            else
            {
                // Log a warning if the key could not be parsed
                StaticLogger.Logger.LogWarning($"Warning: Could not parse GagType key: {gagData.Key}");
            }
        }
        return config;
    }

    protected override string SerializeConfig(GagStorageConfig config)
    {
        JObject configObject = new JObject()
        {
            ["Version"] = config.Version // Include the version of GagStorageConfig
        };
        JObject gagEquipDataObject = new JObject();

        foreach (var kvp in config.GagStorage.GagEquipData)
        {
            gagEquipDataObject[kvp.Key.GagName()] = kvp.Value.Serialize();
        }

        configObject["GagStorage"] = new JObject
        {
            ["GagEquipData"] = gagEquipDataObject
        };

        return configObject.ToString(Formatting.Indented);
    }
}
