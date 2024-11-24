using Dalamud.Plugin.Services;
using GagSpeak.WebAPI;

namespace GagSpeak.Achievements;

public class ProgressAchievement : AchievementBase
{
    /// <summary>
    /// The Current Progress made towards the achievement.
    /// </summary>
    public int Progress { get; set; }

    public ProgressAchievement(AchievementModuleKind module, AchievementInfo infoBase, int goal, Action<int, string> onCompleted, string prefix = "", 
        string suffix = "", bool isSecret = false) : base(module, infoBase, goal, prefix, suffix, onCompleted, isSecret)
    {
        Progress = 0;
    }

    public override int CurrentProgress() => IsCompleted ? MilestoneGoal : Progress;

    public override string ProgressString() => PrefixText + " " + (CurrentProgress() + " / " + MilestoneGoal) + " " + SuffixText;

    /// <summary>
    /// Increments the progress towards the achievement.
    /// </summary>
    public void IncrementProgress(int amount = 1)
    {
        if (IsCompleted || !MainHub.IsConnected) 
            return;

        UnlocksEventManager.AchievementLogger.LogTrace($"Incrementing Progress by 1 for {Title}. Total Required: {MilestoneGoal}", LoggerType.AchievementInfo);
        Progress += amount;
        // check for completion after incrementing progress
        CheckCompletion();
    }

    /// <summary>
    /// Check if the Milestone has been met.
    /// </summary>
    public override void CheckCompletion()
    {
        if (IsCompleted || !MainHub.IsConnected) 
            return;

        if (Progress >= MilestoneGoal)
        {
            // Mark the achievement as completed
            MarkCompleted();
        }
    }
    public override AchievementType GetAchievementType() => AchievementType.Progress;
}
