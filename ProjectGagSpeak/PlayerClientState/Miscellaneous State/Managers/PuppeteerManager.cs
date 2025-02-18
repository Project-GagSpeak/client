using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data.Permissions;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerState.Visual;

public sealed class PuppeteerManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly GlobalData _globals;
    private readonly PairManager _pairs;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    public PuppeteerManager(ILogger<PuppeteerManager> logger, GagspeakMediator mediator,
        GlobalData clientData, PairManager pairs, ConfigFileProvider fileNames,
        HybridSaveService saver) : base(logger, mediator)
    {
        _globals = clientData;
        _pairs = pairs;
        _fileNames = fileNames;
        _saver = saver;
    }

    // Cached Information (We copy the entire AliasStorage during edit so we can edit multiple things at once and toggle.
    public AliasStorage? ActiveEditorItem { get; private set; } = null;
    public string? ActiveEditorPair { get; private set; } = null;

    // Stored Information.
    public AliasStorage GlobalAliasStorage { get; private set; } = new AliasStorage();
    public PairAliasStorage PairAliasStorage { get; private set; } = new PairAliasStorage();

    public void OnLogin() { }

    public void OnLogout() { }

    /// <summary> We can only add new items while editing </summary>
    /// <remarks> Append it to the active editors storage. </remarks>
    public bool CreateNew()
    {
        if (ActiveEditorItem is null)
            return false;

        var newItem = new AliasTrigger();
        ActiveEditorItem.Add(newItem);
        Logger.LogInformation("Added new Alias Trigger to AliasStorage being currently edited.", LoggerType.Puppeteer);
        return true;
    }

    public bool CreateClone(AliasTrigger clone)
    {
        if(ActiveEditorItem is null)
            return false;

        // generate a new design based off the passed in clone. Be sure to give it a new identifier after.
        var newItem = new AliasTrigger(clone, false);
        ActiveEditorItem.Add(newItem);
        Logger.LogInformation("Cloned Alias Trigger to AliasStorage being currently edited.", LoggerType.Puppeteer);
        return true;
    }

    public void Delete(AliasTrigger trigger)
    {
        if (ActiveEditorItem is null)
            return;

        if (ActiveEditorItem.Remove(trigger))
        {
            Logger.LogDebug($"Deleted Alias Trigger in active editor.", LoggerType.Puppeteer);
        }
    }

    public void StartEditingGlobal()
    {
        if (ActiveEditorItem is not null)
            return;
        // Open Globals for editing.
        ActiveEditorItem = GlobalAliasStorage.CloneAliasStorage();
    }

    public void StartEditingPair(string pairUid)
    {
        if(ActiveEditorItem is not null)
            return;

        // if the pair does not exist in the dictionary, we should create a new entry at that index.
        if (!PairAliasStorage.ContainsKey(pairUid))
            PairAliasStorage[pairUid] = new NamedAliasStorage();

        // Open the pair for editing.
        ActiveEditorItem = PairAliasStorage[pairUid].Storage.CloneAliasStorage();
        ActiveEditorPair = pairUid;
    }

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing()
    {
        // Clear the active editor.
        ActiveEditorItem = null;
        ActiveEditorPair = null;
    }

    /// <summary> Injects all the changes made to the Cursed Loot and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (ActiveEditorItem is null)
            return;

        // if the activeEditorPair is not null, then update that pairs storage.
        if (ActiveEditorPair is not null)
        {
            PairAliasStorage[ActiveEditorPair].Storage = ActiveEditorItem;
            Mediator.Publish(new AliasDataChangedMessage(DataUpdateType.AliasListUpdated,
                new(ActiveEditorPair), PairAliasStorage[ActiveEditorPair].Storage.ToAliasData()));
            ActiveEditorPair = null;
        }
        else
        {
            GlobalAliasStorage = ActiveEditorItem;
            ActiveEditorItem = null;
            Mediator.Publish(new AliasDataChangedMessage(DataUpdateType.GlobalListUpdated,
                MainHub.PlayerUserData, GlobalAliasStorage.ToAliasData()));
        }
        Save();
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
        Save();
    }


    #region HybridSavable
    public void Save() => _saver.Save(this);
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
    private void Load()
    {
        var file = _fileNames.Puppeteer;
        GlobalAliasStorage.Clear();
        PairAliasStorage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Global Alias Storage or Pair Alias Storage found at {0}", file);
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
            Executions = ParseExecutions(obj["Executions"] as JObject ?? new JObject())
        };
    }

    private static Dictionary<InvokableActionType, InvokableGsAction> ParseExecutions(JObject obj)
    {
        var executions = new Dictionary<InvokableActionType, InvokableGsAction>();

        if (obj is null) 
            return executions;

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
        }
        return executions;
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable
}
