using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.Interop.Ipc;
using GagSpeak.Restrictions;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
#nullable disable
namespace GagSpeak.GagspeakConfiguration;

public class RestraintsConfigService : ConfigurationServiceBase<RestraintsConfig>
{
    // Might move to an independent restraint system,
    // but if i do that I will need to rework the config service.
    public const string ConfigName = "restraints.json";
    public const bool PerCharacterConfig = true;
    public RestraintsConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

    // apply an override for migrations off the base config service
    protected override JObject MigrateConfig(JObject oldConfigJson)
    {
        var readVersion = oldConfigJson["Version"]?.Value<int>() ?? 0;

        // Perform file migration if any exist.
        string oldStoragePath = Path.Combine(ConfigurationPath, "wardrobe.json");
        if (File.Exists(oldStoragePath))
            oldConfigJson = MigrateWardrobeStorage(oldConfigJson);

        // Perform any migrations necessary.
        /*        if (readVersion < 1)
                    oldConfigJson = MigrateFromV0toV1(oldConfigJson);*/

        return oldConfigJson;
    }

    protected override RestraintsConfig DeserializeConfig(JObject configJson)
    {
        var config = new RestraintsConfig
        {
            Version = configJson["Version"]?.Value<int>() ?? 0
        };

        // Deserialize RestraintsStorage
        if (configJson["Restraints"] is JObject restraintsJson)
        {
            foreach (var setToken in restraintsJson["RestraintSets"] ?? new JArray())
            {
                if (setToken is JObject setObject)
                {
                    var restraintSet = new RestraintSet();
                    restraintSet.LoadRestraintSet(setObject);
                    config.Restraints.Add(restraintSet);
                }
                else
                {
                    throw new Exception("Invalid restraint set data in configuration.");
                }
            }
        }
        else
        {
            StaticLogger.Logger.LogWarning("No Restraints section found in the configuration.");
        }

        return config;
    }

    protected override string SerializeConfig(RestraintsConfig config)
    {
        var configObject = new JObject
        {
            ["Version"] = config.Version,
            ["Restraints"] = new JObject
            {
                ["RestraintSets"] = new JArray(
                    config.Restraints.Select(set => set.Serialize())
                )
            }
        };

        return configObject.ToString(Formatting.Indented);
    }

    // Safely update data for new format.
    private JObject MigrateFromV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
        return oldConfigJson;
    }

    // Safely update data for new format.
    private JObject MigrateWardrobeStorage(JObject oldConfigJson)
    {
        // create a new JObject to store the new config
        JObject newConfigJson = new();
        // set the version to 0
        newConfigJson["Version"] = 0;

        // locate the file of wardrobe.json in the same ConfigurationPath.
        // If it exists, be sure to migrate it, then remove it after transfer.
        string storagePath = Path.Combine(ConfigurationPath, "wardrobe.json");
        if (File.Exists(storagePath))
        {
            string storageJson = File.ReadAllText(storagePath);
            JObject storageObject = JObject.Parse(storageJson);

            // Create a new storage for Restraints
            JArray restraintsArray = new JArray();
            foreach (var jTok in storageObject["WardrobeStorage"]!["RestraintSets"] as JArray ?? new JArray())
            {
                // Get the BonusDrawData object (it's assumed to only contain "Glasses")
                var bonusDrawData = jTok["BonusDrawData"]?.FirstOrDefault() as JObject;
                var bonusSlotItem = bonusDrawData["BonusDrawData"]?["CustomItemId"]?.ToString();
                List<ModAssociation> mods = new List<ModAssociation>();
                List<Moodle> moodles = new List<Moodle>();
                if (jTok["AssociatedMods"] is JArray modsArray)
                    mods = modsArray.Select(mod =>
                    {
                        // Check if the data is in the new format
                        if (mod["Name"] != null && mod["Directory"] != null && mod["Enabled"] != null)
                        {
                            // New format: Deserialize directly into ModAssociation
                            var modAssociation = new ModAssociation(
                                new Mod(mod["Name"]?.ToString(), mod["Directory"]?.ToString()),
                                new ModSettings(
                                    mod["Settings"]?.ToObject<Dictionary<string, List<string>>>() ?? new Dictionary<string, List<string>>(),
                                    mod["Priority"]?.ToObject<int>() ?? 0,
                                    mod["Enabled"]?.ToObject<bool>() ?? false,
                                    false,
                                    false
                                )
                            );
                            return modAssociation;
                        }
                        else if (mod["Mod"] != null && mod["ModSettings"] != null)
                        {
                            // Old format: Extract fields and create a ModAssociation
                            var modInfo = new Mod(
                                mod["Mod"]?["Name"]?.ToString(),
                                mod["Mod"]?["DirectoryName"]?.ToString()
                            );

                            var settings = new ModSettings(
                                mod["ModSettings"]?["Settings"]?.ToObject<Dictionary<string, List<string>>>() ?? new Dictionary<string, List<string>>(),
                                mod["ModSettings"]?["Priority"]?.ToObject<int>() ?? 0,
                                mod["ModSettings"]?["Enabled"]?.ToObject<bool>() ?? false,
                                false,
                                false
                            );

                            return new ModAssociation(modInfo, settings);
                        }

                        // Log or ignore if neither format matches
                        StaticLogger.Logger.LogWarning("Unknown Mod format. Skipping...");
                        return null;
                    })
                    .Where(mod => mod != null) // Remove any null entries
                    .ToList() ?? new List<ModAssociation>();

                // Deserialize the AssociatedMoodles
                if (jTok["AssociatedMoodles"] is JArray moodlesArray)
                {
                    moodles = moodlesArray
                        .Select(moodle =>
                        {
                            var moodleId = Guid.TryParse(moodle.Value<string>(), out var id) ? id : Guid.Empty;
                            return new Moodle(MoodleType.Status, moodleId);
                        })
                        .Where(moodle => moodle.Id != Guid.Empty)
                        .ToList();
                }

                // Map the old data to the new RestraintSet structure
                var newRestraintSet = new RestraintSet()
                {
                    Identifier = Guid.TryParse(jTok["RestraintId"]?.Value<string>(), out var guid) ? guid : Guid.NewGuid(),
                    Name = jTok["Name"]?.Value<string>() ?? string.Empty,
                    Description = jTok["Description"]?.Value<string>() ?? string.Empty,
                    DoRedraw = false, // Default since it's not in the old format
                    RestraintSlots = new Dictionary<EquipSlot, RestraintSlotBase>(),
                    Glasses = new GlamourBonusSlot(BonusItemFlag.Glasses, EquipItem.BonusItemNothing(BonusItemFlag.Glasses)),
                    Layers = new List<RestraintLayer>(),
                    HeadgearState = OptionalBool.Null,
                    VisorState = OptionalBool.Null,
                    WeaponState = OptionalBool.Null,
                    RestraintMoodles = moodles,
                    RestraintMods = mods,
                };

                // After transforming it into the new format, serialize it into the json at that keys index.
                restraintsArray.Add(newRestraintSet.Serialize());
            }
            // Add the new RestraintsStorage to the new config
            newConfigJson["Restraints"] = new JArray { ["RestraintSets"] = restraintsArray };

            // remove the old wardrobe file if it exists.
            File.Delete(storagePath);
        }
        // Return the new config
        return newConfigJson;
    }

}
