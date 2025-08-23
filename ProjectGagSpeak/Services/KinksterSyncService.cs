using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

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
            Logger.LogDebug("Client GlamourState was updated, syncing with Kinksters!");
            AddPendingSync(DataSyncKind.Glamourer);
        });

        Mediator.Subscribe<CustomizeProfileChange>(this, (msg) =>
        {
            if (PlayerData.IsZoning || msg.Address == IntPtr.Zero)
                return;
            Logger.LogDebug($"Your C+ profile changed to {msg.Id}, syncing to Kinksters!");
            AddPendingSync(DataSyncKind.CPlus);
        });

        Mediator.Subscribe<HeelsOffsetChanged>(this, (msg) =>
        {
            if (PlayerData.IsZoning)
                return;
            Logger.LogDebug("Your heels offset changed, syncing with Kinksters!");
            AddPendingSync(DataSyncKind.Heels);
        });

        Mediator.Subscribe<HonorificTitleChanged>(this, (msg) =>
        {
            if (PlayerData.IsZoning)
                return;
            // maybe some comparison here, but also could move it elsewhere.
            if (!_lastHonorific.Equals(msg.NewTitle, StringComparison.Ordinal))
                return;
            Logger.LogDebug("Client HonorificTitle was updated, syncing with Kinksters!");
            AddPendingSync(DataSyncKind.Honorific);
        });

        Mediator.Subscribe<PetNamesDataChanged>(this, (msg) =>
        {
            if (PlayerData.IsZoning)
                return;
            // maybe some comparison here, but also could move it elsewhere.
            if (!_lastPetNames.Equals(msg.NicknamesData, StringComparison.Ordinal))
                return;
            Logger.LogDebug("Client PetNames was updated, syncing with Kinksters!");
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
        _syncUpdateCTS.SafeCancelRecreate();
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
        if (_kinksterSyncTask is null || !_kinksterSyncTask.IsCompleted)
            return;

        // create the task to run with a cancelation token.
        _kinksterSyncTask = Task.Run(async () =>
        {
            // await for the processed debounce time, or until cancelled.
            await Task.Delay(GetDebounceTime(), _syncUpdateCTS.Token).ConfigureAwait(false);
            // after the delay is finished, process the update, if there is anyone to send it to.
            var toUpdate = _kinksters.GetVisibleUsers();
            if (toUpdate.Count == 0)
                return;

            try
            {
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
                    await SyncDataToKinksters(toUpdate).ConfigureAwait(false);
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
                Logger.LogDebug("KinksterSync complete!");
            }
        }, _syncUpdateCTS.Token);
    }

    private async Task SyncModManipsToKinksters(List<UserData> visibleKinksters)
    {
        var newManips = _ipc.Penumbra.GetModManipulations();
        if (_lastModManips.Equals(newManips, StringComparison.Ordinal))
            return;
        // update and send.
        Logger.LogTrace($"ModManips changed!", LoggerType.VisiblePairs);
        _lastModManips = newManips;

        Logger.LogDebug("Syncing ModManips to Kinksters");
        await _hub.UserPushIpcModManips(new(visibleKinksters, newManips)).ConfigureAwait(false);
    }

    private async Task SyncGlamourStateToKinksters(List<UserData> visibleKinksters)
    {
        var newGlamStr = await _ipc.Glamourer.GetActorString().ConfigureAwait(false);
        if (_lastGlamourer.Equals(newGlamStr, StringComparison.Ordinal))
            return;
        Logger.LogTrace($"Glamourer changed!", LoggerType.VisiblePairs);
        _lastGlamourer = newGlamStr;

        Logger.LogDebug("Syncing GlamourState to Kinksters");
        await _hub.UserPushIpcGlamourer(new(visibleKinksters, newGlamStr)).ConfigureAwait(false);
    }

    private async Task SyncLightDataToKinksters(List<UserData> visibleKinksters)
    {
        var lightData = new CharaIpcLight();

        // if the data is not different, so not apply it, but otherwise, do so.
        var newCPlus = await _ipc.CustomizePlus.GetClientProfile().ConfigureAwait(false) ?? string.Empty;
        if (!_lastCPlus.Equals(newCPlus, StringComparison.Ordinal))
        {
            Logger.LogTrace($"CPlus changed active profiles!", LoggerType.VisiblePairs);
            _lastCPlus = newCPlus;
            lightData.CustomizeProfile = newCPlus;
        }

        var newHeels = await _ipc.Heels.GetClientOffset().ConfigureAwait(false);
        if (!_lastHeels.Equals(newHeels, StringComparison.Ordinal))
        {
            Logger.LogTrace($"Heels offset changed!", LoggerType.VisiblePairs);
            _lastHeels = newHeels;
            lightData.HeelsOffset = newHeels;
        }

        var newTitle = await _ipc.Honorific.GetTitle().ConfigureAwait(false);
        if (!_lastHonorific.Equals(newTitle, StringComparison.Ordinal))
        {
            Logger.LogTrace($"Honorific title changed!", LoggerType.VisiblePairs);
            _lastHonorific = newTitle;
            lightData.HonorificTitle = newTitle;
        }

        var newNicks = _ipc.PetNames.GetPetNicknames();
        if (!_lastPetNames.Equals(newNicks, StringComparison.Ordinal))
        {
            Logger.LogTrace($"Pet nicknames changed!", LoggerType.VisiblePairs);
            _lastPetNames = newNicks;
            lightData.PetNicknames = newNicks;
        }

        Logger.LogDebug("Syncing Light Data to Kinksters");
        await _hub.UserPushIpcDataLight(new(visibleKinksters, lightData)).ConfigureAwait(false);
    }

    private async Task SyncDataToKinksters(List<UserData> visibleKinksters)
    {
        var toSend = new CharaIpcDataFull();

        var newGlamStr = await _ipc.Glamourer.GetActorString().ConfigureAwait(false);
        if (!_lastGlamourer.Equals(newGlamStr, StringComparison.Ordinal))
        {
            Logger.LogTrace($"Glamourer changed!", LoggerType.VisiblePairs);
            _lastGlamourer = newGlamStr;
            toSend.GlamourerBase64 = newGlamStr;
        }

        //var newManips = _ipc.Penumbra.GetModManipulations();
        //if (!_lastModManips.Equals(newManips, StringComparison.Ordinal))
        //{
        //    Logger.LogTrace($"ModManips changed!", LoggerType.VisiblePairs);
        //    _lastModManips = newManips;
        //    toSend.ModManips = newManips;
        //}

        // if the data is not different, so not apply it, but otherwise, do so.
        var newCPlus = await _ipc.CustomizePlus.GetClientProfile().ConfigureAwait(false) ?? string.Empty;
        if (!_lastCPlus.Equals(newCPlus, StringComparison.Ordinal))
        {
            Logger.LogTrace($"CPlus changed active profiles!", LoggerType.VisiblePairs);
            _lastCPlus = newCPlus;
            toSend.CustomizeProfile = newCPlus;
        }

        var newHeels = await _ipc.Heels.GetClientOffset().ConfigureAwait(false);
        if (!_lastHeels.Equals(newHeels, StringComparison.Ordinal))
        {
            Logger.LogTrace($"Heels offset changed!", LoggerType.VisiblePairs);
            _lastHeels = newHeels;
            toSend.HeelsOffset = newHeels;
        }

        var newTitle = await _ipc.Honorific.GetTitle().ConfigureAwait(false);
        if (!_lastHonorific.Equals(newTitle, StringComparison.Ordinal))
        {
            Logger.LogTrace($"Honorific title changed!", LoggerType.VisiblePairs);
            _lastHonorific = newTitle;
            toSend.HonorificTitle = newTitle;
        }

        var newNicks = _ipc.PetNames.GetPetNicknames();
        if (!_lastPetNames.Equals(newNicks, StringComparison.Ordinal))
        {
            Logger.LogTrace($"Pet nicknames changed!", LoggerType.VisiblePairs);
            _lastPetNames = newNicks;
            toSend.PetNicknames = newNicks;
        }

        // push it out.
        Logger.LogDebug("Compiling Full Data to Kinksters");
        await _hub.UserPushIpcData(new(visibleKinksters, toSend)).ConfigureAwait(false);
    }
}
