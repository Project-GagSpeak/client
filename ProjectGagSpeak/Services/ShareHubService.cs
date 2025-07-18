using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using GagSpeak.PlayerClient;
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
using ImGuiNET;
using GagspeakAPI.Attributes;

namespace GagSpeak.Services;

// Will need to revise this structure soon. Very messy at the moment.
public class ShareHubService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly IpcProvider _ipcProvider;
    private readonly PatternManager _patterns;
    public ShareHubService(ILogger<ShareHubService> logger, GagspeakMediator mediator,
        MainHub hub, IpcProvider ipcProvider, PatternManager patterns) : base(logger, mediator)
    {
        _hub = hub;
        _ipcProvider = ipcProvider;
        _patterns = patterns;

        Mediator.Subscribe<PostConnectionDataRecievedMessage>(this, msg =>
        {
            ClientPublishedPatterns = msg.Info.PublishedPatterns;
            ClientPublishedMoodles = msg.Info.PublishedMoodles;
            FetchedTags = msg.Info.HubTags;
        });
    }

    public string SearchString { get; set; } = string.Empty;
    public string SearchTags { get; set; } = string.Empty;
    public HubFilter SearchFilter { get; set; } = HubFilter.DatePosted;
    public DurationLength SearchDuration { get; set; } = DurationLength.Any;
    public ToyBrandName SearchDevice { get; set; } = ToyBrandName.Unknown;
    public ToyMotor MotorType { get; set; } = ToyMotor.Vibration;
    public HubDirection SortOrder { get; set; } = HubDirection.Descending;
    public List<ServerPatternInfo> LatestPatternResults { get; private set; } = new();
    public List<ServerMoodleInfo> LatestMoodleResults { get; private set; } = new();
    public List<string> FetchedTags { get; private set; } = new List<string>();
    public List<PublishedPattern> ClientPublishedPatterns { get; private set; } = new();
    public List<PublishedMoodle> ClientPublishedMoodles { get; private set; } = new();

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
        => SortOrder = SortOrder is HubDirection.Ascending ? HubDirection.Descending : HubDirection.Ascending;

    // Pattern Tasks
    public void DownloadPattern(Guid patternId) 
        => UiService.SetUITask(DownloadPatternTask(patternId));
    public void PerformPatternLikeAction(Guid patternId) 
        => UiService.SetUITask(LikePatternActionTask(patternId));
    public void UploadPattern(Pattern pattern, string authorName, HashSet<string> tags) 
        => UiService.SetUITask(PatternUploadTask(pattern, authorName, tags));
    public void RemovePattern(Guid patternId) 
        => UiService.SetUITask(PatternRemoveTask(patternId));

    // Moodles Tasks
    public void PerformMoodleSearch() 
        => UiService.SetUITask(FetchMoodleTask());
    public void PerformMoodleLikeAction(Guid moodleId) 
        => UiService.SetUITask(LikeMoodleActionTask(moodleId));
    public void UploadMoodle(string authorName, HashSet<string> tags, MoodlesStatusInfo moodleInfo) 
        => UiService.SetUITask(UploadMoodleTask(authorName, tags, moodleInfo));
    public void RemoveMoodle(Guid idToRemove)
        => UiService.SetUITask(RemoveMoodleTask(idToRemove));

    public void TryOnMoodle(Guid moodleId)
    {
        // apply the moodle to yourself via moodleStatusInfo.
        var match = LatestMoodleResults.FirstOrDefault(x => x.MoodleStatus.GUID == moodleId);
        if (match is null) return;

        var moodleTupleToTry = match.MoodleStatus;
        Logger.LogInformation("Trying on moodle from server. Sending request to Moodles!");
        _ipcProvider.TryOnStatus(moodleTupleToTry);
    }

    #region PatternHub Tasks
    public async Task PerformPatternSearch()
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

    private async Task PatternUploadTask(Pattern pattern, string authorName, HashSet<string> tags)
    {
        try
        {
            var json = JsonConvert.SerializeObject(pattern);
            var compressed = json.Compress(6);
            var base64Pattern = Convert.ToBase64String(compressed);

            var devices = pattern.PlaybackData.DeviceData.Select(x => x.Toy).Distinct().ToArray();
            var motors = pattern.PlaybackData.DeviceData.SelectMany(d => d.MotorData).Select(m => m.Motor).Aggregate(ToyMotor.Unknown, (acc, val) => acc | val);

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
                PrimaryDeviceUsed = devices.Length > 0 ? devices[0] : ToyBrandName.Unknown,
                SecondaryDeviceUsed = devices.Length > 1 ? devices[1] : ToyBrandName.Unknown,
                MotorsUsed = motors,
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
            ClientPublishedPatterns.Add(new PublishedPattern()
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
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
    }

    private async Task DownloadPatternTask(Guid patternId)
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

    private async Task LikePatternActionTask(Guid patternId)
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

    private async Task PatternRemoveTask(Guid IdToRemove)
    {
        try
        {
            if (IdToRemove == Guid.Empty || !ClientPublishedPatterns.Any(p => p.Identifier == IdToRemove))
                return;

            HubResponse res = await _hub.RemovePattern(IdToRemove);
            if (res.ErrorCode is GagSpeakApiEc.Success)
            {
                Logger.LogTrace("RemovePatternTask completed.", LoggerType.ShareHub);
                // if successful. Notify the success.
                ClientPublishedPatterns.RemoveAll(p => p.Identifier == IdToRemove);
                Mediator.Publish(new NotificationMessage("Pattern Removal", "removed successful!", NotificationType.Info));
            }
            else throw new Exception($"Failed to remove pattern from servers: [{res.ErrorCode}]");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
    }
    #endregion PatternHub Tasks

    #region MoodlesHub Tasks
    private async Task FetchMoodleTask()
    {
        // take the comma seperated search string, split them by commas, convert to lowercase, and trim tailing and leading whitespaces.
        var tags = SearchTags.Split(',')
            .Select(x => x.ToLower().Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => x.Length > 0)
            .ToArray();

        // perform the search operation.
        var res = await _hub.SearchMoodles(new(SearchString, tags, SearchFilter, SortOrder));
        var serverMoodles = res.Value ?? [];
        if (serverMoodles.Count <= 0)
            LatestMoodleResults = new List<ServerMoodleInfo>();
        else
        {
            Logger.LogInformation("Retrieved Moodle from servers.", LoggerType.ShareHub);
            LatestMoodleResults = serverMoodles;
        }

        if(!InitialMoodlesCall)
            InitialMoodlesCall = true;
    }
    private async Task LikeMoodleActionTask(Guid moodleId)
    {
        HubResponse res = await _hub.LikeMoodle(moodleId);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogWarning($"Failed to like moodle from servers. Error: {res.ErrorCode}");
            return;
        }

        // It did the worky, yippee
        Logger.LogInformation("Like interaction successful.", LoggerType.ShareHub);
            
        // fetch the appropriate Moodle
        if (LatestMoodleResults.FirstOrDefault(x => x.MoodleStatus.GUID == moodleId) is not { } moodle)
            return;

        moodle.Likes += moodle.HasLikedMoodle ? -1 : 1;
        moodle.HasLikedMoodle = !moodle.HasLikedMoodle;

        if (moodle.HasLikedMoodle)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Liked);
    }

    private async Task UploadMoodleTask(string authorName, HashSet<string> tags, MoodlesStatusInfo moodleInfo)
    {
        try
        {
            Logger.LogTrace("Uploading Moodle to server.", LoggerType.ShareHub);
            HubResponse res = await _hub.UploadMoodle(new(authorName, tags, moodleInfo));
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                throw new Exception($"Failed to upload moodle to servers. Error: {res.ErrorCode}");

            // if the upload was successful, then we can notify the user.
            Mediator.Publish(new NotificationMessage("Moodle Upload", "uploaded successful!", NotificationType.Info));
            ClientPublishedMoodles.Add(new PublishedMoodle() { AuthorName = authorName, MoodleStatus = moodleInfo });
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternHubAction, PatternHubInteractionKind.Published);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
    }

    private async Task RemoveMoodleTask(Guid moodleId)
    {
        if (moodleId == Guid.Empty || !ClientPublishedMoodles.Any(m => m.MoodleStatus.GUID == moodleId))
            return;

        try
        {

            HubResponse res = await _hub.RemoveMoodle(moodleId);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                throw new Exception($"Failed to remove moodle from servers: [{res.ErrorCode}]");

            // if successful, notify the user.
            Logger.LogInformation("RemovePatternTask completed.", LoggerType.ShareHub);
            ClientPublishedMoodles.RemoveAll(m => m.MoodleStatus.GUID == moodleId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
    }
    #endregion PatternHub Tasks

    public void CopyMoodleToClipboard(Guid moodleId)
    {
        // Grab the moodle status of the moodle id we want to copy.
        if (!LatestMoodleResults.Any(moodleInfo => moodleInfo.MoodleStatus.GUID == moodleId))
            return;

        // grab it.
        var moodleStatus = LatestMoodleResults.First(moodleInfo => moodleInfo.MoodleStatus.GUID == moodleId).MoodleStatus;

        // convert the moodle status to a copiable json clipboard.
        var jsonObject = new
        {
            moodleStatus.IconID,
            moodleStatus.Title,
            moodleStatus.Description,
            Type = (int)moodleStatus.Type,
            moodleStatus.Applier,
            moodleStatus.Dispelable,
            moodleStatus.Stacks,
            moodleStatus.StatusOnDispell,
            CustomFXPath = moodleStatus.CustomVFXPath,
            moodleStatus.StackOnReapply,
            moodleStatus.StacksIncOnReapply,
            moodleStatus.Days,
            moodleStatus.Hours,
            moodleStatus.Minutes,
            moodleStatus.Seconds,
            moodleStatus.NoExpire,
            moodleStatus.AsPermanent
        };
        var stringToCopy = JsonConvert.SerializeObject(jsonObject, Formatting.None);
        ImGui.SetClipboardText(stringToCopy);
        Logger.LogInformation("Copied moodle to clipboard.");
    }
}
