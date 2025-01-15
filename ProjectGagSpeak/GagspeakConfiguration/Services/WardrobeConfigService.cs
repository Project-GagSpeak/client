using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
#nullable disable
namespace GagSpeak.GagspeakConfiguration;

public class WardrobeConfigService : ConfigurationServiceBase<WardrobeConfig>
{
    public const string ConfigName = "wardrobe.json";
    public const bool PerCharacterConfig = true;
    public WardrobeConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

    // apply an override for migrations off the base config service
    protected override JObject MigrateConfig(JObject oldConfigJson, int readVersion)
    {
        if (readVersion < 1)
        {
            oldConfigJson = MigrateFromV0toV1(oldConfigJson);
        }

        return oldConfigJson;
    }

    private JObject MigrateFromV0toV1(JObject oldConfigJson)
    {
        var restraintSetsArray = oldConfigJson["WardrobeStorage"]?["RestraintSets"]?.Value<JArray>();
        if (restraintSetsArray != null)
        {
            foreach (var item in restraintSetsArray)
            {
                var itemValue = item.Value<JObject>();
                if (itemValue != null)
                {
                    if (itemValue["LockType"] != null)
                    {
                        itemValue["Padlock"] = itemValue["LockType"];
                        itemValue.Remove("LockType");
                    }
                    if (itemValue["LockPassword"] != null)
                    {
                        itemValue["Password"] = itemValue["LockPassword"];
                        itemValue.Remove("LockPassword");
                    }
                    if (itemValue["LockedUntil"] != null)
                    {
                        itemValue["Timer"] = itemValue["LockedUntil"];
                        itemValue.Remove("LockedUntil");
                    }
                    if (itemValue["LockedBy"] != null)
                    {
                        itemValue["Assigner"] = itemValue["LockedBy"];
                        itemValue.Remove("LockedBy");
                    }
                }
            }
        }

        oldConfigJson["Version"] = 1;
        return oldConfigJson;
    }

    protected override WardrobeConfig DeserializeConfig(JObject configJson)
    {
        var config = new WardrobeConfig();
        // Deserialize WardrobeStorage
        JToken wardrobeStorageToken = configJson["WardrobeStorage"];
        if (wardrobeStorageToken != null)
        {
            WardrobeStorage wardrobeStorage = new WardrobeStorage();

            var restraintSetsArray = wardrobeStorageToken["RestraintSets"]?.Value<JArray>();
            if (restraintSetsArray == null)
            {
                throw new Exception("RestraintSets property is missing in RestraintSets.json");
            }

            // we are in version 1, so we should deserialize appropriately
            foreach (var item in restraintSetsArray)
            {
                var restraintSet = new RestraintSet();
                var ItemValue = item.Value<JObject>();
                if (ItemValue != null)
                {
                    restraintSet.Deserialize(ItemValue);
                    wardrobeStorage.RestraintSets.Add(restraintSet);
                }
                else
                {
                    throw new Exception("restraint set contains invalid property");
                }
            }

            // Assuming BlindfoldInfo follows a similar pattern
            JToken blindfoldInfoToken = wardrobeStorageToken["BlindfoldInfo"];
            if (blindfoldInfoToken != null)
            {
                BlindfoldModel blindfoldModel = new BlindfoldModel();
                blindfoldModel.Deserialize((JObject)blindfoldInfoToken);
                wardrobeStorage.BlindfoldInfo = blindfoldModel;
            }
            else
            {
                wardrobeStorage.BlindfoldInfo = new BlindfoldModel();
            }

            config.WardrobeStorage = wardrobeStorage; // loads the wardrobe storage into the stored config.
        }
        return config;
    }

    protected override string SerializeConfig(WardrobeConfig config)
    {
        // serialize here.
        JObject configObject = new JObject
        {
            ["Version"] = config.Version,
            ["WardrobeStorage"] = new JObject()
        };

        JArray restraintSetsArray = new JArray();
        foreach (RestraintSet restraintSet in config.WardrobeStorage.RestraintSets)
        {
            restraintSetsArray.Add(restraintSet.Serialize());
        }
        configObject["WardrobeStorage"]["RestraintSets"] = restraintSetsArray;

        // Use Serialize method for BlindfoldInfo
        JObject blindfoldInfoObject = config.WardrobeStorage.BlindfoldInfo.Serialize();

        // Add blindfoldInfoObject to the WardrobeStorage JObject
        configObject["WardrobeStorage"]["BlindfoldInfo"] = blindfoldInfoObject;

        return configObject.ToString(Formatting.Indented);
    }
}
