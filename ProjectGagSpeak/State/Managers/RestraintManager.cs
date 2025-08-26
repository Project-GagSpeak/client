using CkCommons;
using CkCommons.Classes;
using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using Penumbra.GameData.Enums;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Managers;
public sealed class RestraintManager : IHybridSavable
{
    private readonly ILogger<RestraintManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly RestrictionManager _restrictions;
    private readonly ModPresetManager _modPresets;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<RestraintSet> _itemEditor = new();
    private CharaActiveRestraint? _serverRestraintData = null;

    public RestraintManager(ILogger<RestraintManager> logger, GagspeakMediator mediator,
        RestrictionManager restrictions, ModPresetManager mods, FavoritesManager favorites, 
        ConfigFileProvider fileNames, HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _restrictions = restrictions;
        _modPresets = mods;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
    }

    // ----------- STORAGE --------------
    public RestraintStorage Storage { get; private set; } = new RestraintStorage();
    public RestraintSet? ItemInEditor => _itemEditor.ItemInEditor;

    // ----------- ACTIVE DATA --------------
    public CharaActiveRestraint? ServerData => _serverRestraintData;
    public RestraintSet? AppliedRestraint { get; private set; } = new();

    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <remarks> The CacheStateManager must be handled seperately here. </remarks>
    public void LoadServerData(CharaActiveRestraint serverData)
    {
        _serverRestraintData = serverData;
        // iterate through each of the server's gag data.
        AppliedRestraint = Storage.FirstOrDefault(rs => Guid.Equals(rs.Identifier, serverData.Identifier));
        _logger.LogInformation("Synchronized Active RestraintSet with Client-Side Manager.");
    }

    public RestraintSet CreateNew(string restraintName)
    {
        // Ensure that the new name is unique.
        restraintName = RegexEx.EnsureUniqueName(restraintName, Storage, rs => rs.Label);
        var restraint = new RestraintSet { Label = restraintName };
        Storage.Add(restraint);
        _saver.Save(this);
        _logger.LogDebug($"Created new restraint {restraint.Identifier}.");
        _mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Created, restraint, null));
        return restraint;
    }

    public RestraintSet CreateClone(RestraintSet clone, string newName)
    {
        // Ensure that the new name is unique.
        newName = RegexEx.EnsureUniqueName(newName, Storage, rs => rs.Label);
        var clonedItem = new RestraintSet(clone, false) { Label = newName };
        Storage.Add(clonedItem);
        _saver.Save(this);
        _logger.LogDebug($"Cloned restraint {clonedItem.Identifier}.");
        _mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }


    public void Delete(RestraintSet restraint)
    {
        // should never be able to remove active restraints, but if that happens to occur, add checks here.
        if (Storage.Remove(restraint))
        {
            _logger.LogDebug($"Deleted restraint {restraint.Identifier}.");
            _mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Deleted, restraint, null));
            _saver.Save(this);
        }
    }

    public void Rename(RestraintSet restraint, string newName)
    {
        var oldName = restraint.Label;
        if (oldName == newName)
            return;

        restraint.Label = newName;
        _saver.Save(this);
        _logger.LogDebug($"Renamed restraint {restraint.Identifier}.");
        _mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Renamed, restraint, oldName));
    }

    public void UpdateThumbnail(RestraintSet restraint, string newPath)
    {
        // This could have changed by the time this is called, so get it again.
        if(Storage.Contains(restraint))
        {
            _logger.LogDebug($"Thumbnail updated for {restraint.Label} to {restraint.ThumbnailPath}");
            restraint.ThumbnailPath = newPath;
            _saver.Save(this);
            _mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Modified, restraint, null));
        }
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(RestraintSet item) => _itemEditor.StartEditing(Storage, item);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            _logger.LogDebug($"Saved changes to restraint {sourceItem.Identifier}.");
            // _managerCache.UpdateCache(AppliedRestraint);
            _mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

    public void ToggleVisibility(Guid restraintItem)
    {
        if (Storage.TryGetRestraint(restraintItem, out var item))
        {
            item.IsEnabled = !item.IsEnabled;
            _mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Modified, item));
            _saver.Save(this);
        }
    }

    public void AddFavorite(RestraintSet rs) => _favorites.TryAddRestriction(FavoriteIdContainer.Restraint, rs.Identifier);
    public void RemoveFavorite(RestraintSet rs) => _favorites.RemoveRestriction(FavoriteIdContainer.Restraint, rs.Identifier);

    public bool CanApply() => ServerData is { } d && d.CanApply();
    public bool CanLock() => ServerData is { } d && d.CanLock();
    public bool CanUnlock() => ServerData is { } d && d.CanUnlock();
    public bool CanRemove() => ServerData is { } d && d.CanRemove();

    #region Active Set Updates
    /// <summary> Applies the restraint set for the defined GUID in the DTO. </summary>
    /// <returns> True if visuals should be applied and were set, false otherwise. </returns>
    public bool Apply(CharaActiveRestraint newData, string enactor, [NotNullWhen(true)] out RestraintSet? visualSet)
    {
        visualSet = null;

        if (_serverRestraintData is not { } data)
            return false;

        // update values & ping achievement.
        data.Identifier = newData.Identifier;
        data.Enabler = enactor;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintStateChange, data.Identifier, true, data.Enabler);

        // If we obtain the set here, it means we should apply the visual aspect of this change, otherwise return.
        if (!Storage.TryGetRestraint(data.Identifier, out visualSet))
            return false;

        AppliedRestraint = visualSet;
        return true;
    }

    /// <summary> Sets a new active layer configuration, both applying and removing some layers. </summary>
    /// <returns> True if visuals should be applied and were set, false otherwise. </returns>
    public bool SwapLayers(CharaActiveRestraint newData, string enactor, [NotNullWhen(true)] out RestraintSet? visualSet, out RestraintLayer added, out RestraintLayer removed)
    {
        visualSet = null;
        added = RestraintLayer.None;
        removed = RestraintLayer.None;

        if (_serverRestraintData is not { } data)
            return false;

        // set what layers were added and new.
        added = newData.ActiveLayers & ~data.ActiveLayers;
        removed = data.ActiveLayers & ~newData.ActiveLayers;
        // update the active layers.
        data.ActiveLayers = newData.ActiveLayers;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintLayerChange, data.Identifier, added, removed, enactor);

        // If we obtain the visualSet here, it means we should apply the visual aspect of this change, otherwise return.
        return Storage.TryGetRestraint(data.Identifier, out visualSet);
    }


    /// <summary> Applies the restraint set layer(s) for the current equipped restraint set. </summary>
    /// <returns> True if visuals should be applied and were set, false otherwise. </returns>
    public bool ApplyLayers(RestraintLayer newLayers, string enactor, [NotNullWhen(true)] out RestraintSet? visualSet, out RestraintLayer added)
    {
        visualSet = null;
        added = RestraintLayer.None;

        if (_serverRestraintData is not { } data)
            return false;

        // set what layers were added and new.
        added = newLayers & ~data.ActiveLayers;
        // update the active layers.
        data.ActiveLayers |= added;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintLayerChange, data.Identifier, added, true, enactor);

        // If we obtain the visualSet here, it means we should apply the visual aspect of this change, otherwise return.
        return Storage.TryGetRestraint(data.Identifier, out visualSet);
    }

    public void Lock(CharaActiveRestraint newData, string enactor)
    {
        if (_serverRestraintData is not { } data)
            return;

        data.Padlock = newData.Padlock;
        data.Password = newData.Password;
        data.Timer = newData.Timer;
        data.PadlockAssigner = newData.PadlockAssigner;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, data.Identifier, newData.Padlock, true, enactor);
    }

    public void Unlock(string enactor)
    {
        // Server validated padlock alteration, so simply assign them here and invoke the achievements.
        if (_serverRestraintData is not { } data)
            return;

        var prevLock = data.Padlock;
        var prevAssigner = data.PadlockAssigner;
        data.Padlock = Padlocks.None;
        data.Password = string.Empty;
        data.Timer = DateTimeOffset.MinValue;
        data.PadlockAssigner = string.Empty;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, data.Identifier, prevLock, false, enactor);

        if ((prevAssigner != MainHub.UID) && (enactor != MainHub.UID) && (enactor != prevAssigner))
            GagspeakEventManager.AchievementEvent(UnlocksEvent.SoldSlave);
    }

    public bool RemoveLayers(RestraintLayer remLayers, string enactor, [NotNullWhen(true)] out RestraintSet? visualSet, out RestraintLayer removed)
    {
        visualSet = null;
        removed = RestraintLayer.None;
        if (_serverRestraintData is not { } data)
            return false;
        // Determine which layers are being removed (were present but not anymore)
        removed = data.ActiveLayers & ~remLayers;
        // Remove those layers
        data.ActiveLayers &= ~removed;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintLayerChange, data.Identifier, removed, false, enactor);
        // If we obtain the visualSet here, it means we should apply the visual aspect of this change, otherwise return.
        return Storage.TryGetRestraint(data.Identifier, out visualSet);
    }

    public bool Remove(string enactor, [NotNullWhen(true)] out RestraintSet? visualSet, out RestraintLayer removedLayers)
    {
        visualSet = null;
        removedLayers = RestraintLayer.None;

        if (_serverRestraintData is not { } data)
            return false;

        var removedRestraint = data.Identifier;
        var setEnabler = data.Enabler;
        removedLayers = data.ActiveLayers;
        // Update values, then fire achievements.
        data.Identifier = Guid.Empty;
        data.Enabler = string.Empty;
        data.ActiveLayers = RestraintLayer.None;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintStateChange, removedRestraint, false, enactor);

        // set was applied by one person and removed by another where neither was the client.
        if ((setEnabler != MainHub.UID) && (enactor != MainHub.UID) && (enactor != setEnabler))
            GagspeakEventManager.AchievementEvent(UnlocksEvent.AuctionedOff);

        // Update the affected visual states, if item is enabled.
        if (!Storage.TryGetRestraint(removedRestraint, out visualSet))
            return false;
        
        AppliedRestraint = null;
        return true;
    }
    #endregion Active Set Updates

    #region HybridSaver
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.RestraintSets).Item2;

    public void WriteToStream(StreamWriter writer)
        => throw new NotImplementedException();

    private bool AllowSaving = true;

    public string JsonSerialize()
    {
        if(!AllowSaving)
            throw new Exception("Attempted to serialize RestraintManager while saving is disabled.");

        var restraintSets = new JArray();
        foreach (var set in Storage)
        {
            try
            {
                restraintSets.Add(set.Serialize());
            }
            catch (Bagagwa e)
            {
                _logger.LogError($"Failed to serialize RestraintSet: {e}");
            }
        }

        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["RestraintSets"] = restraintSets
        }.ToString(Formatting.Indented);
    }

    // our CUSTOM defined load and migration.
    public void Load()
    {
        var file = _fileNames.RestraintSets;
        _logger.LogInformation($"Loading in Restraints Config for file: {file}");

        Storage.Clear();
        JObject jObject;
        // Read the json from the file.
        if (!File.Exists(file))
        {
            _logger.LogWarning($"No Restraints Config file found at {file}");
            // create a new file with default values.

            var oldFormatFile = Path.Combine(_fileNames.CurrentPlayerDirectory, "wardrobe.json");
            if (File.Exists(oldFormatFile))
            {
                var oldText = File.ReadAllText(oldFormatFile);
                var oldObject = JObject.Parse(oldText);
                jObject = ConfigMigrator.MigrateWardrobeConfig(oldObject, _fileNames, oldFormatFile);
            }
            else
            {
                _logger.LogWarning("No Config file found for: " + oldFormatFile);
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
        // Perform Migrations if any, and then load the data.

        var version = jObject["Version"]?.Value<int>() ?? 0;
        switch (version)
        {
            case 0:
                LoadV0(jObject["RestraintSets"]);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        _mediator.Publish(new ReloadFileSystem(GagspeakModule.Restraint));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JArray restraintArray)
            return;

        // otherwise, parse it out and stuff YIPPEE
        foreach (var setToken in restraintArray)
        {
            try
            {
                // probably the single line of code that i fear the most out of this entire plugin.
                var loadedSet = RestraintSet.FromToken(setToken, _modPresets, _restrictions);
                Storage.Add(loadedSet);
                _logger.LogInformation($"LOADED IN RETRAINTSET: {loadedSet.Label}");
            }
            catch (Bagagwa ex)
            {
                _logger.LogError($"Failed to load Restraint Set.\nError {ex}\nFrom JSON: {setToken}");
                // Do not allow this to continue loading, just fucking crash the game i dont care. We need to see why it didnt load.
                AllowSaving = false;
            }
        }

    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }
    #endregion HybridSaver
}
