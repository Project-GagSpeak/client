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
    private const int RequiredFeatureVersion = 3;

    // ID displayed in penumbra when the mod settings are set.
    private const string GAGSPEAK_ID = "ProjectGagSpeak";
    // Key used to associate with glamourer
    // value is Cordy's handle WUV = 01010111 01010101 01010110 = 5723478 (hey, don't cringe! I thought it was cute <3) 
    private const int GAGSPEAK_KEY = 5723478;
    private const int PLAYER_OBJECT_IDX = 0;

    private bool _shownPenumbraUnavailable = false; // safety net to prevent notification spam.
    public static string? ModDirectory { get; private set; } = null;

    private readonly EventSubscriber                                        OnInitialized;
    private readonly EventSubscriber                                        OnDisposed;
    private readonly EventSubscriber<ChangedItemType, uint>                 TooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint>    ItemClickedSubscriber;
    // private readonly EventSubscriber<ModSettingChange, Guid, string, bool>  OnModSettingsChanged;
    private readonly EventSubscriber<nint, int>                             OnRedrawFinished;

    public EventSubscriber<string>                                       OnModAdded;
    public EventSubscriber<string>                                       OnModDeleted;
    public EventSubscriber<string, string>                               OnModMoved;

    private ApiVersion                          Version;               // Obtains the current version of Penumbra's API.
    private RedrawObject                        RedrawClient;          // Can force the client to Redraw.
    private GetModDirectory                     GetModDirectory;       // Retrieves the root mod directory path.
    private GetModPath                          GetModPath;            // Retrieves the path of the mod with its directory and name, allowing for folder sorting.
    private GetModList                          GetModList;            // Retrieves the client's mod list. (DirectoryName, ModName)
    private GetCollection                       GetActiveCollection;   // Obtains the client's currently active collection. (may not need this)
    private GetPlayerMetaManipulations          GetMetaManipulations;  // Obtains the client's mod metadata manipulations.
    private GetAvailableModSettings             GetModSettingsAll;     // Obtains _ALL_ the options for a given mod.
    private GetCurrentModSettings               GetModSettingsSelected;// Obtains the currently chosen options for a mod.
    private GetGameObjectResourcePaths          GetResourcePaths;      // Gets the resource paths for a game object.

    private SetCollectionForObject              SetCollectionForObject;// Defines a collection placed on a spesific object.
    private SetTemporaryModSettingsPlayer       SetOrUpdateTempMod;    // Temporarily sets and locks a Mod with defined settings. Can be updated.
    private RemoveTemporaryModSettingsPlayer    RemoveTempMod;         // Removes a temporary mod we set. Used for cleanup.
    private RemoveAllTemporaryModSettingsPlayer RemoveAllTempMod;      // Removes all temporary mods we set. Used for cleanup.
    private ResolvePlayerPathsAsync             ResolvedClientModPaths;// Resolves the mod paths for the given player.
    public IpcCallerPenumbra(ILogger<IpcCallerPenumbra> logger, GagspeakMediator mediator, OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
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
        OnRedrawFinished = GameObjectRedrawn.Subscriber(Svc.PluginInterface, ObjectRedrawnEvent);

        Version = new ApiVersion(Svc.PluginInterface);
        RedrawClient = new RedrawObject(Svc.PluginInterface);
        GetModDirectory = new GetModDirectory(Svc.PluginInterface);
        GetModPath = new GetModPath(Svc.PluginInterface);
        GetModList = new GetModList(Svc.PluginInterface);
        GetActiveCollection = new GetCollection(Svc.PluginInterface);
        GetMetaManipulations = new GetPlayerMetaManipulations(Svc.PluginInterface);
        GetModSettingsAll = new GetAvailableModSettings(Svc.PluginInterface);
        GetModSettingsSelected = new GetCurrentModSettings(Svc.PluginInterface);
        GetResourcePaths = new GetGameObjectResourcePaths(Svc.PluginInterface);

        SetCollectionForObject = new SetCollectionForObject(Svc.PluginInterface);
        SetOrUpdateTempMod = new SetTemporaryModSettingsPlayer(Svc.PluginInterface);
        RemoveTempMod = new RemoveTemporaryModSettingsPlayer(Svc.PluginInterface);
        RemoveAllTempMod = new RemoveAllTemporaryModSettingsPlayer(Svc.PluginInterface);
        ResolvedClientModPaths = new ResolvePlayerPathsAsync(Svc.PluginInterface);

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

        OnDisposed.Dispose();
        OnInitialized.Dispose();
        TooltipSubscriber.Dispose();
        ItemClickedSubscriber.Dispose();
        OnRedrawFinished.Dispose();
    }

    public void AssignTargetTheClientCollection(ushort objectIdx)
    {
        // grab the current collection.
        var currentCollection = GetActiveCollection.Invoke(ApiCollectionType.Current);
        if (!currentCollection.HasValue)
            return;
        // assign that collection to the object index.
        var result = SetCollectionForObject.Invoke(objectIdx, currentCollection.Value.Id, true, false);
        if (result.Item1 is not PenumbraApiEc.Success)
            Logger.LogError($"Failed to assign collection {currentCollection.Value.Name} to object {objectIdx}, Penumbra returned {result.Item1}");

        Logger.LogInformation($"Assigned collection {currentCollection.Value.Name} to object {objectIdx}.");
        Logger.LogDebug($"Old Collection was: {(result.OldCollection.HasValue ? result.OldCollection.Value.Name : "")}");
    }

    /// <summary> Attempts to perform a manual redraw on the client. </summary>
    /// <remarks> This can also trick mare into receiving updated animations & immediately redrawing, so others see it first time. </remarks>
    public void RedrawObject()
    {
        Logger.LogWarning("Manually redrawing the client!", LoggerType.IpcPenumbra);
        RedrawClient.Invoke(0, RedrawType.Redraw);
    }

    // to get the list of on-screen mod data from a kinkster.
    public async Task<Dictionary<string, HashSet<string>>?> GetKinksterModData(ushort objIdx)
    {
        if (!APIAvailable) return null;

        return await Svc.Framework.RunOnFrameworkThread(() =>
        {
            Logger.LogTrace("Calling On IPC: GetGameObjectResourcePaths");
            return GetResourcePaths.Invoke(objIdx)[0];
        }).ConfigureAwait(false);
    }

    // not sure when we will need these but keep for now.
    public async Task<(string[] forward, string[][] reverse)> ResolveModPathsAsync(string[] forward, string[] reverse)
        => await ResolvedClientModPaths.Invoke(forward, reverse).ConfigureAwait(false);

    public string GetModManipulations()
        => APIAvailable ? GetMetaManipulations.Invoke() : string.Empty;

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


    /// <summary> Used for Setting or updating the settings of a mod, binding it to GagSpeak as a temporary mod. </summary>
    /// <remarks> While active, these mods settings will be locked in penumbra to ensure further helplessness. </remarks>
    public PenumbraApiEc SetOrUpdateTemporaryMod(ModSettingsPreset modPreset)
    {
        if (!APIAvailable)
            return PenumbraApiEc.NothingChanged;

        var readOnlyModSettings = modPreset.ModSettings
            .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value);

        // set, or update, the temporary mod settings for the mod.
        return SetOrUpdateTempMod.Invoke(PLAYER_OBJECT_IDX,
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
        return RemoveTempMod.Invoke(PLAYER_OBJECT_IDX, modPreset.Container.DirectoryPath, GAGSPEAK_KEY, modPreset.Container.ModName);
    }

    /// <summary> Used to clear the temporary mod settings managed by GagSpeak upon plugin shutdown or logout. </summary>
    /// <returns> NothingChanged if IPC API is not available, penumbra status code otherwise </returns>
    public PenumbraApiEc ClearAllTemporaryMods()
    {
        if (!APIAvailable)
            return PenumbraApiEc.NothingChanged;
        // Remove all temporary mods we set.
        return RemoveAllTempMod.Invoke(PLAYER_OBJECT_IDX, GAGSPEAK_KEY);
    }
}
