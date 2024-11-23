using GagspeakAPI.Data.IPC;

namespace GagSpeak.Achievements;

public interface IAchievementItem
{
    /// <summary>
    /// The Unique Identifier for the Achievement.
    /// </summary>
    int AchievementId { get; }

    /// <summary>
    /// The Title of the Achievement.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// The Description of the Achievement.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The Cosmetic's related Component that this unlocks.
    /// </summary>
    ProfileComponent RewardComponent { get; }
    /// <summary>
    /// The cosmetics related style type it unlocks (background, border, overlay)
    /// </summary>
    StyleKind RewardStyleType { get; }

    /// <summary>
    /// The index of the style that it unlocks. (used to parse between different style enums)
    /// </summary>
    int RewardStyleIndex { get; }

    /// <summary>
    /// Goal that must be reached by an achievement.
    /// </summary>
    public int MilestoneGoal { get; init; }

    /// <summary>
    /// Displayed before the progressString.
    /// </summary>
    public string PrefixText { get; init; }

    /// <summary>
    /// Displayed After the progressString.
    /// </summary>
    public string SuffixText { get; init; }

    /// <summary>
    /// If the achievement has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// If it's a secret achievement or not.
    /// </summary>
    public bool IsSecretAchievement { get; init; }
}
