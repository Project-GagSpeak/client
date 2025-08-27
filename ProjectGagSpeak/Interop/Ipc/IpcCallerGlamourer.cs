using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Interop;

public sealed class IpcCallerGlamourer : IIpcCaller
{
    // API Version
    private readonly ApiVersion ApiVersion;
    // API EVENTS
    public EventSubscriber<nint, StateChangeType>       OnStateChanged;   // Informs us when ANY Glamour Change has occurred.
    public EventSubscriber<nint, StateFinalizationType> OnStateFinalized; // Informs us when any Glamourer operation has FINISHED.
    // API GETTERS
    private readonly GetState       GetState;  // Get the JSONObject of the client's current state.
    private readonly GetStateBase64 GetBase64; // Get the Base64string of the client's current state.
    // API ENACTORS
    private readonly ApplyState      ApplyState;           // Applies a JSONObject of an actors state to a player.
    private readonly SetItem         SetItem;              // Useful for enforcing bondage.
    private readonly SetMetaState    SetMetaState;         // Useful for enforcing bondage.
    private readonly UnlockState     UnlockKinkster;       // Unlocks a Kinkster's glamour state for modification.
    private readonly UnlockStateName UnlockKinksterByName; // Unlock a Kinkster's glamour state by name. (try to avoid?)
    private readonly RevertState     RevertKinkster;       // Revert a Kinkster to their game state.
    private readonly RevertStateName RevertKinksterByName; // Revert a kinkster to their game state by their name. (try to avoid?)

    private readonly ILogger<IpcCallerGlamourer> _logger;
    private readonly GagspeakMediator _mediator;
    // value is Cordy's handle WUV = 01010111 01010101 01010110 = 5723478 (hey, don't cringe! I thought it was cute <3) 
    private const uint GAGSPEAK_LOCK = 0x05723478;
    private bool _shownGlamourerUnavailable = false;

    public IpcCallerGlamourer(ILogger<IpcCallerGlamourer> logger, GagspeakMediator mediator) 
    {
        _logger = logger;
        _mediator = mediator;

        ApiVersion = new ApiVersion(Svc.PluginInterface);

        GetState = new GetState(Svc.PluginInterface);
        GetBase64 = new GetStateBase64(Svc.PluginInterface);

        ApplyState = new ApplyState(Svc.PluginInterface);
        SetItem = new SetItem(Svc.PluginInterface);
        SetMetaState = new SetMetaState(Svc.PluginInterface);
        UnlockKinkster = new UnlockState(Svc.PluginInterface);
        UnlockKinksterByName = new UnlockStateName(Svc.PluginInterface);
        RevertKinkster = new RevertState(Svc.PluginInterface);
        RevertKinksterByName = new RevertStateName(Svc.PluginInterface);

        CheckAPI();
    }

    public void Dispose() 
    { }

    public static bool APIAvailable { get; private set; } = false;
    public void CheckAPI()
    {
        var apiAvailable = false; // assume false at first
        Generic.Safe(() =>
        {
            if (ApiVersion.Invoke() is { Major: 1, Minor: >= 3 })
                apiAvailable = true;
            _shownGlamourerUnavailable = _shownGlamourerUnavailable && !apiAvailable;
        }, true);
        // update available state.
        APIAvailable = apiAvailable;
        if (!apiAvailable && !_shownGlamourerUnavailable)
        {
            _shownGlamourerUnavailable = true;
            _mediator.Publish(new NotificationMessage("Glamourer inactive", "Features Using Glamourer will not function.", NotificationType.Error));
        }
    }

    private void OnGlamourerReady()
    {
        _logger.LogInformation("Glamourer is now Ready!", LoggerType.IpcGlamourer);
        _mediator.Publish(new GlamourerReady());
    }

    /// <summary>
    ///     Obtains the JObject of the client's current Actor State
    /// </summary>
    public JObject? GetActorState() 
        => GetState.Invoke(0).Item2;

    /// <summary>
    ///     Obtains the Base64String of the client's current Actor State
    /// </summary>
    public async Task<string> GetActorString()
    {
        if (!APIAvailable) return string.Empty;
        return await Svc.Framework.RunOnFrameworkThread(() => GetBase64.Invoke(0).Item2 ?? string.Empty).ConfigureAwait(false);
    }

    /// <summary>
    ///     Enforces the Clients EquipSlot data to reflect their bondage state.
    /// </summary>
    public async Task SetClientItemSlot(ApiEquipSlot slot, ulong item, IReadOnlyList<byte> dye, uint variant)
    {
        if (!APIAvailable || PlayerData.IsZoning) return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() => SetItem.Invoke(0, slot, item, dye, GAGSPEAK_LOCK)).ConfigureAwait(false);
        });
    }

    /// <summary>
    ///     Enforces the Clients Metadata bondage state.
    /// </summary>
    public async Task SetMetaStates(MetaFlag metaTypes, bool newValue)
    {
        if (!APIAvailable || PlayerData.IsZoning) return;
        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() => SetMetaState.Invoke(0, metaTypes, newValue, GAGSPEAK_LOCK)).ConfigureAwait(false);
        });
    }

    /// <summary>
    ///     Applies any restrained customizations to the client's state.
    /// </summary>
    public async Task SetClientCustomize(JToken customizations, JToken parameters)
    {
        // if the glamourerApi is not active, then return an empty string for the customization
        if (!APIAvailable || PlayerData.IsZoning) return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() =>
            {
                var playerState = GetActorState();
                playerState!["Customize"] = customizations;
                playerState!["Parameters"] = parameters;
                ApplyState.Invoke(playerState!, 0, flags: ApplyFlag.Customization);
            }).ConfigureAwait(false);
        });
    }

    #region Kinkster Glamour Sync
    /// <summary>
    ///     Applies another Kinkster's Glamourer state with their actor data.
    /// </summary>
    public async Task ApplyKinksterGlamour(PairHandler kinkster, string? actorData)
    {
        if (!APIAvailable || PlayerData.IsZoning || string.IsNullOrEmpty(actorData))
            return;
        // do not apply if not visible.
        if (kinkster.PairObject is not { } visibleObj)
            return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() =>
            {
                _logger.LogDebug($"Updating ({kinkster.PlayerName}'s) glamourer data.", LoggerType.IpcGlamourer);
                ApplyState.Invoke(actorData, kinkster.PairObject.ObjectIndex, GAGSPEAK_LOCK);
            }).ConfigureAwait(false);
        });
    }

    public async Task ReleaseKinkster(PairHandler kinkster)
    {
        if (!APIAvailable || PlayerData.IsZoning)
            return;
        // do not apply if not visible.
        if (kinkster.PairObject is not { } visibleObj)
            return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() =>
            {
                _logger.LogDebug($"Reverting Kinkster {kinkster.PlayerName}'s Glamourer data!", LoggerType.IpcGlamourer);
                RevertKinkster.Invoke(visibleObj.ObjectIndex, GAGSPEAK_LOCK);
                _logger.LogDebug($"Unlocking Kinkster {kinkster.PlayerName}'s Glamourer data!", LoggerType.IpcGlamourer);
                UnlockKinkster.Invoke(visibleObj.ObjectIndex, GAGSPEAK_LOCK);
                // maybe some redraw or refresh i dont know.
            });
        });
    }

    public async Task ReleaseKinksterByName(string kinksterName)
    {
        if (!APIAvailable || PlayerData.IsZoning)
            return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() =>
            {
                _logger.LogDebug($"Reverting Kinkster {kinksterName}'s Glamourer data!", LoggerType.IpcGlamourer);
                RevertKinksterByName.Invoke(kinksterName, GAGSPEAK_LOCK);
                _logger.LogDebug($"Unlocking Kinkster {kinksterName}'s Glamourer data!", LoggerType.IpcGlamourer);
                UnlockKinksterByName.Invoke(kinksterName, GAGSPEAK_LOCK);
            }).ConfigureAwait(false);
        });
    }

    #endregion Kinkster Glamour Sync

    #region Glamour Enforcement Helpers
    // Map between the slot strings and their EquipSlot enum values.
    private static readonly Dictionary<string, EquipSlot> GearSlotMap = new()
    {
        { "Head", EquipSlot.Head },
        { "Body", EquipSlot.Body },
        { "Hands", EquipSlot.Hands },
        { "Legs", EquipSlot.Legs },
        { "Feet", EquipSlot.Feet },
    };

    private static readonly Dictionary<string, EquipSlot> AccessorySlotMap = new()
    {
        { "Ears", EquipSlot.Ears },
        { "Neck", EquipSlot.Neck },
        { "Wrists", EquipSlot.Wrists },
        { "RFinger", EquipSlot.RFinger },
        { "LFinger", EquipSlot.LFinger }
    };

    public bool TryObtainActorGear(bool asOverlay, out Dictionary<EquipSlot, RestraintSlotBasic> curGear)
        => TryObtainFromSlots(asOverlay, GearSlotMap, out curGear);

    public bool TryObtainActorAccessories(bool asOverlay, out Dictionary<EquipSlot, RestraintSlotBasic> curAccessories)
        => TryObtainFromSlots(asOverlay, AccessorySlotMap, out curAccessories);

    public bool TryObtainActorCustomization(out JObject customize, out JObject parameters)
    {
        customize = new JObject();
        parameters = new JObject();

        // Get the customization JObject for customize & parameters. If either is not found, return.
        if (GetActorState() is not JObject ps || ps["Customize"] is not JObject customizeData || ps["Parameters"] is not JObject parametersData)
            return false;

        // Set the thingies.
        customize = customizeData;
        parameters = parametersData;
        return true;
    }

    public bool TryObtainMaterials(out JObject materials)
    {
        materials = new JObject();

        // Get the materials JObject, and if not found, return.
        if (GetActorState() is not JObject playerState || playerState["Materials"] is not JObject materialsData)
            return false;

        // Set the thingies.
        materials = materialsData;
        return true;
    }

    private bool TryObtainFromSlots(bool asOverlay, Dictionary<string, EquipSlot> slotMap, out Dictionary<EquipSlot, RestraintSlotBasic> result)
    {
        result = new Dictionary<EquipSlot, RestraintSlotBasic>();

        // Get the equipment JObject, and if not found, return.
        if (GetActorState() is not JObject state || state["Equipment"] is not JObject equipment)
            return false;

        // Go through our slot map and fetch all the data, appending it as new BasicSlots for the restraint set.
        foreach (var (name, equipSlot) in slotMap)
        {
            // Note: Apply flags for this function assume that it will only be used in the restraint sets UI.
            // The base flag for all glamour slots must be set and the overlay can be added conditionally.
            var applyFlags = RestraintFlags.Glamour;
            if (asOverlay)
                applyFlags ^= RestraintFlags.IsOverlay;
            result[equipSlot] = new RestraintSlotBasic
            {
                ApplyFlags = applyFlags,
                Glamour = new GlamourSlot(equipSlot, ItemSvc.NothingItem(equipSlot))
            };

            if (equipment[name] is not JObject itemData)
                continue;

            var customItemId = itemData["ItemId"]?.Value<ulong>() ?? 4294967164;
            var stain = itemData["Stain"]?.Value<int>() ?? 0;
            var stain2 = itemData["Stain2"]?.Value<int>() ?? 0;

            result[equipSlot].Glamour = new GlamourSlot
            {
                Slot = equipSlot,
                GameItem = ItemSvc.Resolve(equipSlot, customItemId),
                GameStain = new StainIds((StainId)stain, (StainId)stain2),
            };
        }

        return true;
    }
    #endregion Glamour Enforcement Helpers
}
