using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.Patterns;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using Lumina.Text.ReadOnly;

namespace GagSpeak.Services;

// handles the global chat and pattern discovery social features.
public class ShareHubService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly IpcCallerMoodles _moodles;
    private readonly IpcProvider _ipcProvider;

    private Task? CurrentShareHubTask = null;
    public ShareHubService(ILogger<ShareHubService> logger, GagspeakMediator mediator,
        MainHub apiHubMain, ClientConfigurationManager clientConfigs,
        IpcCallerMoodles moodles, IpcProvider ipcProvider) : base(logger, mediator)
    {
        _apiHubMain = apiHubMain;
        _clientConfigs = clientConfigs;
        _moodles = moodles;
        _ipcProvider = ipcProvider;

        Mediator.Subscribe<MainHubConnectedMessage>(this, _ =>
        {
            if (MainHub.ConnectionDto is null) return;
            ClientPublishedPatterns = MainHub.ConnectionDto.PublishedPatterns;
            ClientPublishedMoodles = MainHub.ConnectionDto.PublishedMoodles;

            // grab the tags.
            FetchLatestTags().ConfigureAwait(false);
        });
    }

    public string SearchString { get; set; } = string.Empty;
    public string SearchTags { get; set; } = string.Empty;
    public ResultFilter SearchFilter { get; set; } = ResultFilter.DatePosted;
    public DurationLength SearchDuration { get; set; } = DurationLength.Any;
    public SupportedTypes SearchType { get; set; } = SupportedTypes.Vibration;
    public SearchSort SearchSort { get; set; } = SearchSort.Descending;
    public List<ServerPatternInfo> LatestPatternResults { get; private set; } = new List<ServerPatternInfo>();
    public List<ServerMoodleInfo> LatestMoodleResults { get; private set; } = new List<ServerMoodleInfo>();
    public HashSet<string> FetchedTags { get; private set; } = new HashSet<string>();
    public List<PublishedPattern> ClientPublishedPatterns { get; private set; } = new List<PublishedPattern>();
    public List<PublishedMoodle> ClientPublishedMoodles { get; private set; } = new List<PublishedMoodle>();

    // This makes sure that we only automatically fetch the patterns and moodles and tags once automatically.
    // Afterwards, manual updates are requied.
    public bool InitialPatternsCall { get; private set; } = false;
    public bool InitialMoodlesCall { get; private set; } = false;
    public bool HasPatternResults => LatestPatternResults.Count > 0;
    public bool HasMoodleResults => LatestMoodleResults.Count > 0;
    public bool HasTags => FetchedTags.Count > 0;
    public bool CanShareHubTask => MainHub.IsConnected && (CurrentShareHubTask is null || CurrentShareHubTask.IsCompleted);

    public void ToggleSortDirection() => SearchSort = SearchSort == SearchSort.Ascending ? SearchSort.Descending : SearchSort.Ascending;

    // Pattern Tasks
    public void PerformPatternSearch() { if (CanShareHubTask) CurrentShareHubTask = FetchPatternsTask(); }
    public void DownloadPattern(Guid patternId) { if (CanShareHubTask) CurrentShareHubTask = DownloadPatternTask(patternId); }
    public void PerformPatternLikeAction(Guid patternId) { if (CanShareHubTask) CurrentShareHubTask = LikePatternActionTask(patternId); }
    public void UploadPattern(PatternData pattern, string authorName, HashSet<string> tags)
    { 
        if (CanShareHubTask) CurrentShareHubTask = PatternUploadTask(pattern, authorName, tags); 
    }
    public void RemovePattern(Guid patternId) { if (CanShareHubTask) CurrentShareHubTask = PatternRemoveTask(patternId); }

    // Moodles Tasks
    public void PerformMoodleSearch() { if (CanShareHubTask) CurrentShareHubTask = FetchMoodleTask(); }
    public void PerformMoodleLikeAction(Guid moodleId) { if (CanShareHubTask) CurrentShareHubTask = LikeMoodleActionTask(moodleId); }
    public void UploadMoodle(string authorName, HashSet<string> tags, MoodlesStatusInfo moodleInfo) { if (CanShareHubTask) CurrentShareHubTask = UploadMoodleTask(authorName, tags, moodleInfo); }
    public void RemoveMoodle(Guid idToRemove) { if (CanShareHubTask) CurrentShareHubTask = RemoveMoodleTask(idToRemove); }
    public void TryOnMoodle(Guid moodleId)
    {
        // apply the moodle to yourself via moodleStatusInfo.
        var match = LatestMoodleResults.FirstOrDefault(x => x.MoodleStatus.GUID == moodleId);
        if (match is null) return;

        var moodleTupleToTry = match.MoodleStatus;
        Logger.LogInformation("Trying on moodle from server. Sending request to Moodles!");
        _ipcProvider.TryOnStatus(moodleTupleToTry);
    }

    public void CopyMoodleToClipboard(Guid moodleId)
    {
        // Grab the moodle status of the moodle id we want to copy.
        if(!LatestMoodleResults.Any(moodleInfo => moodleInfo.MoodleStatus.GUID == moodleId)) 
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

    private async Task FetchLatestTags()
    {
        try
        {
            var latestTags = await _apiHubMain.FetchSearchTags();
            FetchedTags = latestTags.OrderBy(x => x).ToHashSet();
            Logger.LogInformation("Retrieved tags from servers.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to retrieve tags from servers.");
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }

    #region PatternHub Tasks
    private async Task FetchPatternsTask()
    {
        try
        {
            // take the comma seperated search string, split them by commas, convert to lowercase, and trim tailing and leading whitespaces.
            var tags = SearchTags.Split(',')
                .Select(x => x.ToLower().Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet();

            // Firstly, we should compose the Dto for the search operation.
            PatternSearchDto dto = new(SearchString, tags, SearchFilter, SearchDuration, SearchType, SearchSort);

            // perform the search operation.
            var result = await _apiHubMain.SearchPatterns(dto);

            // if the result contains an empty list, then we failed to retrieve patterns.
            if (result.Count == 0)
            {
                Logger.LogError("Failed to retrieve patterns from servers.");
                // clean up the search results
                LatestPatternResults.Clear();
            }
            else
            {
                Logger.LogInformation("Retrieved patterns from servers.", LoggerType.PatternHub);
                LatestPatternResults = result;
            }
            if(!InitialPatternsCall) InitialPatternsCall = true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to retrieve patterns from servers.");
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }

    private async Task PatternUploadTask(PatternData pattern, string authorName, HashSet<string> tags)
    {
        try
        {
            string json = JsonConvert.SerializeObject(pattern);
            var compressed = json.Compress(6);
            string base64Pattern = Convert.ToBase64String(compressed);
            // construct the serverPatternInfo for the upload.
            ServerPatternInfo patternInfo = new ServerPatternInfo()
            {
                Identifier = pattern.UniqueIdentifier,
                Name = pattern.Name,
                Description = pattern.Description,
                Author = authorName,
                Tags = tags,
                Length = pattern.Duration,
                Looping = pattern.ShouldLoop,
                UsesVibrations = true,
                UsesRotations = false,
            };
            Logger.LogTrace("Uploading Pattern to server.", LoggerType.PatternHub);
            // construct the dto for the upload.
            PatternUploadDto patternDto = new(MainHub.PlayerUserData, patternInfo, base64Pattern);
            // perform the api call for the upload.
            var result = await _apiHubMain.UploadPattern(patternDto);
            if (result)
            {
                Mediator.Publish(new NotificationMessage("Pattern Upload", "uploaded successful!", NotificationType.Info));
                ClientPublishedPatterns.Add(new PublishedPattern()
                {
                    Identifier = pattern.UniqueIdentifier,
                    Name = pattern.Name,
                    Description = pattern.Description,
                    Author = authorName,
                    Looping = pattern.ShouldLoop,
                    Length = pattern.Duration,
                    UploadedDate = DateTime.UtcNow
                });
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Published, Guid.Empty, false);
            }
            else
            {
                Logger.LogError("Failed to upload pattern to servers.");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }

    private async Task DownloadPatternTask(Guid patternId)
    {
        try
        {
            var downloadedData = await _apiHubMain.DownloadPattern(patternId);
            if (downloadedData.IsNullOrWhitespace())
            {
                Logger.LogError("Failed to download pattern from servers.");
            }
            else
            {
                Logger.LogInformation("Downloaded pattern from servers.", LoggerType.PatternHub);
                // add one download count to the pattern.
                var matchedPattern = LatestPatternResults.FirstOrDefault(x => x.Identifier == patternId);
                if(matchedPattern is not null) matchedPattern.Downloads++;
                // grab the result and deserialize to base64
                var bytes = Convert.FromBase64String(downloadedData);
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
                PatternData pattern = JsonConvert.DeserializeObject<PatternData>(decompressed) ?? new PatternData();

                // Set the active pattern
                _clientConfigs.AddNewPattern(pattern);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Downloaded, pattern.UniqueIdentifier, false);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to download pattern from servers.");
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }

    private async Task LikePatternActionTask(Guid patternId)
    {
        try
        {
            var result = await _apiHubMain.LikePattern(patternId);
            if (result)
            {
                Logger.LogInformation("Like interaction successful.", LoggerType.PatternHub);
                // update the pattern stuff
                var pattern = LatestPatternResults.FirstOrDefault(x => x.Identifier == patternId);
                if (pattern is not null)
                {
                    pattern.Likes += pattern.HasLiked ? -1 : 1;
                    pattern.HasLiked = !pattern.HasLiked;

                    if (pattern.HasLiked)
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Liked, pattern.Identifier, false);
                }
            }
            else
            {
                Logger.LogError("Like interaction failed.");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to interact with pattern.");
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }

    private async Task PatternRemoveTask(Guid IdToRemove)
    {
        try
        {
            if (IdToRemove == Guid.Empty || !ClientPublishedPatterns.Any(p => p.Identifier == IdToRemove))
                return;

            var result = await _apiHubMain.RemovePattern(IdToRemove);
            if (result)
            {
                Logger.LogTrace("RemovePatternTask completed.", LoggerType.PatternHub);
                // if successful. Notify the success.
                ClientPublishedPatterns.RemoveAll(p => p.Identifier == IdToRemove);
                Mediator.Publish(new NotificationMessage("Pattern Removal", "removed successful!", NotificationType.Info));
            }
            else throw new Exception("Failed to remove pattern from servers.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }
    #endregion PatternHub Tasks

    #region MoodlesHub Tasks
    private async Task FetchMoodleTask()
    {
        try
        {
            // take the comma seperated search string, split them by commas, convert to lowercase, and trim tailing and leading whitespaces.
            var tags = SearchTags.Split(',')
                .Select(x => x.ToLower().Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet();

            // Firstly, we should compose the Dto for the search operation.
            MoodleSearchDto dto = new(SearchString, tags, SearchFilter, SearchSort);

            // perform the search operation.
            var result = await _apiHubMain.SearchMoodles(dto);

            // if the result contains an empty list, then we failed to retrieve Moodle.
            if (result.Count == 0)
            {
                Logger.LogError("Failed to retrieve Moodle from servers.");
                // clean up the search results
                LatestMoodleResults.Clear();
            }
            else
            {
                Logger.LogInformation("Retrieved Moodle from servers.", LoggerType.PatternHub);
                LatestMoodleResults = result;
            }
            if(!InitialMoodlesCall) InitialMoodlesCall = true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to retrieve Moodle from servers.");
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }
    private async Task LikeMoodleActionTask(Guid moodleId)
    {
        try
        {
            var result = await _apiHubMain.LikeMoodle(moodleId);
            if (result)
            {
                Logger.LogInformation("Like interaction successful.", LoggerType.PatternHub);
                // update the moodleId stuff
                var pattern = LatestMoodleResults.FirstOrDefault(x => x.MoodleStatus.GUID == moodleId);
                if (pattern is not null)
                {
                    pattern.Likes += pattern.HasLikedMoodle ? -1 : 1;
                    pattern.HasLikedMoodle = !pattern.HasLikedMoodle;

                    if (pattern.HasLikedMoodle)
                    {
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Liked, pattern.MoodleStatus.GUID, false);
                    }
                }
            }
            else
            {
                Logger.LogError("Like interaction failed.");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to interact with pattern.");
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }

    private async Task UploadMoodleTask(string authorName, HashSet<string> tags, MoodlesStatusInfo moodleInfo)
    {
        try
        {
            // construct the dto with the given information.
            MoodleUploadDto dto = new(authorName, tags, moodleInfo);
            Logger.LogTrace("Uploading Moodle to server.", LoggerType.PatternHub);
            // perform the api call for the upload.
            var result = await _apiHubMain.UploadMoodle(dto);
            if (result)
            {
                Mediator.Publish(new NotificationMessage("Moodle Upload", "uploaded successful!", NotificationType.Info));
                ClientPublishedMoodles.Add(new PublishedMoodle() { AuthorName = authorName, MoodleStatus = moodleInfo });
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Published, Guid.Empty, false);
            }
            else throw new Exception("Failed to upload pattern to servers.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }

    private async Task RemoveMoodleTask(Guid moodleId)
    {
        try
        {
            if (moodleId == Guid.Empty || !ClientPublishedMoodles.Any(m => m.MoodleStatus.GUID == moodleId))
                return;

            var result = await _apiHubMain.RemoveMoodle(moodleId);
            if (result)
            {
                Logger.LogInformation("RemovePatternTask completed.", LoggerType.PatternHub);
                ClientPublishedMoodles.RemoveAll(m => m.MoodleStatus.GUID == moodleId);
            }
            else throw new Exception("Failed to remove moodle from servers.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload pattern to servers.");
            Mediator.Publish(new NotificationMessage("Pattern Upload", "upload failed!", NotificationType.Warning));
        }
        finally
        {
            CurrentShareHubTask = null;
        }
    }
    #endregion PatternHub Tasks
}
