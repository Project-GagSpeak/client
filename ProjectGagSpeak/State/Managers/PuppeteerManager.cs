using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Managers;

public sealed class PuppeteerManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly KinksterManager _pairs;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<AliasTrigger> _itemEditor = new();

    public PuppeteerManager(ILogger<PuppeteerManager> logger, GagspeakMediator mediator,
        KinksterManager pairs, ConfigFileProvider fileNames, HybridSaveService saver) 
        : base(logger, mediator)
    {
        _pairs = pairs;
        _fileNames = fileNames;
        _saver = saver;
    }
    public AliasStorage GlobalAliasStorage { get; private set; } = new AliasStorage();
    public PairAliasStorage PairAliasStorage { get; private set; } = new PairAliasStorage();
    public AliasTrigger? ItemInEditor => _itemEditor.ItemInEditor;

    public AliasTrigger? CreateNew(string? userUid = null)
    {
        if (userUid is not null && !ValidatePairStorage(userUid))
            return null;

        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage[userUid].Storage;
        if (storage is null)
            return null;

        // Create a new AliasTrigger with a unique name
        var newItem = new AliasTrigger();
        storage.Items.Add(newItem);
        _saver.Save(this);

        Logger.LogDebug("Added new Alias Trigger to " + nameof(storage), LoggerType.Puppeteer);
        return newItem;
    }


    public AliasTrigger? CreateClone(AliasTrigger clone, string? userUid = null)
    {
        if (userUid is not null && !ValidatePairStorage(userUid))
            return null;

        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage[userUid].Storage;
        if (storage is null) 
            return null;

        var cloneName = RegexEx.EnsureUniqueName(clone.Label, storage.Items, at => at.Label);
        var newItem = new AliasTrigger(clone, false) { Label = cloneName };
        storage.Items.Add(newItem);
        _saver.Save(this);

        Logger.LogDebug("Cloned Alias Trigger to " + nameof(storage), LoggerType.Puppeteer);
        return newItem;
    }

    public void Delete(AliasTrigger trigger, string? userUid = null)
    {
        if (userUid is not null && !ValidatePairStorage(userUid))
            return;

        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage[userUid].Storage;
        if (storage is null)
            return;
        
        if (storage.Items.Remove(trigger))
        {
            Logger.LogDebug($"Deleted Alias Trigger in {nameof(storage)}", LoggerType.Puppeteer);
            _saver.Save(this);
        }
    }

    public void ToggleState(AliasTrigger trigger, string? userUid = null)
    {
        if(userUid is not null && !ValidatePairStorage(userUid))
            return;

        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage[userUid].Storage;
        if (storage is null)
            return;

        trigger.Enabled = !trigger.Enabled;
        Logger.LogDebug($"Toggled Alias Trigger in {nameof(storage)}", LoggerType.Puppeteer);
        _saver.Save(this);
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(AliasTrigger trigger, string? userUid = null)
    {
        if (userUid is not null && !ValidatePairStorage(userUid))
            return;

        var storage = userUid is null ? GlobalAliasStorage : PairAliasStorage[userUid].Storage;
        if (storage is null)
            return;

        _itemEditor.StartEditing(storage, trigger);
    }

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

    public bool TryGetListenerPairPerms(string name, string world, [NotNullWhen(true)] out Kinkster matchedPair)
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
        if(ValidatePairStorage(pairUid))
        {
            // set the name.
            PairAliasStorage[pairUid].StoredNameWorld = listenerName;
            _saver.Save(this);
        }
    }

    /// <summary> Validates the puppeteer storage for a user UID </summary>
    /// <returns> True if valid, false otherwise. </returns>
    private bool ValidatePairStorage(string userUid)
    {
        // If the storage does not yet exist.
        if (!PairAliasStorage.ContainsKey(userUid))
        {
            // Add it only if the pair itself exists.
            if (_pairs.GetUserDataFromUID(userUid) is { } userData)
            {
                PairAliasStorage[userUid] = new NamedAliasStorage();
                return true;
            }
            return false;
        }
        return true;
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
            ["GlobalStorage"] = JArray.FromObject(GlobalAliasStorage.Items),
            // Serialize PairAliasStorage (a dictionary of string -> NamedAliasStorage)
            ["PairStorage"] = JObject.FromObject(
                PairAliasStorage.ToDictionary(
                    pair => pair.Key,
                    pair => new JObject
                    {
                        ["StoredNameWorld"] = pair.Value.StoredNameWorld,
                        ["Storage"] = JArray.FromObject(pair.Value.Storage.Items),
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

        GlobalAliasStorage.Items.Clear();
        PairAliasStorage.Clear();

        JObject jObject;
        // Read the json from the file.
        if (!File.Exists(file))
        {
            Logger.LogWarning($"No Restraints Config file found at {file}");
            // create a new file with default values.

            var oldFormatFile = Path.Combine(_fileNames.CurrentPlayerDirectory, "alias-lists.json");
            if (File.Exists(oldFormatFile))
            {
                var oldText = File.ReadAllText(oldFormatFile);
                var oldObject = JObject.Parse(oldText);
                jObject = ConfigMigrator.MigratePuppeteerAliasConfig(oldObject, _fileNames, oldFormatFile);
            }
            else
            {
                Svc.Logger.Warning("No Config file found for: " + oldFormatFile);
                _saver.Save(this);
                return;
                // create a new file with default values.
            }
        }
        else
        {
            var jsonText = File.ReadAllText(file);
            jObject = JObject.Parse(jsonText);
        }
        // Read the json from the file.
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
        Mediator.Publish(new ReloadFileSystem(GagspeakModule.Puppeteer));
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
                try
                {
                    if (item is JObject aliasObject)
                    {
                        var aliasTrigger = ParseAliasTrigger(aliasObject);
                        GlobalAliasStorage.Items.Add(aliasTrigger);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("A GlobalAliasStorage Item failed to parse or had an empty GUID. Skipping.: " + ex.Message, LoggerType.Puppeteer);
                    continue;
                }
            }
        }

        // Deserialize PairAliasStorage (Dictionary<string, NamedAliasStorage>)
        if (storage["PairStorage"] is not JObject pairStorageObj)
            return;

        foreach (var property in pairStorageObj.Properties())
        {
            if (property.Value is not JObject pairStorageValue)
                continue;

            var aliasStorage = ParseNamedAliasStorage(pairStorageValue);
            if (aliasStorage is not null)
                PairAliasStorage[property.Name] = aliasStorage;
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
        if(obj["Storage"] is not JArray aliasListArray)
            return aliasStorage;

        foreach (var item in aliasListArray)
            if (item is JObject aliasObject)
            {
                var aliasTrigger = ParseAliasTrigger(aliasObject);
                aliasStorage.Storage.Items.Add(aliasTrigger);
            }

        return aliasStorage;
    }

    private AliasTrigger ParseAliasTrigger(JObject obj)
    {
        return new AliasTrigger
        {
            Identifier = Guid.TryParse(obj["Identifier"]?.Value<string>(), out var guid) ? guid : throw new InvalidOperationException("Invalid GUID"),
            Enabled = obj["Enabled"]?.Value<bool>() ?? false,
            Label = obj["Label"]?.Value<string>() ?? string.Empty,
            InputCommand = obj["InputCommand"]?.Value<string>() ?? string.Empty,
            Actions = ParseExecutions(obj["Actions"] as JArray)
        };
    }

    private static HashSet<InvokableGsAction> ParseExecutions(JArray? array)
    {
        var executions = new HashSet<InvokableGsAction>();

        if (array == null)
            return executions;

        foreach (var token in array)
        {
            var typeValue = token["ActionType"]?.Value<int>() ?? -1;
            var executionType = (InvokableActionType)typeValue;
            
            InvokableGsAction? action = executionType switch
            {
                InvokableActionType.TextOutput => token.ToObject<TextAction>() ?? new TextAction(),
                InvokableActionType.Gag => token.ToObject<GagAction>() ?? new GagAction(),
                InvokableActionType.Restriction => token.ToObject<RestrictionAction>() ?? new RestrictionAction(),
                InvokableActionType.Restraint => token.ToObject<RestraintAction>() ?? new RestraintAction(),
                InvokableActionType.Moodle => token.ToObject<MoodleAction>() ?? new MoodleAction(),
                InvokableActionType.ShockCollar => token.ToObject<PiShockAction>() ?? new PiShockAction(),
                InvokableActionType.SexToy => token.ToObject<SexToyAction>() ?? new SexToyAction(),
                _ => throw new NotImplementedException()
            };

            if (action != null)
                executions.Add(action);
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
