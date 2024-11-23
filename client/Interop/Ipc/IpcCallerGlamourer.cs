using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Enums;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Utils;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Handlers;

namespace GagSpeak.Interop.Ipc;

/// <summary>
/// Create a sealed class for our interop manager.
/// </summary>
public sealed class IpcCallerGlamourer : DisposableMediatorSubscriberBase, IIpcCaller
{
    /* ------------- Class Attributes ------------- */
    private readonly IDalamudPluginInterface _pi;
    private readonly GagspeakConfigService _gagspeakConfig;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly IpcFastUpdates _fastUpdates;
    private bool _shownGlamourerUnavailable = false; // safety net to prevent notification spam.

    /* --------- Glamourer API Event Subscribers -------- */
    public readonly EventSubscriber<nint, StateChangeType> _glamourChanged;

    /* ---------- Glamourer API IPC Subscribers --------- */
    private readonly ApiVersion _ApiVersion; // the API version of glamourer
    private readonly GetState _glamourerGetState;
    private readonly ApplyState _ApplyState;
    private readonly SetItem _SetItem;
    private readonly RevertState _RevertCharacter;
    private readonly RevertToAutomation _RevertToAutomation;

    public IpcCallerGlamourer(ILogger<IpcCallerGlamourer> logger, GagspeakMediator mediator,
        IDalamudPluginInterface pluginInterface, GagspeakConfigService clientConfigs, 
        ClientMonitorService clientService, OnFrameworkService frameworkUtils,
        IpcFastUpdates fastUpdates) : base(logger, mediator)
    {
        _pi = pluginInterface;
        _gagspeakConfig = clientConfigs;
        _frameworkUtils = frameworkUtils;
        _clientService = clientService;
        _fastUpdates = fastUpdates;

        // set IPC callers
        _ApiVersion = new ApiVersion(_pi);
        _glamourerGetState = new GetState(_pi);
        _ApplyState = new ApplyState(_pi);
        _SetItem = new SetItem(_pi);
        _RevertCharacter = new RevertState(_pi);
        _RevertToAutomation = new RevertToAutomation(_pi);

        // check API status.
        CheckAPI();

        // set event subscribers
        _glamourChanged = StateChangedWithType.Subscriber(_pi, GlamourerChanged);
        _glamourChanged.Enable();
    }

    public enum MetaData { None, Hat, Visor, Both }
    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        bool apiAvailable = false; // assume false at first
        try
        {
            var version = _ApiVersion.Invoke();
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _glamourChanged.Disable();
        _glamourChanged?.Dispose();

        // revert our character back to the base game state (But dont if we are closing the game)
        Task.Run(() => _frameworkUtils.RunOnFrameworkThread(() =>
        {
            // do not run this if we are closing out of the game.
            if (_frameworkUtils.IsFrameworkUnloading)
            {
                Logger.LogWarning("Not reverting character as we are closing the game.", LoggerType.IpcGlamourer);
                return;
            }

            // revert the character. (for disabling the plugin)
            switch (_gagspeakConfig.Current.RevertStyle)
            {
                case RevertStyle.RevertToGame: GlamourerRevertToGame(false).ConfigureAwait(false); break;
                case RevertStyle.RevertEquipToGame: GlamourerRevertToGame(true).ConfigureAwait(false); break;
                case RevertStyle.RevertToAutomation: GlamourerRevertToAutomation(false).ConfigureAwait(false); break;
                case RevertStyle.RevertEquipToAutomation: GlamourerRevertToAutomation(true).ConfigureAwait(false); break;
            }
        }));
    }

    /// <summary> ========== BEGIN OUR IPC CALL MANAGEMENT UNDER ASYNC TASKS ========== </summary>
    public async Task SetItemToCharacterAsync(ApiEquipSlot slot, ulong item, IReadOnlyList<byte> dye, uint variant)
    {
        // if the glamourerApi is not active, then return an empty string for the customization
        if (!APIAvailable || _clientService.IsZoning) return;
        try
        {
            // await for us to be running on the framework thread. Once we are:
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                _SetItem!.Invoke(0, slot, item, dye, 1337);
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // if at any point this errors, return an empty string as well.
            Logger.LogWarning($"Failed to set item to character with slot {slot}, item {item}, dye {dye.ToArray().ToString()}, and key {variant}, {ex}", LoggerType.IpcGlamourer);
            return;
        }
    }

    public JObject? GetState()
    {
        try
        {
            var success = _glamourerGetState.Invoke(0);
            return success.Item2;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error during GetState: {ex}", LoggerType.IpcGlamourer);
            return null;
        }
    }

    public bool SetRestraintEquipmentFromState(RestraintSet setToEdit)
    {

        // Get the player state and equipment JObject
        var playerState = GetState();
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
    }

    public void SetRestraintCustomizationsFromState(RestraintSet setToEdit)
    {
        var playerState = GetState();
        setToEdit.CustomizeObject = playerState!["Customize"] ?? new JObject();
        setToEdit.ParametersObject = playerState!["Parameters"] ?? new JObject();
    }

    private EquipDrawData? UpdateItem(JToken? item, string slotName)
    {
        if (item == null) return null;

        var customItemId = item["ItemId"]?.Value<ulong>() ?? 4294967164;
        var stain = item["Stain"]?.Value<int>() ?? 0;
        var stain2 = item["Stain2"]?.Value<int>() ?? 0;

        StainIds gameStain = new StainIds((StainId)stain, (StainId)stain2);
        return new EquipDrawData(ItemIdVars.NothingItem((EquipSlot)Enum.Parse(typeof(EquipSlot), slotName)))
        {
            Slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), slotName),
            IsEnabled = true,
            GameItem = ItemIdVars.Resolve((EquipSlot)Enum.Parse(typeof(EquipSlot), slotName), new CustomItemId(customItemId)),
            GameStain = gameStain
        };
    }

    public async Task<bool> ForceSetMetaData(MetaData metaData, bool? forcedState = null)
    {
        // if the glamourerApi is not active, then return an empty string for the customization
        if (!APIAvailable || _clientService.IsZoning) return false;
        try
        {
            return await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                // grab the JObject of the character state.
                var playerState = GetState();

                if (metaData is MetaData.Both or MetaData.Hat)
                {
                    playerState!["Equipment"]!["Hat"]!["Show"] = forcedState ?? !(((bool?)playerState?["Equipment"]?["Hat"]?["Show"]) ?? false); ;
                    playerState!["Equipment"]!["Hat"]!["Apply"] = true;
                }
                if (metaData is MetaData.Both or MetaData.Visor)
                {
                    playerState!["Equipment"]!["Visor"]!["IsToggled"] = forcedState ?? !(((bool?)playerState?["Equipment"]?["Visor"]?["IsToggled"]) ?? false); ;
                    playerState!["Equipment"]!["Visor"]!["Apply"] = true;
                }

                var ret = _ApplyState.Invoke(playerState!, 0);
                return ret == GlamourerApiEc.Success;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error during SetMetaData: {ex}", LoggerType.IpcGlamourer);
            return false;
        }
    }

    public async Task ForceSetCustomize(JToken customizations, JToken parameters)
    {
        // if the glamourerApi is not active, then return an empty string for the customization
        if (!APIAvailable || _clientService.IsZoning) return;
        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                // grab the JObject of the character state.
                var playerState = GetState();

                // set the customizations and parameters
                playerState!["Customize"] = customizations;
                playerState!["Parameters"] = parameters;

                // apply the new state.
                _ApplyState.Invoke(playerState!, 0);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error during ForceSetCustomize: {ex}", LoggerType.IpcGlamourer);
        }
    }

    public async Task GlamourerRevertToAutomation(bool reapply)
    {
        if (!APIAvailable || _clientService.IsZoning) return;

        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                Logger.LogTrace("Calling on IPC: GlamourerRevertToAutomation", LoggerType.IpcGlamourer);
                var result = _RevertToAutomation.Invoke(0);

                if (result != GlamourerApiEc.Success)
                {
                    Logger.LogWarning($"Revert to automation failed, reverting to game instead", LoggerType.IpcGlamourer);
                    _RevertCharacter.Invoke(0);
                }
            });

            Logger.LogTrace("Finished RevertToAutomation", LoggerType.IpcGlamourer);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error during GlamourerRevertToAutomation: {ex}", LoggerType.IpcGlamourer);
        }
    }

    public async Task GlamourerRevertToGame(bool reapply)
    {
        if (!APIAvailable || _clientService.IsZoning) return;

        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                Logger.LogTrace("Calling on IPC: GlamourerRevertToGame", LoggerType.IpcGlamourer);
                var flagsToUse = reapply ? ApplyFlag.Equipment : ApplyFlag.Equipment | ApplyFlag.Customization;
                _RevertCharacter.Invoke(0, flags: flagsToUse);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error during GlamourerRevertToGame: {ex}", LoggerType.IpcGlamourer);
        }
    }

    public static DateTime LastBlockedItemTime = DateTime.MinValue;

    /// <summary> Fired upon by the IPC event subscriber when the glamourer changes. </summary>
    /// <param name="address"> The address of the character that changed. </param>
    /// <param name="changeType"> The type of change that occurred. </param>"
    private void GlamourerChanged(nint address, StateChangeType changeType)
    {
        // do not accept if coming from other player besides us.
        if (address != _clientService.Address) return;

        // block if we are not desiring to listen to changes yet.
        if (OnFrameworkService.GlamourChangeEventsDisabled) 
        {
            Logger.LogTrace($"GlamourEvent Blocked Change Events Still disabled!: {changeType}", LoggerType.IpcGlamourer);
            return;
        }

        // Block if we are currently processing an Appearance Change Task
        if (AppearanceManager.IsApplierProcessing)
        {
            if(changeType is not (StateChangeType.Equip or StateChangeType.Stains))
                Logger.LogTrace($"GlamourEvent Blocked: [Appearance Managers Applier is Processing]", LoggerType.IpcGlamourer);
            return;
        }


        if(changeType is StateChangeType.Reset or StateChangeType.Design or StateChangeType.Reapply or StateChangeType.Equip or StateChangeType.Stains)
        {
            Logger.LogTrace($"StateChangeType is {changeType}", LoggerType.IpcGlamourer);
            IpcFastUpdates.InvokeGlamourer(GlamourUpdateType.ReapplyAll);
            return;
        }

        // See if the change type is a type we are looking for
        if (changeType is StateChangeType.Equip or StateChangeType.Stains or StateChangeType.Other)
        {
            Logger.LogTrace($"StateChangeType is {changeType}", LoggerType.IpcGlamourer);
            IpcFastUpdates.InvokeGlamourer(GlamourUpdateType.RefreshAll);
            return;
        }

        Logger.LogTrace($"GlamourerChanged event was not a type we care about, so skipping (Type was: {changeType})", LoggerType.IpcGlamourer);
    }
}
