using GagspeakAPI.Data.IPC;

namespace GagSpeak.Achievements;
public abstract class AchievementBase : IAchievementItem
{
    private AchievementInfo BaseInfo { get; init; }
    public int AchievementId => BaseInfo.Id;
    public string Title => BaseInfo.Title;
    public string Description => BaseInfo.Description;
    public ProfileComponent RewardComponent => BaseInfo.UnlockReward.Component;
    public StyleKind RewardStyleType => BaseInfo.UnlockReward.Type;
    public int RewardStyleIndex => BaseInfo.UnlockReward.Value;
    
    public AchievementModuleKind Module { get; init; }
    public int MilestoneGoal { get; init; }
    public string PrefixText { get; init; }
    public string SuffixText { get; init; }
    public bool IsCompleted { get; set; } = false;
    public bool IsSecretAchievement { get; init; }
    private readonly Action<int, string> ActionOnCompleted;
    protected AchievementBase(AchievementModuleKind module, AchievementInfo infoBase, int goal, string prefix, string suffix, 
        Action<int, string> onCompleted, bool isSecret = false)
    {
        IsCompleted = false;
        BaseInfo = infoBase;
        Module = module;
        MilestoneGoal = goal;
        PrefixText = prefix;
        SuffixText = suffix;
        ActionOnCompleted = onCompleted;
        IsSecretAchievement = isSecret;
    }

    /// <summary>
    /// Check if the achievement is complete
    /// </summary>
    public abstract void CheckCompletion();

    /// <summary>
    /// Force a requirement that all achievement types must report their progress
    /// </summary>
    public abstract int CurrentProgress();

    /// <summary>
    /// The string representation of our Progress. Used for Achievement Display.
    /// </summary>
    public abstract string ProgressString();

    /// <summary>
    /// Mark the achievement as completed
    /// </summary>
    protected void MarkCompleted()
    {
        IsCompleted = true;
        ActionOnCompleted?.Invoke(AchievementId, Title);
    }

    /// <summary>
    /// Useful for quick compression when doing data transfer
    /// </summary>
    public abstract AchievementType GetAchievementType();
}
