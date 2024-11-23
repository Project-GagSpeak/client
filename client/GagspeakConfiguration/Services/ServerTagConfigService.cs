using GagSpeak.GagspeakConfiguration.Configurations;

namespace GagSpeak.GagspeakConfiguration;
public class ServerTagConfigService : ConfigurationServiceBase<ServerTagConfig>
{
    public const string ConfigName = "servertags.json";
    public const bool PerCharacterConfig = false;
    public ServerTagConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

}
