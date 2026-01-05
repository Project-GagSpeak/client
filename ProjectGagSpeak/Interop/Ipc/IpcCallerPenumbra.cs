using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using System.Diagnostics.CodeAnalysis;

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
    private readonly EventSubscriber OnInitialized;
    private readonly EventSubscriber OnDisposed;

    // API Events
    private readonly EventSubscriber<ChangedItemType, uint>                 TooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint>    ItemClickedSubscriber;
    private readonly EventSubscriber<ModSettingChange, Guid, string, bool>  OnModSettingsChanged;
    private readonly EventSubscriber<nint, string, string>                  OnGameObjectResourcePathResolved;
    private readonly EventSubscriber<nint, int>                             OnRedrawFinished;
    // API Public Events
    public EventSubscriber<string>          OnModAdded;
    public EventSubscriber<string>          OnModDeleted;
    public EventSubscriber<string, string>  OnModMoved;
    // API Getters
    private GetModDirectory         GetModDirectory;       // Retrieves the root mod directory path.
    private GetModPath              GetModPath;            // Retrieves the path of the mod with its directory and name, allowing for folder sorting.
    private GetModList              GetModList;            // Retrieves the client's mod list. (DirectoryName, ModName)
    private GetCollection           GetActiveCollection;   // Obtains the client's currently active collection. (may not need this)
    private GetAvailableModSettings GetModSettingsAll;     // Obtains _ALL_ the options for a given mod.
    private GetCurrentModSettings   GetModSettingsSelected;// Obtains the currently chosen options for a mod.
    // API Enactors
    private SetTemporaryModSettingsPlayer       SetOrUpdateTempModSettings; // Temporarily sets and locks a Mod with defined settings. Can be updated.
    private RemoveTemporaryModSettingsPlayer    RemoveTempModSettings;      // Removes a temporary mod we set. Used for cleanup.
    private RemoveAllTemporaryModSettingsPlayer RemoveAllTempModSettings;   // Removes all temporary mods we set. Used for cleanup.
    // REDRAWER
    private RedrawObject RedrawClient;

    public IpcCallerPenumbra(ILogger<IpcCallerPenumbra> logger, GagspeakMediator mediator)
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
        OnModSettingsChanged = ModSettingChanged.Subscriber(Svc.PluginInterface, ModSettingsChanged);
        OnGameObjectResourcePathResolved = GameObjectResourcePathResolved.Subscriber(Svc.PluginInterface, GameObjectResourceLoaded);
        OnRedrawFinished = GameObjectRedrawn.Subscriber(Svc.PluginInterface, ObjectRedrawnEvent);
        // Getters
        GetModDirectory = new GetModDirectory(Svc.PluginInterface);
        GetModPath = new GetModPath(Svc.PluginInterface);
        GetModList = new GetModList(Svc.PluginInterface);
        GetActiveCollection = new GetCollection(Svc.PluginInterface);
        GetModSettingsAll = new GetAvailableModSettings(Svc.PluginInterface);
        GetModSettingsSelected = new GetCurrentModSettings(Svc.PluginInterface);
        // Enactors
        RedrawClient = new RedrawObject(Svc.PluginInterface);
        SetOrUpdateTempModSettings = new SetTemporaryModSettingsPlayer(Svc.PluginInterface);
        RemoveTempModSettings = new RemoveTemporaryModSettingsPlayer(Svc.PluginInterface);
        RemoveAllTempModSettings = new RemoveAllTemporaryModSettingsPlayer(Svc.PluginInterface);

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

    // Notifies us whenever anything is changed with our mods inside penumbra. Can be the mod itself, collection inheritance, pretty much whatever makes appearance change.
    private void ModSettingsChanged(ModSettingChange change, Guid collectionId, string modDir, bool inherited)
    {
        // Logger.LogTrace($"OnModSettingChange: [Change: {change}] [Collection: {collectionId}] [ModDir: {modDir}] [Inherited: {inherited}]");
        Mediator.Publish(new PenumbraSettingsChanged());
    }

    /// <summary>
    ///     An event firing every time an objects resource path is resolved. <para />
    ///     We use this to fetch the changes in data that <see cref="GetKinksterModData"/> fails to obtain. <para />
    /// </summary>
    /// <remarks>
    ///     Maybe if one day this is fixed by penumbra a lot of overhead could be reduced, if they made it 
    ///     so before firing a resource loaded, they updated the paths shown on screen to the client with it.
    /// </remarks>
    private unsafe void GameObjectResourceLoaded(IntPtr address, string gamePath, string resolvedPath)
    {
        // this wont work because its called outside the framework thread, which creates even more problems for us lol.
        if (PlayerData.Address != address)
            return;
        // Logger.LogTrace($"ResourcePathLoaded: [GamePath: {gamePath}] [ResolvedPath: {resolvedPath}]");
        // Dont do anything with this at the moment, use it for tracking, and debugging.
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

        OnGameObjectResourcePathResolved.Dispose();
        OnModSettingsChanged.Dispose();
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
