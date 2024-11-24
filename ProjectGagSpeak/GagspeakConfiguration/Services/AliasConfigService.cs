using GagSpeak.GagspeakConfiguration.Configurations;

namespace GagSpeak.GagspeakConfiguration;

// will probably change this in the future considering we use a different config storage approach in gagspeak
public class AliasConfigService : ConfigurationServiceBase<AliasConfig>
{
    public const string ConfigName = "alias-lists.json";
    public const bool PerCharacterConfig = true;
    public AliasConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

}
