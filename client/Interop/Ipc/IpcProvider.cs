using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Dto.IPC;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Interop.Ipc;

/// <summary>
/// The IPC Provider for GagSpeak to interact with other plugins by sharing information about visible players.
/// </summary>
public class IpcProvider : IHostedService, IMediatorSubscriber
{
    private const int GagspeakApiVersion = 1;

    private readonly ILogger<IpcProvider> _logger;
    private readonly PairManager _pairManager;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly IDalamudPluginInterface _pi;

    public GagspeakMediator Mediator { get; init; }
    private GameObjectHandler? _playerObject = null;

    /// <summary>
    /// Stores the visible game object, and the moodles permissions 
    /// for the pair belonging to that object.
    /// This is not accessible by other plugins.
    /// </summary>
    private readonly List<(GameObjectHandler, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)> VisiblePairObjects = [];

    /// <summary>
    /// Stores the list of handled players by the GagSpeak plugin.
    /// <para> String Stored is in format [Player Name@World] </para>
    /// </summary>
    private ICallGateProvider<List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>>? _handledVisiblePairs;

    /// <summary>
    /// Obtains an ApplyStatusToPair message from Moodles, and invokes the update to the player if permissions allow it.
    /// <para> THIS WILL NOT WORK IF THE PAIR HAS NOT GIVEN YOU PERMISSION TO APPLY </para>
    /// </summary>
    private ICallGateProvider<string, string, List<MoodlesStatusInfo>, bool, object?>? _applyStatusesToPairRequest;

    /// <summary>
    /// An action event to let other plugins know when our list is updated.
    /// This allows them to not need to call upon the list every frame.
    /// </summary>
    private static ICallGateProvider<object>? _listUpdated;

    private static ICallGateProvider<int>? GagSpeakApiVersion;
    private static ICallGateProvider<object>? GagSpeakReady;
    private static ICallGateProvider<object>? GagSpeakDisposing;

    public IpcProvider(ILogger<IpcProvider> logger, GagspeakMediator mediator,
        PairManager pairManager, OnFrameworkService frameworkUtils,
        IDalamudPluginInterface pi)
    {
        _logger = logger;
        _pairManager = pairManager;
        _frameworkUtils = frameworkUtils;
        _pi = pi;
        Mediator = mediator;

        Mediator.Subscribe<MoodlesReady>(this, (_) => NotifyListChanged());

        Mediator.Subscribe<MoodlesPermissionsUpdated>(this, (msg) =>
        {
            // update the visible pair objects with their latest permissions.
            int idxOfPair = VisiblePairObjects.FindIndex(p => p.Item1.NameWithWorld == msg.NameWithWorld);
            if (idxOfPair != -1)
            {
                var newPerms = _pairManager.GetMoodlePermsForPairByName(msg.NameWithWorld);
                // replace the item 2 and 3 of the index where the pair is.
                VisiblePairObjects[idxOfPair] = (VisiblePairObjects[idxOfPair].Item1, newPerms.Item1, newPerms.Item2);

                // notify the update
                NotifyListChanged();
            }
        });

        Mediator.Subscribe<MoodlesUpdateNotifyMessage>(this, (_) => NotifyListChanged());

        Mediator.Subscribe<GameObjectHandlerCreatedMessage>(this, (msg) =>
        {
            _logger.LogInformation("Received GameObjectHandlerCreatedMessage for " + msg.GameObjectHandler.NameWithWorld, LoggerType.IpcGagSpeak);
            if (msg.OwnedObject)
            {
                _playerObject = msg.GameObjectHandler;
                return;
            }
            // obtain the moodles permissions for this pair.
            var moodlePerms = _pairManager.GetMoodlePermsForPairByName(msg.GameObjectHandler.NameWithWorld);
            VisiblePairObjects.Add((msg.GameObjectHandler, moodlePerms.Item1, moodlePerms.Item2));
            // notify that our list is changed
            NotifyListChanged();
        });
        Mediator.Subscribe<GameObjectHandlerDestroyedMessage>(this, (msg) =>
        {
            _logger.LogInformation("Received GameObjectHandlerDestroyedMessage for " + msg.GameObjectHandler.NameWithWorld, LoggerType.IpcGagSpeak);
            if (msg.OwnedObject)
            {
                _playerObject = null;
                return;
            }
            VisiblePairObjects.RemoveAll(pair => pair.Item1.NameWithWorld == msg.GameObjectHandler.NameWithWorld);
            // notify that our list is changed
            NotifyListChanged();
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting IpcProviderService");

        GagSpeakApiVersion = _pi.GetIpcProvider<int>("GagSpeak.GetApiVersion");
        GagSpeakApiVersion.RegisterFunc(() => GagspeakApiVersion);

        GagSpeakReady = _pi.GetIpcProvider<object>("GagSpeak.Ready");
        GagSpeakDisposing = _pi.GetIpcProvider<object>("GagSpeak.Disposing");

        _handledVisiblePairs = _pi.GetIpcProvider<List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>>("GagSpeak.GetHandledVisiblePairs");
        _handledVisiblePairs.RegisterFunc(GetVisiblePairs);

        // Register our action.
        _applyStatusesToPairRequest = _pi.GetIpcProvider<string, string, List<MoodlesStatusInfo>, bool, object?>("GagSpeak.ApplyStatusesToPairRequest");
        _applyStatusesToPairRequest.RegisterAction(HandleApplyStatusesToPairRequest);

        _listUpdated = _pi.GetIpcProvider<object>("GagSpeak.VisiblePairsUpdated");

        _logger.LogInformation("Started IpcProviderService");
        NotifyReady();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping IpcProvider Service");
        NotifyDisposing();

        GagSpeakApiVersion?.UnregisterFunc();
        GagSpeakReady?.UnregisterFunc();
        GagSpeakDisposing?.UnregisterFunc();

        _handledVisiblePairs?.UnregisterFunc();
        _applyStatusesToPairRequest?.UnregisterAction();
        _listUpdated?.UnregisterAction();

        Mediator.UnsubscribeAll(this);

        return Task.CompletedTask;
    }

    private static void NotifyReady() => GagSpeakReady?.SendMessage();
    private static void NotifyDisposing() => GagSpeakDisposing?.SendMessage();
    private static void NotifyListChanged() => _listUpdated?.SendMessage();


    private List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)> GetVisiblePairs()
    {
        var ret = new List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>();


        return VisiblePairObjects.Where(g => g.Item1.NameWithWorld != string.Empty && g.Item1.Address != nint.Zero)
            .Select(g => ((g.Item1.NameWithWorld), (g.Item2), (g.Item3)))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Handles the request from our clients moodles plugin to update another one of our pairs status.
    /// </summary>
    /// <param name="requester">The name of the player requesting the apply (SHOULD ALWAYS BE OUR CLIENT PLAYER) </param>
    /// <param name="recipient">The name of the player to apply the status to. (SHOULD ALWAYS BE A PAIR) </param>
    /// <param name="statuses">The list of statuses to apply to the recipient. </param>
    private void HandleApplyStatusesToPairRequest(string requester, string recipient, List<MoodlesStatusInfo> statuses, bool isPreset)
    {
        // use linQ to iterate through the handled visible game objects to find the object that is an owned object, and compare its NameWithWorld to the recipient.
        if (_playerObject == null)
        {
            _logger.LogWarning("The Client Player Character Object is currently null (changing areas or loading?) So not updating.");
            return;
        }

        // we should throw a warning and return if the requester is not a visible pair.
        var recipientObject = VisiblePairObjects.FirstOrDefault(g => g.Item1.NameWithWorld == recipient);
        if (recipientObject.Item1 == null)
        {
            _logger.LogWarning("Received ApplyStatusesToPairRequest for {recipient} but could not find the recipient", recipient);
            return;
        }

        // the moodle and permissions are valid.
        UserData pairUser = _pairManager.DirectPairs.FirstOrDefault(p => p.PlayerNameWithWorld == recipient)!.UserData;
        if (pairUser == null)
        {
            _logger.LogWarning("Received ApplyStatusesToPairRequest for {recipient} but could not find the UID for the pair", recipient);
            return;
        }

        // fetch the UID for the pair to apply for.
        _logger.LogInformation("Received ApplyStatusesToPairRequest for {recipient} from {requester}, applying statuses", recipient, requester);
        var dto = new ApplyMoodlesByStatusDto(pairUser, statuses, (isPreset ? IpcToggleType.MoodlesPreset : IpcToggleType.MoodlesStatus));
        Mediator.Publish(new MoodlesApplyStatusToPair(dto));
    }
}

