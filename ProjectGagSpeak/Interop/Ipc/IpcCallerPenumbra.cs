using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Common.Lua;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop.WinRT;

namespace GagSpeak.Interop;

/// <summary> Contains all information about a mod. </summary>
/// <remarks> This should be used as a way to synchronize with storage, but not intertwined with it. </remarks>
public record ModInfo(
    string DirPath, // where it's located on the computer, local to the root path.
    string Name, // the name in the mod that displays on the title.
    string FsPath, // the penumbra filesystem path the mod has, including its folder.
    int Priority, // current priority.
    Dictionary<string, (string[] Options, GroupType GroupType)> AllSettings // ALL available settings.
) : IComparable<ModInfo>
{
    public ModInfo() 
        : this(string.Empty, string.Empty, string.Empty, 0, new Dictionary<string, (string[] Options, GroupType GroupType)>())
    { }

    public int CompareTo(ModInfo? other)
    {
        if (other is null) return 1;

        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        return nameComparison != 0
            ? nameComparison
            : string.Compare(DirPath, other.DirPath, StringComparison.Ordinal);
    }

    public override string ToString()
        => $"[{Name}] ({DirPath}) - Priority<{Priority}> - Path: {FsPath}";
}

public class IpcCallerPenumbra : DisposableMediatorSubscriberBase, IIpcCaller
{
    private int API_CurrentMajor = 0;
    private int API_CurrentMinor = 0;
    private const int RequiredBreakingVersion = 5;
    private const int RequiredFeatureVersion = 8;

    // ID displayed in penumbra when the mod settings are set.
    private const string GAGSPEAK_ID = "ProjectGagSpeak";
    // Key used to associate with glamourer
    // value is Cordy's handle WUV = 01010111 01010101 01010110 = 5723478 (hey, don't cringe! I thought it was cute <3) 
    private const int GAGSPEAK_KEY = 5723478;

    private bool _shownPenumbraUnavailable = false; // safety net to prevent notification spam.
    public static string? ModDirectory { get; private set; } = null;

    // API Version
    private ApiVersion Version;
    // API Events
    private readonly EventSubscriber                                        OnInitialized;
    private readonly EventSubscriber                                        OnDisposed;
    private readonly EventSubscriber<ChangedItemType, uint>                 TooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint>    ItemClickedSubscriber;
    private readonly EventSubscriber<ModSettingChange, Guid, string, bool>  OnModSettingsChanged;
    private readonly EventSubscriber<nint, int>                             OnRedrawFinished;
    // API Public Events
    public EventSubscriber<string>         OnModAdded;
    public EventSubscriber<string>         OnModDeleted;
    public EventSubscriber<string, string> OnModMoved;
    // API Getters
    private GetModDirectory             GetModDirectory;       // Retrieves the root mod directory path.
    private GetModPath                  GetModPath;            // Retrieves the path of the mod with its directory and name, allowing for folder sorting.
    private GetModList                  GetModList;            // Retrieves the client's mod list. (DirectoryName, ModName)
    private GetCollection               GetActiveCollection;   // Obtains the client's currently active collection. (may not need this)
    private GetPlayerMetaManipulations  GetMetaManipulations;  // Obtains the client's mod metadata manipulations.
    private GetAvailableModSettings     GetModSettingsAll;     // Obtains _ALL_ the options for a given mod.
    private GetCurrentModSettings       GetModSettingsSelected;// Obtains the currently chosen options for a mod.
    // API Enactors
    private RedrawObject                        RedrawClient;               // Can force the client to Redraw.
    private SetCollectionForObject              SetCollectionForObject;     // Defines a collection placed on a spesific object.
    private SetTemporaryModSettingsPlayer       SetOrUpdateTempModSettings; // Temporarily sets and locks a Mod with defined settings. Can be updated.
    private RemoveTemporaryModSettingsPlayer    RemoveTempModSettings;      // Removes a temporary mod we set. Used for cleanup.
    private RemoveAllTemporaryModSettingsPlayer RemoveAllTempModSettings;   // Removes all temporary mods we set. Used for cleanup.
    // API Mod Synchronization (Can include any from above catagories)
    private GetGameObjectResourcePaths  GetObjectResourcePaths;
    private ConvertTextureFile          ConvertModTexture;
    private ResolvePlayerPathsAsync     ResolveOnScreenActorPaths;
    private CreateTemporaryCollection   CreateTempCollection;
    private AssignTemporaryCollection   AssignTempCollection;
    private AddTemporaryMod             AddTempMod;
    private RemoveTemporaryMod          RemoveTempMod;
    private DeleteTemporaryCollection   DeleteTempCollection;
    public IpcCallerPenumbra(ILogger<IpcCallerPenumbra> logger, GagspeakMediator mediator, OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        // API Version.
        Version = new ApiVersion(Svc.PluginInterface);
        // Events
        OnInitialized = Initialized.Subscriber(Svc.PluginInterface, () => 
        {
            APIAvailable = true;
            CheckModDirectory();
            Mediator.Publish(new PenumbraInitialized());
        });
        OnDisposed = Disposed.Subscriber(Svc.PluginInterface, () =>
        {
            APIAvailable = false;
            Mediator.Publish(new PenumbraDisposed());
        });
        TooltipSubscriber = ChangedItemTooltip.Subscriber(Svc.PluginInterface);
        ItemClickedSubscriber = ChangedItemClicked.Subscriber(Svc.PluginInterface);
        // OnModSettingsChanged = ModSettingChanged.Subscriber(Svc.PluginInterface, (c, id, modDir, temp) => Mediator.Publish(new PenumbraSettingsChanged()));
        OnRedrawFinished = GameObjectRedrawn.Subscriber(Svc.PluginInterface, ObjectRedrawnEvent);
        // Getters
        GetModDirectory = new GetModDirectory(Svc.PluginInterface);
        GetModPath = new GetModPath(Svc.PluginInterface);
        GetModList = new GetModList(Svc.PluginInterface);
        GetActiveCollection = new GetCollection(Svc.PluginInterface);
        GetMetaManipulations = new GetPlayerMetaManipulations(Svc.PluginInterface);
        GetModSettingsAll = new GetAvailableModSettings(Svc.PluginInterface);
        GetModSettingsSelected = new GetCurrentModSettings(Svc.PluginInterface);
        // Enactors
        RedrawClient = new RedrawObject(Svc.PluginInterface);
        SetCollectionForObject = new SetCollectionForObject(Svc.PluginInterface);
        SetOrUpdateTempModSettings = new SetTemporaryModSettingsPlayer(Svc.PluginInterface);
        RemoveTempModSettings = new RemoveTemporaryModSettingsPlayer(Svc.PluginInterface);
        RemoveAllTempModSettings = new RemoveAllTemporaryModSettingsPlayer(Svc.PluginInterface);
        // Mod Synchronization
        GetObjectResourcePaths = new GetGameObjectResourcePaths(Svc.PluginInterface);
        ConvertModTexture = new ConvertTextureFile(Svc.PluginInterface);
        ResolveOnScreenActorPaths = new ResolvePlayerPathsAsync(Svc.PluginInterface);
        CreateTempCollection = new CreateTemporaryCollection(Svc.PluginInterface);
        AssignTempCollection = new AssignTemporaryCollection(Svc.PluginInterface);
        AddTempMod = new AddTemporaryMod(Svc.PluginInterface);
        RemoveTempMod = new RemoveTemporaryMod(Svc.PluginInterface);
        DeleteTempCollection = new DeleteTemporaryCollection(Svc.PluginInterface);

        CheckAPI();
        CheckModDirectory();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            (API_CurrentMajor, API_CurrentMinor) = Version.Invoke();
        }
        catch
        {
            API_CurrentMajor = 0; API_CurrentMinor = 0;
        }

        // State in which version is invalid.
        APIAvailable = (API_CurrentMajor != RequiredBreakingVersion || API_CurrentMinor < RequiredFeatureVersion) ? false : true;

        // the penumbra unavailable flag
        _shownPenumbraUnavailable = _shownPenumbraUnavailable && !APIAvailable;

        if (!APIAvailable && !_shownPenumbraUnavailable)
        {
            _shownPenumbraUnavailable = true;
            Logger.LogError($"Invalid Version {API_CurrentMajor}.{API_CurrentMinor:D4}, required major " +
                $"Version {RequiredBreakingVersion} with feature greater or equal to {RequiredFeatureVersion}.");
            Mediator.Publish(new NotificationMessage("Penumbra inactive", "Features using Penumbra will not function properly.", NotificationType.Error));
        }
    }

    public void CheckModDirectory()
    {
        var value = !APIAvailable ? string.Empty : GetModDirectory!.Invoke().ToLowerInvariant();
        if (!string.Equals(ModDirectory, value, StringComparison.Ordinal))
        {
            ModDirectory = value;
            Mediator.Publish(new PenumbraDirectoryChanged(ModDirectory));
        }
    }

    public event Action<MouseButton, ChangedItemType, uint> Click
    {
        add => ItemClickedSubscriber.Event += value;
        remove => ItemClickedSubscriber.Event -= value;
    }

    public event Action<ChangedItemType, uint> Tooltip
    {
        add => TooltipSubscriber.Event += value;
        remove => TooltipSubscriber.Event -= value;
    }

    private void ObjectRedrawnEvent(IntPtr objectAddress, int objectTableIndex)
    {
        // We can do something here when the object is the client player (0), but unknown yet.
    }

    protected override void Dispose(bool disposing)
    {
        // call disposal of IPC subscribers
        base.Dispose(disposing);

        // clear all temporary mods.
        ClearAllTemporaryMods();

        // OnModSettingsChanged.Dispose();
        OnDisposed.Dispose();
        OnInitialized.Dispose();
        TooltipSubscriber.Dispose();
        ItemClickedSubscriber.Dispose();
        OnRedrawFinished.Dispose();
    }

    /// <summary> Attempts to perform a manual redraw on the client. </summary>
    public void RedrawObject()
    {
        Logger.LogWarning("Manually redrawing the client!", LoggerType.IpcPenumbra);
        RedrawClient.Invoke(0, RedrawType.Redraw);
    }
    
    public string GetClientManipulations()
        => APIAvailable ? GetMetaManipulations.Invoke() : string.Empty;

    #region Mod Collection Synchronization
    public async Task AssignKinksterCollection(Guid id, int objIdx)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            var ret = AssignTempCollection.Invoke(id, objIdx, true);
            Logger.LogTrace($"Assigning Kinkster Collection to {Svc.Objects[objIdx]?.Name ?? "UNK"}, Success: [{ret}] ({id})");
            return ret;
        }).ConfigureAwait(false);
    }

    // Here for Yinah to use later if they want.
    public async Task CompressTexture(Dictionary<string, string[]> textures, IProgress<(string, int)> progress, CancellationToken token)
    {
        if (!APIAvailable) return;

        // Inform other areas parsing files to halt then scan of textures until the method is finished.
        // this may not even be nessisary if we centralize and await the method on its own properly.
        Mediator.Publish(new HaltFileScan(nameof(CompressTexture)));

        int currentTexture = 0;
        foreach (var texture in textures)
        {
            if (token.IsCancellationRequested) break;

            progress.Report((texture.Key, ++currentTexture));

            Logger.LogInformation($"Converting Texture {texture.Key} to {TextureType.Bc7Tex}");
            // run the conversion and await it.
            var convertTask = ConvertModTexture.Invoke(texture.Key, texture.Key, TextureType.Bc7Tex, mipMaps: true);
            await convertTask.ConfigureAwait(false);
            // once complete, verify if the conversion left any duplicates to copy over.
            if (convertTask.IsCompletedSuccessfully && texture.Value.Any())
            {
                foreach (var duplicatedTexture in texture.Value)
                {
                    Logger.LogInformation($"Migrating duplicate {duplicatedTexture}");
                    Generic.Safe(() => File.Copy(texture.Key, duplicatedTexture, overwrite: true));
                }
            }
        }

        Mediator.Publish(new ResumeFileScan(nameof(CompressTexture)));

        // redraw the object that had it's settings changed (maybe can remove this, not sure, seems unessisary)
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (Svc.Objects.CreateObjectReference(PlayerData.ObjectAddress) is { } obj)
                RedrawClient.Invoke(obj.ObjectIndex, setting: RedrawType.Redraw);
        }).ConfigureAwait(false);

        // Run the mod conversion for the textures within the framework thread.
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            int count = 0;
            int total = textures.Count;
            foreach (var tex in textures)
            {
                if (token.IsCancellationRequested) break;

                string inputFile = tex.Key;
                foreach (var outputFile in tex.Value)
                {
                    if (token.IsCancellationRequested) break;
                    
                    Logger.LogTrace($"Converting Texture: {inputFile} => {outputFile}");
                    // was origiunally awaited but throws lots of obsoletes when included so see if fine for now.
                    ConvertModTexture.Invoke(inputFile, outputFile, TextureType.Bc7Dds, true).ConfigureAwait(false);
                }
                count++;
                progress.Report((inputFile, (int)((count / (float)total) * 100)));
            }
        }).ConfigureAwait(false);
    }

    // Create a new temporary collection for the kinkster to use.
    public async Task<Guid> CreateKinksterCollection(string kinksterUid)
    {
        if (!APIAvailable) return Guid.Empty;

        return await Svc.Framework.RunOnFrameworkThread(() =>
        {
            var collectionName = $"KinksterCache_{kinksterUid}";
            var ret = CreateTempCollection.Invoke(GAGSPEAK_ID, collectionName, out Guid id);
            Logger.LogTrace($"Temp Collection Created for Identity [{GAGSPEAK_ID}] for ({collectionName}), given ID: {id} [RetCode: {ret}]");
            return id;
        }).ConfigureAwait(false);
    }

    // to get the list of on-screen mod data from a kinkster.
    public async Task<Dictionary<string, HashSet<string>>?> GetKinksterModData(ushort objIdx)
    {
        if (!APIAvailable) return null;

        return await Svc.Framework.RunOnFrameworkThread(() =>
        {
            Logger.LogTrace("Calling On IPC: GetGameObjectResourcePaths");
            return GetObjectResourcePaths.Invoke(objIdx)[0];
        }).ConfigureAwait(false);
    }

    public async Task RemoveKinksterCollection(Guid id)
    {
        if (!APIAvailable) return;

        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            Logger.LogTrace($"Removing Kinkster Collection: {id}");
            var ret = DeleteTempCollection.Invoke(id);
            Logger.LogTrace($"Deleted Kinkster Collection: {id}, Success: [{ret}]");
        }).ConfigureAwait(false);
    }

    // not sure when we will need these but keep for now.
    public async Task<(string[] forward, string[][] reverse)> ResolveModPaths(string[] forward, string[] reverse)
        => await ResolveOnScreenActorPaths.Invoke(forward, reverse).ConfigureAwait(false);

    public async Task SetKinksterManipulations(Guid collection, string manipData)
    {
        if (!APIAvailable) return;

        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            Logger.LogTrace($"Setting Manipulation Data for Collection: {collection}");
            // do this by adding a temporary mod that only includes the metadata manipulations.
            // keep the same name so it is replaced each time.
            var retAdded = AddTempMod.Invoke("GS_Kinkster_Meta", collection, [], manipData, 0);
            Logger.LogTrace($"Added Manipulation Mod for Collection: {collection}, Success: [{retAdded}]");
        }).ConfigureAwait(false);
    }
    
    // where we would create a mod defined by the modpaths within our cache directory stored.
    public async Task AssignKinksterMods(Guid collection, Dictionary<string, string> modPaths)
    {
        if (!APIAvailable) return;

        Logger.LogTrace($"Assigning Temporary Mod to Collection [{collection}] using ({modPaths.Count}) modpath replacements.");
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            foreach (var mod in modPaths)
                Logger.LogTrace($"[TempModCreation] Change: {mod.Key} => {mod.Value}");
            // remove the existing temporary mod (seems incredibly inefficient but maybe we can optimize later?)
            var retRemove = RemoveTempMod.Invoke("GS_Kinkster_ModFiles", collection, 0);
            Logger.LogTrace($"Removed Existing Temp Mod for Collection: {collection}, Success: [{retRemove}]");
            // add the new temporary mod with the new paths.
            var retAdded = AddTempMod.Invoke("GS_Kinkster_ModFiles", collection, modPaths, string.Empty, 0);
            Logger.LogTrace($"Added Temp Mod for Collection: {collection}, Success: [{retAdded}]");
        }).ConfigureAwait(false);
    }
    #endregion Mod Collection Synchronization

    #region Mod Information
    // When penumbra first initializes, we should fetch all current mod info to synchronize our current data.
    // This should return all mod info's along with their current settings.
    public IReadOnlyList<(ModInfo ModInfo, Dictionary<string, List<string>> CurrentSettings)> GetModListInfo()
    {
        if (!APIAvailable)
            return Array.Empty<(ModInfo ModInfo, Dictionary<string, List<string>> CurrentSettings)>();

        try
        {
            // Get the mod information and the collection info first
            var allMods = GetModList.Invoke();
            var collection = GetActiveCollection.Invoke(ApiCollectionType.Current);

            return allMods
                // Select the mod,      its current settings,                                       all available settings,             and FileSystemPath.
                .Select(mod => (mod, GetModSettingsSelected.Invoke(collection!.Value.Id, mod.Key), GetModSettingsAll.Invoke(mod.Key), GetModPath.Invoke(mod.Key)))
                // From here, create the mod info item, and the mod current settings item.
                .Select(t =>
                {
                    // Mods don't necessarily need to have any valid settings associated with them in a collection.
                    // If a mod does not have settings in the current collection, we give it 0 defaults and empty dictionary.
                    var priority = t.Item2.Item2.HasValue ? t.Item2.Item2!.Value.Item2 : 0;
                    var allSettings = t.Item3 is not null && t.Item3.Count > 0
                        ? t.Item3!.ToDictionary(t => t.Key, t => t.Value)
                        : new Dictionary<string, (string[] Options, GroupType GroupType)>();

                    var ModInfo = new ModInfo(t.mod.Key, t.mod.Value, t.Item4.FullPath, priority, allSettings);
                    var ModSettings = t.Item2.Item2.HasValue ? t.Item2.Item2!.Value.Item3 : new Dictionary<string, List<string>>();
                    return (ModInfo, ModSettings);
                })
                .OrderBy(p => p.ModInfo.Name)
                .ThenBy(p => p.ModInfo.DirPath)
                .ThenByDescending(p => p.ModInfo.Priority)
                .ToList();
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Error fetching mods from Penumbra:\n{ex}");
            return Array.Empty<(ModInfo ModInfo, Dictionary<string, List<string>> CurrentSettings)>();
        }
    }

    public (ModInfo Info, Dictionary<string, List<string>> CurrentSettings) GetModInfo(string directory)
    {
        if (!APIAvailable)
            return (new ModInfo(), new());

        var allMods = GetModList.Invoke();
        if (!allMods.TryGetValue(directory, out var modName))
            return (new ModInfo(), new());

        var collection = GetActiveCollection.Invoke(ApiCollectionType.Current);
        if (collection is null)
            return (new ModInfo(), new());

        var currentSettings = GetModSettingsSelected.Invoke(collection.Value.Id, directory);
        if (currentSettings.Item1 is not PenumbraApiEc.Success || !currentSettings.Item2.HasValue)
            return (new ModInfo(), new());

        var modPathRes = GetModPath.Invoke(directory);
        if (modPathRes.Item1 is not PenumbraApiEc.Success)
            return (new ModInfo(), new());

        var allSettingsResult = GetModSettingsAll.Invoke(directory);
        var allSettings = (allSettingsResult?.Count > 0)
            ? allSettingsResult.ToDictionary(t => t.Key, t => t.Value)
            : new();

        var modInfo = new ModInfo(directory, modName, modPathRes.Item2, currentSettings.Item2.Value.Item2, allSettings);
        return (modInfo, currentSettings.Item2.Value.Item3);
    }


    public (PenumbraApiEc, string FullPath, bool FullDefault, bool NameDefault) GetFileSystemModPath(string directory, string modName = "")
    {
        if (!APIAvailable)
            return (PenumbraApiEc.NothingChanged, string.Empty, false, false);
        // Get the mod path from penumbra.
        var res = GetModPath.Invoke(directory, modName);
        if (res.Item1 is not PenumbraApiEc.Success)
            return (res.Item1, string.Empty, false, false);

        return res;
    }


    // Sometimes, we only want simply the updated name (Sadly penumbra has no quick way of doing this)
    public bool TryGetModName(string directory, [NotNullWhen(true)] out string? name)
    {
        if (!APIAvailable)
            return (name = null) is not null;

        return GetModList.Invoke().TryGetValue(directory, out name);
    }

    // Sometimes, we have the directory and name, but only want to get the current settings for that specific mod.
    public Dictionary<string, List<string>> GetCurrentSettings(string directory)
    {
        if (!APIAvailable)
            return new Dictionary<string, List<string>>();

        var collection = GetActiveCollection.Invoke(ApiCollectionType.Current);
        var res = GetModSettingsSelected.Invoke(collection!.Value.Id, directory);
        if (res.Item1 is not PenumbraApiEc.Success)
            return new Dictionary<string, List<string>>();

        return (res.Item2.HasValue) ? res.Item2!.Value.Item3 : new Dictionary<string, List<string>>();
    }

    // Sometimes, we only want to get all options for a single mod. (maybe remove later)
    public IReadOnlyDictionary<string, (string[] Options, GroupType GroupType)> GetAllOptions(string directory)
    {
        if (!APIAvailable)
            return new Dictionary<string, (string[] Options, GroupType GroupType)>();

        var res = GetModSettingsAll.Invoke(directory);
        return res is not null ? res : new Dictionary<string, (string[] Options, GroupType GroupType)>();
    }
    #endregion Mod Information

    #region Temporary Mod Alterations
    /// <summary> Used for Setting or updating the settings of a mod, binding it to GagSpeak as a temporary mod. </summary>
    /// <remarks> While active, these mods settings will be locked in penumbra to ensure further helplessness. </remarks>
    public PenumbraApiEc SetOrUpdateTemporaryMod(ModSettingsPreset modPreset)
    {
        if (!APIAvailable)
            return PenumbraApiEc.NothingChanged;

        var readOnlyModSettings = modPreset.ModSettings
            .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value);

        // set, or update, the temporary mod settings for the mod.
        return SetOrUpdateTempModSettings.Invoke(0,
            modPreset.Container.DirectoryPath,
            false,
            true,
            modPreset.Container.Priority + 25,
            readOnlyModSettings,
            GAGSPEAK_ID,
            GAGSPEAK_KEY,
            modPreset.Container.ModName);
    }

    /// <summary> Used for removing a temporary mod from the client. </summary>
    /// <returns> The penumbra status code of success. </returns>
    public PenumbraApiEc RemoveTemporaryMod(ModSettingsPreset modPreset)
    {
        if (!APIAvailable)
            return PenumbraApiEc.NothingChanged;

        // Remove the temporary mod we set. This also undoes the priority shift we set for it.
        return RemoveTempModSettings.Invoke(0, modPreset.Container.DirectoryPath, GAGSPEAK_KEY, modPreset.Container.ModName);
    }

    /// <summary> Used to clear the temporary mod settings managed by GagSpeak upon plugin shutdown or logout. </summary>
    /// <returns> NothingChanged if IPC API is not available, penumbra status code otherwise </returns>
    public PenumbraApiEc ClearAllTemporaryMods()
    {
        if (!APIAvailable)
            return PenumbraApiEc.NothingChanged;
        // Remove all temporary mods we set.
        return RemoveAllTempModSettings.Invoke(0, GAGSPEAK_KEY);
    }
    #endregion Temporary Mod Alterations
}
