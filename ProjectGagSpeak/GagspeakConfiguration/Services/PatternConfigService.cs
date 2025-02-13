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

    protected override PatternConfig DeserializeConfig(JObject configJson)
    {
        PatternConfig config = new PatternConfig();

        // create a new Pattern Storage object
        config.PatternStorage = new PatternStorage();
        config.PatternStorage.Patterns = new List<Pattern>();

        // read in the pattern data from the config file
        var PatternsList = configJson["PatternStorage"]!["Patterns"] as JArray ?? new JArray();
        // if the patterns list had data
        if (PatternsList != null)
        {
            // then for each pattern in the list
            foreach (var pattern in PatternsList)
            {
                if (pattern is JObject patternObject)
                {
                    var patternData = new Pattern();
                    patternData.Deserialize(patternObject);
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

        JArray patternsArray = new JArray();
        foreach (Pattern pattern in config.PatternStorage.Patterns)
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
