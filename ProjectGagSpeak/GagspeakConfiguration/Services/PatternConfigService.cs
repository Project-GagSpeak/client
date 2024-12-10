using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration;

public class PatternConfigService : ConfigurationServiceBase<PatternConfig>
{
    public const string ConfigName = "patterns.json";
    public const bool PerCharacterConfig = false;
    public PatternConfigService(string configDir) : base(configDir) { }

    protected override bool PerCharacterConfigPath => PerCharacterConfig;
    protected override string ConfigurationName => ConfigName;


    // apply an override for migrations off the baseconfigservice
    protected override JObject MigrateConfig(JObject oldConfigJson, int readVersion)
    {
        JObject newConfigJson;

        // Check the version of the config file
        switch (readVersion)
        {
            case 0:
                newConfigJson = MigrateFromV0toV1(oldConfigJson);
                break;
            default:
                // no migration needed
                newConfigJson = oldConfigJson;
                break;
        }
        // return the updated config
        return newConfigJson;
    }

    // Safely update data for new format.
    // Migration function to handle changes from version 1 to version 2
    private JObject MigrateFromV0toV1(JObject oldConfigJson)
    {
        // Create a new JObject for the updated config
        JObject newConfigJson = new JObject
        {
            ["Version"] = 1 // Set the new version number
        };

        // Ensure PatternStorage exists in the old config
        if (oldConfigJson["PatternStorage"] is JObject oldPatternStorage)
        {
            // Create a new PatternStorage JObject
            JObject newPatternStorage = new JObject();

            // Ensure Patterns list exists in the old PatternStorage
            if (oldPatternStorage["Patterns"] is JArray oldPatternsArray)
            {
                JArray newPatternsArray = new JArray();

                foreach (var pattern in oldPatternsArray)
                {
                    if (pattern is JObject patternObject)
                    {
                        // Create a new JObject for the migrated pattern
                        JObject newPatternObject = new JObject(patternObject);

                        // Remove the fields that are no longer needed
                        newPatternObject.Remove("Author");
                        newPatternObject.Remove("Tags");
                        newPatternObject.Remove("CreatedByClient");
                        newPatternObject.Remove("IsPublished");

                        // Add the migrated pattern to the new array
                        newPatternsArray.Add(newPatternObject);
                    }
                }

                // Add the updated Patterns array to the new PatternStorage
                newPatternStorage["Patterns"] = newPatternsArray;
            }

            // Add the updated PatternStorage to the new config
            newConfigJson["PatternStorage"] = newPatternStorage;
        }

        return newConfigJson;
    }


    protected override PatternConfig DeserializeConfig(JObject configJson)
    {
        PatternConfig config = new PatternConfig();

        // create a new Pattern Storage object
        config.PatternStorage = new PatternStorage();
        // create a new list of patternData inside it
        config.PatternStorage.Patterns = new List<PatternData>();

        // read in the pattern data from the config file
        var PatternsList = configJson["PatternStorage"]!["Patterns"] as JArray ?? new JArray();
        // if the patterns list had data
        if (PatternsList != null)
        {
            // then for each pattern in the list
            foreach (var pattern in PatternsList)
            {
                // Ensure the pattern is a JObject and not null
                if (pattern is JObject patternObject)
                {
                    // Create a new pattern object
                    var patternData = new PatternData();
                    // Deserialize the object
                    patternData.Deserialize(patternObject);
                    // Add the pattern to the list
                    config.PatternStorage.Patterns.Add(patternData);
                }
            }
        }
        return config;
    }

    protected override string SerializeConfig(PatternConfig config)
    {
        JObject configObject = new JObject()
        {
            ["Version"] = config.Version
        };

        // create the array to write to
        JArray patternsArray = new JArray();
        // for each of the patterns in the pattern storage
        foreach (PatternData pattern in config.PatternStorage.Patterns)
        {
            // add the serialized pattern to the array
            patternsArray.Add(pattern.Serialize());
        }

        // add the patterns array to the config object
        configObject["PatternStorage"] = new JObject
        {
            ["Patterns"] = patternsArray
        };

        return configObject.ToString(Formatting.Indented);
    }
}
