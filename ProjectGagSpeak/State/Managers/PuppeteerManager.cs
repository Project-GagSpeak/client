using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.Kinksters;
using GagSpeak.Localization;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Managers;

public sealed class PuppeteerPlayer
{
    // make a hashset if we want to allow multiple players on a profile
    public string NameWithWorld { get; set; } = string.Empty;
    public int OrdersRecieved { get; set; } = 0;
    public int SitOrders { get; set; } = 0;
    public int EmoteOrders { get; set; } = 0;
    public int AliasOrders { get; set; } = 0;
    public int OtherOrders { get; set; } = 0;
}

public sealed class PuppeteerManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<AliasTrigger> _itemEditor = new();

    public PuppeteerManager(ILogger<PuppeteerManager> logger, GagspeakMediator mediator, ConfigFileProvider fileNames, HybridSaveService saver)
        : base(logger, mediator)
    {
        _fileNames = fileNames;
        _saver = saver;
    }

    public AliasStorage Storage { get; private set; } = new AliasStorage();
    // UID -> PlayerName & Respective Statistics
    public Dictionary<string, PuppeteerPlayer> Puppeteers { get; private set; } = [];

    public AliasTrigger? ItemInEditor => _itemEditor.ItemInEditor;

    public AliasTrigger CreateNew(string aliasLabel)
    {
        // Strip private formatting codes.
        aliasLabel = CkGui.TooltipTokenRegex().Replace(aliasLabel, string.Empty);
        // Ensure that the new name is unique.
        aliasLabel = RegexEx.EnsureUniqueName(aliasLabel, Storage.Items, rs => rs.Label);
        var alias = new AliasTrigger { Label = aliasLabel};
        Storage.Items.Add(alias);
        _saver.Save(this);
        Logger.LogDebug($"Created new Alias {alias.Label} ({alias.Identifier}).", LoggerType.Puppeteer);
        Mediator.Publish(new ConfigAliasItemChanged(StorageChangeType.Created, alias, null));
        return alias;
    }
    public AliasTrigger CreateClone(AliasTrigger clone, string newName)
    {
        // Strip private formatting codes.
        newName = CkGui.TooltipTokenRegex().Replace(newName, string.Empty);
        // Ensure that the new name is unique.
        newName = RegexEx.EnsureUniqueName(newName, Storage.Items, rs => rs.Label);
        var clonedItem = new AliasTrigger(clone, false) { Label = newName };
        Storage.Items.Add(clonedItem);
        _saver.Save(this);
        Logger.LogDebug($"Cloned Alias {clonedItem.Identifier}.", LoggerType.Puppeteer);
        Mediator.Publish(new ConfigAliasItemChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void Delete(AliasTrigger alias)
    {
        // should never be able to remove active restraints, but if that happens to occur, add checks here.
        if (Storage.Items.Remove(alias))
        {
            Logger.LogDebug($"Deleted AliasTrigger {alias.Label} ({alias.Identifier})", LoggerType.Puppeteer);
            Mediator.Publish(new ConfigAliasItemChanged(StorageChangeType.Deleted, alias, null));
            _saver.Save(this);
        }
    }

    public void Rename(AliasTrigger alias, string newName)
    {
        var oldName = alias.Label;
        if (oldName == newName)
            return;
        // strip special formatting codes
        newName = CkGui.TooltipTokenRegex().Replace(newName, string.Empty);
        // ensure the new name is unique.
        newName = RegexEx.EnsureUniqueName(newName, Storage.Items, rs => rs.Label);
        alias.Label = newName;
        _saver.Save(this);
        Logger.LogDebug($"Renamed restraint {alias.Label} ({alias.Identifier}).", LoggerType.Puppeteer);
        Mediator.Publish(new ConfigAliasItemChanged(StorageChangeType.Renamed, alias, oldName));
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(AliasTrigger trigger)
        => _itemEditor.StartEditing(Storage, trigger);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing()
        => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var source))
        {
            Logger.LogDebug($"Saved changes to {source.Label} ({source.Identifier}).", LoggerType.Puppeteer);
            // _managerCache.UpdateCache(AppliedRestraint);
            Mediator.Publish(new ConfigAliasItemChanged(StorageChangeType.Modified, source));
            _saver.Save(this);
        }
    }

    public void Save()
        => _saver.Save(this);

    public void SetEnabledState(AliasTrigger alias, bool newState)
    {
        alias.Enabled = newState;
        Logger.LogDebug($"Set EnabledState: {alias.Label} to {(alias.Enabled ? "Enabled" : "Disabled")}", LoggerType.Puppeteer);
        _saver.Save(this);
        Mediator.Publish(new EnabledItemChanged(GSModule.Puppeteer, alias.Identifier, newState));
    }

    public void SetEnabledState(IEnumerable<AliasTrigger> aliases, bool newState)
    {
        foreach (var a in aliases)
            a.Enabled = newState;
        _saver.Save(this);
        Logger.LogDebug($"SetEnabledState for ({string.Join(", ", aliases.Select(a => a.Label))})", LoggerType.Puppeteer);
        Mediator.Publish(new EnabledItemsChanged(GSModule.Puppeteer, aliases.Select(a => a.Identifier), newState));
    }

    public string? GetPuppeteerUid(string name, string world)
        => GetPuppeteerUid($"{name}@{world}");

    public string? GetPuppeteerUid(string nameWorld)
    {
        foreach (var (uid, puppeteer) in Puppeteers)
            if (puppeteer.NameWithWorld == nameWorld)
                return uid;
        return null;
    }

    public IEnumerable<AliasTrigger> GetGlobalAliases()
        => Storage.Items.Where(a => a.Enabled && a.ValidAlias() && a.WhitelistedUIDs.Count is 0);

    public IEnumerable<AliasTrigger> GetAliasesForPuppeteer(string puppeteerUid)
    {
        if (!Puppeteers.ContainsKey(puppeteerUid))
            return Enumerable.Empty<AliasTrigger>();
        // Return all aliases containing this UID that are enabled.
        return Storage.Items.Where(a => a.Enabled && a.ValidAlias() && a.WhitelistedUIDs.Contains(puppeteerUid));
    }

    #region HybridSavable
    public int ConfigVersion => 1;
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
            ["AliasList"] = JArray.FromObject(Storage.Items),
            ["Puppeteers"] = JObject.FromObject(Puppeteers)
        };
        return configObject.ToString(Formatting.Indented);
    }
    public void Load()
    {
        var file = _fileNames.Puppeteer;
        Logger.LogInformation($"Loading in Puppeteer Config for file: {file}");
        Storage.Items.Clear();
        Puppeteers.Clear();

        JObject jObject;
        // Read the json from the file.
        if (!File.Exists(file))
        {
            Logger.LogWarning($"No Puppeteer file found at {file}");
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
                Logger.LogWarning($"No Config file found for: " + oldFormatFile);
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
        var version = jObject["Version"]?.Value<int>() ?? 1;

        // Perform Migrations if any, and then load the data.
        switch (version)
        {
            case 0:
                jObject = MigrateV0ToV1(jObject);
                goto case 1;

            case 1:
                LoadV1(jObject);
                break;

            default:
                Logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(GSModule.Puppeteer));
    }

    private static JObject MigrateV0ToV1(JObject v0)
    {
        var v1 = new JObject
        {
            ["Version"] = 1
        };

        // Migrate Global Alias Storage to our AliasList
        if (v0["GlobalStorage"] is JArray globalStorage)
            v1["AliasList"] = new JArray(globalStorage);
        // Otherwise make a new one.
        else
            v1["AliasList"] = new JArray();

        // Then initialize the puppeteers as a new object.
        v1["Puppeteers"] = new JObject();

        // NOTE:
        // - PairStorage is intentionally ignored
        // - StoredNameWorld is dropped
        // - Per-pair aliases are not migrated
        return v1;
    }

    private void LoadV1(JObject data)
    {
        if (data["AliasList"] is JArray aliasArray)
        {
            foreach (var item in aliasArray)
            {
                try
                {
                    if (item is JObject aliasObject)
                    {
                        var trigger = ParseAliasTrigger(aliasObject);
                        Storage.Items.Add(trigger);
                    }
                }
                catch (Bagagwa ex)
                {
                    Logger.LogError($"Alias failed to parse, skipping: {ex.Message}");
                }
            }
        }

        // Puppeteers
        if (data["Puppeteers"] is JObject puppeteersObj)
        {
            foreach (var (uid, puppeteer) in puppeteersObj)
            {
                if (puppeteer is not JObject puppeteerObj)
                    continue;
                // Append the puppeteer
                Puppeteers[uid] = puppeteerObj.ToObject<PuppeteerPlayer>() ?? new PuppeteerPlayer();
            }
        }
    }

    private AliasTrigger ParseAliasTrigger(JObject obj)
    {
        return new AliasTrigger
        {
            Identifier = Guid.TryParse(obj["Identifier"]?.Value<string>(), out var guid) ? guid : throw new InvalidOperationException("Invalid GUID"),
            Enabled = obj["Enabled"]?.Value<bool>() ?? false,
            Label = obj["Label"]?.Value<string>() ?? string.Empty,
            InputCommand = obj["InputCommand"]?.Value<string>() ?? string.Empty,
            Actions = ParseExecutions(obj["Actions"] as JArray),
            WhitelistedUIDs = obj["WhitelistedUIDs"] is JArray arr ? arr.Select(uid => uid.Value<string>() ?? string.Empty).ToHashSet() : new HashSet<string>()
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
