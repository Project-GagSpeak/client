using GagSpeak.Kinksters;
using GagSpeak.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Network;
using GagspeakAPI.User;

namespace GagSpeak.PlayerClient;

/// <summary> 
///   Manages all kinkster requests for the client. <para />
///   This includes both incoming and outgoing requests.
/// </summary>
public sealed class RequestsManager : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly OnlineKinksterManager _onlineUsers;
    private readonly KinksterManager _kinksters;

    // Potentially turn to dictionary/concurrent dict if entry format changes later.
    private readonly HashSet<RequestEntry> _allRequests = [];

    // Seperation of request types.
    private List<RequestEntry> _incomingInternal;
    private List<RequestEntry> _outgoingInternal;
    // Distinct Users involved requests as either sender or target.
    private HashSet<string> _involvedInRequests;

    public RequestsManager(ILogger<RequestsManager> logger, GagspeakMediator mediator,
        MainConfig config, OnlineKinksterManager onlineUsers, KinksterManager kinksters)
        : base(logger, mediator)
    {
        _config = config;
        _onlineUsers = onlineUsers;
        _kinksters = kinksters;

        UpdateCache();
        Mediator.Subscribe<DisconnectedMessage>(this, _ =>
        {
            Logger.LogDebug("Clearing all requests on disconnect.", LoggerType.PairManagement);
            _allRequests.Clear();
            UpdateCache();
        });
    }
    public int TotalRequests => _allRequests.Count;
    public IReadOnlyList<RequestEntry> Incoming => _incomingInternal;
    public IReadOnlyList<RequestEntry> Outgoing => _outgoingInternal;

    public bool IsInRequests(string userUid)
        => _involvedInRequests.Contains(userUid);

    public bool IsInRequests(UserData user)
        => _involvedInRequests.Contains(user.UID);

    public void AddNewRequest(KinksterRequest newRequest)
    {
        var entry = new RequestEntry(newRequest);
        if (_allRequests.Contains(entry))
            return;
        // Add it to the requests.
        Logger.LogDebug($"Adding new request entry to manager.", LoggerType.PairManagement);
        _allRequests.Add(entry);
        // If we have it set to play sounds, play them if for us.
        if (!entry.FromClient && _config.Data.AlertKind.HasAny(AlertKind.Audio))
            _config.PlaySound();

        UpdateCache();
    }

    public void AddNewRequests(IEnumerable<KinksterRequest> newRequests)
    {
        // Assume we can add all requests.
        var toAdd = newRequests.Select(r => new RequestEntry(r));
        // Trim out any that already exist.
        var validToAdd = toAdd.Except(_allRequests).ToList();
        if (validToAdd.Count is 0)
            return;
        // Add them to the requests.
        Logger.LogDebug($"Adding {validToAdd.Count} new request entries to manager.", LoggerType.PairManagement);
        _allRequests.UnionWith(validToAdd);
        UpdateCache();
    }

    // From UI Callback.
    public void RemoveRequest(RequestEntry requestEntry)
    {
        if (!_allRequests.Remove(requestEntry))
            return;
        // Removed successfully.
        Logger.LogDebug($"Removed request entry from manager.", LoggerType.PairManagement);
        UpdateCache();
    }

    public void RemoveRequests(IEnumerable<RequestEntry> requestEntries)
    {
        _allRequests.ExceptWith(requestEntries);
        Logger.LogDebug($"Removed {requestEntries.Count()} request entries from manager.", LoggerType.PairManagement);
        UpdateCache();
    }

    // From server callback.
    public void RemoveRequest(KinksterRequest requestEntry)
    {
        var entry = new RequestEntry(requestEntry);
        if (!_allRequests.Remove(entry))
            return;
        // Removed successfully.
        Logger.LogDebug($"Removed request entry from manager.", LoggerType.PairManagement);
        UpdateCache();
    }

    // Still in testing.
    public void AcceptRequest(RequestEntry acceptedRequest, AddedKinksterPair addedPair)
    {
        // Ensure the request is removed.
        _allRequests.Remove(acceptedRequest);
        // Regardless, follow up by adding the pair, and marking them online if true
        _kinksters.AddKinkster(addedPair.Pair);
        if (addedPair.OnlineInfo is { } onlineInfo)
            _kinksters.MarkKinksterOnline(onlineInfo);

        Logger.LogDebug($"Accepted request, adding pair: {addedPair.Pair.User.AliasOrUID}.", LoggerType.PairManagement);
        UpdateCache();
    }

    public void AcceptRequests(List<RequestEntry> relatedRequests, List<AddedKinksterPair> addedPairs)
    {
        // Ensure all removal
        foreach (var request in relatedRequests)
            _allRequests.Remove(request);
        // Add all pairs
        foreach (var addedPair in addedPairs)
        {
            _kinksters.AddKinkster(addedPair.Pair);
            if (addedPair.OnlineInfo is { } onlineInfo)
                _kinksters.MarkKinksterOnline(onlineInfo);
        }

        Logger.LogDebug($"Accepted requests in bulk, adding pairs: {string.Join(", ", addedPairs.Select(ap => ap.Pair.User.AliasOrUID))}", LoggerType.PairManagement);
        UpdateCache();
    }

    #region helpers
    private void UpdateCache()
    {
        // Update internals
        _incomingInternal = [.. _allRequests.Where(r => !r.FromClient).OrderByDescending(r => r.TimeToRespond)];
        _outgoingInternal = [.. _allRequests.Where(r => r.FromClient).OrderByDescending(r => r.TimeToRespond)];
        _involvedInRequests = [.. _allRequests.SelectMany(r => new[] { r.Data.User.UID, r.Data.Target.UID })];
        Logger.LogInformation($"Updated partitioned caches with {_allRequests.Count} total requests. ({Incoming.Count} in, {Outgoing.Count} out)", LoggerType.PairManagement);
        Mediator.Publish(new DDSUpdateRequests());
    }
    #endregion
}
