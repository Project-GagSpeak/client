using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using OtterGui.Tasks;
using System.Windows.Forms;
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

    private int GetDebounceTime() => _pendingTypes.HasAny(DataSyncKind.Glamourer) 
        ? 1000 : _pendingTypes.HasAny(DataSyncKind.Heels | DataSyncKind.CPlus) ? 750 : 500;

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
                    // Only one update type occured.
                    if (FlagEx.IsSingleFlagSet((byte)_pendingTypes))
                        await SyncSingleToKinksters(toUpdate, _pendingTypes).ConfigureAwait(false);
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

    private async Task SyncSingleToKinksters(List<UserData> visibleKinksters, DataSyncKind type)
    {
        string? newData = type switch
        {
            DataSyncKind.Glamourer => await GetNewGlamourData().ConfigureAwait(false),
            DataSyncKind.CPlus => await GetNewCPlusData().ConfigureAwait(false),
            DataSyncKind.Heels => await GetNewHeelsData().ConfigureAwait(false),
            DataSyncKind.Honorific => await GetNewHonorificData().ConfigureAwait(false),
            DataSyncKind.PetNames => GetNewPetNamesData(),
            _ => null
        };
        // do not update if the same data.
        if (newData is null)
            return;      
        Logger.LogDebug($"Syncing {type} to visible kinksters");
        await _hub.UserPushIpcDataSingle(new(visibleKinksters, type, newData)).ConfigureAwait(false);
    }

    private async Task SyncNewDataToKinksters(List<UserData> visibleKinksters)
    {
        // gather all the data at once.
        var result = await Task.WhenAll(GetNewGlamourData(), GetNewCPlusData(), GetNewHeelsData(), GetNewHonorificData()).ConfigureAwait(false);
        var newPetNicks = GetNewPetNamesData();
        // append them if new.
        var toSend = new CharaIpcDataFull()
        {
            GlamourerBase64 = result[0],
            CustomizeProfile = result[1],
            HeelsOffset = result[2],
            HonorificTitle = result[3],
            PetNicknames = newPetNicks
        };
        // if nothing changed, return.
        if (toSend.IsEmpty())
            return;
        // push it out.
        Logger.LogDebug("Compiling Full Data to Kinksters");
        await _hub.UserPushIpcData(new(visibleKinksters, toSend)).ConfigureAwait(false);
    }

    // forced update call.
    public async Task SyncAppearanceToNewKinksters(List<UserData> visibleKinksters)
    {
        // reset the pending types / changes along with any pending sync task.
        _pendingTypes = DataSyncKind.None;
        _syncUpdateCTS = _syncUpdateCTS.SafeCancelRecreate();
        Logger.LogInformation($"Syncing Full Appearance to Kinksters: ({string.Join(",", visibleKinksters.Select(k => k.AliasOrUID))})");
        // gather all the data at once.
        var ipcCalls = await Task.WhenAll(
            _ipc.Glamourer.GetActorString(),
            _ipc.CustomizePlus.GetClientProfile(),
            _ipc.Heels.GetClientOffset(),
            _ipc.Honorific.GetTitle()
        ).ConfigureAwait(false);
        var ipcNicks = GetNewPetNamesData();
        _lastGlamourer = ipcCalls[0] ?? string.Empty;
        _lastCPlus = ipcCalls[1] ?? string.Empty;
        _lastHeels = ipcCalls[2] ?? string.Empty;
        _lastHonorific = ipcCalls[3] ?? string.Empty;
        _lastPetNames = ipcNicks ?? string.Empty;
        var appearance = new CharaIpcDataFull()
        {
            GlamourerBase64 = ipcCalls[0],
            CustomizeProfile = ipcCalls[1],
            HeelsOffset = ipcCalls[2],
            HonorificTitle = ipcCalls[3],
            PetNicknames = ipcNicks,
        };
        // push it out.
        Logger.LogDebug("Pushing full appearnace update to Kinksters");
        await _hub.UserPushIpcData(new(visibleKinksters, appearance)).ConfigureAwait(false);
    }

    // Helpers.
    private async Task<string?> GetNewGlamourData()
    {
        if (!IpcCallerGlamourer.APIAvailable)
            return null;
        var newGlamStr = await _ipc.Glamourer.GetActorString().ConfigureAwait(false);
        if (_lastGlamourer.Equals(newGlamStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New GlamourAPI Actor String: {newGlamStr.ToString()}");
        _lastGlamourer = newGlamStr;
        return newGlamStr;
    }

    private async Task<string?> GetNewCPlusData()
    {
        if (!IpcCallerCustomize.APIAvailable)
            return null;
        var newCPlusStr = await _ipc.CustomizePlus.GetClientProfile().ConfigureAwait(false);
        if (_lastCPlus.Equals(newCPlusStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New CPlus Profile String: {newCPlusStr.ToString()}");
        _lastCPlus = newCPlusStr;
        return newCPlusStr;
    }

    private async Task<string?> GetNewHeelsData()
    {
        if (!IpcCallerHeels.APIAvailable)
            return null;
        var newHeelsStr = await _ipc.Heels.GetClientOffset().ConfigureAwait(false);
        if (_lastHeels.Equals(newHeelsStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New Heels Offset String: {newHeelsStr.ToString()}");
        _lastHeels = newHeelsStr;
        return newHeelsStr;
    }

    private async Task<string?> GetNewHonorificData()
    {
        if (!IpcCallerHonorific.APIAvailable)
            return null;
        var newTitleStr = await _ipc.Honorific.GetTitle().ConfigureAwait(false);
        if (_lastHonorific.Equals(newTitleStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New Honorific Title String: {newTitleStr.ToString()}");
        _lastHonorific = newTitleStr;
        return newTitleStr;
    }

    private string? GetNewPetNamesData()
    {
        if (!IpcCallerPetNames.APIAvailable)
            return null;
        var newNicksStr = _ipc.PetNames.GetPetNicknames();
        if (_lastPetNames.Equals(newNicksStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New PetNames Nicknames String: {newNicksStr.ToString()}");
        _lastPetNames = newNicksStr;
        return newNicksStr;
    }
}
