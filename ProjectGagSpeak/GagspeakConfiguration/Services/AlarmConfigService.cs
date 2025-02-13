using GagSpeak.GagspeakConfiguration.Configurations;

namespace GagSpeak.GagspeakConfiguration;

public class AlarmConfigService : ConfigurationServiceBase<AlarmConfig>
{
    public const string ConfigName = "alarms.json";
    public const bool PerCharacterConfig = true;

    public AlarmConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

    protected override JObject MigrateConfig(JObject oldConfigJson)
    {
        var readVersion = oldConfigJson["Version"]?.Value<int>() ?? 0;

        // Perform any migrations necessary.
        return oldConfigJson;
    }

    // Safely update data for new format. (Whenever we need it)
    private JObject MigrateFromV0toV1(JObject oldConfigJson)
    {
        // create a new JObject to store the new config
        JObject newConfigJson = new();
        // set the version to 1
        newConfigJson["Version"] = 1;

        return oldConfigJson;
    }

    // already does deserialization and serialization for us.
}
