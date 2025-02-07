using Dalamud.Plugin.Services;
using GagSpeak.WebAPI;
using System;

namespace GagSpeak.Achievements;

public class ThresholdAchievement : AchievementBase
{
    /// <summary>
    /// The condition that determines what our threshold is.
    /// </summary>
    private int LastRecordedThreshold { get; set; }

    public ThresholdAchievement(AchievementModuleKind module, AchievementInfo infoBase, int goal, Action<int, string> onCompleted, string prefix = "", 
        string suffix = "", bool isSecret = false) : base(module, infoBase, goal, prefix, suffix, onCompleted, isSecret)
    {
        LastRecordedThreshold = 0;
    }

    public override int CurrentProgress() => IsCompleted ? MilestoneGoal : LastRecordedThreshold;
    public override float CurrentProgressPercentage() => (float)(CurrentProgress() / MilestoneGoal);

    public override string ProgressString() => PrefixText + " " + (CurrentProgress() + " / " + MilestoneGoal) + " " + SuffixText;

    public void UpdateThreshold(int threshold)
    {
        if (IsCompleted || !MainHub.IsConnected) 
            return;

        LastRecordedThreshold = threshold;
        UnlocksEventManager.AchievementLogger.LogTrace($"Updating Threshold for {Title}. Current Threshold: {LastRecordedThreshold}" +
            $" -- Total Required: {MilestoneGoal}", LoggerType.AchievementInfo);
        CheckCompletion();
    }


    /// <summary>
    /// Check if the condition is satisfied
    /// </summary>
    public override void CheckCompletion()
    {
        if (IsCompleted || !MainHub.IsConnected) 
            return;

        if (LastRecordedThreshold >= MilestoneGoal)
        {
            // Mark the achievement as completed
            MarkCompleted();
        }
    }

    public override AchievementType GetAchievementType() => AchievementType.Threshold;
}




