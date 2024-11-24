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
