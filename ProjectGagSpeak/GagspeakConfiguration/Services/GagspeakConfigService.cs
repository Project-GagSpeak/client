using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.Hardcore.ForcedStay;

namespace GagSpeak.GagspeakConfiguration;
public class GagspeakConfigService : ConfigurationServiceBase<GagspeakConfig>
{
    public const string ConfigName = "config-testing.json";
    public const bool PerCharacterConfig = false;
    public GagspeakConfigService(string configDir) : base(configDir) { }

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
}
