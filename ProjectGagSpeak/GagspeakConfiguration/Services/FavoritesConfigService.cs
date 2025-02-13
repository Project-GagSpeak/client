using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Configurations;

namespace Glamourer.Unlocks;

public class FavoritesConfigService : ConfigurationServiceBase<GagspeakConfig>
{
    public const string ConfigName = "favorites.json";
    public const bool PerCharacterConfig = true;
    public FavoritesConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;
}
