using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;

namespace GagSpeak.PlayerClient;

/// <summary> 
///     Holds all personal information about the client's Kinkster information. <para />
///     This includes GlobalPerms, HardcoreState, and Pair Requests. <para />
///     GlobalPerms and HardcoreState can be accessed statically, as this is singleton, 
///     and makes readonly access less of a hassle considering how frequently they are accessed.
/// </summary>
public sealed class ClientData
{
    private readonly ILogger<ClientData> _logger;
    private readonly GagspeakMediator _mediator;
    public ClientData(ILogger<ClientData> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;

        Svc.ClientState.Logout += OnLogout;
    }

    private static GlobalPerms? _clientGlobals;
    private static HardcoreState? _clientHardcore;
    private HashSet<KinksterRequest> _pairingRequests = new();
    private HashSet<CollarRequest> _collarRequests = new();

    public static IReadOnlyGlobalPerms? Globals => _clientGlobals;
    public static IReadOnlyHardcoreState? Hardcore => _clientHardcore;
    public bool HasKinksterRequests => _pairingRequests.Count > 0;
    public bool HasCollarRequests => _collarRequests.Count > 0;
    public IEnumerable<KinksterRequest> OutgoingKinksterRequests => _pairingRequests.Where(x => x.User.UID == MainHub.UID);
    public IEnumerable<KinksterRequest> IncomingKinksterRequests => _pairingRequests.Where(x => x.Target.UID == MainHub.UID);
    public IEnumerable<CollarRequest> CollarRequestsOutgoing => _collarRequests.Where(x => x.User.UID == MainHub.UID);
    public IEnumerable<CollarRequest> CollarRequestsIncoming => _collarRequests.Where(x => x.Target.UID == MainHub.UID);

    public void Dispose()
    {
        Svc.ClientState.Logout -= OnLogout;
    }

    private void OnLogout(int type, int code)
    {
        _logger.LogInformation("Clearing Global Permissions on Logout.");
        _clientGlobals = null;
        _clientHardcore = null;
        _pairingRequests.Clear();
        _collarRequests.Clear();
    }

    public void InitClientData(ConnectionResponse connectionDto)
    {
        var prevGlobals = _clientGlobals;
        var prevHardcore = _clientHardcore;
        _clientGlobals = connectionDto.GlobalPerms;
        _clientHardcore = connectionDto.HardcoreState;
    }

    public void InitRequests(List<KinksterRequest> kinksterRequests, List<CollarRequest> collarRequests)
    {
        _pairingRequests = kinksterRequests.ToHashSet();
        _collarRequests = collarRequests.ToHashSet();
        _logger.LogInformation("Initialized Kinkster and Collar Requests.");
        _mediator.Publish(new RefreshUiMessage());
    }

    /// <summary>
    ///     Should not call this on initialization. Call <see cref="InitClientData"/> instead.
    /// </summary>
    public void ApplyBulkGlobals(GlobalPerms newGlobals)
    {
        var prevGlobals = _clientGlobals;
        _clientGlobals = newGlobals;
        _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.BulkUpdate, "Global Permissions Updated in Bulk")));
    }

    //public void UpdatePermissionValue(UserData enactor, string permName, object newValue)
    //{
    //    if (string.Equals(enactor.UID, MainHub.UID))
    //        PerformPermissionChange(enactor, permName, newValue);
    //    else if (_kinksters.TryGetKinkster(enactor, out var kinkster))
    //        PerformPermissionChange(enactor, permName, newValue, kinkster);
    //    else
    //        throw new Exception($"Change not from self, and [{enactor.AliasOrUID}] is not a Kinkster Pair. Invalid change for [{permName}]!");
    //}

    public void PerformPermissionChange(UserData enactor, string permName, object newValue, Kinkster? pair = null)
    {
        // Attempt to set the property. if this fails, which it never should if validated previously, throw an exception.
        if (!PropertyChanger.TrySetProperty(_clientGlobals, permName, newValue, out var _))
            throw new InvalidOperationException($"Failed to set property [{permName}] to [{newValue}] on Global Permissions.");
        // Then perform the log.
        _mediator.Publish(new EventMessage(new(pair?.GetNickAliasOrUid() ?? "Self-Update", enactor.UID, InteractionType.ForcedPermChange, $"[{permName}] changed to [{newValue}]")));
    }

    public void AddPairRequest(KinksterRequest dto)
    {
        _pairingRequests.Add(dto);
        _logger.LogInformation("New pair request added!", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiMessage());
    }

    public void RemovePairRequest(KinksterRequest dto)
    {
        var res = _pairingRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.Target.UID == dto.Target.UID);
        _logger.LogInformation("Removed " + res + " pair requests.", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiMessage());
    }
}
