using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Factories;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;

namespace GagSpeak.PlayerData.Services;

#pragma warning disable MA0040

public struct CacheData
{
    public GameObjectHandler? GameObj { get; set; }
    public Enum UpdateKind { get; set; }
    public Guid Guid { get; set; }

    public CacheData(GameObjectHandler? handler, Enum updateKind, Guid guid)
    {
        GameObj = handler;
        UpdateKind = updateKind;
        Guid = guid;
    }
}


// Made for the player character.
// Holds the cached information about changes to make to the player.
// Changes are pushed to the visible player manager. Which performs the API calls.
public sealed class CacheCreationService : DisposableMediatorSubscriberBase
{
    private readonly SemaphoreSlim _cacheCreateLock = new(1);
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtil;
    private readonly IpcManager _ipcManager;
    private readonly CancellationTokenSource _cts = new();
    private Task? _cacheCreationTask;
    private bool _isZoning = false;

    // player object cache to create. Item1 == NULL while no changes have occurred.
    private CacheData _cacheToCreate;

    private readonly CharaIPCData _playerIpcData = new(); // handler for our player character's IPC data.
    private readonly GameObjectHandler _playerObject;         // handler for player characters object.

    public CacheCreationService(ILogger<CacheCreationService> logger, GagspeakMediator mediator,
        GameObjectHandlerFactory gameObjectHandlerFactory, ClientMonitorService clientService,
        OnFrameworkService frameworkUtil, IpcManager ipcManager) : base(logger, mediator)
    {
        _clientService = clientService;
        _frameworkUtil = frameworkUtil;
        _ipcManager = ipcManager;

        _playerObject = gameObjectHandlerFactory.Create(() => _clientService.Address, isWatched: true).GetAwaiter().GetResult();


        // called upon whenever a new cache should be added to the cache creation service.
        Mediator.Subscribe<CreateCacheForObjectMessage>(this, async (msg) =>
        {
            Logger.LogDebug("Received CreateCacheForObject for " + msg.ObjectToCreateFor + ", updating", LoggerType.ClientPlayerData);
            _cacheCreateLock.Wait();
            if (IpcCallerMoodles.APIAvailable) await FetchLatestMoodlesDataAsync().ConfigureAwait(false);
            _cacheToCreate = new CacheData(msg.ObjectToCreateFor, IpcUpdateType.UpdateVisible, Guid.Empty);
            _cacheCreateLock.Release();
        });

        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (msg) => _isZoning = true);
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, (msg) => _isZoning = false);

        Mediator.Subscribe<ClearCacheForObjectMessage>(this, (msg) =>
        {
            _ = Task.Run(() =>
            {
                _playerIpcData.MoodlesData = string.Empty;
                _playerIpcData.MoodlesStatuses.Clear();
                _playerIpcData.MoodlesPresets.Clear();
                Logger.LogDebug("Clearing cache for " + msg.ObjectToCreateFor, LoggerType.ClientPlayerData);
                Mediator.Publish(new IpcDataCreatedMessage(_playerIpcData, IpcUpdateType.MoodlesCleared));
                // If we are in a cutscene, we should publish a mediator event to let our appearance handler know we need to redraw.
                // This is a safe workaround from executing things on cutscene start because glamourer takes precedence first.
                // However, this toggle consistently occurs after Glamourer finishes its draws.
                if (_clientService.InCutscene)
                    Mediator.Publish(new ClientPlayerInCutscene());
            });
        });

        Mediator.Subscribe<MoodlesReady>(this, async (_) =>
        {
            // run an api check before this to force update 

            await FetchLatestMoodlesDataAsync().ConfigureAwait(false);
            Logger.LogDebug("Moodles is now ready, fetching latest info and pushing to all visible pairs", LoggerType.IpcMoodles);
            Mediator.Publish(new IpcDataCreatedMessage(_playerIpcData, IpcUpdateType.UpdateVisible));
        });

        Mediator.Subscribe<MoodlesStatusManagerUpdate>(this, (msg) =>
        {
            if (_isZoning) return;
            // Assuming _playerObject is now a single GameObjectHandler instance
            if (_playerObject != null && _playerObject.Address != nint.Zero)
            {
                Logger.LogDebug("Received new MoodlesStatusManagerUpdate", LoggerType.IpcMoodles);
                _ = AddPlayerCacheToCreate(IpcUpdateType.MoodlesStatusManagerChanged, Guid.Empty);
            }
        });

        Mediator.Subscribe<MoodlesStatusModified>(this, (msg) =>
        {
            if (_isZoning) return;
            // Assuming _playerObject is now a single GameObjectHandler instance
            if (_playerObject != null && _playerObject.Address != nint.Zero)
            {
                Logger.LogDebug("Updating visible pairs with latest Moodles Data. [You Changed Settings of a Status!]", LoggerType.IpcMoodles);
                _ = AddPlayerCacheToCreate(IpcUpdateType.MoodlesStatusesUpdated, msg.Guid);
            }
        });

        Mediator.Subscribe<MoodlesPresetModified>(this, (msg) =>
        {
            if (_isZoning) return;
            if (_playerObject != null && _playerObject.Address != nint.Zero)
            {
                Logger.LogDebug("Received a Status Manager change for Moodles. Updating player with latest IPC", LoggerType.IpcMoodles);
                _ = AddPlayerCacheToCreate(IpcUpdateType.MoodlesPresetsUpdated, msg.Guid);
            }
        });

        Mediator.Subscribe<FrameworkUpdateMessage>(this, (msg) => ProcessCacheCreation());
    }

    private async Task FetchLatestMoodlesDataAsync()
    {
        _playerIpcData.MoodlesData = await _ipcManager.Moodles.GetStatusAsync(_playerObject.NameWithWorld).ConfigureAwait(false) ?? string.Empty;
        _playerIpcData.MoodlesDataStatuses = await _ipcManager.Moodles.GetStatusInfoAsync(_playerObject.NameWithWorld).ConfigureAwait(false) ?? new();
        AppearanceManager.LatestClientMoodleStatusList = _playerIpcData.MoodlesDataStatuses; // Sync with latest Data
        _playerIpcData.MoodlesStatuses = await _ipcManager.Moodles.GetMoodlesInfoAsync().ConfigureAwait(false) ?? new();
        _playerIpcData.MoodlesPresets = await _ipcManager.Moodles.GetPresetsInfoAsync().ConfigureAwait(false) ?? new();
        Logger.LogDebug("Latest Data from Moodles Fetched.", LoggerType.IpcMoodles);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _playerObject.Dispose();
        _cts.Dispose();
    }

    private async Task AddPlayerCacheToCreate(IpcUpdateType updateKind, Guid guid = default)
    {
        await _cacheCreateLock.WaitAsync().ConfigureAwait(false);
        _cacheToCreate = new CacheData(_playerObject, updateKind, guid);
        _cacheCreateLock.Release();
    }

    private void ProcessCacheCreation()
    {
        if (_isZoning) return;

        if (_cacheToCreate.GameObj != null && (_cacheCreationTask?.IsCompleted ?? true))
        {
            _cacheCreateLock.Wait();
            var toCreate = _cacheToCreate;
            // set the cache to create to null
            _cacheToCreate.GameObj = null;
            _cacheCreateLock.Release();

            _cacheCreationTask = Task.Run(async () =>
            {
                try
                {
                    await BuildCharacterData(_playerIpcData, toCreate, _cts.Token).ConfigureAwait(false);
                    Mediator.Publish(new IpcDataCreatedMessage(_playerIpcData, (IpcUpdateType)toCreate.UpdateKind));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error during Cache Creation Processing");
                }
                finally
                {
                    Logger.LogDebug("Cache Creation complete", LoggerType.ClientPlayerData);
                }
            }, _cts.Token);
        }
        else if (_cacheToCreate.GameObj != null)
        {
            Logger.LogDebug("Cache Creation stored until previous creation finished", LoggerType.ClientPlayerData);
        }
    }

    /*   Creating and Buildering Character Information from IPC data */
    public async Task BuildCharacterData(CharaIPCData prevData, CacheData playerObjData, CancellationToken token)
    {
        if (playerObjData.GameObj == null || playerObjData.GameObj.Address == nint.Zero) return;

        try
        {
            var pointerIsZero = await CheckForNullDrawObject(playerObjData.GameObj.Address).ConfigureAwait(false);
            if (pointerIsZero)
            {
                Logger.LogTrace("Pointer was zero for object being built", LoggerType.ClientPlayerData);
                return;
            }

            var start = DateTime.UtcNow;
            // Obtain the Status Manager State for the player object.
            switch (playerObjData.UpdateKind)
            {
                case IpcUpdateType.MoodlesStatusesUpdated:
                    {
                        await StatusSettingsUpdate(prevData, playerObjData, playerObjData.Guid).ConfigureAwait(false);
                    }
                    break;
                case IpcUpdateType.MoodlesPresetsUpdated:
                    {
                        await PresetSettingsUpdate(prevData, playerObjData, playerObjData.Guid).ConfigureAwait(false);
                    }
                    break;
                case IpcUpdateType.None:
                case IpcUpdateType.UpdateVisible:
                    {
                        prevData.MoodlesData = await _ipcManager.Moodles.GetStatusAsync(playerObjData.GameObj.NameWithWorld).ConfigureAwait(false) ?? string.Empty;
                        prevData.MoodlesDataStatuses = await _ipcManager.Moodles.GetStatusInfoAsync(playerObjData.GameObj.NameWithWorld).ConfigureAwait(false) ?? new();
                        AppearanceManager.LatestClientMoodleStatusList = prevData.MoodlesDataStatuses; // Sync with latest Data
                        prevData.MoodlesStatuses = await _ipcManager.Moodles.GetMoodlesInfoAsync().ConfigureAwait(false) ?? new();
                        prevData.MoodlesPresets = await _ipcManager.Moodles.GetPresetsInfoAsync().ConfigureAwait(false) ?? new();
                    }
                    break;
                case IpcUpdateType.MoodlesStatusManagerChanged:
                    {
                        await StatusManagerUpdate(prevData, playerObjData).ConfigureAwait(false);
                    }
                    break;
                case IpcUpdateType.MoodlesCleared:
                    Logger.LogTrace("Clearing Moodles Data for " + playerObjData, LoggerType.IpcMoodles);
                    break;
                default: Logger.LogWarning("Unknown Update Kind for {object}", playerObjData); break;
            }
            Logger.LogInformation("IPC Update for player object took " + TimeSpan.FromTicks(DateTime.UtcNow.Ticks - start.Ticks).TotalMilliseconds 
                + "ms", LoggerType.ClientPlayerData);
            //Logger.LogTrace("Data: {data}", prevData.MoodlesData);
            //Logger.LogTrace("StatusManager Statuses: {data}", prevData.MoodlesDataStatuses.Count);
            //Logger.LogTrace("Statuses: {data}", prevData.MoodlesStatuses.Count);
            //Logger.LogTrace("Presets: {data}", prevData.MoodlesPresets.Count);
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("Cancelled creating Character data for "+playerObjData, LoggerType.ClientPlayerData);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Failed to create {object} data", playerObjData);
        }
    }

    private async Task StatusManagerUpdate(CharaIPCData data, CacheData playerObjData)
    {
        data.MoodlesData = await _ipcManager.Moodles.GetStatusAsync(playerObjData.GameObj!.NameWithWorld).ConfigureAwait(false) ?? string.Empty;
        data.MoodlesDataStatuses = await _ipcManager.Moodles.GetStatusInfoAsync(playerObjData.GameObj.NameWithWorld).ConfigureAwait(false) ?? new();
        AppearanceManager.LatestClientMoodleStatusList = data.MoodlesDataStatuses; // Sync with latest Data
    }

    private async Task StatusSettingsUpdate(CharaIPCData data, CacheData playerObjData, Guid guid)
    {
        // Find the index of the tuple containing the GUID.
        var index = data.MoodlesStatuses.FindIndex(x => x.GUID == guid);
        if (index != -1)
        {
            data.MoodlesStatuses[index] = await _ipcManager.Moodles.GetMoodleInfoAsync(guid).ConfigureAwait(false) ?? new();
        }
        else
        {
            data.MoodlesStatuses = await _ipcManager.Moodles.GetMoodlesInfoAsync().ConfigureAwait(false) ?? new();
        }
    }

    private async Task PresetSettingsUpdate(CharaIPCData data, CacheData playerObjData, Guid guid)
    {
        // Find the index containing the GUID.
        var index = data.MoodlesPresets.FindIndex(x => x.Item1 == guid);
        if (index != -1)
        {
            data.MoodlesPresets[index] = await _ipcManager.Moodles.GetPresetInfoAsync(guid).ConfigureAwait(false) ?? new();
        }
        else
        {
            data.MoodlesPresets = await _ipcManager.Moodles.GetPresetsInfoAsync().ConfigureAwait(false) ?? new();
        }
    }

    private async Task<bool> CheckForNullDrawObject(nint playerPointer)
        => await _frameworkUtil.RunOnFrameworkThread(() => CheckForNullDrawObjectUnsafe(playerPointer)).ConfigureAwait(false);

    private unsafe bool CheckForNullDrawObjectUnsafe(nint playerPointer)
        => ((Character*)playerPointer)->GameObject.DrawObject == null;
}
#pragma warning restore MA0040
