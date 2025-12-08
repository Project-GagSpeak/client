using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Ipc;
using FFXIVClientStructs.FFXIV.Client.System.Timer;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Network;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara.Delegates;

namespace GagSpeak.Interop;

public sealed class IpcCallerMoodles : IIpcCaller
{
    // TERMINOLOGY:
    // StatusManager == The manager handling the current active statuses on you.
    // Status == The individual "Moodle" in your Moodles tab under the Moodles UI.
    // Preset == The collection of Statuses to apply at once. Stored in a preset.

    // Remember, all these are called only when OUR client changes. Not other pairs.
    private readonly ICallGateSubscriber<int> _moodlesApiVersion;

    public readonly ICallGateSubscriber<IPlayerCharacter, object> OnStatusManagerModified;  // The status manager of a player has changed.
    public readonly ICallGateSubscriber<Guid, object> OnStatusSettingsModified; // Client changed a status's settings.
    public readonly ICallGateSubscriber<Guid, object> OnPresetModified;         // Client changed a preset's settings.

    // API Getters
    private readonly ICallGateSubscriber<Guid, MoodlesStatusInfo> GetStatusInfo;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>> GetStatusInfoList;
    private readonly ICallGateSubscriber<Guid, MoodlePresetInfo> GetPresetInfo;
    private readonly ICallGateSubscriber<List<MoodlePresetInfo>> GetPresetsInfoList;

    private readonly ICallGateSubscriber<string> GetOwnStatusManager;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>> GetOwnStatusManagerInfo;
    private readonly ICallGateSubscriber<string, string> GetOtherStatusManager;
    private readonly ICallGateSubscriber<string, List<MoodlesStatusInfo>> GetOtherStatusManagerInfo;

    // Used when a pair applies their statuses to us.
    private readonly ICallGateSubscriber<string, List<MoodlesStatusInfo>, object> ApplyStatusFromPair;

    // API Enactors
    private readonly ICallGateSubscriber<Guid, string, object> ApplyStatus;
    private readonly ICallGateSubscriber<Guid, string, object> ApplyPreset;
    private readonly ICallGateSubscriber<List<Guid>, string, object> RemoveStatuses;
    private readonly ICallGateSubscriber<string, string, object> SetStatusManager;
    private readonly ICallGateSubscriber<string, object> ClearStatusManager;

    private readonly ILogger<IpcCallerMoodles> _logger;
    private readonly GagspeakMediator _mediator;

    public IpcCallerMoodles(ILogger<IpcCallerMoodles> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;

        _moodlesApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Moodles.Version");

        // API Getters
        GetStatusInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodlesStatusInfo>("Moodles.GetStatusInfoV2");
        GetStatusInfoList = Svc.PluginInterface.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetStatusInfoListV2");
        GetPresetInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodlePresetInfo>("Moodles.GetPresetInfoV2");
        GetPresetsInfoList = Svc.PluginInterface.GetIpcSubscriber<List<MoodlePresetInfo>>("Moodles.GetPresetsInfoListV2");
        GetOwnStatusManager = Svc.PluginInterface.GetIpcSubscriber<string>("Moodles.GetClientStatusManagerV2");
        GetOtherStatusManager = Svc.PluginInterface.GetIpcSubscriber<string, string>("Moodles.GetStatusManagerByNameV2");
        GetOwnStatusManagerInfo = Svc.PluginInterface.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetClientStatusManagerInfoV2");
        GetOtherStatusManagerInfo = Svc.PluginInterface.GetIpcSubscriber<string, List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoByNameV2");

        // API Enactors
        ApplyStatus = Svc.PluginInterface.GetIpcSubscriber<Guid, string, object>("Moodles.AddOrUpdateStatusByNameV2");
        ApplyPreset = Svc.PluginInterface.GetIpcSubscriber<Guid, string, object>("Moodles.ApplyPresetByNameV2");
        RemoveStatuses = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, string, object>("Moodles.RemoveMoodlesByNameV2");
        SetStatusManager = Svc.PluginInterface.GetIpcSubscriber<string, string, object>("Moodles.SetStatusManagerByNameV2");
        ClearStatusManager = Svc.PluginInterface.GetIpcSubscriber<string, object>("Moodles.ClearStatusManagerByNameV2");
        ApplyStatusFromPair = Svc.PluginInterface.GetIpcSubscriber<string, List<MoodlesStatusInfo>, object>("Moodles.GagSpeak.StatusInfoAppliedByPair");

        // API Action Events:
        OnStatusManagerModified = Svc.PluginInterface.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        OnStatusSettingsModified = Svc.PluginInterface.GetIpcSubscriber<Guid, object>("Moodles.StatusModified");
        OnPresetModified = Svc.PluginInterface.GetIpcSubscriber<Guid, object>("Moodles.PresetModified");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            var result = _moodlesApiVersion.InvokeFunc() >= 3;
            if (!APIAvailable && result)
                _mediator.Publish(new MoodlesReady());
            APIAvailable = result;
        }
        catch
        {
            // Moodles was not ready yet / went offline. Set back to false. (Statuses are auto-cleared by moodles)
            APIAvailable = false;
        }
    }

    public void Dispose()
    {
        // Disposing my pain, sweat, tears, and blood that this had to become async to work.
        // If this is being read down the line, I hope support is added back.
    }

    /// <summary> This method gets the moodles info for a provided GUID from the client. </summary>
    public async Task<MoodlesStatusInfo> GetStatusDetails(Guid guid)
    {
        return await ExecuteIpcOnThread(() => GetStatusInfo.InvokeFunc(guid));
    }

    /// <summary> This method gets the list of all our clients Moodles Info </summary>
    public async Task<IEnumerable<MoodlesStatusInfo>> GetStatusListDetails()
    {
        return await ExecuteIpcOnThread(GetStatusInfoList.InvokeFunc) ?? Enumerable.Empty<MoodlesStatusInfo>();
    }

    /// <summary> This method gets the preset info for a provided GUID from the client. </summary>
    public async Task<MoodlePresetInfo> GetPresetDetails(Guid guid)
    {
        return await ExecuteIpcOnThread(() => GetPresetInfo.InvokeFunc(guid));
    }

    /// <summary> This method gets the list of all our clients Presets Info </summary>
    public async Task<IEnumerable<MoodlePresetInfo>> GetPresetListDetails()
    {
        return await ExecuteIpcOnThread(GetPresetsInfoList.InvokeFunc) ?? Enumerable.Empty<MoodlePresetInfo>();
    }

    /// <summary> This method gets the status information of our client player </summary>
    public async Task<IEnumerable<MoodlesStatusInfo>> GetStatusManagerDetails()
    {
        return await ExecuteIpcOnThread(GetOwnStatusManagerInfo.InvokeFunc) ?? Enumerable.Empty<MoodlesStatusInfo>();
    }

    /// <summary> Obtain the status information of a visible player </summary>
    public async Task<IEnumerable<MoodlesStatusInfo>> GetStatusManagerDetails(string playerNameWithWorld)
    {
        return await ExecuteIpcOnThread(() => GetOtherStatusManagerInfo.InvokeFunc(playerNameWithWorld)) ?? Enumerable.Empty<MoodlesStatusInfo>();
    }

    /// <summary> Gets the ClientPlayer's StatusManager string. </summary>
    public async Task<string> GetStatusManagerString()
    {
        return await ExecuteIpcOnThread(GetOwnStatusManager.InvokeFunc) ?? string.Empty;
    }

    /// <summary> Gets the status of the moodles for a particular PlayerCharacter </summary>
    public async Task<string> GetStatusManagerString(string playerNameWithWorld)
    {
        return await ExecuteIpcOnThread(() => GetOtherStatusManager.InvokeFunc(playerNameWithWorld)) ?? string.Empty;
    }

    public async Task ApplyOwnStatusByGUID(Guid guid, string clientName)
    {
        await ExecuteIpcOnThread(() => ApplyStatus.InvokeAction(guid, clientName));
    }

    public async Task ApplyOwnStatusByGUID(IEnumerable<Guid> guidsToAdd)
    {
        await ExecuteIpcOnThread(() =>
        {
            var clientNameWorld = PlayerData.NameWithWorld;
            foreach (var guid in guidsToAdd)
                ApplyStatus.InvokeAction(guid, clientNameWorld);
        });
    }

    public async Task ApplyOwnPresetByGUID(Guid guid)
    {
        await ExecuteIpcOnThread(() => ApplyPreset.InvokeAction(guid, PlayerData.NameWithWorld));
    }

    /// <summary> This method applies the statuses from a pair to the client </summary>
    public async Task ApplyStatusesFromPairToSelf(string applierNameWithWorld, string recipientNameWithWorld, IEnumerable<MoodlesStatusInfo> statuses)
    {
        await ExecuteIpcOnThread(() => ApplyStatusFromPair.InvokeAction(applierNameWithWorld, recipientNameWithWorld, [.. statuses]));
    }

    /// <summary> This method removes the moodles from the client </summary>
    public async Task RemoveOwnStatusByGuid(IEnumerable<Guid> guidsToRemove)
    {
        await ExecuteIpcOnThread(() => RemoveStatuses.InvokeAction(guidsToRemove.ToList(), PlayerData.NameWithWorld));
    }

    public async Task SetStatus(string playerNameWithWorld, string statusBase64)
    {
        await ExecuteIpcOnThread(() => SetStatusManager.InvokeAction(playerNameWithWorld, statusBase64));
    }

    public async Task ClearStatus()
    {
        await ClearStatus(PlayerData.NameWithWorld);
    }

    /// <summary> Reverts the status of the moodles for a GameObject specified by the pointer</summary>
    public async Task ClearStatus(string playerNameWithWorld)
    {
        await ExecuteIpcOnThread(() => ClearStatusManager.InvokeAction(playerNameWithWorld));
    }

    /// <summary> Executes a Moodles Ipc Action on the framework thread. </summary>
    /// <remarks> This action will not execute if APIAvailable is false. </remarks>
    private async Task ExecuteIpcOnThread(Action act)
    {
        if (!APIAvailable)
            return;

        try
        {
            await Svc.Framework.RunOnFrameworkThread(() => act()).ConfigureAwait(false);
        }
        catch (Bagagwa ex)
        {
            _logger.LogWarning(ex, "Moodles IPC Action Operation had an Error:\n");
        }
    }

    /// <summary> Executes a Moodles Ipc Func on the framework thread and returns a result. </summary>
    /// <returns> Returns default(T) on failure. </returns>
    /// <remarks> not all types may have valid default(T)'s. If you find this to be the case, do a result check. </remarks>
    private async Task<T?> ExecuteIpcOnThread<T>(Func<T> func)
    {
        if (!APIAvailable)
            return default;

        try
        {
            return await Svc.Framework.RunOnFrameworkThread(() => func()).ConfigureAwait(false);
        }
        catch (Bagagwa ex)
        {
            _logger.LogWarning(ex, "Moodles IPC Func Operation had an Error:\n");
            return default;
        }
    }

}
