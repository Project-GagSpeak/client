namespace GagSpeak.PlayerClient;

public struct LightAchievement
{
    /// <summary>
    ///     Only the ID madders for this. It is unique, and constant, will not change between versions.
    /// </summary>
    public int AchievementId { get; set; }

    /// <summary>
    ///     The kind of achievement the light achievement is. Nessisary for knowing what to import.
    /// </summary>
    public AchievementType Type { get; set; }

    /// <summary>
    ///     Gets if the achievement was completed or not.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    ///     Latest progress (for ProgressAchievements & ConditionalProgressAchievements & TimedProgress)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    ///     Gets if the conditionalTaskBegin is true (for ConditionalProgressAchievements)
    /// </summary>
    public bool ConditionalTaskBegun { get; set; }

    /// <summary>
    ///     Gets StartTime (for TimedProgressAchievements & TimeRequired/TimeLimited)
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    ///     Stores recorded times things in TimedProgressAchievements handle.
    /// </summary>
    public List<DateTime> RecordedDateTimes { get; set; }

    /// <summary>
    ///     The list of items that are being monitored (for duration achievements)
    /// </summary>
    public List<TrackedItem> ActiveItems { get; set; }
}

public struct TrackedItem
{
    public string Item { get; init; }
    public string UIDAffected { get; init; }
    public DateTime TimeAdded { get; init; }

    public TrackedItem(string item, string uidAffected)
    {
        Item = item;
        UIDAffected = uidAffected;
        TimeAdded = DateTime.UtcNow;
    }
}

