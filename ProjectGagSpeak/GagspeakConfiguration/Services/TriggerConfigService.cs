using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data.IPC;
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

        // check the version of the config file
        switch (readVersion)
        {
            case 0:
                newConfigJson = MigrateFromV0toV1(oldConfigJson);
                break;
            case 1:
                // no migration needed
                newConfigJson = oldConfigJson;
                break;
            default:
                throw new Exception($"Unknown config version {readVersion}");
        }

        return newConfigJson;
    }

    // Safely update data for new format.
    private JObject MigrateFromV0toV1(JObject oldConfigJson)
    {
        // Create a new JObject to store the new config
        JObject newConfigJson = new JObject
        {
            ["Version"] = 1
        };

        // Extract the old triggers
        JArray oldTriggers = oldConfigJson["Triggers"] as JArray ?? new JArray();

        // Create a new array for the new triggers
        JArray newTriggers = new JArray();

        foreach (JObject oldTrigger in oldTriggers)
        {
            JObject newTrigger = new JObject
            {
                ["Identifier"] = oldTrigger["TriggerIdentifier"] ?? Guid.NewGuid().ToString(),
                ["Enabled"] = oldTrigger["Enabled"] ?? false,
                ["Priority"] = oldTrigger["Priority"] ?? 0,
                ["Name"] = oldTrigger["Name"] ?? "",
                ["Description"] = oldTrigger["Description"] ?? "",
            };

            // other stuff here.

            // Add the new trigger to the list
            newTriggers.Add(newTrigger);
        }

        // Add the new triggers to the config
        newConfigJson["TriggerStorage"] = new JObject
        {
            ["Triggers"] = newTriggers
        };

        return newConfigJson;
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
                StaticLogger.Logger.LogWarning("Triggers property is missing in Triggers.json");
                throw new Exception("Triggers property is missing in Triggers.json");
            }

            // we are in version 1, so we should deserialize appropriately
            foreach (var triggerToken in triggerArray)
            {
                // we need to obtain the information from the triggers "Type" property to know which kind of trigger to create, as we cant create an abstract "Trigger".
                if (Enum.TryParse(triggerToken["Type"]?.ToString(), out TriggerKind triggerType))
                {
                    Trigger triggerAbstract = triggerType switch
                    {
                        TriggerKind.SpellAction => triggerToken.ToObject<SpellActionTrigger>() ?? new SpellActionTrigger(),
                        TriggerKind.HealthPercent => triggerToken.ToObject<HealthPercentTrigger>() ?? new HealthPercentTrigger(),
                        TriggerKind.RestraintSet => triggerToken.ToObject<RestraintTrigger>() ?? new RestraintTrigger(),
                        TriggerKind.GagState => triggerToken.ToObject<GagTrigger>() ?? new GagTrigger(),
                        TriggerKind.SocialAction => triggerToken.ToObject<SocialTrigger>() ?? new SocialTrigger(),
                        _ => throw new Exception("Invalid Trigger Type")
                    };
                    // Safely parse the integer to ActionExecutionType
                    if (Enum.TryParse(triggerToken["ExecutionType"]?.ToString(), out ActionExecutionType executionType))
                    {
                        IActionGS executableAction = executionType switch
                        {
                            ActionExecutionType.TextOutput => triggerToken["ExecutableAction"]?.ToObject<TextAction>() ?? new TextAction(),
                            ActionExecutionType.Gag => triggerToken["ExecutableAction"]?.ToObject<GagAction>() ?? new GagAction(),
                            ActionExecutionType.Restraint => triggerToken["ExecutableAction"]?.ToObject<RestraintAction>() ?? new RestraintAction(),
                            ActionExecutionType.Moodle => triggerToken["ExecutableAction"]?.ToObject<MoodleAction>() ?? new MoodleAction(),
                            ActionExecutionType.ShockCollar => triggerToken["ExecutableAction"]?.ToObject<PiShockAction>() ?? new PiShockAction(),
                            ActionExecutionType.SexToy => triggerToken["ExecutableAction"]?.ToObject<SexToyAction>() ?? new SexToyAction(),
                            _ => throw new Exception("Invalid Execution Type")
                        };

                        if (executableAction is not null) triggerAbstract.ExecutableAction = executableAction;
                        else throw new Exception("Failed to deserialize ExecutableAction");
                    }
                    else
                    {
                        throw new Exception("Invalid Execution Type");
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

    protected override string SerializeConfig(TriggerConfig config)
    {
        return base.SerializeConfig(config);
    }
}
