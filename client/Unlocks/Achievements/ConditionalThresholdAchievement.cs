using Dalamud.Plugin.Services;
using GagSpeak.WebAPI;
using System;

namespace GagSpeak.Achievements;

public class ConditionalThresholdAchievement : AchievementBase
{

    /// <summary>
    /// The condition that must be met to complete the achievement
    /// </summary>
    private Func<bool> Condition;

    /// <summary>
    /// The condition that determines what our threshold is.
    /// </summary>
    private int LastRecordedThreshold { get; set; }

    public ConditionalThresholdAchievement(AchievementModuleKind module, AchievementInfo infoBase, int goal, Func<bool> condition, 
        Action<int, string> onCompleted, string prefix = "", string suffix = "", bool isSecret = false) 
        : base(module, infoBase, goal, prefix, suffix, onCompleted, isSecret)
    {
        Condition = condition;
        LastRecordedThreshold = 0;
    }

    public override int CurrentProgress() => IsCompleted ? MilestoneGoal : LastRecordedThreshold;

    public override string ProgressString() => PrefixText + " " + (CurrentProgress() + " / " + MilestoneGoal) + " " + SuffixText;

    public void UpdateThreshold(int newestThreshold)
    {
        if (IsCompleted || !MainHub.IsConnected) 
            return;

        // if the condition is met, we should update it, otherwise, we should reset the threshold down to 0.
        if(Condition())
        {
            LastRecordedThreshold = newestThreshold;
            UnlocksEventManager.AchievementLogger.LogTrace($"Updating Threshold for {Title}. Current Threshold: {LastRecordedThreshold}" +
                $" -- Total Required: {MilestoneGoal}", LoggerType.AchievementInfo);
            CheckCompletion();
        }
        else
        {
            LastRecordedThreshold = 0;
        }
    }


    /// <summary>
    /// Check if the condition is satisfied
    /// </summary>
    public override void CheckCompletion()
    {
        if (IsCompleted || !MainHub.IsConnected) return;

        if (LastRecordedThreshold >= MilestoneGoal)
        {
            // Mark the achievement as completed
            MarkCompleted();
        }
    }

    public override AchievementType GetAchievementType() => AchievementType.ConditionalThreshold;
}




