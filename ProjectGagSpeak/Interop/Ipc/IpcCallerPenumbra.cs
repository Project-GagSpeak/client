using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;

namespace GagSpeak.Interop.Ipc;

/// <summary> reads/gets the directory name & label name of the mod. </summary>
public readonly record struct Mod(string DirectoryName, string Name) : IComparable<Mod>
{
    // constructor by KVP
    public Mod(KeyValuePair<string, string> kvp) : this(kvp.Key, kvp.Value) { }

    public int CompareTo(Mod other)
    {
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return string.Compare(DirectoryName, other.DirectoryName, StringComparison.Ordinal);
    }
}

/// <summary> gets the settings for the mod that are currently selected/chosen. </summary>
/// <remarks> The ForceInherit and Remove inputs currently have unknown purpose / effect. </remarks>
public readonly record struct ModSettings(Dictionary<string, List<string>> Settings, int Priority, bool Enabled, bool ForceInherit, bool Remove)
{
    public ModSettings() : this(new Dictionary<string, List<string>>(), 0, false, false, false) { }

    public static ModSettings Empty => new();
}

/// <summary> The Storage of all a Mods Groups and options for each group of the mod. </summary>
/// <remarks> Useful for obtaining when we want to edit what our temporary mod settings will be. </remarks>
public readonly record struct ModSettingOptions(IReadOnlyDictionary<string, (string[] Options, GroupType GroupType)> Options)
{
    public ModSettingOptions() : this(new Dictionary<string, (string[] Options, GroupType GroupType)>()) { }

    public static ModSettingOptions Empty => new();
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

    private readonly EventSubscriber                                     OnInitialized;
    private readonly EventSubscriber                                     OnDisposed;
    private readonly EventSubscriber<ChangedItemType, uint>              TooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint> ItemClickedSubscriber;
    private readonly EventSubscriber<nint, int>                          OnRedrawFinished;
    public EventSubscriber<string, string>                               OnModMoved; // Fires when mod directory changes. Handled by CustomModSettingManager.

    private ApiVersion                          Version;               // Obtains the current version of Penumbra's API.
    private RedrawObject                        RedrawClient;          // Can force the client to Redraw.
    private GetModList                          GetModList;            // Retrieves the client's mod list. (DirectoryName, ModName)
    private GetCollection                       GetActiveCollection;   // Obtains the client's currently active collection. (may not need this)
    private GetAvailableModSettings             GetModSettingsAll;     // Obtains _ALL_ the options for a given mod.
    private GetCurrentModSettings               GetModSettingsCurrent; // Obtains the currently chosen options for a mod.
    private SetTemporaryModSettingsPlayer       SetOrUpdateTempMod;    // Temporarily sets and locks a Mod with defined settings. Can be updated.
    private RemoveTemporaryModSettingsPlayer    RemoveTempMod;         // Removes a temporary mod we set. Used for cleanup.
    private RemoveAllTemporaryModSettingsPlayer RemoveAllTempMod;      // Removes all temporary mods we set. Used for cleanup.

    public IpcCallerPenumbra(ILogger<IpcCallerPenumbra> logger, GagspeakMediator mediator,
        OnFrameworkService frameworkUtils, IDalamudPluginInterface pi) : base(logger, mediator)
    {
        OnInitialized = Initialized.Subscriber(pi, () => 
        {
            APIAvailable = true;
            Mediator.Publish(new PenumbraInitializedMessage());
        });
        OnDisposed = Disposed.Subscriber(pi, () => Mediator.Publish(new PenumbraDisposedMessage()));

        TooltipSubscriber = ChangedItemTooltip.Subscriber(pi);
        ItemClickedSubscriber = ChangedItemClicked.Subscriber(pi);
        OnRedrawFinished = GameObjectRedrawn.Subscriber(pi, ObjectRedrawnEvent);

        Version = new ApiVersion(pi);
        RedrawClient = new RedrawObject(pi);
        GetModList = new GetModList(pi);
        GetActiveCollection = new GetCollection(pi);
        GetModSettingsAll = new GetAvailableModSettings(pi);
        GetModSettingsCurrent = new GetCurrentModSettings(pi);
        SetOrUpdateTempMod = new SetTemporaryModSettingsPlayer(pi);
        RemoveTempMod = new RemoveTemporaryModSettingsPlayer(pi);
        RemoveAllTempMod = new RemoveAllTemporaryModSettingsPlayer(pi);

        CheckAPI();
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

    /// <summary> Attempts to perform a manual redraw on the client. </summary>
    /// <remarks> This can also trick mare into receiving updated animations & immediately redrawing, so others see it first time. </remarks>
    public void RedrawObject()
    {
        Logger.LogInformation("Manually redrawing the client!", LoggerType.IpcPenumbra);
        RedrawClient.Invoke(0, RedrawType.Redraw);
    }

    public List<Mod> GetMods()
    {
        if(!APIAvailable)
            return new List<Mod>();
        var allMods = GetModList.Invoke();
        return allMods.Select(m => new Mod(m)).OrderByDescending(mod => mod.Name).ToList();
    }


    // for our get mod list for the table
    public IReadOnlyList<(Mod Mod, ModSettings Settings)> GetModInfos()
    {
        if (!APIAvailable)
            return Array.Empty<(Mod Mod, ModSettings Settings)>();

        try
        {
            var allMods = GetModList.Invoke();
            var collection = GetActiveCollection.Invoke(ApiCollectionType.Current);
            return allMods
                .Select(m => (m.Key, m.Value, GetModSettingsCurrent.Invoke(collection!.Value.Id, m.Key)))
                .Where(t => t.Item3.Item1 is PenumbraApiEc.Success)
                .Select(t => (new Mod(t.Item2, t.Item1),
                    !t.Item3.Item2.HasValue
                        ? ModSettings.Empty
                        : new ModSettings(t.Item3.Item2!.Value.Item3, t.Item3.Item2!.Value.Item2, t.Item3.Item2!.Value.Item1, false, false)))
                .OrderByDescending(p => p.Item2.Enabled)
                .ThenBy(p => p.Item1.Name)
                .ThenBy(p => p.Item1.DirectoryName)
                .ThenByDescending(p => p.Item2.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error fetching mods from Penumbra:\n{ex}");
            return Array.Empty<(Mod Mod, ModSettings Settings)>();
        }
    }

    public ModSettings GetSettingsForMod(Mod mod)
    {
        if (!APIAvailable)
            return ModSettings.Empty;

        var collection = GetActiveCollection.Invoke(ApiCollectionType.Current);
        var res = GetModSettingsCurrent.Invoke(collection!.Value.Id, mod.DirectoryName);
        if(res.Item1 is not PenumbraApiEc.Success)
            return ModSettings.Empty;
        
        return (res.Item2.HasValue) ? new ModSettings(res.Item2!.Value.Item3, res.Item2!.Value.Item2, res.Item2!.Value.Item1, false, false) : ModSettings.Empty;
    }

    public ModSettingOptions GetAllOptionsForMod(Mod mod)
    {
        if (!APIAvailable)
            return ModSettingOptions.Empty;

        // grab the settings for the mod.
        var res = GetModSettingsAll.Invoke(mod.DirectoryName);
        return (res is null) ? ModSettingOptions.Empty : new ModSettingOptions(res);
    }

    /// <summary> Used for Setting or updating the settings of a mod, binding it to GagSpeak as a temporary mod. </summary>
    /// <remarks> While active, these mods settings will be locked in penumbra to ensure further helplessness. </remarks>
    public PenumbraApiEc SetOrUpdateTemporaryMod(Mod mod, ModSettings PresetSettings)
    {
        if (!APIAvailable)
            return PenumbraApiEc.NothingChanged;

        // set, or update, the temporary mod settings for the mod.
        return SetOrUpdateTempMod.Invoke(PLAYER_OBJECT_IDX,
            mod.DirectoryName,
            false,
            true,
            PresetSettings.Priority + 25,
            PresetSettings.Settings.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value),
            GAGSPEAK_ID,
            GAGSPEAK_KEY,
            mod.Name);
    }

    /// <summary> Used for removing a temporary mod from the client. </summary>
    /// <returns> The penumbra status code of success. </returns>
    public PenumbraApiEc RemoveTemporaryMod(Mod mod)
    {
        if (!APIAvailable)
            return PenumbraApiEc.NothingChanged;

        // Remove the temporary mod we set. This also undoes the priority shift we set for it.
        return RemoveTempMod.Invoke(PLAYER_OBJECT_IDX, mod.DirectoryName, GAGSPEAK_KEY, mod.Name);
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
