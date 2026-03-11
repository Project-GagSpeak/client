using Dalamud.Interface.ImGuiNotification;
using CkCommons;
using GagSpeak.Interop;
using GagSpeak.State.Models;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.Sharehub;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Dalamud.Bindings.ImGui;
using GagspeakAPI.Attributes;

namespace GagSpeak.Services;

// Will need to revise this structure soon. Very messy at the moment.
public class ShareHubService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly IpcCallerLoci _loci;
    private readonly PatternManager _patterns;
    public ShareHubService(ILogger<ShareHubService> logger, GagspeakMediator mediator,
        MainHub hub, IpcCallerLoci loci, PatternManager patterns) : base(logger, mediator)
    {
        _hub = hub;
        _loci = loci;
        _patterns = patterns;

        Mediator.Subscribe<ConnectedDataSyncedMessage>(this, msg =>
        {
            PublishedPatterns = msg.Info.PublishedPatterns;
            PublishedLociData = msg.Info.PublishedLociData;
            FetchedTags = msg.Info.HubTags;
        });
    }

    public string SearchString { get; set; } = string.Empty;
    public string SearchTags { get; set; } = string.Empty;
    public HubFilter SearchFilter { get; set; } = HubFilter.Likes;
    public DurationLength SearchDuration { get; set; } = DurationLength.Any;
    public ToyBrandName SearchDevice { get; set; } = ToyBrandName.Unknown;
    public ToyMotor MotorType { get; set; } = ToyMotor.Vibration;
    public HubDirection SortOrder { get; set; } = HubDirection.Descending;
    public List<ServerPatternInfo> LatestPatternResults { get; private set; } = new();
    public List<ServerLociInfo> LatestMoodleResults { get; private set; } = new();
    public List<string> FetchedTags { get; private set; } = new List<string>();
    public List<PublishedPattern> PublishedPatterns { get; private set; } = new();
    public List<PublishedLociData> PublishedLociData { get; private set; } = new();

    // This makes sure that we only automatically fetch the patterns and moodles and tags once automatically.
    // Afterwards, manual updates are requied.
    public bool InitialPatternsCall { get; private set; } = false;
    public bool InitialMoodlesCall { get; private set; } = false;
    public bool HasPatternResults 
        => LatestPatternResults.Count > 0;
    public bool HasMoodleResults 
        => LatestMoodleResults.Count > 0;
    public bool HasTags 
        => FetchedTags.Count > 0;
    public void ToggleSortDirection()
    {
        SortOrder = SortOrder is HubDirection.Ascending ? HubDirection.Descending : HubDirection.Ascending;
        // update the results to reflect the new sort order.
        LatestMoodleResults.Reverse();
        LatestPatternResults.Reverse();
    }

    public void TryOnMoodle(Guid moodleId)
    {
        // apply the moodle to yourself via moodleStatusInfo.
        var match = LatestMoodleResults.FirstOrDefault(x => x.Status.GUID == moodleId);
        if (match is null) return;

        var moodleTupleToTry = match.Status;
        Logger.LogInformation("Trying on moodle from server. Sending request to Moodles!");
        // Cant do this anymore.
        _loci.ApplyLociStatus(moodleTupleToTry, false).ConfigureAwait(false);
    }

    #region PatternHub Tasks
    public async Task SearchPatterns()
    {
        Logger.LogTrace("Performing Pattern Search.", LoggerType.ShareHub);
        // take the comma seperated search string, split them by commas, convert to lowercase, and trim tailing and leading whitespaces.
        var tags = SearchTags.Split(',')
            .Select(x => x.ToLower().Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => x.Length > 0);

        // Firstly, we should compose the Dto for the search operation.
        var dto = new PatternSearch(SearchString, tags.ToArray(), SearchFilter, SortOrder)
        {
            Duration = SearchDuration,
            Toy = SearchDevice,
            Motor = MotorType
        };

        var hubResponse = await _hub.SearchPatterns(dto);
        var result = hubResponse.Value ?? new List<ServerPatternInfo>();

        // if the result contains an empty list, then we failed to retrieve patterns.
        if (result.Count <= 0)
        {
            Logger.LogError("Failed to retrieve patterns from servers.");
            LatestPatternResults.Clear();
        }
        else
        {
            Logger.LogInformation("Retrieved patterns from servers.", LoggerType.ShareHub);
            LatestPatternResults = result;
        }

        // if we have not called the initial patterns call, then we set it to true.
        if (!InitialPatternsCall)
            InitialPatternsCall = true;
    }

    public async Task UploadPattern(Pattern pattern, string authorName, HashSet<string> tags)
    {
        try
        {
            var json = JsonConvert.SerializeObject(pattern);
            var compressed = json.Compress(6);
            var base64Pattern = Convert.ToBase64String(compressed);
            // construct the serverPatternInfo for the upload.
            var patternInfo = new ServerPatternInfo()
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
            Logger.LogTrace("Uploading Pattern to server.", LoggerType.ShareHub);
            // construct the dto for the upload.
            PatternUpload patternDto = new(patternInfo, base64Pattern);
            // perform the api call for the upload.
            var result = await _hub.UploadPattern(patternDto);
            if (result.ErrorCode is not GagSpeakApiEc.Success)
                throw new Exception($"Failed to upload pattern to servers. Error: {result.ErrorCode}");
            
            // Publish that the pattern was uploaded.
            Mediator.Publish(new NotificationMessage("Pattern Upload", "uploaded successful!", NotificationType.Info));
            PublishedPatterns.Add(new PublishedPattern()
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
    }

    public async Task DownloadPattern(Guid patternId)
    {
        HubResponse<string> res = await _hub.DownloadPattern(patternId);
        // if the response is not successful, then we failed to download the pattern.
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogWarning($"Failed to download pattern from servers. Error: {res.ErrorCode}");
            return;
        }
        // if the response value is null or whitespace, then we failed to get the pattern data.
        else if (res.Value.IsNullOrWhitespace())
        {
            Logger.LogWarning("Failed to download pattern from servers. No data returned.");
            return;
        }
        else
        {
            Logger.LogInformation("Downloaded pattern from servers.", LoggerType.ShareHub);
            // add one download count to the pattern.
            var matchedPattern = LatestPatternResults.FirstOrDefault(x => x.Identifier == patternId);
            if(matchedPattern is not null) matchedPattern.Downloads++;
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
    }

    public async Task LikePattern(Guid patternId)
    {
        HubResponse res = await _hub.LikePattern(patternId);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogWarning($"Failed to like pattern from servers. Error: {res.ErrorCode}");
            return;
        }
            
        // otherwise, it worked.
        Logger.LogInformation("Like interaction successful.", LoggerType.ShareHub);
        // update the pattern stuff
        if (LatestPatternResults.FirstOrDefault(x => x.Identifier == patternId) is { } pattern)
        {
            pattern.Likes += pattern.HasLiked ? -1 : 1;
            pattern.HasLiked = !pattern.HasLiked;

            if (pattern.HasLiked)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Liked);
        }
    }

    public async Task RemovePattern(Guid IdToRemove)
    {
        try
        {
            if (IdToRemove == Guid.Empty || !PublishedPatterns.Any(p => p.Identifier == IdToRemove))
                return;

            HubResponse res = await _hub.DelistPattern(IdToRemove);
            if (res.ErrorCode is GagSpeakApiEc.Success)
            {
                Logger.LogTrace("RemovePatternTask completed.", LoggerType.ShareHub);
                // if successful. Notify the success.
                PublishedPatterns.RemoveAll(p => p.Identifier == IdToRemove);
                Mediator.Publish(new NotificationMessage("Pattern Removal", "removed successful!", NotificationType.Info));
            }
            else throw new Exception($"Failed to remove pattern from servers: [{res.ErrorCode}]");
        }
        catch (Bagagwa e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
    }
    #endregion PatternHub Tasks

    #region MoodlesHub Tasks
    public async Task SearchMoodles()
    {
        // take the comma seperated search string, split them by commas, convert to lowercase, and trim tailing and leading whitespaces.
        var tags = SearchTags.Split(',')
            .Select(x => x.ToLower().Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => x.Length > 0)
            .ToArray();

        // perform the search operation.
        var res = await _hub.SearchLociData(new(SearchString, tags, SearchFilter, SortOrder));
        var serverMoodles = res.Value ?? [];
        if (serverMoodles.Count <= 0)
            LatestMoodleResults = new List<ServerLociInfo>();
        else
        {
            Logger.LogInformation("Retrieved Moodle from servers.", LoggerType.ShareHub);
            LatestMoodleResults = serverMoodles;
        }

        if(!InitialMoodlesCall)
            InitialMoodlesCall = true;
    }

    public async Task LikeLociData(Guid moodleId)
    {
        HubResponse res = await _hub.LikeLociData(moodleId);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogWarning($"Failed to like moodle from servers. Error: {res.ErrorCode}");
            return;
        }

        // It did the worky, yippee
        Logger.LogInformation("Like interaction successful.", LoggerType.ShareHub);
            
        // fetch the appropriate Moodle
        if (LatestMoodleResults.FirstOrDefault(x => x.Status.GUID == moodleId) is not { } moodle)
            return;

        moodle.Likes += moodle.HasLiked ? -1 : 1;
        moodle.HasLiked = !moodle.HasLiked;

        if (moodle.HasLiked)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Liked);
    }

    public async Task UploadMoodle(string authorName, HashSet<string> tags, LociStatusInfo moodleInfo)
    {
        try
        {
            Logger.LogTrace("Uploading Moodle to server.", LoggerType.ShareHub);
            HubResponse res = await _hub.UploadLociStatus(new(authorName, tags, moodleInfo));
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                throw new Exception($"Failed to upload moodle to servers. Error: {res.ErrorCode}");

            // if the upload was successful, then we can notify the user.
            Mediator.Publish(new NotificationMessage("Moodle Upload", "uploaded successful!", NotificationType.Info));
            PublishedLociData.Add(new PublishedLociData() { AuthorName = authorName, Status = moodleInfo });
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Published);
        }
        catch (Bagagwa e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
    }

    public async Task DelistLociData(Guid lociDataId)
    {
        if (lociDataId == Guid.Empty || !PublishedLociData.Any(m => m.Status.GUID == lociDataId))
            return;

        try
        {
            HubResponse res = await _hub.DelistLociData(lociDataId);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                throw new Exception($"Failed to remove moodle from servers: [{res.ErrorCode}]");

            // if successful, notify the user.
            Logger.LogInformation("DelistLociDataTask completed.", LoggerType.ShareHub);
            PublishedLociData.RemoveAll(m => m.Status.GUID == lociDataId);
        }
        catch (Bagagwa e)
        {
            Logger.LogError(e, "Failed to delist LociData from servers.");
            Mediator.Publish(new NotificationMessage("Delisting", "removal failed!", NotificationType.Warning));
        }
    }
    #endregion PatternHub Tasks

    public void CopyMoodleToClipboard(Guid moodleId)
    {
        // Grab the moodle status of the moodle id we want to copy.
        if (!LatestMoodleResults.Any(moodleInfo => moodleInfo.Status.GUID == moodleId))
            return;

        // grab it.
        var status = LatestMoodleResults.First(moodleInfo => moodleInfo.Status.GUID == moodleId).Status;

        var totalTime = status.ExpireTicks == -1 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(status.ExpireTicks);

        // convert the moodle status to a copiable json clipboard.
        var jsonObject = new
        {
            IconID          = status.IconID,
            Title           = status.Title,
            Description     = status.Description,
            CustomFXPath    = status.CustomVFXPath,
            
            Type            = (int)status.Type,
            Modifiers       = status.Modifiers,

            Stacks          = status.Stacks,
            StackSteps      = status.StackSteps,
            StackToChain    = status.StackToChain,

            ChainedGUID     = status.ChainedGUID,
            ChainedType     = status.ChainType,
            ChainTrigger    = status.ChainTrigger,

            Applier         = status.Applier,
            Dispeller       = status.Dispeller,

            Days            = totalTime.Days,
            Hours           = totalTime.Hours,
            Minutes         = totalTime.Minutes,
            Seconds         = totalTime.Seconds,

            NoExpire        = status.ExpireTicks == -1,
        };
        var stringToCopy = JsonConvert.SerializeObject(jsonObject, Formatting.None);
        ImGui.SetClipboardText(stringToCopy);
        Logger.LogInformation("Copied moodle to clipboard.");
    }
}
