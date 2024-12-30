using GagSpeak.WebAPI;

namespace GagSpeak.Achievements;

public class DurationAchievement : AchievementBase
{
    private readonly TimeSpan MilestoneDuration; // Required duration to achieve

    // The Current Active Item(s) being tracked. (can be multiple because of gags.
    public List<TrackedItem> ActiveItems { get; set; } = new List<TrackedItem>();

    public DurationTimeUnit TimeUnit { get; init; }

    public DurationAchievement(AchievementModuleKind module, AchievementInfo infoBase, TimeSpan duration, Action<int, string> onCompleted,
        DurationTimeUnit timeUnit = DurationTimeUnit.Minutes, string prefix = "", string suffix = "", bool isSecret = false) 
        : base(module, infoBase, ConvertToUnit(duration, timeUnit), prefix, suffix, onCompleted, isSecret)
    {
        MilestoneDuration = duration;
        TimeUnit = timeUnit;
    }

    private static int ConvertToUnit(TimeSpan duration, DurationTimeUnit unit)
    {
        return unit switch
        {
            DurationTimeUnit.Seconds => (int)duration.TotalSeconds,
            DurationTimeUnit.Minutes => (int)duration.TotalMinutes,
            DurationTimeUnit.Hours => (int)duration.TotalHours,
            DurationTimeUnit.Days => (int)duration.TotalDays,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), "Invalid time unit")
        };
    }

    public override int CurrentProgress()
    {
        // if completed, return the milestone goal.
        if (IsCompleted || !MainHub.IsConnected)
            return MilestoneGoal;

        // otherwise, return the ActiveItem with the longest duration from the DateTime.UtcNow and return its value in total minutes.
        var elapsed = ActiveItems.Any() ? (DateTime.UtcNow - ActiveItems.Min(x => x.TimeAdded)) : TimeSpan.Zero;

        // Return progress based on the specified unit
        return TimeUnit switch
        {
            DurationTimeUnit.Seconds => (int)elapsed.TotalSeconds,
            DurationTimeUnit.Minutes => (int)elapsed.TotalMinutes,
            DurationTimeUnit.Hours => (int)elapsed.TotalHours,
            DurationTimeUnit.Days => (int)elapsed.TotalDays,
            _ => 0 // Default case, should not be hit
        };
    }

    public override float CurrentProgressPercentage()
    {
        // If completed or no active items, the percentage is 100% or 0% respectively
        if (IsCompleted)
            return 1f;

        if (!ActiveItems.Any() || !MainHub.IsConnected)
            return 0f;

        // Calculate elapsed time
        var elapsed = DateTime.UtcNow - ActiveItems.Min(x => x.TimeAdded);

        // Calculate percentage of milestone duration elapsed
        float percentage = (float)(elapsed.TotalMilliseconds / MilestoneDuration.TotalMilliseconds);

        // Ensure percentage is clamped between 0.0 and 1.0
        return Math.Clamp(percentage, 0f, 1f);
    }


    public override string ProgressString()
    {
        if (IsCompleted)
        {
            return PrefixText + " " + (MilestoneGoal + "/" + MilestoneGoal) + " " + SuffixText;
        }
        // Get the current longest equipped thing.
        var elapsed = ActiveItems.Any() ? (DateTime.UtcNow - ActiveItems.Min(x => x.TimeAdded)) : TimeSpan.Zero;

        // Construct the string to output for the progress.
        string outputStr = "";
        if (elapsed == TimeSpan.Zero)
        {
            outputStr = "0s";
        }
        else
        {
            if (elapsed.Days > 0) outputStr += elapsed.Days + "d ";
            if (elapsed.Hours > 0) outputStr += elapsed.Hours + "h ";
            if (elapsed.Minutes > 0) outputStr += elapsed.Minutes + "m ";
            if (elapsed.Seconds >= 0) outputStr += elapsed.Seconds + "s ";
        }
        // Add the Ratio
        return PrefixText + " " + outputStr + " / " + MilestoneGoal + " " + SuffixText;
    }

    public string GetActiveItemProgressString()
    {
        // join together every item in the dictionary with the time elapsed on each item, displaying the UID its on, and the item identifier, and the time elapsed.
        return string.Join("\n", ActiveItems.Select(x => "Item: " + x.Item + ", Applied on: " + x.UIDAffected + " @ " + (DateTime.UtcNow - x.TimeAdded).ToString(@"hh\:mm\:ss")));
    }

    /// <summary>
    /// Begin tracking the time period of a duration achievement
    /// </summary>
    public void StartTracking(string item, string affectedUID)
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        if (!ActiveItems.Any(x => x.Item == item && x.UIDAffected == affectedUID))
        {
            UnlocksEventManager.AchievementLogger.LogTrace($"Started Tracking item {item} on {affectedUID} for {Title}", LoggerType.Achievements);
            ActiveItems.Add(new TrackedItem(item, affectedUID)); // Start tracking time
        }
        else
        {
            UnlocksEventManager.AchievementLogger.LogTrace($"Item {item} on {affectedUID} is already being tracked for {Title}, ignoring. (Likely loading in from reconnect)", LoggerType.AchievementInfo);
        }
    }

    /// <summary>
    /// Cleans up any items no longer present on the UID that are still cached.
    /// </summary>
    public void CleanupTracking(string uidToScan, List<string> itemsStillActive)
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        // determine the items to remove by taking all items in the existing list that contain the matching affecteduid.
        // and select all from that subset that's item doesnt exist in the list of active items.
        var itemsToRemove = ActiveItems
            .Where(x => x.UIDAffected == uidToScan && !itemsStillActive.Contains(x.Item))
            .ToList();

        foreach (var trackedItem in itemsToRemove)
        {
            // if the item is no longer present, we should first
            // calculate the the current datetime, subtract from time added. and see if it passes the milestone duration.

            // Add some wavier duration to ensure timers set for the same time as the achievment dont end up a second off.
            if ((DateTime.UtcNow - trackedItem.TimeAdded) + TimeSpan.FromSeconds(10) >= MilestoneDuration && uidToScan != MainHub.UID)
            {
                // if it does, we should mark the achievement as completed.
                UnlocksEventManager.AchievementLogger.LogInformation($"Achievement {Title} has been been active for the required Duration. Marking as finished!", LoggerType.AchievementInfo);
                MarkCompleted();
                // clear the list and exit.
                ActiveItems.Clear();
                return;
            }

            // otherwise, it failed to meet the expected duration, so we should remove it from tracking.
            UnlocksEventManager.AchievementLogger.LogTrace("Kinkster: "+uidToScan +" no longer has "+ trackedItem.Item +" applied, removing from tracking.", LoggerType.AchievementInfo);
            ActiveItems.Remove(trackedItem);
        }

        // for the remaining items, we should cleanup the tracking for any items that are still present but exceeded the milestone duration.
        // For now, only do this for SELF. If we do this for others, we run into the issue of assuming they have finished it while offline.
        var clientPlayerItems = ActiveItems.Where(x => x.UIDAffected == MainHub.UID).ToList();
        // for any items in this subsequent list that exceed the milestone duration, we should mark the achievement as completed and remove the item from tracking.
        foreach (var trackedItem in clientPlayerItems)
        {
            // Add some wavier duration to ensure timers set for the same time as the achievment dont end up a second off.
            if ((DateTime.UtcNow - trackedItem.TimeAdded) + TimeSpan.FromSeconds(10) >= MilestoneDuration)
            {
                UnlocksEventManager.AchievementLogger.LogInformation($"Achievement {Title} has been been active for the required Duration. Marking as finished!", LoggerType.AchievementInfo);
                MarkCompleted();
                // clear the list and exit.
                ActiveItems.Clear();
                return;
            }
        }
    }

    /// <summary>
    /// Stop tracking the time period of a duration achievement
    /// </summary>
    public void StopTracking(string item, string fromThisUID)
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        UnlocksEventManager.AchievementLogger.LogTrace($"Stopped Tracking item "+item+" on "+fromThisUID+" for "+Title, LoggerType.AchievementInfo);

        // check completion before we stop tracking.
        CheckCompletion();
        // if not completed, remove the item from tracking.
        if (!IsCompleted)
        {
            if (ActiveItems.Any(x => x.Item == item && x.UIDAffected == fromThisUID))
            {
                UnlocksEventManager.AchievementLogger.LogTrace($"Item "+item+" from "+fromThisUID+" was not completed, removing from tracking.", LoggerType.AchievementInfo);
                ActiveItems.RemoveAll(x => x.Item == item && x.UIDAffected == fromThisUID);
            }
            else
            {
                // Log all currently active tracked items for debugging.
                UnlocksEventManager.AchievementLogger.LogTrace($"Items Currently still being tracked: {string.Join(", ", ActiveItems.Select(x => x.Item))}", LoggerType.AchievementInfo);
            }
        }
    }

    /// <summary>
    /// Check if the condition is satisfied
    /// </summary>
    public override void CheckCompletion()
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        // if any of the active items exceed the required duration, mark the achievement as completed
        // Add some wavier duration to ensure timers set for the same time as the achievment dont end up a second off.
        if (ActiveItems.Any(x => ((DateTime.UtcNow - x.TimeAdded) + TimeSpan.FromSeconds(10)) >= MilestoneDuration))
        {
            // Mark the achievement as completed
            UnlocksEventManager.AchievementLogger.LogInformation($"Achievement {Title} has been been active for the required Duration. "
                + "Marking as finished!", LoggerType.AchievementInfo);
            MarkCompleted();
            // clear the list.
            ActiveItems.Clear();
        }
    }

    public override AchievementType GetAchievementType() => AchievementType.Duration;
}
