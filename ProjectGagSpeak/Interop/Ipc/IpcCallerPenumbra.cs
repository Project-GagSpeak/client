using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using GagSpeak.Services;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using Penumbra.Api.IpcSubscribers;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.GagspeakConfiguration.Models;
using Lumina.Excel.Sheets;
using GagSpeak.PlayerData.Services;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.StateManagers;
using GagspeakAPI.Extensions;
using Microsoft.VisualBasic.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GagSpeak.Interop.Ipc;


/// <summary> reads/gets the name and directory name of the mod. </summary>
public readonly record struct Mod(string Name, string DirectoryName) : IComparable<Mod>
{
    public int CompareTo(Mod other)
    {
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return string.Compare(DirectoryName, other.DirectoryName, StringComparison.Ordinal);
    }
}

/// <summary> gets the settings for the mod, including all details about it. </summary>
public readonly record struct ModSettings(Dictionary<string, List<string>> Settings, int Priority, bool Enabled)
{
    public ModSettings() : this(new Dictionary<string, List<string>>(), 0, false) { }

    public static ModSettings Empty
        => new();
}

// the penumbra service that we will use to interact with penumbra
public unsafe class IpcCallerPenumbra : DisposableMediatorSubscriberBase, IIpcCaller
{
    // ID displayed in penumbra when the mod settings are set.
    private const string GAGSPEAK_ID = "GagSpeak";
    // Key used to associate with glamourer
    // value is Cordy's handle WUV = 01010111 01010101 01010110 = 5723478 (hey, don't cringe! I thought it was cute <3) 
    // If no lock is desired, make negative.
    private const int GAGSPEAK_KEY = 5723478;
    // Note the Player ObjectId is __always__ 0
    private const int PLAYER_ID = 0;
    /* ------- Class Attributes ---------- */
    private readonly IDalamudPluginInterface _pi;
    private readonly OnFrameworkService _frameworkService;
    private readonly GagspeakMediator _mediator;
    private bool _shownPenumbraUnavailable = false; // safety net to prevent notification spam.

    /* ------- Penumbra API Event Subscribers ---------- */
    private readonly EventSubscriber _penumbraInitialized;
    private readonly EventSubscriber _penumbraDisposed;
    private readonly EventSubscriber<ChangedItemType, uint> _tooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint> _clickSubscriber;
    private readonly EventSubscriber<nint, int> _penumbraObjectRedrawnSubscriber;


    /* -------- Penumbra IPC Event Subscribers */
    private RedrawObject? _redrawSubscriber;           // when a target redraws
    private GetModList? _getMods;                      // gets the mod list for our table
    private GetCollection? _currentCollection;         // gets the current collection of our character (0)
    private GetCurrentModSettings? _getCurrentSettings;// we shouldnt need this necessarily  
    private SetTemporaryModSettingsPlayer? _setTempMod;             // set the mod to be enabled or disabled
    private RemoveTemporaryModSettingsPlayer? _removeTempMod;       // Removes the temporary mods
    private RemoveAllTemporaryModSettingsPlayer? _removeAllTempMod; // change the mod priority while active to that it overrides other things
    private ApiVersion _penumbraApiVersion;                         // Version of penumbra's API

    public IpcCallerPenumbra(ILogger<IpcCallerPenumbra> logger, 
        IDalamudPluginInterface pi, OnFrameworkService frameworkService, 
        GagspeakMediator mediator) : base(logger, mediator)
    {
        _pi = pi;
        _frameworkService = frameworkService;
        _mediator = mediator;


        _penumbraInitialized = Initialized.Subscriber(pi, PenumbraInitialized);
        _penumbraDisposed = Disposed.Subscriber(pi, PenumbraDisposed);
        _penumbraObjectRedrawnSubscriber = GameObjectRedrawn.Subscriber(pi, ObjectRedrawnEvent);


        _tooltipSubscriber = ChangedItemTooltip.Subscriber(pi);
        _clickSubscriber = ChangedItemClicked.Subscriber(pi);

        _penumbraApiVersion = new ApiVersion(pi);

        CheckAPI();
        // possibly remove this. 
        PenumbraInitialized();
    }

    public static bool APIAvailable { get; private set; } = false;
    public int API_CurrentMajor { get; private set; }
    public int API_CurrentMinor { get; private set; }
    public const int RequiredPenumbraAPIBreakingVersion = 5;
    public const int RequiredPenumbraAPIFeatureVersion = 0;

    public void CheckAPI()
    {
        try
        {
            try
            {
                (API_CurrentMajor, API_CurrentMinor) = _penumbraApiVersion.Invoke();
            }
            catch
            {
                try
                {
                    (API_CurrentMajor, API_CurrentMinor) = new global::Penumbra.Api.IpcSubscribers.Legacy.ApiVersions(_pi).Invoke();
                }
                catch
                {
                    API_CurrentMajor = 0;
                    API_CurrentMinor = 0;
                    throw;
                }
            }
            // if its broken, dont reattach
            if (API_CurrentMajor != RequiredPenumbraAPIBreakingVersion || API_CurrentMinor < RequiredPenumbraAPIFeatureVersion)
            {
                throw new Exception(
                    $"Invalid Version {API_CurrentMajor}.{API_CurrentMinor:D4}, required major " +
                    $"Version {RequiredPenumbraAPIBreakingVersion} with feature greater or equal to {RequiredPenumbraAPIFeatureVersion}.");
            }
            // API check sucessful.
            APIAvailable = true;
            _shownPenumbraUnavailable = _shownPenumbraUnavailable && !APIAvailable;
        }
        catch // caught by the exception thrown if not compatible.
        {
            if (!APIAvailable && !_shownPenumbraUnavailable)
            {
                _shownPenumbraUnavailable = true;

                _mediator.Publish(new NotificationMessage("Penumbra inactive", "Features using Penumbra will not function properly.", NotificationType.Error));
            }
        }
    }


    public event Action<MouseButton, ChangedItemType, uint> Click
    {
        add => _clickSubscriber.Event += value;
        remove => _clickSubscriber.Event -= value;
    }

    public event Action<ChangedItemType, uint> Tooltip
    {
        add => _tooltipSubscriber.Event += value;
        remove => _tooltipSubscriber.Event -= value;
    }

    /// <summary> 
    /// Try to redraw the given actor. 
    /// We force this method to trigger a immediate redraw from Mare, so it can redraw a player the moment their changes are applied.
    /// This allows animation mods to be updated instantly.
    /// </summary>
    public void RedrawObject(int objectIndex, RedrawType settings)
    {
        Logger.LogInformation("Redrawing ClientPlayer object due to set toggle!", LoggerType.IpcPenumbra);
        // Let us know that we are manually invoking a redraw.
        if(objectIndex is 0)
            AppearanceManager.ManualRedrawProcessing = true;
        // Invoke the subscriber
        _redrawSubscriber!.Invoke(objectIndex, settings);
    }


    private void ObjectRedrawnEvent(IntPtr objectAddress, int objectTableIndex)
    {
        // if the object index is 0, perform a reapply all
        if (objectTableIndex is 0)
        {
            Logger.LogInformation("Redrawing Called via command or has finished a redraw", LoggerType.IpcPenumbra);
            AppearanceManager.ManualRedrawProcessing = false;
            // Invoke a reapply all here. This will ensure that we reapply all information once we are valid.
            IpcFastUpdates.InvokeGlamourer(GlamourUpdateType.ReapplyAll);
        }
    }




    // for our get mod list for the table
    public IReadOnlyList<(Mod Mod, ModSettings Settings)> GetMods()
    {
        if (!APIAvailable)
            return Array.Empty<(Mod Mod, ModSettings Settings)>();

        try
        {
            var allMods = _getMods!.Invoke();
            var collection = _currentCollection!.Invoke(ApiCollectionType.Current);
            return allMods
                .Select(m => (m.Key, m.Value, _getCurrentSettings!.Invoke(collection!.Value.Id, m.Key)))
                .Where(t => t.Item3.Item1 is PenumbraApiEc.Success)
                .Select(t => (new Mod(t.Item2, t.Item1),
                    !t.Item3.Item2.HasValue
                        ? ModSettings.Empty
                        : new ModSettings(t.Item3.Item2!.Value.Item3, t.Item3.Item2!.Value.Item2, t.Item3.Item2!.Value.Item1)))
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

    /// <summary>
    /// Used to clear the temporary mod settings managed by GagSpeak
    /// </summary>
    /// <returns> NothingChanged if IPC API is not available, penumbra status code otherwise </returns>
    public PenumbraApiEc ClearAllTemporaryMods() {
        if (!APIAvailable) 
            return PenumbraApiEc.NothingChanged;
        else
            return _removeAllTempMod!.Invoke(PLAYER_ID, GAGSPEAK_KEY);
    }
    /// <summary>
    /// Used to modify the priority and enable/disable state of a given mod within the current collection.
    /// </summary>
    /// <param name="AssociatedMod">The mod to modify.</param>
    /// <param name="modState">The new state of the mod. (ENABLED or DISABLED) </param>
    public PenumbraApiEc ModifyModState(AssociatedMod AssociatedMod, NewState modState = NewState.Enabled)
    {
        if (!APIAvailable) 
            return PenumbraApiEc.NothingChanged;

        // create error code, assume success
        var errorCode = PenumbraApiEc.Success;
        try
        {
            // get the collection of our character
            // If we wanted to Enable the mod, and we are not only adjusting priority, enable it.
            if (modState is NewState.Enabled)
            {
                errorCode = _setTempMod!.Invoke(PLAYER_ID,
                    AssociatedMod.Mod.DirectoryName,
                    false,
                    true,
                    AssociatedMod.ModSettings.Priority + 50,
                    AssociatedMod.ModSettings.Settings.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value),
                    GAGSPEAK_ID,
                    // Magic number found in the settings, used to lock, will need to decide if we want a key or not.
                    GAGSPEAK_KEY,
                    AssociatedMod.Mod.Name);
                switch (errorCode)
                {
                    case PenumbraApiEc.ModMissing: return PenumbraApiEc.ModMissing;
                    case PenumbraApiEc.CollectionMissing: return PenumbraApiEc.CollectionMissing;
                }

            } 
            // If we wanted to disable the mod, and we are not only adjusting priority, disable it.
            else if (modState is NewState.Disabled)
            {
                // Adjust the priority of the mod back to its original value
                errorCode = _removeTempMod!.Invoke(PLAYER_ID,
                    AssociatedMod.Mod.DirectoryName,
                    GAGSPEAK_KEY,
                    AssociatedMod.Mod.Name);
                
                if (errorCode is not PenumbraApiEc.Success and not PenumbraApiEc.NothingChanged)
                    return PenumbraApiEc.UnknownError;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error modifying mod state in Penumbra:\n{ex}");
            return PenumbraApiEc.UnknownError;
        }
        // Default return.
        return errorCode;
    }

    /// <summary> Reattach to the currently running Penumbra IPC provider. Unattaches before if necessary. </summary>
    public void PenumbraInitialized()
    {
        try
        {
            // unattach from the current penumbra to reset subscribers.
            PenumbraDisposed();

            CheckAPI();
            // attach to the penumbra
            _tooltipSubscriber.Enable();
            _clickSubscriber.Enable();
            _redrawSubscriber   = new RedrawObject(_pi);
            _getMods            = new GetModList(_pi);
            _currentCollection  = new GetCollection(_pi);
            _getCurrentSettings = new GetCurrentModSettings(_pi);
            _setTempMod         = new SetTemporaryModSettingsPlayer(_pi);
            _removeTempMod      = new RemoveTemporaryModSettingsPlayer(_pi);
            _removeAllTempMod   = new RemoveAllTemporaryModSettingsPlayer(_pi);
            _mediator.Publish(new PenumbraInitializedMessage());
        }
        catch (Exception e)
        {
            Logger.LogDebug($"Could not attach to Penumbra:\n{e}");
        }
    }

    /// <summary> Unattach from the currently running Penumbra IPC provider. </summary>
    private void PenumbraDisposed()
    {
        _tooltipSubscriber.Disable();
        _clickSubscriber.Disable();
        if (APIAvailable)
        {
            APIAvailable = false;
            _mediator.Publish(new PenumbraDisposedMessage());
        }
    }


    protected override void Dispose(bool disposing)
    {
        // call disposal of IPC subscribers
        base.Dispose(disposing);
        ClearAllTemporaryMods();
        // call the penumbra dispose to disable the enabled the API Event subscribers
        PenumbraDisposed();
        // dispose of the penumbra event subscribers
        _tooltipSubscriber.Dispose();
        _clickSubscriber.Dispose();
        _penumbraInitialized.Dispose();
        _penumbraDisposed.Dispose();
        _penumbraObjectRedrawnSubscriber.Dispose();
    }
}
