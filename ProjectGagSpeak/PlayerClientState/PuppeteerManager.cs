using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerState.Visual;

public sealed class PuppeteerManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly GlobalData _globals;
    private readonly PairManager _pairs;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<AliasTrigger> _itemEditor = new();

    public PuppeteerManager(ILogger<PuppeteerManager> logger, GagspeakMediator mediator,
        GlobalData clientData, PairManager pairs, ConfigFileProvider fileNames,
        HybridSaveService saver) : base(logger, mediator)
    {
        _globals = clientData;
        _pairs = pairs;
        _fileNames = fileNames;
        _saver = saver;
    }
    public AliasStorage GlobalAliasStorage { get; private set; } = new AliasStorage();
    public PairAliasStorage PairAliasStorage { get; private set; } = new PairAliasStorage();
    public AliasTrigger? ItemInEditor => _itemEditor.ItemInEditor;

    public AliasTrigger? CreateNew(string? userUid = null)
    {
        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage.GetValueOrDefault(userUid)?.Storage;
        if (storage is null)
            return null;

        // Create a new AliasTrigger with a unique name
        var newItem = new AliasTrigger();
        storage.Add(newItem);
        _saver.Save(this);

        Logger.LogDebug("Added new Alias Trigger to " + nameof(storage), LoggerType.Puppeteer);
        return newItem;
    }


    public AliasTrigger? CreateClone(AliasTrigger clone, string? userUid = null)
    {
        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage.GetValueOrDefault(userUid)?.Storage;
        if (storage is null) 
            return null;

        var cloneName = RegexEx.EnsureUniqueName(clone.Label, storage, at => at.Label);
        var newItem = new AliasTrigger(clone, false) { Label = cloneName };
        storage.Add(newItem);
        _saver.Save(this);

        Logger.LogDebug("Cloned Alias Trigger to " + nameof(storage), LoggerType.Puppeteer);
        return newItem;
    }

    public void Delete(AliasTrigger trigger, string? userUid = null)
    {
        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage.GetValueOrDefault(userUid)?.Storage;
        if (storage is null)
            return;
        
        if (storage.Remove(trigger))
        {
            Logger.LogDebug($"Deleted Alias Trigger in {nameof(storage)}", LoggerType.Puppeteer);
            _saver.Save(this);
        }
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(AliasTrigger trigger) => _itemEditor.StartEditing(trigger);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var source))
        {
            // We need to figure out where the source was from, to know what update to send to the server.
            Logger.LogDebug("Saved changes to " + source.Label, LoggerType.Puppeteer);
            _saver.Save(this);
        }
    }

    public bool TryGetListenerPairPerms(string name, string world, [NotNullWhen(true)] out Pair matchedPair)
    {
        matchedPair = null!;
        var nameWithWorld = name + "@" + world;
        // go through the pair alias storage and find the pair that matches the name.
        if (PairAliasStorage.FirstOrDefault(x => x.Value.StoredNameWorld == nameWithWorld).Key is not { } match)
            return false;

        // we have the UID, so get its permissions.
        if(_pairs.DirectPairs.FirstOrDefault(p => p.UserData.UID == match) is not { } pair)
            return false;

        matchedPair = pair;
        return true;
    }

    public void UpdateStoredAliasName(string pairUid, string listenerName)
    {
        if (!PairAliasStorage.ContainsKey(pairUid))
            PairAliasStorage[pairUid] = new NamedAliasStorage();
        // set the name.
        PairAliasStorage[pairUid].StoredNameWorld = listenerName;
        _saver.Save(this);
    }


    #region HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.Puppeteer).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // Constructing the config object to serialize
        var configObject = new JObject()
        {
            ["Version"] = ConfigVersion,
            // Serialize GlobalAliasStorage (AliasStorage contains a List<AliasTrigger>)
            ["GlobalStorage"] = JArray.FromObject(GlobalAliasStorage),
            // Serialize PairAliasStorage (a dictionary of string -> NamedAliasStorage)
            ["PairStorage"] = new JObject(
            PairAliasStorage.ToDictionary(
                pair => pair.Key,
                pair => new JObject
                {
                    ["StoredNameWorld"] = pair.Value.StoredNameWorld,
                    // Serialize the AliasStorage list (List<AliasTrigger>) in NamedAliasStorage
                    ["AliasList"] = JArray.FromObject(pair.Value.Storage)
                })
            )
        };

        // Convert JObject to string
        return configObject.ToString(Formatting.Indented);
    }
    public void Load()
    {
        var file = _fileNames.Puppeteer;
        Logger.LogInformation("Loading in Puppeteer Config for file: " + file);

        GlobalAliasStorage.Clear();
        PairAliasStorage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Puppeteer Config file found at {0}", file);
            // create a new file with default values.
            _saver.Save(this);
            return;
        }

        // Read the json from the file.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Perform Migrations if any, and then load the data.
        switch (version)
        {
            case 0:
                LoadV0(jObject);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(ModuleSection.Puppeteer));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JObject storage)
            return;


        // Deserialize GlobalAliasStorage (AliasStorage, which is a List<AliasTrigger>)
        if (storage["GlobalStorage"] is JArray globalStorageArray)
        {
            foreach (var item in globalStorageArray)
            {
                if (item is JObject aliasObject)
                {
                    var aliasTrigger = ParseAliasTrigger(aliasObject);
                    GlobalAliasStorage.Add(aliasTrigger);
                }
            }
        }

        // Deserialize PairAliasStorage (Dictionary<string, NamedAliasStorage>)
        if (storage["PairStorage"] is not JObject pairStorageObj)
            return;

        foreach (var property in pairStorageObj.Properties())
        {
            var key = property.Name;
            var aliasStorageValue = property.Value as JObject;
            if (aliasStorageValue is not null)
            {
                var aliasStorage = ParseNamedAliasStorage(aliasStorageValue);
                if (aliasStorage is not null)
                    PairAliasStorage[key] = aliasStorage;
            }
        }
    }

    private NamedAliasStorage ParseNamedAliasStorage(JObject obj)
    {
        var aliasStorage = new NamedAliasStorage
        {
            StoredNameWorld = obj["StoredNameWorld"]?.Value<string>() ?? string.Empty,
            Storage = new AliasStorage()
        };

        // Parse AliasList
        if(obj["AliasList"] is not JArray aliasListArray)
            return aliasStorage;

        foreach (var item in aliasListArray)
            if (item is JObject aliasObject)
            {
                var aliasTrigger = ParseAliasTrigger(aliasObject);
                aliasStorage.Storage.Add(aliasTrigger);
            }

        return aliasStorage;
    }

    private AliasTrigger ParseAliasTrigger(JObject obj)
    {
        return new AliasTrigger
        {
            Identifier = Guid.TryParse(obj["AliasIdentifier"]?.Value<string>(), out var guid) ? guid : Guid.Empty,
            Enabled = obj["Enabled"]?.Value<bool>() ?? false,
            Label = obj["Label"]?.Value<string>() ?? string.Empty,
            InputCommand = obj["InputCommand"]?.Value<string>() ?? string.Empty,
            Actions = ParseExecutions(obj["Actions"] as JObject ?? new JObject())
        };
    }

    private static HashSet<InvokableGsAction> ParseExecutions(JObject obj)
    {
        var executions = new HashSet<InvokableGsAction>();

        if (obj is null) 
            return executions;
/*
        foreach (var property in obj.Properties())
        {
            if (Enum.TryParse(property.Name, out InvokableActionType executionType))
            {
                var executionObj = property.Value as JObject;
                if (executionObj == null) continue;

                InvokableGsAction action = executionType switch
                {
                    InvokableActionType.TextOutput => executionObj.ToObject<TextAction>() ?? new TextAction(),
                    InvokableActionType.Gag => executionObj.ToObject<GagAction>() ?? new GagAction(),
                    InvokableActionType.Restraint => executionObj.ToObject<RestraintAction>() ?? new RestraintAction(),
                    InvokableActionType.Moodle => executionObj.ToObject<MoodleAction>() ?? new MoodleAction(),
                    InvokableActionType.ShockCollar => executionObj.ToObject<PiShockAction>() ?? new PiShockAction(),
                    InvokableActionType.SexToy => executionObj.ToObject<SexToyAction>() ?? new SexToyAction(),
                    _ => throw new NotImplementedException()
                };

                if (action is not null)
                {
                    executions[executionType] = action;
                }
            }
        }*/
        return executions;
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable
}
