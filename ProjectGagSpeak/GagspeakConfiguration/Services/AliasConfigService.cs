using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;

namespace GagSpeak.GagspeakConfiguration;

// will probably change this in the future considering we use a different config storage approach in gagspeak
public class AliasConfigService : ConfigurationServiceBase<AliasConfig>
{
    public const string ConfigName = "alias-lists.json";
    public const bool PerCharacterConfig = true;
    public AliasConfigService(string configDir) : base(configDir) { }

    protected override string ConfigurationName => ConfigName;
    protected override bool PerCharacterConfigPath => PerCharacterConfig;

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
        // create a new JObject to store the new config
        JObject newConfigJson = new();
        // set the version to 1
        newConfigJson["Version"] = 1;

        // Migrate AliasStorage
        var oldAliasStorage = oldConfigJson["AliasStorage"] as JObject;
        if (oldAliasStorage != null)
        {
            JObject newAliasStorage = new();
            foreach (var property in oldAliasStorage.Properties())
            {
                string key = property.Name;
                JObject? oldAliasStorageValue = property.Value as JObject;
                if (oldAliasStorageValue is not null)
                {
                    JObject newAliasStorageValue = new();
                    newAliasStorageValue["HasNameStored"] = oldAliasStorageValue["HasNameStored"];
                    newAliasStorageValue["CharacterNameWithWorld"] = oldAliasStorageValue["CharacterNameWithWorld"];

                    // Migrate AliasList
                    var oldAliasList = oldAliasStorageValue["AliasList"] as JArray;
                    if (oldAliasList != null)
                    {
                        JArray newAliasList = new();
                        foreach (var oldAliasItem in oldAliasList)
                        {
                            if (oldAliasItem is JObject oldAliasObject)
                            {
                                JObject newAliasItem = new();
                                newAliasItem["AliasIdentifier"] = Guid.NewGuid().ToString();
                                newAliasItem["Enabled"] = oldAliasObject["Enabled"];
                                newAliasItem["Name"] = oldAliasObject["AliasLabel"];
                                newAliasItem["InputCommand"] = oldAliasObject["InputCommand"];

                                // Convert OutputCommand to Executions
                                JObject executions = new();
                                JObject textOutput = new();
                                textOutput["ExecutionType"] = 0; // Assuming 0 is the enum value for TextOutput
                                textOutput["OutputCommand"] = oldAliasObject["OutputCommand"];
                                executions["TextOutput"] = textOutput;

                                newAliasItem["Executions"] = executions;
                                newAliasList.Add(newAliasItem);
                            }
                        }
                        newAliasStorageValue["AliasList"] = newAliasList;
                    }

                    newAliasStorage[key] = newAliasStorageValue;
                }
            }
            newConfigJson["AliasStorage"] = newAliasStorage;
        }

        return newConfigJson;
    }

    protected override AliasConfig DeserializeConfig(JObject configJson)
    {
        var aliasConfig = new AliasConfig();

        // Parse Version
        aliasConfig.Version = configJson["Version"]?.Value<int>() ?? AliasConfig.CurrentVersion;

        // parse the GlobalAliasList
        var globalAliasListArray = configJson["GlobalAliasList"] as JArray;
        aliasConfig.GlobalAliasList = new List<AliasTrigger>();
        if (globalAliasListArray is not null)
        {
            foreach (var item in globalAliasListArray)
            {
                if (item is JObject aliasObject)
                {
                    var aliasTrigger = ParseAliasTrigger(aliasObject);
                    aliasConfig.GlobalAliasList.Add(aliasTrigger);
                }
            }
        }

        // Parse AliasStorage
        var aliasStorageObj = configJson["AliasStorage"] as JObject;
        if (aliasStorageObj != null)
        {
            foreach (var property in aliasStorageObj.Properties())
            {
                string key = property.Name;
                JObject? aliasStorageValue = property.Value as JObject;
                if (aliasStorageValue is not null)
                {
                    var aliasStorage = ParseAliasStorage(aliasStorageValue);
                    if (aliasStorage is not null)
                        aliasConfig.AliasStorage[key] = aliasStorage;
                }
            }
        }

        return aliasConfig;
    }

    private static AliasStorage ParseAliasStorage(JObject obj)
    {
        var aliasStorage = new AliasStorage
        {
            // Append the CharaNameWorld if one is present for us.
            CharacterNameWithWorld = obj["CharacterNameWithWorld"]?.Value<string>() ?? string.Empty
        };

        // Parse AliasList
        var aliasListArray = obj["AliasList"] as JArray;
        aliasStorage.AliasList = new List<AliasTrigger>();
        if (aliasListArray is not null)
        {
            foreach (var item in aliasListArray)
            {
                if (item is JObject aliasObject)
                {
                    var aliasTrigger = ParseAliasTrigger(aliasObject);
                    aliasStorage.AliasList.Add(aliasTrigger);
                }
            }
        }
        return aliasStorage;
    }

    private static AliasTrigger ParseAliasTrigger(JObject obj)
    {
        var identifier = Guid.TryParse(obj["AliasIdentifier"]?.Value<string>(), out var guid) ? guid : Guid.Empty;
        var enabled = obj["Enabled"]?.Value<bool>() ?? false;
        var name = obj["Name"]?.Value<string>() ?? string.Empty;
        var inputCommand = obj["InputCommand"]?.Value<string>() ?? string.Empty;
        var executions = ParseExecutions(obj["Executions"] as JObject ?? new JObject());

        return new AliasTrigger
        {
            AliasIdentifier = identifier,
            Enabled = enabled,
            Name = name,
            InputCommand = inputCommand,
            Executions = executions
        };
    }

    private static Dictionary<ActionExecutionType, IActionGS> ParseExecutions(JObject obj)
    {
        var executions = new Dictionary<ActionExecutionType, IActionGS>();

        if (obj == null) return executions;

        foreach (var property in obj.Properties())
        {
            if (Enum.TryParse(property.Name, out ActionExecutionType executionType))
            {
                var executionObj = property.Value as JObject;
                if (executionObj == null) continue;

                IActionGS action = executionType switch
                {
                    ActionExecutionType.TextOutput => executionObj.ToObject<TextAction>() ?? new TextAction(),
                    ActionExecutionType.Gag => executionObj.ToObject<GagAction>() ?? new GagAction(),
                    ActionExecutionType.Restraint => executionObj.ToObject<RestraintAction>() ?? new RestraintAction(),
                    ActionExecutionType.Moodle => executionObj.ToObject<MoodleAction>() ?? new MoodleAction(),
                    ActionExecutionType.ShockCollar => executionObj.ToObject<PiShockAction>() ?? new PiShockAction(),
                    ActionExecutionType.SexToy => executionObj.ToObject<SexToyAction>() ?? new SexToyAction(),
                    _ => throw new NotImplementedException()
                };

                if (action is not null)
                {
                    executions[executionType] = action;
                }
            }
        }

        return executions;
    }
}
