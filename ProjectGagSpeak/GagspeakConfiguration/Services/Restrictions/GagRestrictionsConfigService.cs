/*using FFXIVClientStructs.FFXIV.Client.Game;
using GagSpeak.CkCommons;
using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Restrictions;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.GagspeakConfiguration;

public class GagRestrictionsConfigService : ConfigurationServiceBase<GagRestrictionsConfig>
{
    public const string ConfigName = "gag-restrictions.json";
    public const bool PerCharacterConfig = true;
    public GagRestrictionsConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

    // apply an override for migrations off the base config service
    protected override JObject MigrateConfig(JObject oldConfigJson)
    {
        var readVersion = oldConfigJson["Version"]?.Value<int>() ?? 0;

        // Perform file migration if any exist.
        string gagStoragePath = Path.Combine(ConfigurationPath, "gag-storage.json");
        if (File.Exists(gagStoragePath))
            oldConfigJson = MigrateGagStorage(oldConfigJson);

        // Perform any migrations necessary.
        if (readVersion < 1)
    oldConfigJson = MigrateFromV0toV1(oldConfigJson);

        return oldConfigJson;
    }

    protected override GagRestrictionsConfig DeserializeConfig(JObject configJson)
{
    GagRestrictionsConfig config = new GagRestrictionsConfig();

    // Assuming GagStorage has a default constructor
    config.GagRestrictions = new GagRestrictionStorage();
    // Deserialize the `GagRestrictions` storage.
    if (configJson["GagRestrictions"]?["GagItems"] is JObject gagItemObject)
    {
        foreach (var gagData in gagItemObject)
        {
            // Parse the GagType from the key.
            if (Enum.TryParse(typeof(GagType), gagData.Key, out var gagTypeObj) && gagData.Value is JObject itemObject)
            {
                var gagType = (GagType)gagTypeObj;

                // Create and populate the GarblerRestriction object.
                var restriction = new GarblerRestriction() { Label = gagType.GagName() };
                restriction.LoadRestriction(itemObject);

                // Add the restriction to the GagRestrictions storage.
                config.GagRestrictions[gagType] = restriction;
            }
            else
            {
                // Log a warning if the key could not be parsed or the value is not a JObject.
                StaticLogger.Logger.LogWarning($"Could not parse GagType key or invalid data: {gagData.Key}");
            }
        }
    }
    return config;
}

protected override string SerializeConfig(GagRestrictionsConfig config)
{
    JObject configObject = new JObject()
    {
        ["Version"] = config.Version // Include the version of GagStorageConfig
    };

    // Create the JSON object for `GagRestrictions`.
    JObject gagRestrictionsObject = new JObject();

    // Iterate over each restriction in the storage and serialize it.
    foreach (var kvp in config.GagRestrictions)
    {
        GagType gagType = kvp.Key;
        GarblerRestriction restriction = kvp.Value;

        // Serialize the restriction object.
        gagRestrictionsObject[gagType.GagName()] = restriction.Serialize();
    }

    // Attach the serialized `GagRestrictions` to the main configuration object.
    configObject["GagRestrictions"] = new JObject
    {
        ["GagItems"] = gagRestrictionsObject
    };
    // Return the serialized configuration object.
    return configObject.ToString(Formatting.Indented);
}

// Safely update data for new format.
private JObject MigrateGagStorage(JObject oldConfigJson)
{
    // create a new JObject to store the new config
    JObject newConfigJson = new();
    // set the version to 0
    newConfigJson["Version"] = 0;

    // locate the file of gag-storage.json in the same ConfigurationPath.
    // If it exists, be sure to migrate it, then remove it after transfer.
    string gagStoragePath = Path.Combine(ConfigurationPath, "gag-storage.json");
    if (File.Exists(gagStoragePath))
    {
        string gagStorageJson = File.ReadAllText(gagStoragePath);
        JObject gagStorageObject = JObject.Parse(gagStorageJson);

        // Create a new storage for GagRestrictions
        JObject newGagStorageJson = new();
        foreach (var gagData in gagStorageObject["GagStorage"]!["GagEquipData"] as JObject ?? new JObject())
        {
            if (Enum.TryParse<GagType>(gagData.Key, out var gagType) && gagData.Value is JObject itemObject)
            {
                // Map the old data to the new GarblerRestriction structure
                ulong customItemId = itemObject["CustomItemId"]?.Value<ulong>() ?? 4294967164;

                var restriction = new GarblerRestriction
                {
                    Glamour = new GlamourSlot
                    {
                        Slot = Enum.TryParse<EquipSlot>(itemObject["Slot"]?.Value<string>(), out var slot) ? slot : EquipSlot.Head,
                        GameItem = ItemIdVars.Resolve(slot, new CustomItemId(customItemId)),
                        GameStain = StainIds.None, // Not dealing with this.
                    },
                    HeadgearState = JsonHelp.FromJObject(itemObject["ForceHeadgear"]),
                    VisorState = JsonHelp.FromJObject(itemObject["ForceVisor"]),
                    ProfileGuid = Guid.TryParse(itemObject["CustomizeGuid"]?.Value<string>(), out var profileGuid) ? profileGuid : Guid.Empty,
                    ProfilePriority = itemObject["CustomizePriority"]?.Value<uint>() ?? 0,
                    DoRedraw = itemObject["IsEnabled"]?.Value<bool>() ?? false
                };
                // After transforming it into the new format, serialize it into the json at that keys index.
                newGagStorageJson[gagData.Key] = restriction.Serialize();
            }
            else
            {
                // If the item is not present, create a fresh garbler restriction item at that key.
                newGagStorageJson[gagData.Key] = new GarblerRestriction().Serialize();
            }
        }
        // Add the new GagStorage to the new config
        newConfigJson["GagRestrictions"] = new JObject { ["GagItems"] = newGagStorageJson };

        // Remove the old gag-storage.json file
        File.Delete(gagStoragePath);
    }
    // Return the new config
    return newConfigJson;
}

private JObject MigrateV0toV1(JObject oldConfigJson) { return oldConfigJson; }
}
*/
