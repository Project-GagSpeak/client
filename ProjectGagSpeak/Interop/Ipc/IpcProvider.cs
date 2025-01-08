using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.UiWardrobe;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Extensions;
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
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientData _clientData;

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

    private List<string>? _cachedGagTypes;

    private ICallGateProvider<List<string>>? _gagTypes;
    private ICallGateProvider<List<string>>? _wornGags;
    private ICallGateProvider<Guid?>? _wornRestraint;
    private ICallGateProvider<List<(string, Guid)>>? _restraintSets;

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
        PairManager pairManager, OnFrameworkService frameworkUtils,
        IDalamudPluginInterface pi, ClientConfigurationManager clientConfigs, ClientData clientData)
    {
        _logger = logger;
        _pairManager = pairManager;
        _frameworkUtils = frameworkUtils;
        _pi = pi;
        Mediator = mediator;
        _clientConfigs = clientConfigs;
        _clientData = clientData;

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
        _gagTypes = _pi.GetIpcProvider<List<string>>("GagSpeak.GetGagTypes");
        _gagTypes.RegisterFunc(GetGagTypes);
        _wornGags = _pi.GetIpcProvider<List<string>>("GagSpeak.GetWornGags");
        _wornGags.RegisterFunc(GetWornGags);
        _wornRestraint = _pi.GetIpcProvider<Guid?>("GagSpeak.GetWornRestraint");
        _wornRestraint.RegisterFunc(GetWornRestraint);
        _restraintSets = _pi.GetIpcProvider<List<(string, Guid)>>("GagSpeak.GetRestraintSets");
        _restraintSets.RegisterFunc(GetRestraintSets);

        // Register our action.
        _applyStatusesToPairRequest = _pi.GetIpcProvider<string, string, List<MoodlesStatusInfo>, bool, object?>("GagSpeak.ApplyStatusesToPairRequest");
        _applyStatusesToPairRequest.RegisterAction(HandleApplyStatusesToPairRequest);

        // This is an action that we send off whenever our pairs update.
        GagSpeakListUpdated = _pi.GetIpcProvider<object>("GagSpeak.VisiblePairsUpdated");
        GagSpeakTryMoodleStatus = _pi.GetIpcProvider<MoodlesStatusInfo, object?>("GagSpeak.TryOnMoodleStatus");

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
        _gagTypes?.UnregisterFunc();
        _wornGags?.UnregisterFunc();
        _wornRestraint?.UnregisterFunc();
        _restraintSets?.UnregisterFunc();
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

    private List<string> GetGagTypes()
    {
        _cachedGagTypes ??= Enum.GetValues<GagType>().Skip(1).Select((gag) => gag.GagName()).ToList(); // Retrieves names from the enum, skips over the "None" enum, and caches it.
        return _cachedGagTypes;
    }

    private List<string> GetWornGags()
    {
        return _clientData.CurrentGagNames;
    }

    private Guid? GetWornRestraint()
    {
        return _clientConfigs.GetActiveSet()?.RestraintId;
    }

    private List<(string, Guid)> GetRestraintSets()
    {
        return _clientConfigs.StoredRestraintSets.Select((restraint) => (restraint.Name, restraint.RestraintId)).ToList();
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

    /// <summary>
    /// Used to call upon Moodles to try on a MoodleStatusInfo to the client player.
    /// Called directly from GagSpeak's provider to prevent missuse of bypassing permissions.
    /// </summary>
    public void TryOnStatus(MoodlesStatusInfo status) => GagSpeakTryMoodleStatus?.SendMessage(status);
}

