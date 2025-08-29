using CkCommons;
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
    private readonly List<(KinksterGameObj, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)> VisiblePairObjects = [];

    /// <summary> The shared list of handles players from the GagSpeakPlugin. Format provides the player name and moodles permissions. </summary>
    /// <remarks> String Stored is in format [Player Name@World] </remarks>
    /// </summary>
    private ICallGateProvider<List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>>? HandledVisiblePairs;

    /// <summary>
    /// Obtains an ApplyStatusToPair message from Moodles, and invokes the update to the player if permissions allow it.
    /// <para> THIS WILL NOT WORK IF THE PAIR HAS NOT GIVEN YOU PERMISSION TO APPLY </para>
    /// </summary>
    private ICallGateProvider<string, string, List<MoodlesStatusInfo>, bool, object?>? ApplyStatusesToPairRequest;

    /// <summary>
    /// Create an action event that can send off a MoodleStatusInfo tuple to other plugins and inform them that our information is updated.
    /// </summary>
    private static ICallGateProvider<MoodlesStatusInfo, object?>?       ApplyMoodleStatus;      // ACTION
    private static ICallGateProvider<List<MoodlesStatusInfo>, object?>? ApplyMoodleStatusList;  // ACTION

    private static ICallGateProvider<int>?    ApiVersion; // FUNC 
    private static ICallGateProvider<object>? ListUpdated; // ACTION
    private static ICallGateProvider<object>? Ready; // FUNC
    private static ICallGateProvider<object>? Disposing; // FUNC
    private static ICallGateProvider<string, List<MoodlesStatusInfo>, object?>? StatusesAppliedByPair; // ACTION

    /// <summary>
    ///     The following exists for Glyceri's desired memes.
    /// </summary>
    private static ICallGateProvider<Guid, string, object?>? AddOrUpdateStatusByName;
    private static ICallGateProvider<Guid, string, object?>? ApplyPresetByName;
    private static ICallGateProvider<List<Guid>, string, object?>? RemoveMoodlesByName;
    private static ICallGateProvider<string, string, object?>? SetStatusManagerByName;
    private static ICallGateProvider<string, object?>? ClearStatusManagerByName;


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
            if (VisiblePairObjects.FirstOrDefault(vpo => vpo.Item1.NameWithWorld == msg.Kinkster.PlayerNameWithWorld) is { } match)
            {
                // found a match, so update their permission values within the list.
                var newPerms = _pairManager.GetMoodlePermsForPairByName(msg.Kinkster.PlayerNameWithWorld);
                match.Item2 = newPerms.Item1;
                match.Item3 = newPerms.Item2;
                // inform list change.
                NotifyListChanged();
            }
        });

        Mediator.Subscribe<VisibleKinkstersChanged>(this, (_) => NotifyListChanged());

        Mediator.Subscribe<KinksterGameObjCreatedMessage>(this, (msg) =>
        {
            _logger.LogInformation("Created Moodles provider for [" + msg.KinksterGameObj.NameWithWorld + "]", LoggerType.IpcGagSpeak);
            var moodlePerms = _pairManager.GetMoodlePermsForPairByName(msg.KinksterGameObj.NameWithWorld);
            VisiblePairObjects.Add((msg.KinksterGameObj, moodlePerms.Item1, moodlePerms.Item2));
            NotifyListChanged();
        });
        Mediator.Subscribe<KinksterGameObjDestroyedMessage>(this, (msg) =>
        {
            _logger.LogInformation("Removing PairGameObject for [" + msg.KinksterGameObj.NameWithWorld + "]", LoggerType.IpcGagSpeak);
            VisiblePairObjects.RemoveAll(pair => pair.Item1.NameWithWorld == msg.KinksterGameObj.NameWithWorld);
            NotifyListChanged();
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting IpcProviderService");
        // init API
        ApiVersion = Svc.PluginInterface.GetIpcProvider<int>("GagSpeak.GetApiVersion");
        // init Events
        Ready = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Ready");
        Disposing = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Disposing");
        // init Getters
        HandledVisiblePairs = Svc.PluginInterface.GetIpcProvider<List<(string, MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms)>>("GagSpeak.GetHandledVisiblePairs");
        // init appliers
        ApplyStatusesToPairRequest = Svc.PluginInterface.GetIpcProvider<string, string, List<MoodlesStatusInfo>, bool, object?>("GagSpeak.ApplyStatusesToPairRequest");
        ListUpdated = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.VisiblePairsUpdated");
        ApplyMoodleStatus = Svc.PluginInterface.GetIpcProvider<MoodlesStatusInfo, object?>("GagSpeak.ApplyMoodleStatus");
        ApplyMoodleStatusList = Svc.PluginInterface.GetIpcProvider<List<MoodlesStatusInfo>, object?>("GagSpeak.ApplyMoodleStatusList");
        StatusesAppliedByPair = Svc.PluginInterface.GetIpcProvider<string, List<MoodlesStatusInfo>, object?>("GagSpeak.StatusesAppliedByPair");

        AddOrUpdateStatusByName = Svc.PluginInterface.GetIpcProvider<Guid, string, object?>("GagSpeak.AddOrUpdateMoodleByName");
        ApplyPresetByName = Svc.PluginInterface.GetIpcProvider<Guid, string, object?>("GagSpeak.ApplyPresetByName");
        RemoveMoodlesByName = Svc.PluginInterface.GetIpcProvider<List<Guid>, string, object?>("GagSpeak.RemoveMoodlesByName");
        SetStatusManagerByName = Svc.PluginInterface.GetIpcProvider<string, string, object?>("GagSpeak.SetStatusManagerByName");
        ClearStatusManagerByName = Svc.PluginInterface.GetIpcProvider<string, object?>("GagSpeak.ClearStatusManagerByName");

        // register api
        ApiVersion.RegisterFunc(() => GagspeakApiVersion);
        // register getters
        HandledVisiblePairs.RegisterFunc(GetVisiblePairs);
        // register appliers
        ApplyStatusesToPairRequest.RegisterAction(HandleApplyStatusesToPairRequest);

        _logger.LogInformation("Started IpcProviderService");
        NotifyReady();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping IpcProvider Service");
        NotifyDisposing();

        ApiVersion?.UnregisterFunc();
        Ready?.UnregisterFunc();
        Disposing?.UnregisterFunc();

        HandledVisiblePairs?.UnregisterFunc();
        ApplyStatusesToPairRequest?.UnregisterAction();
        ListUpdated?.UnregisterAction();
        ApplyMoodleStatus?.UnregisterAction();
        ApplyMoodleStatusList?.UnregisterAction();
        StatusesAppliedByPair?.UnregisterAction();

        AddOrUpdateStatusByName?.UnregisterAction();
        ApplyPresetByName?.UnregisterAction();
        RemoveMoodlesByName?.UnregisterAction();
        SetStatusManagerByName?.UnregisterAction();
        ClearStatusManagerByName?.UnregisterAction();

        Mediator.UnsubscribeAll(this);

        return Task.CompletedTask;
    }

    private static void NotifyReady() => Ready?.SendMessage();
    private static void NotifyDisposing() => Disposing?.SendMessage();
    private static void NotifyListChanged() => ListUpdated?.SendMessage();

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
            _logger.LogWarning($"ApplyStatusesToPairRequest for {recipient} recieved, but they were not visible.");
            return;
        }

        // the moodle and permissions are valid.
        var pairUser = _pairManager.DirectPairs.FirstOrDefault(p => p.PlayerNameWithWorld == recipient)!.UserData;
        if (pairUser == null)
        {
            _logger.LogWarning($"ApplyStatusesToPairRequest for {recipient} received, but couldn't find their UID.");
            return;
        }

        // fetch the UID for the pair to apply for.
        _logger.LogInformation($"ApplyStatusesToPairRequest for {recipient} from {requester}, applying statuses");
        var dto = new MoodlesApplierByStatus(pairUser, statuses, (isPreset ? MoodleType.Preset : MoodleType.Status));
        Mediator.Publish(new MoodlesApplyStatusToPair(dto));
    }

    /// <summary>
    ///     Applies a <see cref="MoodlesStatusInfo"/> tuple to the CLIENT ONLY via Moodles. <para />
    ///     This helps account for trying on Moodle Presets, or applying the preset's StatusTuples. <para />
    ///     Method is invoked via GagSpeak's IpcProvider to prevent miss-use of bypassing permissions.
    /// </summary>
    public void ApplyStatusTuple(MoodlesStatusInfo status) => ApplyMoodleStatus?.SendMessage(status);

    /// <summary>
    ///     Applies a group of <see cref="MoodlesStatusInfo"/> tuples to the CLIENT ONLY via Moodles. <para />
    ///     This helps account for trying on Moodle Presets, or applying the preset's StatusTuples. <para />
    ///     Method is invoked via GagSpeak's IpcProvider to prevent miss-use of bypassing permissions.
    /// </summary>
    public void ApplyStatusTuples(IEnumerable<MoodlesStatusInfo> statuses) => ApplyMoodleStatusList?.SendMessage(statuses.ToList());

    // Applied statuses sent by another kinkster, intended to be applied onto us.
    public void ApplyKinkstersStatusesToClient(string kinksterName, IEnumerable<MoodlesStatusInfo> statuses)
        => StatusesAppliedByPair?.SendMessage(kinksterName, statuses.ToList());

    // AddOrUpdateStatusByName
    public void AddOrUpdateStatus(Guid statusGuid, string playerNameWithWorld)
        => AddOrUpdateStatusByName?.SendMessage(statusGuid, playerNameWithWorld);

    // ApplyPresetByName
    public void ApplyPreset(Guid presetGuid, string playerNameWithWorld)
        => ApplyPresetByName?.SendMessage(presetGuid, playerNameWithWorld);

    // RemoveMoodlesByName
    public void RemoveMoodles(List<Guid> statusGuids)
        => RemoveMoodlesByName?.SendMessage(statusGuids, PlayerData.NameWithWorld);

    // SetStatusManagerByName
    public void SetStatusManager(string statusManagerString, string playerNameWithWorld)
        => SetStatusManagerByName?.SendMessage(statusManagerString, playerNameWithWorld);

    // ClearStatusManagerByName
    public void ClearStatusManager(string playerNameWithWorld)
        => ClearStatusManagerByName?.SendMessage(playerNameWithWorld);
}

