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
}
