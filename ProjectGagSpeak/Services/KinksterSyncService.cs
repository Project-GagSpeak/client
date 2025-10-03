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

        //// IPC Data Updaters
        //Mediator.Subscribe<GlamourerChanged>(this, (msg) =>
        //{
        //    if (PlayerData.IsZoning)
        //        return;
        //    Logger.LogInformation("Client GlamourState was updated!");
        //    AddPendingSync(DataSyncKind.Glamourer);
        //});

        //Mediator.Subscribe<CustomizeProfileChange>(this, (msg) =>
        //{
        //    if (PlayerData.IsZoning || msg.Address == IntPtr.Zero)
        //        return;
        //    Logger.LogInformation($"Your C+ profile changed to {msg.Id}!");
        //    AddPendingSync(DataSyncKind.CPlus);
        //});

        //Mediator.Subscribe<HeelsOffsetChanged>(this, (msg) =>
        //{
        //    if (PlayerData.IsZoning)
        //        return;
        //    Logger.LogInformation("Your heels offset changed!");
        //    AddPendingSync(DataSyncKind.Heels);
        //});

        //Mediator.Subscribe<HonorificTitleChanged>(this, (msg) =>
        //{
        //    if (PlayerData.IsZoning) return;
        //    if (LastHonorific.Equals(msg.NewTitle, StringComparison.Ordinal)) return;

        //    Logger.LogInformation("Client HonorificTitle was updated!");
        //    AddPendingSync(DataSyncKind.Honorific);
        //});

        //Mediator.Subscribe<PetNamesDataChanged>(this, (msg) =>
        //{
        //    if (PlayerData.IsZoning) return;
        //    if (LastPetNames.Equals(msg.NicknamesData, StringComparison.Ordinal)) return;

        //    Logger.LogInformation("Client PetNames was updated!");
        //    AddPendingSync(DataSyncKind.PetNames);
        //});

        //Mediator.Subscribe<PenumbraSettingsChanged>(this, (msg) =>
        //{
        //    if (PlayerData.IsZoning) return;
        //    Logger.LogInformation("Client Penumbra ModManips were updated!");
        //    AddPendingSync(DataSyncKind.ModManips);
        //});

        // Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (msg) => ProcessKinksterSync());
    }

    // internal references to the last sent clientString states.
    private DataSyncKind _pendingTypes = DataSyncKind.None;
    // If we could create associated meta manips to link to pcp's it might be helpful but otherwise is bloated space.
    public static string LastModManips { get; private set; } = string.Empty;
    public static string LastGlamourer { get; private set; } = string.Empty;
    public static string LastCPlus { get; private set; } = string.Empty;
    public static string LastHeels { get; private set; } = string.Empty;
    public static string LastHonorific { get; private set; } = string.Empty;
    public static string LastPetNames { get; private set; } = string.Empty;

    private void AddPendingSync(DataSyncKind syncType)
    {
        // Halt the current update task delay and recreate for the new type.
        _pendingTypes |= syncType;
        _syncUpdateCTS = _syncUpdateCTS.SafeCancelRecreate();
    }

    private int GetDebounceTime()
        => _pendingTypes switch
        {
            DataSyncKind.ModManips => 1000,
            DataSyncKind.Glamourer => 1000,
            DataSyncKind.Heels => 750,
            DataSyncKind.CPlus => 750,
            DataSyncKind.Honorific => 250,
            DataSyncKind.PetNames => 150,
            _ => 1500,
        };

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
                // after the delay is finished, process the update, if there is anyone to send it to.
                var toUpdate = _kinksters.GetVisibleUsers();
                if (toUpdate.Count == 0)
                {
                    _pendingTypes = DataSyncKind.None;
                    return;
                }

                Logger.LogInformation($"KinksterSync for changes: {_pendingTypes}");
                try
                {
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
                }
            }, _syncUpdateCTS.Token);
        }
    }

    private async Task SyncSingleToKinksters(List<UserData> toUpdate, DataSyncKind type)
    {
        string? newData = type switch
        {
            // DataSyncKind.ModManips => GetNewModManips(),
            DataSyncKind.Glamourer => await GetNewGlamourData().ConfigureAwait(false),
            DataSyncKind.CPlus => await GetNewCPlusData().ConfigureAwait(false),
            DataSyncKind.Heels => await GetNewHeelsData().ConfigureAwait(false),
            DataSyncKind.Honorific => await GetNewHonorificData().ConfigureAwait(false),
            DataSyncKind.PetNames => GetNewPetNamesData(),
            _ => null
        };
        // do not update if the same data.
        if (newData is null)
        {
            Logger.LogDebug($"Aborting send for {type} (no different).");
            return;
        }
        Logger.LogDebug($"Syncing {type} to {toUpdate.Count} kinksters");
        await _hub.UserPushIpcDataSingle(new(toUpdate, type, newData)).ConfigureAwait(false);
    }

    private async Task SyncNewDataToKinksters(List<UserData> toUpdate)
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
        {
            Logger.LogDebug($"Aborting send for (no different).");
            return;
        }
        // push it out.
        Logger.LogDebug($"Sending Appearance update to {toUpdate.Count} Kinksters");
        await _hub.UserPushIpcData(new(toUpdate, toSend)).ConfigureAwait(false);
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
        // var ipcManips = _ipc.Penumbra.GetClientManipulations();
        var ipcNicks = GetNewPetNamesData();
        // LastModManips = ipcManips;
        LastGlamourer = ipcCalls[0] ?? string.Empty;
        LastCPlus = ipcCalls[1] ?? string.Empty;
        LastHeels = ipcCalls[2] ?? string.Empty;
        LastHonorific = ipcCalls[3] ?? string.Empty;
        LastPetNames = ipcNicks ?? string.Empty;
        var appearance = new CharaIpcDataFull()
        {
            // ModManips = IpcCallerPenumbra.APIAvailable ? ipcManips : null,
            GlamourerBase64 = ipcCalls[0],
            CustomizeProfile = ipcCalls[1],
            HeelsOffset = ipcCalls[2],
            HonorificTitle = ipcCalls[3],
            PetNicknames = IpcCallerPetNames.APIAvailable ? ipcNicks : null
        };
        // push it out.
        Logger.LogDebug("Pushing full appearnace update to Kinksters");
        await _hub.UserPushIpcData(new(visibleKinksters, appearance)).ConfigureAwait(false);
    }

    // Helpers.
    private string? GetNewModManips()
    {
        if (!IpcCallerPenumbra.APIAvailable)
            return null;
        var newManipStr = _ipc.Penumbra.GetClientManipulations();
        if (LastModManips.Equals(newManipStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New Penumbra Manipulations String: {newManipStr.ToString()}");
        LastModManips = newManipStr;
        return newManipStr;
    }
    private async Task<string?> GetNewGlamourData()
    {
        if (!IpcCallerGlamourer.APIAvailable)
            return null;
        var newGlamStr = await _ipc.Glamourer.GetActorString().ConfigureAwait(false);
        if (LastGlamourer.Equals(newGlamStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New GlamourAPI Actor String: {newGlamStr.ToString()}");
        LastGlamourer = newGlamStr;
        return newGlamStr;
    }

    private async Task<string?> GetNewCPlusData()
    {
        if (!IpcCallerCustomize.APIAvailable)
            return null;
        var newCPlusStr = await _ipc.CustomizePlus.GetClientProfile().ConfigureAwait(false);
        if (LastCPlus.Equals(newCPlusStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New CPlus Profile String: {newCPlusStr.ToString()}");
        LastCPlus = newCPlusStr;
        return newCPlusStr;
    }

    private async Task<string?> GetNewHeelsData()
    {
        if (!IpcCallerHeels.APIAvailable)
            return null;
        var newHeelsStr = await _ipc.Heels.GetClientOffset().ConfigureAwait(false);
        if (LastHeels.Equals(newHeelsStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New Heels Offset String: {newHeelsStr.ToString()}");
        LastHeels = newHeelsStr;
        return newHeelsStr;
    }

    private async Task<string?> GetNewHonorificData()
    {
        if (!IpcCallerHonorific.APIAvailable)
            return null;
        var newTitleStr = await _ipc.Honorific.GetTitle().ConfigureAwait(false);
        if (LastHonorific.Equals(newTitleStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New Honorific Title String: {newTitleStr.ToString()}");
        LastHonorific = newTitleStr;
        return newTitleStr;
    }

    private string? GetNewPetNamesData()
    {
        if (!IpcCallerPetNames.APIAvailable)
            return null;
        var newNicksStr = _ipc.PetNames.GetPetNicknames();
        if (LastPetNames.Equals(newNicksStr, StringComparison.Ordinal))
            return null;
        // it was different, so update and return it.
        Logger.LogTrace($"New PetNames Nicknames String: {newNicksStr.ToString()}");
        LastPetNames = newNicksStr;
        return newNicksStr;
    }
}
