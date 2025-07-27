using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Interop;

public sealed class IpcCallerGlamourer : DisposableMediatorSubscriberBase, IIpcCaller, IGlamourer
{
    /* ------------- Class Attributes ------------ */
    private readonly OnFrameworkService _frameworkUtils;

    private bool _shownGlamourerUnavailable = false;

    public EventSubscriber<nint, StateChangeType> StateWasChanged;         // Informs us when ANY Glamour Change has occurred.
    public EventSubscriber<nint, StateFinalizationType> StateWasFinalized; // Informs us when any Glamourer operation has FINISHED.

    private readonly ApiVersion ApiVersion;     // the API version of Glamourer
    private readonly GetState GetState;         // Gets the state of the Client at any given moment. (Primarily for caching latest state after OnStateFinalized)
    private readonly ApplyState ApplyState;     // Sets the actors state at any given moment. (Mostly used for applying Customizations)
    private readonly SetItem SetItem;           // Update a single Item on the Client. (Also restores items on slots no longer enabled for modification)
    private readonly SetMetaState SetMetaState; // Changes the metadata state(s) on the Client.

    public IpcCallerGlamourer(ILogger<IpcCallerGlamourer> logger, GagspeakMediator mediator, OnFrameworkService frameworkUtils) 
        : base(logger, mediator)
    {
        _frameworkUtils = frameworkUtils;

        ApiVersion = new ApiVersion(Svc.PluginInterface);
        GetState = new GetState(Svc.PluginInterface);
        ApplyState = new ApplyState(Svc.PluginInterface);
        SetItem = new SetItem(Svc.PluginInterface);
        SetMetaState = new SetMetaState(Svc.PluginInterface);

        // check API status.
        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    #region Generic IPC Setup
    public void CheckAPI()
    {
        var apiAvailable = false; // assume false at first
        try
        {
            var version = ApiVersion.Invoke();
            if (version is { Major: 1, Minor: >= 3 })
            {
                apiAvailable = true;
            }
            _shownGlamourerUnavailable = _shownGlamourerUnavailable && !apiAvailable;
        }
        catch { /* Do not allow legacy catch checks, consume */ }
        finally
        {
            APIAvailable = apiAvailable;
            if (!apiAvailable && !_shownGlamourerUnavailable)
            {
                _shownGlamourerUnavailable = true;
                Mediator.Publish(new NotificationMessage("Glamourer inactive", "Features Using Glamourer will not function.", NotificationType.Warning));
            }
        }
    }

    private void OnGlamourerReady()
    {
        Logger.LogWarning("Glamourer is now Ready!", LoggerType.IpcGlamourer);
        Mediator.Publish(new GlamourerReady());
    }

    protected override void Dispose(bool disposing) => base.Dispose(disposing);
    #endregion Generic IPC Setup

    // ===================== IPC STATE CALLS =====================
    public JObject? GetClientGlamourerState() => GetState.Invoke(0).Item2;

    public async Task SetClientItemSlot(ApiEquipSlot slot, ulong item, IReadOnlyList<byte> dye, uint variant)
    {
        if (!APIAvailable || PlayerData.IsZoning) return;
        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() => SetItem.Invoke(0, slot, item, dye, 1337)).ConfigureAwait(true);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Failed to set Item {item} on Slot {slot} with dyes {dye.ToArray().ToString()}. Reason: {ex}");
            return;
        }
    }

    public async Task SetMetaStates(MetaFlag metaTypes, bool newValue)
    {
        if (!APIAvailable || PlayerData.IsZoning) return;
        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() => SetMetaState.Invoke(0, metaTypes, newValue, 1337)).ConfigureAwait(false);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Error during SetMetaStates: {ex}");
        }
    }

    public async Task SetCustomize(JToken customizations, JToken parameters)
    {
        // if the glamourerApi is not active, then return an empty string for the customization
        if (!APIAvailable || PlayerData.IsZoning) return;
        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                var playerState = GetClientGlamourerState();
                playerState!["Customize"] = customizations;
                playerState!["Parameters"] = parameters;
                ApplyState.Invoke(playerState!, 0, flags: ApplyFlag.Customization);
            }).ConfigureAwait(false);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Error during ForceSetCustomize: {ex}");
        }
    }

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
        if (GetClientGlamourerState() is not JObject ps || ps["Customize"] is not JObject customizeData || ps["Parameters"] is not JObject parametersData)
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
        if (GetClientGlamourerState() is not JObject playerState || playerState["Materials"] is not JObject materialsData)
            return false;

        // Set the thingies.
        materials = materialsData;
        return true;
    }

    private bool TryObtainFromSlots(bool asOverlay, Dictionary<string, EquipSlot> slotMap, out Dictionary<EquipSlot, RestraintSlotBasic> result)
    {
        result = new Dictionary<EquipSlot, RestraintSlotBasic>();

        // Get the equipment JObject, and if not found, return.
        if (GetClientGlamourerState() is not JObject state || state["Equipment"] is not JObject equipment)
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
}
