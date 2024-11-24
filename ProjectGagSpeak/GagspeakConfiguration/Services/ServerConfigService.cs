using GagSpeak.GagspeakConfiguration.Configurations;

namespace GagSpeak.GagspeakConfiguration;

public class ServerConfigService : ConfigurationServiceBase<ServerConfig>
{
    public const string ConfigName = "server.json";
    public const bool PerCharacterConfig = false;
    public ServerConfigService(string configDir) : base(configDir) { }

    protected override bool PerCharacterConfigPath => PerCharacterConfig;
    protected override string ConfigurationName => ConfigName;

    // apply an override for migrations off the baseconfigservice
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
