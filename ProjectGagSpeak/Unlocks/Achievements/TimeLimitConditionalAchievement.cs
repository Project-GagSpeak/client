using Dalamud.Plugin.Services;
using GagSpeak.WebAPI;

namespace GagSpeak.Achievements;

public class TimeLimitConditionalAchievement : AchievementBase
{
    private readonly TimeSpan MilestoneDuration;
    public DateTime StartPoint { get; set; } = DateTime.MinValue;
    public Func<bool> RequiredCondition;
    public DurationTimeUnit TimeUnit { get; init; }
    private CancellationTokenSource _cancellationTokenSource;
    private bool TaskStarted = false;


    public TimeLimitConditionalAchievement(AchievementModuleKind module, AchievementInfo infoBase, TimeSpan duration, Func<bool> condition, 
        Action<int, string> onCompleted, DurationTimeUnit unit, string prefix = "", string suffix = "", bool isSecret = false) 
        : base(module, infoBase, ConvertToUnit(duration, unit), prefix, suffix, onCompleted, isSecret)
    {
        MilestoneDuration = duration;
        RequiredCondition = condition;
        TimeUnit = unit;
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
        if (IsCompleted) return MilestoneGoal;

        var elapsed = StartPoint != DateTime.MinValue ? DateTime.UtcNow - StartPoint : TimeSpan.Zero;

        return TimeUnit switch
        {
            DurationTimeUnit.Seconds => (int)elapsed.TotalSeconds,
            DurationTimeUnit.Minutes => (int)elapsed.TotalMinutes,
            DurationTimeUnit.Hours => (int)elapsed.TotalHours,
            DurationTimeUnit.Days => (int)elapsed.TotalDays,
            _ => 0
        };
    }

    public override string ProgressString()
    {
        if (IsCompleted) 
            return "Completed Within " + MilestoneGoal + " " + TimeUnit + " " + SuffixText;

        // If not completed and the StartPoint is DateTime.MinValue, display that that the state is not yet begun.
        if(StartPoint == DateTime.MinValue)
            return PrefixText + " Within " + MilestoneGoal + " " + TimeUnit + " " + SuffixText;

        // Grab our remaining time.
        var remaining = MilestoneDuration - (StartPoint != DateTime.MinValue ? DateTime.UtcNow - StartPoint : TimeSpan.Zero);
        string outputStr = "";
        if (remaining == TimeSpan.Zero)
        {
            outputStr = "0s";
        }
        else
        {
            if (remaining.Days > 0) outputStr += remaining.Days + "d ";
            if (remaining.Hours > 0) outputStr += remaining.Hours + "h ";
            if (remaining.Minutes > 0) outputStr += remaining.Minutes + "m ";
            if (remaining.Seconds >= 0) outputStr += remaining.Seconds + "s ";
            outputStr += " Remaining";
        }
        return outputStr;
    }

    public override void CheckCompletion()
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        if (RequiredCondition())
        {
            if (StartPoint != DateTime.MinValue && DateTime.UtcNow - StartPoint < MilestoneDuration)
                CompleteTask();
        }
        else
        {
            UnlocksEventManager.AchievementLogger.LogTrace($"Condition for {Title} not met. Resetting the timer.", LoggerType.AchievementInfo);
            ResetTask();
        }
    }

    // Method to Start the Task/Timer
    public void StartTask()
    {
        if (IsCompleted || !MainHub.IsConnected || TaskStarted)
            return;

        if (RequiredCondition())
        {
            UnlocksEventManager.AchievementLogger.LogTrace($"Condition for {Title} met. Starting the timer.", LoggerType.AchievementInfo);
            StartPoint = DateTime.UtcNow;
            TaskStarted = true;
            StartTimer();
        }
    }

    // Method to interrupt and reset the task
    public void InterruptTask()
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        UnlocksEventManager.AchievementLogger.LogTrace($"Interrupting task for {Title}.", LoggerType.AchievementInfo);
        TaskStarted = false;
        ResetTask();
    }

    // Method to Complete the Task when time and condition are met
    private void CompleteTask()
    {
        UnlocksEventManager.AchievementLogger.LogTrace($"Time and condition met for {Title}. Marking as completed.", LoggerType.AchievementInfo);
        MarkCompleted();
        _cancellationTokenSource?.Cancel();
        TaskStarted = false;
    }

    // Starts the timer task
    private void StartTimer()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(MilestoneDuration, token);
                if (!token.IsCancellationRequested && RequiredCondition())
                    ResetTask(); // reset if we take longer than the requirement.
            }
            catch (TaskCanceledException)
            {
                UnlocksEventManager.AchievementLogger.LogDebug($"Timer for {Title} was canceled.", LoggerType.AchievementInfo);
            }
        }, token);
    }

    // Resets the task if condition fails or is interrupted
    private void ResetTask()
    {
        UnlocksEventManager.AchievementLogger.LogTrace($"Failed to complete {Title} in time, resetting task..", LoggerType.AchievementInfo);
        StartPoint = DateTime.MinValue;
        _cancellationTokenSource?.Cancel();
        TaskStarted = false;
    }

    public override AchievementType GetAchievementType() => AchievementType.TimeLimitConditional;
}




