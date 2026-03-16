using CkCommons;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.Services;

public class LociHubService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly IpcCallerLoci _loci;

    private static Task? _sharehubTask;
    private static CancellationTokenSource _sharehubCts = new();

    // Modified only internally
    private List<PublishedLociData> _publications = [];
    private List<SharehubLociStatus> _searchResults = [];

    private SortDirection _sortDirection = SortDirection.Descending;

    public LociHubService(ILogger<LociHubService> logger, GagspeakMediator mediator,
        MainHub hub, IpcCallerLoci loci, PatternManager patterns)
        : base(logger, mediator)
    {
        _hub = hub;
        _loci = loci;
        // Upon each connection, we should reset our search and perform an initial search once.
        Mediator.Subscribe<ConnectedDataSyncedMessage>(this, _ => OnConnection(_.Info));
    }
    
    public static bool InUpdate => _sharehubTask is not null && !_sharehubTask.IsCompleted;
    public IReadOnlyList<PublishedLociData> Publications => _publications;
    public IReadOnlyList<SharehubLociStatus> SearchResults => _searchResults;

    // The following are all publically modifiable
    public HubSortBy SortBy = HubSortBy.DatePosted;
    public SortDirection SortDirection
    {
        get => _sortDirection;
        set
        {
            _sortDirection = value;
            _searchResults.Reverse();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _sharehubCts.SafeCancelDispose();
    }

    private async void OnConnection(LobbyAndHubInfoResponse data)
    {
        // Update the publications, then perform an initial search after force canceling any existing one.
        _publications = data.PublishedLociData;
        // Halt any current task
        _sharehubCts = _sharehubCts.SafeCancelRecreate();
        // Run an initial search to the sharehub task.
        _sharehubTask = Task.Run(async () =>
        {
            // Run a blank search
            var res = await _hub.SearchLociData(new(string.Empty, [], SortBy, SortDirection)).ConfigureAwait(false);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogError($"Failed to perform initial loci search on connection. Error: {res.ErrorCode}");
                return;
            }
            // Otherwise update the results
            _searchResults = res.Value!;
        }, _sharehubCts.Token);
    }

    public void ToggleSortDirection()
        => SortDirection = SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;

    private string[] GetTagArray(string tags)
        => tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

    public void Search(string search, string tags)
    {
        if (InUpdate) return;

        _sharehubTask = Task.Run(async () =>
        {
            var res = await _hub.SearchLociData(new(search, GetTagArray(tags), SortBy, SortDirection)).ConfigureAwait(false);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogError($"Failed to perform pattern search. Error: {res.ErrorCode}");
                return;
            }
            _searchResults = res.Value!;
        }, _sharehubCts.Token);
    }

    public void TryLociStatus(Guid statusId)
    {
        if (SearchResults.FirstOrDefault(x => x.Status.GUID == statusId) is not { } match)
            return;
        // Can apply multiple times if stackable!
        _loci.ApplyStatusInfo(match.Status.ToTuple(), false).ConfigureAwait(false);
    }

    public void Upload(LociStatusInfo statusInfo, string authorName, HashSet<string> tags)
    {
        if (InUpdate)
            return;

        if (statusInfo.GUID == Guid.Empty)
            return;

        _sharehubTask = Task.Run(async () =>
        {
            try
            {
                Logger.LogTrace("Uploading Loci Status to server.", LoggerType.ShareHub);
                var res = await _hub.UploadLociStatus(new(authorName, tags, statusInfo.ToStruct()));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    throw new Exception($"Failed to upload status to servers. Error: {res.ErrorCode}");

                // if the upload was successful, then we can notify the user.
                Mediator.Publish(new NotificationMessage("Loci Status Upload", "uploaded successful!", NotificationType.Info));
                _publications.Add(new(authorName, statusInfo.ToStruct()));
            }
            catch (Bagagwa e)
            {
                Logger.LogError(e, "Failed to upload status to servers.");
                Mediator.Publish(new NotificationMessage("Loci Status Upload", "upload failed!", NotificationType.Warning));
            }
        }, _sharehubCts.Token);
    }

    // Could add import maybhe
    public void CopyToClipboard(Guid statusId)
    {
        if (_searchResults.FirstOrDefault(s => s.Status.GUID == statusId) is not { } match)
            return;

        var status = match.Status;
        var totalTime = status.ExpireTicks == -1 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(status.ExpireTicks);

        // convert the loci status to a copiable json.
        var jsonObject = new
        {
            IconID = status.IconID,
            Title = status.Title,
            Description = status.Description,
            CustomFXPath = status.CustomVFXPath,

            Type = (int)status.Type,
            Modifiers = status.Modifiers,

            Stacks = status.Stacks,
            StackSteps = status.StackSteps,
            StackToChain = status.StackToChain,

            ChainedGUID = status.ChainedGUID,
            ChainedType = status.ChainType,
            ChainTrigger = status.ChainTrigger,

            Applier = status.Applier,
            Dispeller = status.Dispeller,

            Days = totalTime.Days,
            Hours = totalTime.Hours,
            Minutes = totalTime.Minutes,
            Seconds = totalTime.Seconds,

            NoExpire = status.ExpireTicks == -1,
        };
        var stringToCopy = JsonConvert.SerializeObject(jsonObject, Formatting.None);
        ImGui.SetClipboardText(stringToCopy);
        Logger.LogInformation("Copied loci status to clipboard.");
    }

    public void ToggleLike(Guid statusId)
    {
        if (InUpdate) return;

        _sharehubTask = Task.Run(async () =>
        {
            var res = await _hub.LikeLociStatus(statusId).ConfigureAwait(false);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogWarning($"Failed to like status from servers. Error: {res.ErrorCode}");
                return;
            }

            Logger.LogInformation("Like interaction successful.", LoggerType.ShareHub);
            if (_searchResults.FirstOrDefault(x => x.Status.GUID == statusId) is not { } status)
                return;

            status.Likes += status.HasLiked ? -1 : 1;
            status.HasLiked = !status.HasLiked;
        }, _sharehubCts.Token);
    }

    public void Unpublish(Guid IdToRemove)
    {
        if (InUpdate) return;

        _sharehubTask = Task.Run(async () =>
        {
            if (_publications.FirstOrDefault(m => m.Status.GUID == IdToRemove) is not { } match)
                return;

            var res = await _hub.DelistLociStatus(IdToRemove).ConfigureAwait(false);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogWarning($"Failed to remove status from servers. Error: {res.ErrorCode}");
                return;
            }

            // if successful, notify the user.
            Logger.LogInformation("Unpublish completed.", LoggerType.ShareHub);
            _publications.Remove(match);
        }, _sharehubCts.Token);
    }
}
