using CkCommons;
using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Network;

namespace GagSpeak.State.Managers;
public sealed class CollarManager : IHybridSavable
{
    private readonly ILogger<CollarManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ModPresetManager _modPresets;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<GagSpeakCollar> _itemEditor = new();
    private CharaActiveCollar? _serverCollarData = null;

    public CollarManager(ILogger<CollarManager> logger, GagspeakMediator mediator,
        ModPresetManager mods, FavoritesManager favorites, ConfigFileProvider fileNames, 
        HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _modPresets = mods;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
    }

    // ----------- STORAGE --------------
    public CollarStorage Storage { get; private set; } = new CollarStorage();
    public GagSpeakCollar? ItemInEditor => _itemEditor.ItemInEditor;

    // ----------- ACTIVE DATA --------------
    public CharaActiveCollar? ServerCollarData => _serverCollarData;
    public GagSpeakCollar? ActiveCollarData { get; private set; } = new();
    public bool CollarWithVisualsActive => ServerCollarData != null && ActiveCollarData != null;

    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <remarks> The CacheStateManager must be handled separately here. </remarks>
    public void LoadServerData(CharaActiveCollar serverData)
    {
        _serverCollarData = serverData;
        ActiveCollarData = Storage.FirstOrDefault(rs => rs.Identifier.Equals(serverData.Identifier));
        _logger.LogInformation("Synchronized Active GagSpeakCollar with Client-Side Manager.");
    }
     
    public GagSpeakCollar CreateNew(string name)
    {
        // Ensure that the new name is unique.
        name = RegexEx.EnsureUniqueName(name, Storage, rs => rs.Label);
        var collar = new GagSpeakCollar { Label = name };
        Storage.Add(collar);
        _saver.Save(this);
        _logger.LogDebug($"Created new collar: {collar.Label} ({collar.Identifier})");
        _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Created, collar, null));
        return collar;
    }

    public void Delete(GagSpeakCollar collar)
    {
        // should never be able to remove active collars, but if that happens to occur, add checks here.
        if (Storage.Remove(collar))
        {
            _logger.LogDebug($"Deleted collar {collar.Identifier}.");
            _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Deleted, collar, null));
            _saver.Save(this);
        }
    }

    public void Rename(GagSpeakCollar collar, string newName)
    {
        var oldName = collar.Label;
        if (oldName == newName)
            return;

        collar.Label = newName;
        _saver.Save(this);
        _logger.LogDebug($"Renamed collar {collar.Identifier}.");
        _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Renamed, collar, oldName));
    }

    public void UpdateThumbnail(GagSpeakCollar collar, string newPath)
    {
        // This could have changed by the time this is called, so get it again.
        if(Storage.Contains(collar))
        {
            _logger.LogDebug($"Thumbnail updated for {collar.Label} to {collar.ThumbnailPath}");
            collar.ThumbnailPath = newPath;
            _saver.Save(this);
            _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Modified, collar, null));
        }
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(GagSpeakCollar item) 
        => _itemEditor.StartEditing(Storage, item);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() 
        => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagSpeakCollar and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            _logger.LogDebug($"Saved changes to collar {sourceItem.Identifier}.");
            _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

    public void AddFavorite(GagSpeakCollar rs) 
        => _favorites.TryAddRestriction(FavoriteIdContainer.Collar, rs.Identifier);
    public void RemoveFavorite(GagSpeakCollar rs)
        => _favorites.RemoveRestriction(FavoriteIdContainer.Collar, rs.Identifier);

    /// <summary> 
    ///     Applies the collar for the defined GUID. Assumes no collar is on. <para />
    /// 
    ///     Unlike other visual managers, this does not need to pass out the storage item, 
    ///     as it is set to <see cref="AppliedCollar"/> if visuals are enabled. <para />
    ///     
    ///     This is how we know when to make changes. (but may need to revisit if we find caveats).
    /// </summary>
    public void Apply(KinksterUpdateActiveCollar dto)
    {
        if (_serverCollarData is not { } data)
            return;

        // enable the collar along with all of its initial information.
        data.Identifier = dto.NewData.Identifier;
        data.OwnerUIDs = dto.NewData.OwnerUIDs;
        data.Visuals = dto.NewData.Visuals;
        data.Dye1 = dto.NewData.Dye1;
        data.Dye2 = dto.NewData.Dye2;
        data.Moodle = dto.NewData.Moodle;
        data.Writing = dto.NewData.Writing;
        data.CollaredAccess = dto.NewData.CollaredAccess;
        data.OwnerAccess = dto.NewData.OwnerAccess;
        
        // Achievements.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.CollarStateChange, data.Identifier, true, dto.Enactor);

        // If we obtain the set here, it means we should apply the visual aspect of this change, otherwise return.
        if (Storage.TryGetCollar(data.Identifier, out var collarItem) && data.Visuals)
            ActiveCollarData = collarItem;
    }

    // there might be an issue server-side where updates can occur while a collar isn't applied maybe? idk.
    // should also have a way to know if the visuals got toggled or whatever but that is a future issue.
    public void UpdateActive(KinksterUpdateActiveCollar dto)
    {
        if (_serverCollarData is not { } data)
            return;

        // Update the collar data based on the state.
        switch (dto.Type)
        {
            case DataUpdateType.OwnersUpdated:
                data.OwnerUIDs = dto.NewData.OwnerUIDs;
                break;
            case DataUpdateType.VisibilityChange:
                data.Visuals = !data.Visuals;
                // Still keep the active collar data, just have visuals off.
                break;
            case DataUpdateType.DyesChange:
                data.Dye1 = dto.NewData.Dye1;
                data.Dye2 = dto.NewData.Dye2;
                break;
            case DataUpdateType.CollarMoodleChange:
                data.Moodle = dto.NewData.Moodle;
                break;
            case DataUpdateType.CollarWritingChange:
                data.Writing = dto.NewData.Writing;
                break;
            case DataUpdateType.CollarPermChange:
                data.CollaredAccess = dto.NewData.CollaredAccess;
                break;
            case DataUpdateType.CollarOwnerPermChange:
                data.OwnerAccess = dto.NewData.OwnerAccess;
                break;

            default: // prevent other cases from triggering achievement.
                return;
        }

        GagspeakEventManager.AchievementEvent(UnlocksEvent.CollarUpdated, data, dto.Type, dto.Enactor);
    }

    public void Remove(KinksterUpdateActiveCollar dto)
    {
        if (_serverCollarData is not { } data)
            return;

        // reset collar to defaults.
        data = new CharaActiveCollar();
        ActiveCollarData = null;

        // trigger achievement.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.CollarStateChange, dto.PreviousCollar, false, dto.Enactor);

    }

    #region HybridSaver
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.Collars).Item2;

    public void WriteToStream(StreamWriter writer)
        => throw new NotImplementedException();

    private bool AllowSaving = true;

    public string JsonSerialize()
    {
        if(!AllowSaving)
            throw new Exception("Attempted to serialize CollarManager while saving is disabled.");

        var collars = new JArray();
        foreach (var set in Storage)
            Generic.Safe(() => collars.Add(set.Serialize()));

        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Collars"] = collars
        }.ToString(Formatting.Indented);
    }

    // our CUSTOM defined load and migration.
    public void Load()
    {
        var file = _fileNames.Collars;
        _logger.LogInformation($"Loading Collars Config: ({file})");

        Storage.Clear();
        JObject jObject;
        // Read the json from the file.
        if (!File.Exists(file))
        {
            _logger.LogWarning($"No Collars Config found at ({file}), creating new one.");
            _saver.Save(this);
            return;
        }

        // read & parse the text by version.
        var jsonText = File.ReadAllText(file);
        jObject = JObject.Parse(jsonText);
        
        var version = jObject["Version"]?.Value<int>() ?? 0;
        switch (version)
        {
            case 0:
                LoadV0(jObject["Collars"]);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        _mediator.Publish(new ReloadFileSystem(GagspeakModule.Collar));
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
                Storage.Add(GagSpeakCollar.FromToken(setToken, _modPresets));
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
