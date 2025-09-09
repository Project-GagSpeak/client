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

// Contrary to other managers, this collar manager only handles a single collar.
// Treated similarly to MainConfig.cs, with the benifit of a ItemEditor.
public sealed class CollarManager : IHybridSavable
{
    private readonly ILogger<CollarManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ModPresetManager _modPresets;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private SingleItemEditor<GagSpeakCollar> _itemEditor = new();
    private CharaActiveCollar? _serverData = null;

    public CollarManager(ILogger<CollarManager> logger, GagspeakMediator mediator,
        ModPresetManager mods, ConfigFileProvider fileNames, HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _modPresets = mods;
        _fileNames = fileNames;
        _saver = saver;
    }

    // ----------- Stored Data --------------
    public GagSpeakCollar ClientCollar { get; private set; } = new();
    public GagSpeakCollar? ItemInEditor => _itemEditor.ItemInEditor;

    // ----------- ACTIVE INFO --------------
    public bool IsEditing => ItemInEditor is not null;
    public bool IsActive => _serverData?.Applied ?? false;
    public bool ShowVisuals => _serverData?.Visuals ?? false;
    public CharaActiveCollar? SyncedData => _serverData;

    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <remarks> The CacheStateManager must be handled separately here. </remarks>
    public void LoadServerData(CharaActiveCollar serverData)
    {
        _serverData = serverData;
        _logger.LogInformation("Synchronized Active GagSpeakCollar with Client-Side Manager.");
    }

    // For simple renaming without using the item editor.
    public void Rename(string newName)
    {
        // Prevent same name changes.
        if (string.Equals(ClientCollar.Label, newName, StringComparison.Ordinal))
            return;

        // make the name change.
        var oldName = ClientCollar.Label;
        ClientCollar.Label = newName;
        _saver.Save(this);
        _logger.LogDebug($"Renamed collar from [{oldName}] to [{ClientCollar.Label}]");
        _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Renamed, ClientCollar, oldName));
    }

    public void UpdateThumbnail(string newPath)
    {
        _logger.LogDebug($"Thumbnail updated for {ClientCollar.Label} to {ClientCollar.ThumbnailPath}");
        ClientCollar.ThumbnailPath = newPath;
        _saver.Save(this);
        _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Modified, ClientCollar, null));
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing() 
        => _itemEditor.StartEditing(ClientCollar);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() 
        => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagSpeakCollar and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        // Unsure how nessisary this is, might just be able to replace and overwrite after a successful save.
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            _logger.LogDebug($"Saved changes to collar {sourceItem.Label}.");
            _mediator.Publish(new ConfigCollarChanged(StorageChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

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
        if (_serverData is not { } data)
            return;

        // enable the collar along with all of its initial information.
        data.Applied = dto.NewData.Applied; // should always be true here.
        data.OwnerUIDs = dto.NewData.OwnerUIDs;
        data.Visuals = dto.NewData.Visuals;
        data.Dye1 = dto.NewData.Dye1;
        data.Dye2 = dto.NewData.Dye2;
        data.Moodle = dto.NewData.Moodle;
        data.Writing = dto.NewData.Writing;
        data.CollaredAccess = dto.NewData.CollaredAccess;
        data.OwnerAccess = dto.NewData.OwnerAccess;
        
        // Achievements.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.CollarStateChange, data, true, dto.Enactor);
    }

    // there might be an issue server-side where updates can occur while a collar isn't applied maybe? idk.
    // should also have a way to know if the visuals got toggled or whatever but that is a future issue.
    public void UpdateActive(KinksterUpdateActiveCollar dto)
    {
        if (_serverData is not { } data)
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

        // could maybe pass previous and new into here for achievement update types.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.CollarUpdated, data, dto.Type, dto.Enactor);
    }

    public void Remove(KinksterUpdateActiveCollar dto)
    {
        if (_serverData is not { } data)
            return;

        // reset collar to defaults. (Should be the same as the dto.NewData but always play it safe here)
        data = new CharaActiveCollar();

        // trigger achievement.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.CollarStateChange, data, false, dto.Enactor);

    }

    #region HybridSaver
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.CollarData).Item2;

    public void WriteToStream(StreamWriter writer)
        => throw new NotImplementedException();

    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Collar"] = ClientCollar.Serialize(),
        }.ToString(Formatting.Indented);
    }

    // our CUSTOM defined load and migration.
    public void Load()
    {
        var file = _fileNames.CollarData;
        _logger.LogInformation($"Loading CollarData Config: ({file})");
        JObject jObject;
        // Read the json from the file.
        try
        {
            if (!File.Exists(file))
            {
                _logger.LogWarning($"No CollarData Config found at ({file}), creating new one.");
                _saver.Save(this);
                return;
            }

            // read & parse the text by version.
            var jsonText = File.ReadAllText(file);
            jObject = JObject.Parse(jsonText);

            // Read the json from the file.
            var version = jObject["Version"]?.Value<int>() ?? 0;

            // Load the instanced CollarData.          
            switch (version)
            {
                case 0:
                    LoadV0(jObject["Collar"]);
                    break;
                default:
                    _logger.LogError("Invalid Version!");
                    return;
            }

            _logger.LogInformation("Successfully loaded CollarData config.");
            _saver.Save(this);
            _mediator.Publish(new ReloadFileSystem(GagspeakModule.Collar));
        }
        catch (Bagagwa ex) { _logger.LogError("Failed to load config." + ex); }
    }

    private void LoadV0(JToken? data)
        => ClientCollar = GagSpeakCollar.FromToken(data, _modPresets);

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }
    #endregion HybridSaver
}
