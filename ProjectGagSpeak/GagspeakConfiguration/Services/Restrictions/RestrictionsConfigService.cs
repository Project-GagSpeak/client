using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Restrictions;
#nullable disable
namespace GagSpeak.GagspeakConfiguration;

public class RestrictionsConfigService : ConfigurationServiceBase<RestrictionsConfig>
{
    public const string ConfigName = "restrictions.json";
    public const bool PerCharacterConfig = true;
    public RestrictionsConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

    // apply an override for migrations off the base config service
    protected override JObject MigrateConfig(JObject oldConfigJson)
    {
        var readVersion = oldConfigJson["Version"]?.Value<int>() ?? 0;
        // Perform any migrations necessary.
        // if (readVersion < 1)
        //     oldConfigJson = MigrateFromV0toV1(oldConfigJson);
        return oldConfigJson;
    }

    protected override RestrictionsConfig DeserializeConfig(JObject configJson)
    {
        var config = new RestrictionsConfig();
        config.Version = configJson["Version"]?.Value<int>() ?? 0;

        // deserialize the restrictions
        JToken storage = configJson["Restrictions"];
        if (storage is JObject json)
        {
            foreach (var token in json["RestrictionItems"] ?? new JArray())
            {
                if (token is JObject restrictionObject)
                {
                    var itemType = (RestrictionType)Enum.Parse(typeof(RestrictionType), json["Type"]?.Value<string>() ?? string.Empty);
                    IRestrictionItem restrictionItem = itemType switch
                    {
                        RestrictionType.Normal => new RestrictionItem(),
                        RestrictionType.Blindfold => new BlindfoldRestriction(),
                        RestrictionType.Collar => new CollarRestriction(),
                        _ => new RestrictionItem() // Default fallback
                    };

                    restrictionItem.LoadRestriction(restrictionObject);
                    config.Restrictions.Add(restrictionItem); // Assuming Restrictions is a list or collection
                }
                else
                {
                    throw new Exception("Invalid restriction set data in configuration.");
                }
            }
        }
        return config;
    }

    protected override string SerializeConfig(RestrictionsConfig config)
    {
        var json = new JObject
        {
            ["Version"] = config.Version,
            ["Restrictions"] = new JObject
            {
                ["RestrictionItems"] = new JArray(config.Restrictions.Select(x => x.Serialize()))
            }
        };
        return json.ToString(Formatting.Indented);
    }
}
