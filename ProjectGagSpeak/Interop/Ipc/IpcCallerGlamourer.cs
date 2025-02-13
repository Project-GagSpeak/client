using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;

namespace GagSpeak.Interop.Ipc;

public sealed class IpcCallerGlamourer : DisposableMediatorSubscriberBase, IIpcCaller, IGlamourer
{
    /* ------------- Class Attributes ------------ */
    private readonly GlobalData _clientData;
    private readonly ClientMonitorService _clientService;
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
        GlobalData clientData, ClientMonitorService clientService, OnFrameworkService frameworkUtils,
        IDalamudPluginInterface pi) : base(logger, mediator)
    {
        _clientData = clientData;
        _frameworkUtils = frameworkUtils;
        _clientService = clientService;

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
        if (!APIAvailable || _clientService.IsZoning) return;
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
        if (!APIAvailable || _clientService.IsZoning) return;
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
        if (!APIAvailable || _clientService.IsZoning) return;
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

    public bool SetRestraintEquipmentFromState(RestraintSet setToEdit)
        {
            // Get the player state and equipment JObject
            var playerState = GetClientGlamourerState();
            // Store all the equipment items.
            var equipment = playerState?["Equipment"];
            if (equipment == null) return false;

            var slots = new[] { "MainHand", "OffHand", "Head", "Body", "Hands", "Legs", "Feet", "Ears", "Neck", "Wrists", "RFinger", "LFinger" };

            // Update each slot
            foreach (var slotName in slots)
            {
                var item = equipment[slotName];
                var equipDrawData = UpdateItem(item, slotName);

                if (equipDrawData != null)
                    setToEdit.DrawData[equipDrawData.Slot] = equipDrawData;
            }

            return true;
        }*/

    public void SetRestraintCustomizationsFromState(ref  setToEdit)
    {
        var playerState = GetClientState();
        setToEdit.CustomizeObject = playerState!["Customize"] ?? new JObject();
        setToEdit.ParametersObject = playerState!["Parameters"] ?? new JObject();
    }
}
