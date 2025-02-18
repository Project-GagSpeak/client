using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Services;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Interop.Ipc;

public sealed class IpcCallerGlamourer : DisposableMediatorSubscriberBase, IIpcCaller, IGlamourer
{
    /* ------------- Class Attributes ------------ */
    private readonly GlobalData _clientData;
    private readonly ClientMonitor _clientMonitor;
    private readonly OnFrameworkService _frameworkUtils;

    private bool _shownGlamourerUnavailable = false;

    public EventSubscriber<nint, StateChangeType> StateWasChanged;         // Informs us when ANY Glamour Change has occurred.
    public EventSubscriber<nint, StateFinalizationType> StateWasFinalized; // Informs us when any Glamourer operation has FINISHED.

    private readonly ApiVersion ApiVersion;     // the API version of Glamourer
    private readonly GetState GetState;         // Gets the state of the Client at any given moment. (Primarily for caching latest state after OnStateFinalized)
    private readonly ApplyState ApplyState;     // Sets the actors state at any given moment. (Mostly used for applying Customizations)
    private readonly SetItem SetItem;           // Update a single Item on the Client. (Also restores items on slots no longer enabled for modification)
    private readonly SetMetaState SetMetaState; // Changes the metadata state(s) on the Client.

    public IpcCallerGlamourer(ILogger<IpcCallerGlamourer> logger, GagspeakMediator mediator,
        GlobalData clientData, ClientMonitor clientMonitor, OnFrameworkService frameworkUtils,
        IDalamudPluginInterface pi) : base(logger, mediator)
    {
        _clientData = clientData;
        _frameworkUtils = frameworkUtils;
        _clientMonitor = clientMonitor;

        ApiVersion = new ApiVersion(pi);
        GetState = new GetState(pi);
        ApplyState = new ApplyState(pi);
        SetItem = new SetItem(pi);
        SetMetaState = new SetMetaState(pi);

        // check API status.
        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    #region Generic IPC Setup
    public void CheckAPI()
    {
        bool apiAvailable = false; // assume false at first
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
        if (!APIAvailable || _clientMonitor.IsZoning) return;
        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() => SetItem.Invoke(0, slot, item, dye, 1337)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to set Item {item} on Slot {slot} with dyes {dye.ToArray().ToString()}. Reason: {ex}");
            return;
        }
    }

    public async Task SetMetaStates(MetaFlag metaTypes, bool newValue)
    {
        if (!APIAvailable || _clientMonitor.IsZoning) return;
        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() => SetMetaState.Invoke(0, metaTypes, newValue, 1337)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during SetMetaStates: {ex}");
        }
    }

    public async Task SetCustomize(JToken customizations, JToken parameters)
    {
        // if the glamourerApi is not active, then return an empty string for the customization
        if (!APIAvailable || _clientMonitor.IsZoning) return;
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
        catch (Exception ex)
        {
            Logger.LogError($"Error during ForceSetCustomize: {ex}");
        }
    }

    // Map between the slot strings and their EquipSlot enum values.
    private static readonly Dictionary<string, EquipSlot> SlotMap = new()
    {
        { "Head", EquipSlot.Head },
        { "Body", EquipSlot.Body },
        { "Hands", EquipSlot.Hands },
        { "Legs", EquipSlot.Legs },
        { "Feet", EquipSlot.Feet },
        { "Ears", EquipSlot.Ears },
        { "Neck", EquipSlot.Neck },
        { "Wrists", EquipSlot.Wrists },
        { "RFinger", EquipSlot.RFinger },
        { "LFinger", EquipSlot.LFinger }
    };

    public bool TryTransferActorEquipment(ItemService items, bool asOverlay, out Dictionary<EquipSlot, RestraintSlotBasic> currentAppearance)
    {
        currentAppearance = new Dictionary<EquipSlot, RestraintSlotBasic>();
        // Get the player state and equipment JObject
        var playerState = GetClientGlamourerState();
        if (GetClientGlamourerState() is not JObject state)
            return false;

        // Store all the equipment items.
        if (state["Equipment"] is not JObject equipment)
            return false;

        foreach (var (name, equipSlot) in SlotMap)
        {
            // Create the default template.
            currentAppearance[equipSlot] = new RestraintSlotBasic()
            {
                ApplyFlags = asOverlay ? RestraintFlags.Basic : 0,
                Glamour = new GlamourSlot(equipSlot, ItemService.NothingItem(equipSlot))
            };

            // if the item does not contain any valid data, continue.
            if (equipment[name] is not JObject itemData)
                continue;

            // get the glamourSlot.
            var customItemId = itemData["ItemId"]?.Value<ulong>() ?? 4294967164;
            var stain = itemData["Stain"]?.Value<int>() ?? 0;
            var stain2 = itemData["Stain2"]?.Value<int>() ?? 0;

            var newSlotData = new GlamourSlot()
            {
                Slot = equipSlot,
                GameItem = items.Resolve(equipSlot, customItemId),
                GameStain = new StainIds((StainId)stain, (StainId)stain2),
            };

            // set the item.
            currentAppearance[equipSlot].Glamour = newSlotData;
        }
        return true;
    }

    public bool TryTransferActorCustomization(out JObject customize, out JObject parameters)
    {
        customize = new JObject();
        parameters = new JObject();

        if (GetClientGlamourerState() is not JObject playerState)
            return false;

        if(playerState["Customize"] is not JObject customizeData)
            return false;

        if(playerState["Parameters"] is not JObject parametersData)
            return false;

        customize = customizeData;
        parameters = parametersData;
        return true;
    }

    public bool TryTransferMaterials(out JObject materials)
    {
        materials = new JObject();
        if (GetClientGlamourerState() is not JObject playerState)
            return false;

        if(playerState["Materials"] is not JObject materialsData)
            return false;

        materials = materialsData;
        return true;
    }
}
