using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using TerraFX.Interop.Windows;

namespace GagSpeak.Services;

/// <summary>
///     Handles the distribution of ClientData IPC changes to other Visible Kinksters. <para />
///     Received calls for other visible kinkster's are processed by the kinkster listener. <para />
/// </summary>
public sealed class KinksterSyncService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly IpcManager _ipc;
    private readonly KinksterManager _kinksters;

    // The buffer used for an awaiter token on a debounce execution.
    private Task? _kinksterSyncTask;
    private CancellationTokenSource _syncUpdateCTS = new();
    public KinksterSyncService(ILogger<KinksterSyncService> logger, GagspeakMediator mediator, 
        MainHub hub, IpcManager ipc, KinksterManager kinksters) 
        : base(logger, mediator)
    {
        _hub = hub;
        _ipc = ipc;
        _kinksters = kinksters;

        // IPC Data Updaters
        Mediator.Subscribe<GlamourerChanged>(this, (msg) =>
        {
            if (PlayerData.IsZoning)
                return;
            Logger.LogInformation("Client GlamourState was updated, syncing with Kinksters!");
            AddPendingSync(DataSyncKind.Glamourer);
        });

        Mediator.Subscribe<CustomizeProfileChange>(this, (msg) =>
        {
            if (PlayerData.IsZoning || msg.Address == IntPtr.Zero)
                return;
            Logger.LogInformation($"Your C+ profile changed to {msg.Id}, syncing to Kinksters!");
            AddPendingSync(DataSyncKind.CPlus);
        });

        Mediator.Subscribe<HeelsOffsetChanged>(this, (msg) =>
        {
            if (PlayerData.IsZoning)
                return;
            Logger.LogInformation("Your heels offset changed, syncing with Kinksters!");
            AddPendingSync(DataSyncKind.Heels);
        });

        Mediator.Subscribe<HonorificTitleChanged>(this, (msg) =>
        {
            if (PlayerData.IsZoning)
                return;
            // maybe some comparison here, but also could move it elsewhere.
            if (!_lastHonorific.Equals(msg.NewTitle, StringComparison.Ordinal))
                return;
            Logger.LogInformation("Client HonorificTitle was updated, syncing with Kinksters!");
            AddPendingSync(DataSyncKind.Honorific);
        });

        Mediator.Subscribe<PetNamesDataChanged>(this, (msg) =>
        {
            if (PlayerData.IsZoning)
                return;
            // maybe some comparison here, but also could move it elsewhere.
            if (!_lastPetNames.Equals(msg.NicknamesData, StringComparison.Ordinal))
                return;
            Logger.LogInformation("Client PetNames was updated, syncing with Kinksters!");
            AddPendingSync(DataSyncKind.PetNames);
        });

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (msg) => ProcessKinksterSync());
    }

    // internal references to the last sent clientString states.
    private DataSyncKind _pendingTypes = DataSyncKind.None;
    // If we could create associated meta manips to link to pcp's it might be helpful but otherwise is bloated space.
    private string _lastModManips = string.Empty;
    private string _lastGlamourer = string.Empty;
    private string _lastCPlus = string.Empty;
    private string _lastHeels = string.Empty;
    private string _lastHonorific = string.Empty;
    private string _lastPetNames = string.Empty;

    private void AddPendingSync(DataSyncKind syncType)
    {
        // Halt the current update task delay and recreate for the new type.
        _pendingTypes |= syncType;
        _syncUpdateCTS = _syncUpdateCTS.SafeCancelRecreate();
    }

    private int GetDebounceTime() => _pendingTypes.HasAny(DataSyncKind.Heels) 
        ? 1000 : _pendingTypes.HasAny(DataSyncKind.Glamourer | DataSyncKind.CPlus) ? 750 : 500;

    // Occurs every framework tick. If there is nothing to process, simply return.
    private void ProcessKinksterSync()
    {
        // if nothing to update, return.
        if (_pendingTypes is DataSyncKind.None)
            return;
        // if zoning or not available, return.
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;
        // if the task is already being processed, return.
        if (_kinksterSyncTask?.IsCompleted ?? true)
        {
            // create the task to run with a cancelation token.
            _kinksterSyncTask = Task.Run(async () =>
            {
                // await for the processed debounce time, or until cancelled.
                await Task.Delay(GetDebounceTime(), _syncUpdateCTS.Token).ConfigureAwait(false);

                Logger.LogInformation($"Processing KinksterSync for changes: {_pendingTypes}");
                // after the delay is finished, process the update, if there is anyone to send it to.
                var toUpdate = _kinksters.GetVisibleUsers();
                if (toUpdate.Count == 0)
                {
                    Logger.LogDebug("No visible Kinksters to update.");
                    _pendingTypes = DataSyncKind.None;
                    return;
                }

                try
                {
                    Logger.LogDebug($"Found {toUpdate.Count} visible Kinksters to update.");
                    var byteVal = (byte)_pendingTypes;
                    // Only Glamourer.
                    if (byteVal == 1)
                        await SyncGlamourStateToKinksters(toUpdate).ConfigureAwait(false);
                    // Only ModManips.
                    else if (byteVal == 2)
                        await SyncModManipsToKinksters(toUpdate).ConfigureAwait(false);
                    // Only light data.
                    else if (!_pendingTypes.HasAny(DataSyncKind.Glamourer | DataSyncKind.ModManips))
                        await SyncLightDataToKinksters(toUpdate).ConfigureAwait(false);
                    // Full Data.
                    else
                        await SyncNewDataToKinksters(toUpdate).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogDebug("KinksterSync cancelled");
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error during KinksterSync Processing");
                }
                finally
                {
                    _pendingTypes = DataSyncKind.None;
                    Logger.LogDebug("KinksterSync complete!");
                }
            }, _syncUpdateCTS.Token);
        }
    }

    private async Task SyncModManipsToKinksters(List<UserData> visibleKinksters)
    {
        if (!IpcCallerPenumbra.APIAvailable)
            return;

        var newManips = _ipc.Penumbra.GetModManipulations();
        if (_lastModManips.Equals(newManips, StringComparison.Ordinal))
            return;

        Logger.LogTrace($"ModManips changed!", LoggerType.VisiblePairs);
        _lastModManips = newManips;

        Logger.LogDebug("Syncing ModManips to Kinksters");
        await _hub.UserPushIpcModManips(new(visibleKinksters, newManips)).ConfigureAwait(false);
    }

    private async Task SyncGlamourStateToKinksters(List<UserData> visibleKinksters)
    {
        if (!IpcCallerGlamourer.APIAvailable)
            return;
        
        var latestGlamStr = await _ipc.Glamourer.GetActorString().ConfigureAwait(false);
        if (_lastGlamourer.Equals(latestGlamStr, StringComparison.Ordinal))
        {
            Logger.LogTrace("Glamourer string unchanged, not syncing.", LoggerType.VisiblePairs);
            return;
        }

        Logger.LogTrace("Glamourer changed!", LoggerType.VisiblePairs);
        _lastGlamourer = latestGlamStr;
        
        Logger.LogDebug("Syncing GlamourState to Kinksters");
        await _hub.UserPushIpcGlamourer(new(visibleKinksters, _lastGlamourer)).ConfigureAwait(false);
    }

    private async Task SyncLightDataToKinksters(List<UserData> visibleKinksters)
    {
        var toSend = new CharaIpcLight();
        // if the data is not different, so not apply it, but otherwise, do so.
        if (IpcCallerCustomize.APIAvailable)
        {
            var latestCPlus = await _ipc.CustomizePlus.GetClientProfile().ConfigureAwait(false);
            if (_lastCPlus.Equals(latestCPlus, StringComparison.Ordinal))
                return;
            Logger.LogTrace("CPlus changed profiles!", LoggerType.VisiblePairs);
            toSend.CustomizeProfile = latestCPlus;
            _lastCPlus = toSend.CustomizeProfile;
        }
        if (IpcCallerHeels.APIAvailable)
        {
            var latestHeels = await _ipc.Heels.GetClientOffset().ConfigureAwait(false);
            if (_lastHeels.Equals(latestHeels, StringComparison.Ordinal))
                return;
            Logger.LogTrace("Heels offset changed!", LoggerType.VisiblePairs);
            toSend.HeelsOffset = latestHeels;
            _lastHeels = toSend.HeelsOffset;
        }
        if (IpcCallerHonorific.APIAvailable)
        {
            var latestTitle = await _ipc.Honorific.GetTitle().ConfigureAwait(false);
            if (_lastHonorific.Equals(latestTitle, StringComparison.Ordinal))
                return;
            Logger.LogTrace("Honorific title changed!", LoggerType.VisiblePairs);
            toSend.HonorificTitle = latestTitle;
            _lastHonorific = toSend.HonorificTitle;
        }
        if (IpcCallerPetNames.APIAvailable)
        {
            var latestNicks = _ipc.PetNames.GetPetNicknames();
            if (_lastPetNames.Equals(latestNicks, StringComparison.Ordinal))
                return;
            Logger.LogTrace("Pet nicknames changed!", LoggerType.VisiblePairs);
            toSend.PetNicknames = latestNicks;
            _lastPetNames = toSend.PetNicknames;
        }

        Logger.LogDebug("Syncing Light Data to Kinksters");
        await _hub.UserPushIpcDataLight(new(visibleKinksters, toSend)).ConfigureAwait(false);
    }

    private async Task SyncNewDataToKinksters(List<UserData> visibleKinksters)
    {
        var toSend = new CharaIpcDataFull();
        // gather all the data.
        if (IpcCallerGlamourer.APIAvailable)
        {
            var latestGlamStr = await _ipc.Glamourer.GetActorString().ConfigureAwait(false);
            if (_lastGlamourer.Equals(latestGlamStr, StringComparison.Ordinal))
                return;
            Logger.LogTrace("Glamourer changed!", LoggerType.VisiblePairs);
            toSend.GlamourerBase64 = latestGlamStr;
            _lastGlamourer = toSend.GlamourerBase64;
        }
        if (IpcCallerCustomize.APIAvailable)
        {
            var latestCPlus = await _ipc.CustomizePlus.GetClientProfile().ConfigureAwait(false);
            if (_lastCPlus.Equals(latestCPlus, StringComparison.Ordinal))
                return;
            Logger.LogTrace("CPlus changed profiles!", LoggerType.VisiblePairs);
            toSend.CustomizeProfile = latestCPlus;
            _lastCPlus = toSend.CustomizeProfile;
        }
        if (IpcCallerHeels.APIAvailable)
        {
            var latestHeels = await _ipc.Heels.GetClientOffset().ConfigureAwait(false);
            if (_lastHeels.Equals(latestHeels, StringComparison.Ordinal))
                return;
            Logger.LogTrace("Heels offset changed!", LoggerType.VisiblePairs);
            toSend.HeelsOffset = latestHeels;
            _lastHeels = toSend.HeelsOffset;
        }
        if (IpcCallerHonorific.APIAvailable)
        {
            var latestTitle = await _ipc.Honorific.GetTitle().ConfigureAwait(false);
            if (_lastHonorific.Equals(latestTitle, StringComparison.Ordinal))
                return;
            Logger.LogTrace("Honorific title changed!", LoggerType.VisiblePairs);
            toSend.HonorificTitle = latestTitle;
            _lastHonorific = toSend.HonorificTitle;
        }
        if (IpcCallerPetNames.APIAvailable)
        {
            var latestNicks = _ipc.PetNames.GetPetNicknames();
            if (_lastPetNames.Equals(latestNicks, StringComparison.Ordinal))
                return;
            Logger.LogTrace("Pet nicknames changed!", LoggerType.VisiblePairs);
            toSend.PetNicknames = latestNicks;
            _lastPetNames = toSend.PetNicknames;
        }
        // push it out.
        Logger.LogDebug("Compiling Full Data to Kinksters");
        await _hub.UserPushIpcData(new(visibleKinksters, toSend)).ConfigureAwait(false);
    }

    public async Task SyncAppearanceToKinksters(List<UserData> visibleKinksters)
    {
        // reset the pending types / changes along with any pending sync task.
        _pendingTypes = DataSyncKind.None;
        _syncUpdateCTS = _syncUpdateCTS.SafeCancelRecreate();
        Logger.LogInformation($"Syncing Full Appearance to Kinksters: ({string.Join(",", visibleKinksters.Select(k => k.AliasOrUID))})");

        // create the full thing to send to all visible kinksters.
        var appearance = new CharaIpcDataFull();
        // gather all the data.
        if (IpcCallerGlamourer.APIAvailable)
        {
            appearance.GlamourerBase64 = await _ipc.Glamourer.GetActorString().ConfigureAwait(false);
            Logger.LogDebug($"GlamourerAPI was valid, and obtained actor string: {appearance.GlamourerBase64.ToString()}");
            _lastGlamourer = appearance.GlamourerBase64;
        }
        if (IpcCallerCustomize.APIAvailable)
        {
            appearance.CustomizeProfile = await _ipc.CustomizePlus.GetClientProfile().ConfigureAwait(false);
            Logger.LogDebug($"CustomizePlusAPI was valid, and obtained profile string: {appearance.CustomizeProfile.ToString()}");
            _lastCPlus = appearance.CustomizeProfile ?? string.Empty;
        }
        if (IpcCallerHeels.APIAvailable)
        {
            appearance.HeelsOffset = await _ipc.Heels.GetClientOffset().ConfigureAwait(false);
            Logger.LogDebug($"HeelsAPI was valid, and obtained offset string: {appearance.HeelsOffset.ToString()}");
            _lastHeels = appearance.HeelsOffset;
        }
        if (IpcCallerHonorific.APIAvailable)
        {
            appearance.HonorificTitle = await _ipc.Honorific.GetTitle().ConfigureAwait(false);
            Logger.LogDebug($"HonorificAPI was valid, and obtained title string: {appearance.HonorificTitle.ToString()}");
            _lastHonorific = appearance.HonorificTitle;
        }
        if (IpcCallerPetNames.APIAvailable)
        {
            appearance.PetNicknames = _ipc.PetNames.GetPetNicknames();
            Logger.LogDebug($"PetNamesAPI was valid, and obtained nicknames string: {appearance.PetNicknames.ToString()}");
            _lastPetNames = appearance.PetNicknames;
        }
        // push it out.
        Logger.LogDebug("Compiling Full Data to Kinksters");
        await _hub.UserPushIpcData(new(visibleKinksters, appearance)).ConfigureAwait(false);
    }
}
