using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;

namespace GagSpeak.State.Managers;
public sealed class PatternManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly RemoteHandler _handler;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;
    private readonly RemoteService _remotes;

    private PatternStorage _storage = new PatternStorage();
    private StorageItemEditor<Pattern> _itemEditor = new();

    public PatternManager(ILogger<PatternManager> logger, GagspeakMediator mediator,
        RemoteHandler handler, FavoritesManager favorites, ConfigFileProvider fileNames,
        HybridSaveService saver, RemoteService remotes) : base(logger, mediator)
    {
        _handler = handler;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
        _remotes = remotes;
        Load();
    }

    public PatternStorage Storage => _storage;
    public Pattern? ItemInEditor => _itemEditor.ItemInEditor;
    public Guid ActivePatternId => _remotes.ClientData.ActivePattern;
    public Pattern CreateNew(string patternName)
    {
        patternName = RegexEx.EnsureUniqueName(patternName, Storage, t => t.Label);
        var newPattern = new Pattern() { Label = patternName };
        Storage.Add(newPattern);
        _saver.Save(this);

        Logger.LogDebug($"Created new pattern {newPattern.Label}.");
        Mediator.Publish(new ConfigPatternChanged(StorageChangeType.Created, newPattern, null));
        return newPattern;
    }

    public Pattern CreateClone(Pattern other, string newName)
    {
        newName = RegexEx.EnsureUniqueName(newName, Storage, t => t.Label);
        var clonedItem = new Pattern(other, false) { Label = newName };
        Storage.Add(clonedItem);
        _saver.Save(this);

        Logger.LogDebug($"Cloned pattern {other.Label} to {newName}.");
        Mediator.Publish(new ConfigPatternChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void Rename(Pattern pattern, string newName)
    {
        var prevName = pattern.Label;
        newName = RegexEx.EnsureUniqueName(newName, Storage, (t) => t.Label);
        pattern.Label = newName;
        _saver.Save(this);

        Logger.LogDebug($"Storage contained pattern, renaming {pattern.Label} to {newName}.");
        Mediator.Publish(new ConfigPatternChanged(StorageChangeType.Renamed, pattern, prevName));
    }

    public void Delete(Pattern pattern)
    {
        if (Storage.Remove(pattern))
        {
            Logger.LogDebug($"Deleted pattern {pattern.Label}.");
            Mediator.Publish(new ConfigPatternChanged(StorageChangeType.Deleted, pattern, null));
            _saver.Save(this);
        }
    }


    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(Pattern item) => _itemEditor.StartEditing(Storage, item);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            Logger.LogDebug($"Saved changes to pattern {sourceItem.Identifier}.");
            Mediator.Publish(new ConfigPatternChanged(StorageChangeType.Modified, sourceItem, null));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the pattern as a favorite. </summary>
    public bool AddFavorite(Pattern p) => _favorites.TryAddRestriction(FavoriteIdContainer.Pattern, p.Identifier);

    /// <summary> Attempts to remove the pattern as a favorite. </summary>
    public bool RemoveFavorite(Pattern p) => _favorites.RemoveRestriction(FavoriteIdContainer.Pattern, p.Identifier);

    /// <summary> Switches from a currently active pattern to a new one. </summary>
    /// <remarks> If no pattern is currently active, it will simply start one. </remarks>
    public void SwitchPattern(Guid patternId, string enactor)
    {
        // This only actually fires if a pattern is active, and is skipped otherwise.
        if (Storage.TryGetPattern(patternId, out var pattern))
        {
            Logger.LogDebug($"Switching to pattern {pattern.Label} ({patternId}) by {enactor}.");
            _remotes.ClientData.SwitchPlaybackData(pattern, pattern.StartPoint, pattern.Duration, enactor);
        }
        else
        {
            Logger.LogWarning($"Attempted to switch to non-existent pattern {patternId} by {enactor}.");
            return;
        }
    }

    /// <summary> Enables a pattern, beginning the execution to the simulated, or connected sex toy. </summary>
    /// <remarks> If no pattern in the storage is found, no pattern will activate. </remarks>
    public void EnablePattern(Guid patternId, string enactor)
    {
        if(Storage.TryGetPattern(patternId, out var pattern))
            _remotes.ClientData.StartPlaybackData(pattern, pattern.StartPoint, pattern.Duration, enactor);
    }

    public void DisablePattern(Guid patternId, string enactor)
    {
        if (_remotes.ClientData.IsPlayingPattern)
            _remotes.ClientData.EndPlaybackData(enactor);
        else
            Logger.LogWarning("Tried to stop a pattern when no pattern was active??!?");
    }

    #region HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.Patterns).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // we need to iterate through our list of trigger objects and serialize them.
        var patternItems = new JArray();
        foreach (var pattern in Storage)
            patternItems.Add(pattern.Serialize());

        // construct the config object to serialize.
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Patterns"] = patternItems,
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.Patterns;
        Logger.LogInformation("Loading in Patterns Config for file: " + file);

        Storage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Patterns Config file found at {0}", file);
            // create a new file with default values.
            _saver.Save(this);
            return;
        }

        // Read the json from the file.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);

        // Migrate the jObject if it is using the old format.
        if (jObject["PatternStorage"] is JToken)
            jObject = ConfigMigrator.MigratePatternConfig(jObject, _fileNames);

        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Perform Migrations if any, and then load the data.
        MigrateV0toV1(jObject, version);
        LoadV1(jObject["Patterns"]);

        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(GagspeakModule.Pattern));
    }

    private void LoadV1(JToken? data)
    {
        if (data is not JArray patterns)
            return;

        foreach (var pattern in patterns)
        {
            if (pattern is JObject patternObject)
            {
                var newPattern = new Pattern();
                newPattern.Deserialize(patternObject);
                Storage.Add(newPattern);
            }
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson, int version)
    {
        if(version != 0)
            return; // already migrated.

        oldConfigJson["Version"] = 1;

        // We need to update the stored pattern data here.
        if (oldConfigJson["Patterns"] is not JArray patterns)
            return;

        // Do so by migrating the old PatternByteData.
        foreach (var token in patterns)
        {
            if (token is not JObject patternObj)
                continue;

            var patternByteData = patternObj["PatternByteData"]?.ToString();
            if (string.IsNullOrWhiteSpace(patternByteData))
                continue;

            try
            {
                // Parse byte list
                var byteArray = patternByteData.Split(',').Select(s => byte.TryParse(s, out var b) ? b : (byte)0).ToArray();

                // Convert to normalized doubles
                var doubleData = byteArray.Select(b => b / 100.0).ToArray();

                // Debug log
                Svc.Logger.Debug($"[MigrateV0toV1] Migrating pattern {patternObj["Label"]} - {byteArray.Length} values, First few: {string.Join(", ", byteArray.Take(5))}");


                // Build motor/device pattern, defaulted to Hush, but can be migrated later.
                var motor = new MotorStream(ToyMotor.Vibration, 0, doubleData);
                var device = new DeviceStream(ToyBrandName.Hush, [ motor ]);
                var fullPattern = new FullPatternData([ device ]);

                // Set new format
                patternObj["PlaybackData"] = fullPattern.ToCompressedBase64();

                // Remove legacy format
                patternObj.Remove("PatternByteData");
            }
            catch (Bagagwa ex)
            {
                // Log or handle if needed
                Svc.Logger.Error($"[MigrateV0toV1] Failed to migrate pattern: {ex.Message}");
            }
        }

    }

    #endregion HybridSavable
}
