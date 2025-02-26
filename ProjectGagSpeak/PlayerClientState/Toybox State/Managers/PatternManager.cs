using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Character;
using System.Linq;

namespace GagSpeak.PlayerState.Toybox;
public sealed class PatternManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly PatternApplier _applier;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    public PatternManager(ILogger<PatternManager> logger, GagspeakMediator mediator,
        PatternApplier applier, FavoritesManager favorites, ConfigFileProvider fileNames,
        HybridSaveService saver) : base(logger, mediator)
    {
        _applier = applier;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
    }

    // Cached Information
    public Pattern? ActiveEditorItem { get; private set; } = null;
    public Pattern? ActivePattern => _applier.ActivePatternInfo;

    // Storage
    public PatternStorage Storage { get; private set; } = new PatternStorage();

    public void OnLogin() { }

    public void OnLogout() { }

    public Pattern CreateNew(string patternName)
    {
        var newPattern = new Pattern() { Label = patternName };
        Storage.Add(newPattern);
        _saver.Save(this);
        Mediator.Publish(new ConfigPatternChanged(StorageItemChangeType.Created, newPattern, null));
        return newPattern;
    }

    public Pattern CreateClone(Pattern other, string newName)
    {
        var clonedItem = new Pattern(other, false) { Label = newName };
        Storage.Add(clonedItem);
        _saver.Save(this);
        Logger.LogDebug($"Cloned pattern {other.Label} to {newName}.");
        Mediator.Publish(new ConfigPatternChanged(StorageItemChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void Rename(Pattern pattern, string newName)
    {
        if (Storage.Contains(pattern))
        {
            Logger.LogDebug($"Storage contained pattern, renaming {pattern.Label} to {newName}.");
            var newNameReal = RegexEx.EnsureUniqueName(newName, Storage, (t) => t.Label);
            pattern.Label = newNameReal;
            Mediator.Publish(new ConfigPatternChanged(StorageItemChangeType.Renamed, pattern, newNameReal));
            _saver.Save(this);
        }
    }

    public void Delete(Pattern pattern)
    {
        if (ActiveEditorItem is null)
            return;

        if (Storage.Remove(pattern))
        {
            Logger.LogDebug($"Deleted pattern {pattern.Label}.");
            Mediator.Publish(new ConfigPatternChanged(StorageItemChangeType.Deleted, pattern, null));
            _saver.Save(this);
        }

    }

    public void StartEditing(Pattern pattern)
    {
        if (Storage.Contains(pattern))
            ActiveEditorItem = pattern;
    }

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing()
        => ActiveEditorItem = null;

    /// <summary> Injects all the changes made to the Restriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (ActiveEditorItem is null)
            return;

        if (Storage.ByIdentifier(ActiveEditorItem.Identifier) is { } item)
        {
            item = ActiveEditorItem;
            ActiveEditorItem = null;
            Mediator.Publish(new ConfigPatternChanged(StorageItemChangeType.Modified, item, null));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the pattern as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool AddFavorite(Pattern pattern)
        => _favorites.TryAddRestriction(FavoriteIdContainer.Pattern, pattern.Identifier);

    /// <summary> Attempts to remove the pattern as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool RemoveFavorite(Pattern pattern)
        => _favorites.RemoveRestriction(FavoriteIdContainer.Pattern, pattern.Identifier);


    public bool CanEnable(Guid patternId)
    {
        if (ActivePattern is not null)
            return false;
        // currently cannot think of any case where this would not be allowed, so mark as true.
        return true;
    }

    public bool CanDisable(Guid patternId)
    {
        if(ActivePattern is null)
            return false;
        // a pattern is running that we can disable.
        return true;
    }

    /// <summary> Switches from a currently active pattern to a new one. </summary>
    /// <remarks> If no pattern is currently active, it will simply start one. </remarks>
    public void SwitchPattern(Guid patternId, string enactor)
    {
        // This only actually fires if a pattern is active, and is skipped otherwise.
        if(ActivePattern is not null)
            DisablePattern(ActivePattern.Identifier, enactor);
        // now enable it.
        EnablePattern(patternId, enactor);
    }

    /// <summary> Enables a pattern, beginning the execution to the simulated, or connected sex toy. </summary>
    /// <remarks> If no pattern in the storage is found, no pattern will activate. </remarks>
    public void EnablePattern(Guid patternId, string enactor)
    {
        if(Storage.TryGetPattern(patternId, out var pattern))
            _applier.StartPlayback(pattern);
    }

    public void DisablePattern(Guid patternId, string enactor)
    {
        if(ActivePattern is not null && ActivePattern.Identifier == patternId)
            _applier.StopPlayback();
    }

    #region HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.Patterns).Item2;
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

    private void Load()
    {
        var file = _fileNames.Patterns;
        Logger.LogWarning("Loading in Config for file: " + file);

        Storage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Patterns file found at {0}", file);
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
                LoadV0(jObject["Patterns"]);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
    }

    private void LoadV0(JToken? data)
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

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable
}
