using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.Services;

public class PatternHubService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly PatternManager _patterns;

    private static Task? _sharehubTask;
    private static CancellationTokenSource _sharehubCts = new();

    // Modified only internally
    private List<PublishedPattern> _publications = [];
    private List<SharehubPattern> _searchResults = [];

    private SortDirection _sortDirection = SortDirection.Descending;

    public PatternHubService(ILogger<PatternHubService> logger, GagspeakMediator mediator,
        MainHub hub, PatternManager patterns)
        : base(logger, mediator)
    {
        _hub = hub;
        _patterns = patterns;
        // Upon each connection, we should reset our search and perform an initial search once.
        Mediator.Subscribe<ConnectedDataSyncedMessage>(this, _ => OnConnection(_.Info));
    }
    
    public static bool InUpdate => _sharehubTask is not null && !_sharehubTask.IsCompleted;
    public IReadOnlyList<PublishedPattern> Publications => _publications;   
    public IReadOnlyList<SharehubPattern> SearchResults => _searchResults;

    // The following are all publically modifiable
    public HubSortBy      SortBy         = HubSortBy.DatePosted;
    public DurationLength DurationFilter = DurationLength.Any;
    public ToyBrandName   DeviceFilter   = ToyBrandName.Unknown;
    public ToyMotor       MotorFilter    = ToyMotor.Vibration;
    // How these results are displayed, in ascending, or descending.
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
        _publications = data.PublishedPatterns;
        // Halt any current task
        _sharehubCts = _sharehubCts.SafeCancelRecreate();
        // Run an initial search to the sharehub task.
        _sharehubTask = Task.Run(async () =>
        {
            // Run a blank search
            var searchDto = new SearchPattern(string.Empty, [], SortBy, SortDirection)
            {
                Duration = DurationFilter,
                Toy = DeviceFilter,
                Motor = MotorFilter,
            };

            var res = await _hub.SearchPatterns(searchDto).ConfigureAwait(false);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogError($"Failed to perform initial pattern search on connection. Error: {res.ErrorCode}");
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
            var searchDto = new SearchPattern(search, GetTagArray(tags), SortBy, SortDirection)
            {
                Duration = DurationFilter,
                Toy = DeviceFilter,
                Motor = MotorFilter,
            };
            var res = await _hub.SearchPatterns(searchDto).ConfigureAwait(false);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogError($"Failed to perform pattern search. Error: {res.ErrorCode}");
                return;
            }
            _searchResults = res.Value!;
        }, _sharehubCts.Token);
    }

    public void Upload(Pattern pattern, string authorName, HashSet<string> tags)
    {
        if (InUpdate) return;

        _sharehubTask = Task.Run(async () =>
        {

            try
            {
                var json = JsonConvert.SerializeObject(pattern);
                var compressed = json.Compress(6);
                var base64Pattern = Convert.ToBase64String(compressed);
                // construct the serverPatternInfo for the upload.
                var patternInfo = new SharehubPattern()
                {
                    Identifier = pattern.Identifier,
                    Label = pattern.Label,
                    Description = pattern.Description,
                    Author = authorName,
                    Tags = tags,
                    Length = pattern.Duration,
                    Looping = pattern.ShouldLoop,
                    PrimaryDeviceUsed = pattern.PlaybackData.PrimaryDeviceUsed,
                    SecondaryDeviceUsed = pattern.PlaybackData.SecondaryDeviceUsed,
                    MotorsUsed = pattern.PlaybackData.MotorsUsed,
                };

                Logger.LogDebug("Uploading Pattern to server.", LoggerType.ShareHub);
                var res = await _hub.UploadPattern(new(patternInfo, base64Pattern)).ConfigureAwait(false);
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    throw new Exception($"Failed to upload pattern to servers. Error: {res.ErrorCode}");

                // Publish that the pattern was uploaded.
                Mediator.Publish(new NotificationMessage("Pattern Upload", "uploaded successful!", NotificationType.Info));
                _publications.Add(new PublishedPattern()
                {
                    Identifier = pattern.Identifier,
                    Label = pattern.Label,
                    Description = pattern.Description,
                    Author = authorName,
                    Looping = pattern.ShouldLoop,
                    Length = pattern.Duration,
                    UploadedDate = DateTime.UtcNow
                });

                GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Published);
            }
            catch (Bagagwa e)
            {
                Logger.LogError(e, "Failed to upload pattern to servers.");
                Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
            }
        }, _sharehubCts.Token);
    }

    public void Download(Guid patternId)
    {
        if (InUpdate) return;

        _sharehubTask = Task.Run(async () =>
        {
            var res = await _hub.DownloadPattern(patternId);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogWarning($"Failed to download pattern from servers. Error: {res.ErrorCode}");
                return;
            }
            else if (res.Value.IsNullOrWhitespace())
            {
                Logger.LogWarning("Failed to download pattern from servers. No data returned.");
                return;
            }
            else
            {
                Logger.LogInformation("Downloaded pattern from servers.", LoggerType.ShareHub);
                // add one download count to the pattern.
                var matchedPattern = _searchResults.FirstOrDefault(x => x.Identifier == patternId);
                if (matchedPattern is not null)
                    matchedPattern.Downloads++;

                try
                {

                    // grab the result and deserialize to base64
                    var bytes = Convert.FromBase64String(res.Value);
                    var version = bytes[0];
                    version = bytes.DecompressToString(out var decompressed);

                    // We need to account for version differences by checking for some key data. We can do this by parsing it to JObject.
                    var patternObject = JObject.Parse(decompressed);

                    // check if we should migrate from V0 to V1 (to account for old patterns.)
                    if (patternObject.ContainsKey("Author"))
                    {
                        // Remove unnecessary fields
                        patternObject.Remove("Author");
                        patternObject.Remove("Tags");
                        patternObject.Remove("CreatedByClient");
                        patternObject.Remove("IsPublished");

                        // Serialize the modified JObject back into a JSON string
                        decompressed = patternObject.ToString();
                    }
                    // Deserialize the string back to pattern data
                    var pattern = JsonConvert.DeserializeObject<Pattern>(decompressed) ?? new Pattern();

                    // Set the active pattern
                    _patterns.CreateClone(pattern, pattern.Label);
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Downloaded);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to deserialize downloaded pattern.");
                    Mediator.Publish(new NotificationMessage("Pattern Download", "download failed!", NotificationType.Warning));
                }
            }
        }, _sharehubCts.Token);
    }

    public void ToggleLike(Guid patternId)
    {
        if (InUpdate) return;

        _sharehubTask = Task.Run(async () =>
        {
            var res = await _hub.LikePattern(patternId).ConfigureAwait(false);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogWarning($"Failed to like pattern from servers. Error: {res.ErrorCode}");
                return;
            }

            // otherwise, it worked.
            Logger.LogInformation("Like interaction successful.", LoggerType.ShareHub);
            // update the pattern stuff
            if (_searchResults.FirstOrDefault(x => x.Identifier == patternId) is { } pattern)
            {
                pattern.Likes += pattern.HasLiked ? -1 : 1;
                pattern.HasLiked = !pattern.HasLiked;

                if (pattern.HasLiked)
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Liked);
            }
        }, _sharehubCts.Token);
    }

    public void Unpublish(Guid IdToRemove)
    {
        if (InUpdate) return;

        _sharehubTask = Task.Run(async () =>
        {
            try
            {
                if (_publications.FirstOrDefault(p => p.Identifier == IdToRemove) is not { } publication)
                    return;

                var res = await _hub.DelistPattern(IdToRemove);
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    throw new Exception($"Failed to remove pattern from servers: [{res.ErrorCode}]");

                Logger.LogTrace("Unpublish pattern completed.", LoggerType.ShareHub);
                _publications.Remove(publication);
                Mediator.Publish(new NotificationMessage("Pattern Removal", "removed successful!", NotificationType.Info));
            }
            catch (Bagagwa e)
            {
                Logger.LogError(e, "Failed to upload pattern to servers.");
                Mediator.Publish(new NotificationMessage("Pattern Removal", "removal failed!", NotificationType.Warning));
            }
        }, _sharehubCts.Token);
    }
}
