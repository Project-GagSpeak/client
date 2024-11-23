using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Utils;
#nullable disable
namespace GagSpeak.GagspeakConfiguration;

public class CursedLootConfigService : ConfigurationServiceBase<CursedLootConfig>
{
    public const string ConfigName = "cursedloot.json";
    public const bool PerCharacterConfig = true;
    public CursedLootConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

    protected override CursedLootConfig DeserializeConfig(JObject configJson)
    {
        var config = new CursedLootConfig();

        // deserialize the CursedLootStorage into a JToken.
        JToken cursedLootToken = configJson["CursedLootStorage"];
        if (cursedLootToken != null)
        {
            CursedLootStorage cursedLootStorage = new CursedLootStorage();

            // get the array of cursed loot items from the token
            var cursedLootItemsArray = cursedLootToken["CursedItems"]?.Value<JArray>();
            if (cursedLootItemsArray == null)
            {
                throw new Exception("CursedItems property is missing in cursedloot.json");
            }

            // create the new cursted item object from each item in the token array.
            foreach (var cursedItem in cursedLootItemsArray)
            {
                var readCursedItem = new CursedItem();
                var cursedItemValue = cursedItem.Value<JObject>();
                if (cursedItemValue != null)
                {
                    readCursedItem.Deserialize(cursedItemValue);
                    cursedLootStorage.CursedItems.Add(readCursedItem);
                }
                else
                {
                    throw new Exception("cursed item contains invalid property");
                }
            }

            cursedLootStorage.LockRangeLower = TimeSpan.TryParse(cursedLootToken["LockRangeLower"]?.Value<string>(), out var lower) ? lower : TimeSpan.Zero;
            cursedLootStorage.LockRangeUpper = TimeSpan.TryParse(cursedLootToken["LockRangeUpper"]?.Value<string>(), out var upper) ? upper : TimeSpan.FromMinutes(1);
            cursedLootStorage.LockChance = cursedLootToken["LockChance"]?.Value<int>() ?? 0;

            config.CursedLootStorage = cursedLootStorage;
        }
        return config;
    }

    protected override string SerializeConfig(CursedLootConfig config)
    {
        // serialize here.
        JObject configObject = new JObject
        {
            ["Version"] = config.Version,
            ["CursedLootStorage"] = new JObject()
        };

        JArray cursedItemsArray = new JArray();
        foreach (CursedItem cursedItem in config.CursedLootStorage.CursedItems)
        {
            cursedItemsArray.Add(cursedItem.Serialize());
        }
        configObject["CursedLootStorage"]["CursedItems"] = cursedItemsArray;

        // store the remaining variables.
        configObject["CursedLootStorage"]["LockRangeLower"] = config.CursedLootStorage.LockRangeLower.ToString();
        configObject["CursedLootStorage"]["LockRangeUpper"] = config.CursedLootStorage.LockRangeUpper.ToString();
        configObject["CursedLootStorage"]["LockChance"] = config.CursedLootStorage.LockChance;

        return configObject.ToString(Formatting.Indented);
    }
}
