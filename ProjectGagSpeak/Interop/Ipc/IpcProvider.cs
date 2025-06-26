using Dalamud.Plugin.Ipc;
using GagSpeak.Kinksters;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Attributes;
using GagspeakAPI.Network;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Interop;

/// <summary>
/// The IPC Provider for GagSpeak to interact with other plugins by sharing information about visible players.
/// </summary>
public class IpcProvider : IHostedService, IMediatorSubscriber
{
    private const int GagspeakApiVersion = 1;

    private readonly ILogger<IpcProvider> _logger;
    private readonly KinksterManager _pairManager;

    public GagspeakMediator Mediator { get; init; }

    /// <summary> The list of visible game objects (of our pairs) and their associated moodles permissions. </summary>
    /// <remarks> This is not accessible by other plugins. </remarks>
    private readonly List<(GameObjectHandler, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)> VisiblePairObjects = [];

    /// <summary> The shared list of handles players from the GagSpeakPlugin. Format provides the player name and moodles permissions. </summary>
    /// <remarks> String Stored is in format [Player Name@World] </remarks>
    /// </summary>
    private ICallGateProvider<List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>>? _handledVisiblePairs;

    /// <summary>
    /// Obtains an ApplyStatusToPair message from Moodles, and invokes the update to the player if permissions allow it.
    /// <para> THIS WILL NOT WORK IF THE PAIR HAS NOT GIVEN YOU PERMISSION TO APPLY </para>
    /// </summary>
    private ICallGateProvider<string, string, List<MoodlesStatusInfo>, bool, object?>? _applyStatusesToPairRequest;

    /// <summary>
    /// Create an action event that can send off a MoodleStatusInfo tuple to other plugins and inform them that our information is updated.
    /// </summary>
    private static ICallGateProvider<MoodlesStatusInfo, object?>? GagSpeakTryMoodleStatus; // ACTION

    private static ICallGateProvider<int>? GagSpeakApiVersion; // FUNC 
    private static ICallGateProvider<object>? GagSpeakListUpdated; // ACTION
    private static ICallGateProvider<object>? GagSpeakReady; // FUNC
    private static ICallGateProvider<object>? GagSpeakDisposing; // FUNC

    public IpcProvider(ILogger<IpcProvider> logger, GagspeakMediator mediator,
        KinksterManager pairManager, OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _pairManager = pairManager;
        Mediator = mediator;

        Mediator.Subscribe<MoodlesReady>(this, (_) => NotifyListChanged());

        Mediator.Subscribe<MoodlesPermissionsUpdated>(this, (msg) =>
        {
            // update the visible pair objects with their latest permissions.
            var idxOfPair = VisiblePairObjects.FindIndex(p => p.Item1.NameWithWorld == msg.NameWithWorld);
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
            _logger.LogInformation("Created Moodles provider for [" + msg.GameObjectHandler.NameWithWorld + "]", LoggerType.IpcGagSpeak);
            var moodlePerms = _pairManager.GetMoodlePermsForPairByName(msg.GameObjectHandler.NameWithWorld);
            VisiblePairObjects.Add((msg.GameObjectHandler, moodlePerms.Item1, moodlePerms.Item2));
            NotifyListChanged();
        });
        Mediator.Subscribe<GameObjectHandlerDestroyedMessage>(this, (msg) =>
        {
            _logger.LogInformation("Removing PairGameObject for [" + msg.GameObjectHandler.NameWithWorld + "]", LoggerType.IpcGagSpeak);
            VisiblePairObjects.RemoveAll(pair => pair.Item1.NameWithWorld == msg.GameObjectHandler.NameWithWorld);
            NotifyListChanged();
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting IpcProviderService");

        GagSpeakApiVersion = Svc.PluginInterface.GetIpcProvider<int>("GagSpeak.GetApiVersion");
        GagSpeakApiVersion.RegisterFunc(() => GagspeakApiVersion);

        GagSpeakReady = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Ready");
        GagSpeakDisposing = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Disposing");

        _handledVisiblePairs = Svc.PluginInterface.GetIpcProvider<List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>>("GagSpeak.GetHandledVisiblePairs");
        _handledVisiblePairs.RegisterFunc(GetVisiblePairs);

        // Register our action.
        _applyStatusesToPairRequest = Svc.PluginInterface.GetIpcProvider<string, string, List<MoodlesStatusInfo>, bool, object?>("GagSpeak.ApplyStatusesToPairRequest");
        _applyStatusesToPairRequest.RegisterAction(HandleApplyStatusesToPairRequest);

        // This is an action that we send off whenever our pairs update.
        GagSpeakListUpdated = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.VisiblePairsUpdated");
        GagSpeakTryMoodleStatus = Svc.PluginInterface.GetIpcProvider<MoodlesStatusInfo, object?>("GagSpeak.TryOnMoodleStatus");

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
        GagSpeakListUpdated?.UnregisterAction();
        GagSpeakTryMoodleStatus?.UnregisterAction();

        Mediator.UnsubscribeAll(this);

        return Task.CompletedTask;
    }

    private static void NotifyReady() => GagSpeakReady?.SendMessage();
    private static void NotifyDisposing() => GagSpeakDisposing?.SendMessage();
    private static void NotifyListChanged() => GagSpeakListUpdated?.SendMessage();

    private List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)> GetVisiblePairs()
    {
        var ret = new List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>();

        return VisiblePairObjects.Where(g => g.Item1.NameWithWorld != string.Empty && g.Item1.Address != nint.Zero)
            .Select(g => ((g.Item1.NameWithWorld), (g.Item2), (g.Item3)))
            .Distinct()
            .ToList();
    }

    /// <summary> Handles the request from our clients moodles plugin to update another one of our pairs status. </summary>
    /// <param name="requester">The name of the player requesting the apply (SHOULD ALWAYS BE OUR CLIENT PLAYER) </param>
    /// <param name="recipient">The name of the player to apply the status to. (SHOULD ALWAYS BE A PAIR) </param>
    /// <param name="statuses">The list of statuses to apply to the recipient. </param>
    private void HandleApplyStatusesToPairRequest(string requester, string recipient, List<MoodlesStatusInfo> statuses, bool isPreset)
    {
        // we should throw a warning and return if the requester is not a visible pair.
        var recipientObject = VisiblePairObjects.FirstOrDefault(g => g.Item1.NameWithWorld == recipient);
        if (recipientObject.Item1 == null)
        {
            _logger.LogWarning("Received ApplyStatusesToPairRequest for {recipient} but could not find the recipient", recipient);
            return;
        }

        // the moodle and permissions are valid.
        var pairUser = _pairManager.DirectPairs.FirstOrDefault(p => p.PlayerNameWithWorld == recipient)!.UserData;
        if (pairUser == null)
        {
            _logger.LogWarning("Received ApplyStatusesToPairRequest for {recipient} but could not find the UID for the pair", recipient);
            return;
        }

        // fetch the UID for the pair to apply for.
        _logger.LogInformation($"Received ApplyStatusesToPairRequest for {recipient} from {requester}, applying statuses");
        var dto = new MoodlesApplierByStatus(pairUser, statuses, (isPreset ? MoodleType.Preset : MoodleType.Status));
        Mediator.Publish(new MoodlesApplyStatusToPair(dto));
    }

    /// <summary>
    /// Used to call upon Moodles to try on a MoodleStatusInfo to the client player.
    /// Called directly from GagSpeak's provider to prevent missuse of bypassing permissions.
    /// </summary>
    public void TryOnStatus(MoodlesStatusInfo status) => GagSpeakTryMoodleStatus?.SendMessage(status);
}

