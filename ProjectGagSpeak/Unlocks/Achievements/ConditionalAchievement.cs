using Dalamud.Plugin.Services;
using GagSpeak.WebAPI;

namespace GagSpeak.Achievements;

public class ConditionalAchievement : AchievementBase
{
    /// <summary>
    /// The condition that must be met to complete the achievement
    /// </summary>
    private Func<bool> Condition;

    public ConditionalAchievement(AchievementModuleKind module, AchievementInfo infoBase, Func<bool> cond, 
        Action<int, string> onCompleted, string prefix = "", string suffix = "", bool isSecret = false) 
        : base(module, infoBase, 1, prefix, suffix, onCompleted, isSecret)
    {
        Condition = cond;
    }

    public override int CurrentProgress() => IsCompleted ? 1 : 0;

    public override float CurrentProgressPercentage() => (float)CurrentProgress();

    public override string ProgressString() => PrefixText + " " + (IsCompleted ? "1" : "0" + " / 1") + " " + SuffixText;

    public override void CheckCompletion()
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        UnlocksEventManager.AchievementLogger.LogTrace($"Checking if {Title} satisfies conditional", LoggerType.AchievementInfo);

        if (Condition())
        {
            // Mark the achievement as completed
            MarkCompleted();
        }
    }

    public override AchievementType GetAchievementType() => AchievementType.Conditional;
}




