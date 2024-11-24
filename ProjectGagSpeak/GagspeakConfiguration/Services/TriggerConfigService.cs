using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagspeakAPI.Enums;

namespace GagSpeak.GagspeakConfiguration;

public class TriggerConfigService : ConfigurationServiceBase<TriggerConfig>
{
    public const string ConfigName = "triggers.json";
    public const bool PerCharacterConfig = true;
    public TriggerConfigService(string configDir) : base(configDir) { }

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

    protected override TriggerConfig DeserializeConfig(JObject configJson)
    {
        var config = new TriggerConfig();
        // Deserialize WardrobeStorage
        JToken triggerStorageToken = configJson["TriggerStorage"]!;
        if (triggerStorageToken != null)
        {
            TriggerStorage triggerStorage = new TriggerStorage();

            var triggerArray = triggerStorageToken["Triggers"]?.Value<JArray>();
            if (triggerArray == null)
            {
                StaticLogger.Logger.LogWarning("RestraintSets property is missing in RestraintSets.json");
                throw new Exception("RestraintSets property is missing in RestraintSets.json");
            }

            // we are in version 1, so we should deserialize appropriately
            foreach (var triggerToken in triggerArray)
            {
                // we need to obtain the information from the triggers "Type" property to know which kind of trigger to create, as we cant create an abstract "Trigger".
                if (Enum.TryParse(triggerToken["Type"]?.ToString(), out TriggerKind triggerType))
                {
                    Trigger triggerAbstract;
                    switch (triggerType)
                    {
                        case TriggerKind.Chat:
                            triggerAbstract = triggerToken.ToObject<ChatTrigger>()!;
                            break;
                        case TriggerKind.SpellAction:
                            triggerAbstract = triggerToken.ToObject<SpellActionTrigger>()!;
                            break;
                        case TriggerKind.HealthPercent:
                            triggerAbstract = triggerToken.ToObject<HealthPercentTrigger>()!;
                            break;
                        case TriggerKind.RestraintSet:
                            triggerAbstract = triggerToken.ToObject<RestraintTrigger>()!;
                            break;
                        case TriggerKind.GagState:
                            triggerAbstract = triggerToken.ToObject<GagTrigger>()!;
                            break;
                        case TriggerKind.SocialAction:
                            triggerAbstract = triggerToken.ToObject<SocialTrigger>()!;
                            break;
                        // could add a social feed trigger type here.
                        default:
                            throw new Exception("Invalid Trigger Type");
                    }
                    triggerStorage.Triggers.Add(triggerAbstract);
                }
                else
                {
                    throw new Exception("Invalid Trigger Type");
                }
            }

            config.TriggerStorage = triggerStorage;
        }
        return config;
    }
}
