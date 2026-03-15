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
using GagSpeak.Interop.Helpers;

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
    public List<ServerDataLociStatus> LatestLociResults { get; private set; } = new();
    public List<string> FetchedTags { get; private set; } = new List<string>();
    public List<PublishedPattern> PublishedPatterns { get; private set; } = new();
    public List<PublishedLociData> PublishedLociData { get; private set; } = new();

    // This makes sure that we only automatically fetch the patterns and lociData and tags once automatically.
    // Afterwards, manual updates are requied.
    // TODO: Repurpose this into something like the UISetTask but for sharehub.
    public bool InitialPatternsCall { get; private set; } = false;
    public bool InitialLociCall { get; private set; } = false;
    public void ToggleSortDirection()
    {
        SortOrder = SortOrder is HubDirection.Ascending ? HubDirection.Descending : HubDirection.Ascending;
        // update the results to reflect the new sort order.
        LatestLociResults.Reverse();
        LatestPatternResults.Reverse();
    }

    public void TryLociStatus(Guid lociStatusId)
    {
        var match = LatestLociResults.FirstOrDefault(x => x.Status.GUID == lociStatusId);
        if (match is null) return;

        var statusToTry = match.Status;
        Logger.LogInformation("Trying on lociStatus from server..");
        _loci.ApplyStatusInfo(statusToTry.ToTuple(), false).ConfigureAwait(false);
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
        var dto = new SearchPattern(SearchString, tags.ToArray(), SearchFilter, SortOrder)
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
            SharehubUploadPattern patternDto = new(patternInfo, base64Pattern);
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

    #region LociShareHub Tasks
    public async Task SearchLociStatuses()
    {
        // take the comma seperated search string, split them by commas, convert to lowercase, and trim tailing and leading whitespaces.
        var tags = SearchTags.Split(',')
            .Select(x => x.ToLower().Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => x.Length > 0)
            .ToArray();

        // perform the search operation.
        var res = await _hub.SearchLociData(new(SearchString, tags, SearchFilter, SortOrder));
        var fetchedLociStatuses = res.Value ?? [];
        if (fetchedLociStatuses.Count <= 0)
            LatestLociResults = new List<ServerDataLociStatus>();
        else
        {
            Logger.LogInformation("Retrieved LociData from servers.", LoggerType.ShareHub);
            LatestLociResults = fetchedLociStatuses;
        }

        if(!InitialLociCall)
            InitialLociCall = true;
    }

    public async Task HeartLociStatus(Guid statusId)
    {
        var res = await _hub.LikeLociStatus(statusId);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogWarning($"Failed to like status from servers. Error: {res.ErrorCode}");
            return;
        }

        Logger.LogInformation("Like interaction successful.", LoggerType.ShareHub);
        if (LatestLociResults.FirstOrDefault(x => x.Status.GUID == statusId) is not { } status)
            return;

        status.Likes += status.HasLiked ? -1 : 1;
        status.HasLiked = !status.HasLiked;
    }

    public async Task UploadLociStatus(string authorName, HashSet<string> tags, LociStatusInfo statusInfo)
    {
        try
        {
            Logger.LogTrace("Uploading Loci Status to server.", LoggerType.ShareHub);
            var res = await _hub.UploadLociStatus(new(authorName, tags, statusInfo.ToStruct()));
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                throw new Exception($"Failed to upload status to servers. Error: {res.ErrorCode}");

            // if the upload was successful, then we can notify the user.
            Mediator.Publish(new NotificationMessage("Loci Status Upload", "uploaded successful!", NotificationType.Info));
            PublishedLociData.Add(new(authorName, statusInfo.ToStruct()));
        }
        catch (Bagagwa e)
        {
            Logger.LogError(e, "Failed to upload status to servers.");
            Mediator.Publish(new NotificationMessage("Loci Status Upload", "upload failed!", NotificationType.Warning));
        }
    }

    public async Task DelistLociData(Guid lociDataId)
    {
        if (lociDataId == Guid.Empty || !PublishedLociData.Any(m => m.Status.GUID == lociDataId))
            return;

        try
        {
            var res = await _hub.DelistLociStatus(lociDataId);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                throw new Exception($"Failed to remove status from servers: [{res.ErrorCode}]");

            // if successful, notify the user.
            Logger.LogInformation("DelistLociDataTask completed.", LoggerType.ShareHub);
            PublishedLociData.RemoveAll(m => m.Status.GUID == lociDataId);
        }
        catch (Bagagwa e)
        {
            Logger.LogError(e, "Failed to delist LociData from servers.");
            Mediator.Publish(new NotificationMessage("Delist LociData", "removal failed!", NotificationType.Warning));
        }
    }
    #endregion LociShareHub Tasks

    public void CopyLociStatusToClipboard(Guid statusId)
    {
        if (LatestLociResults.FirstOrDefault(s => s.Status.GUID == statusId) is not { } resultMatch)
            return;

        var status = resultMatch.Status;
        var totalTime = status.ExpireTicks == -1 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(status.ExpireTicks);

        // convert the loci status to a copiable json.
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
        Logger.LogInformation("Copied loci status to clipboard.");
    }
}
