using GagSpeak.WebAPI;

namespace GagSpeak.PlayerClient;

public class TimeRequiredConditionalAchievement : AchievementBase
{
    private readonly TimeSpan MilestoneDuration;
    public DateTime StartPoint { get; set; } = DateTime.MinValue;
    public Func<bool> RequiredCondition;
    public DurationTimeUnit TimeUnit { get; init; }
    private CancellationTokenSource _cancellationTokenSource;
    public bool TaskStarted => StartPoint != DateTime.MinValue;

    public TimeRequiredConditionalAchievement(AchievementModuleKind module, AchievementInfo infoBase, TimeSpan dur, Func<bool> cond, 
        Action<int, string> onCompleted, DurationTimeUnit unit, string prefix = "", string suffix = "", bool isSecret = false) 
        : base(module, infoBase, ConvertToUnit(dur, unit), prefix, suffix, onCompleted, isSecret)
    {
        MilestoneDuration = dur;
        RequiredCondition = cond;
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

    public override float CurrentProgressPercentage()
    {
        if (IsCompleted) return 1.0f;

        var elapsed = StartPoint != DateTime.MinValue ? DateTime.UtcNow - StartPoint : TimeSpan.Zero;
        float percentage = (float)(elapsed.TotalMilliseconds / MilestoneDuration.TotalMilliseconds);
        return Math.Clamp(percentage, 0f, 1f);
    }

    public override string ProgressString()
    {
        if (IsCompleted) 
            return PrefixText + " " + MilestoneGoal + " " + TimeUnit + " " + SuffixText;

        if(StartPoint == DateTime.MinValue)
            return PrefixText + " 0s / " + MilestoneGoal + " " + TimeUnit + " " + SuffixText;

        var elapsed = StartPoint != DateTime.MinValue ? DateTime.UtcNow - StartPoint : TimeSpan.Zero;
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
        return PrefixText + " " + outputStr + " / " + MilestoneGoal + " " + TimeUnit + " " + SuffixText;
    }

    public override void CheckCompletion()
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        if (RequiredCondition())
        {
            if (TaskStarted && ((DateTime.UtcNow - StartPoint) + TimeSpan.FromSeconds(10)) >= MilestoneDuration)
                CompleteTask();
        }
        else
        {
            GagspeakEventManager.UnlocksLogger.LogTrace($"Condition for {Title} not met. Resetting the timer.", LoggerType.AchievementInfo);
            ResetTask();
        }
    }

    // Method to Start the Task/Timer
    public void StartTask()
    {
        if (IsCompleted || TaskStarted || !MainHub.IsConnected)
            return;

        if (RequiredCondition())
        {
            GagspeakEventManager.UnlocksLogger.LogTrace($"Condition for {Title} met. Starting the timer.", LoggerType.AchievementInfo);
            StartPoint = DateTime.UtcNow;
            StartTimer();
        }
    }

    // Method to interrupt and reset the task
    public void InterruptTask()
    {
        if (IsCompleted || !MainHub.IsConnected)
            return;

        GagspeakEventManager.UnlocksLogger.LogTrace($"Interrupting task for {Title}.", LoggerType.AchievementInfo);
        ResetTask();
    }

    // Method to Complete the Task when time and condition are met
    private void CompleteTask()
    {
        GagspeakEventManager.UnlocksLogger.LogTrace($"Time and condition met for {Title}. Marking as completed.", LoggerType.AchievementInfo);
        MarkCompleted();
        _cancellationTokenSource?.Cancel();
        StartPoint = DateTime.MinValue;
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
                    CompleteTask(); // reset if we take longer than the requirement.
            }
            catch (TaskCanceledException)
            {
                GagspeakEventManager.UnlocksLogger.LogDebug($"Timer for {Title} was canceled.", LoggerType.AchievementInfo);
            }
        }, token);
    }

    // Resets the task if condition fails or is interrupted
    private void ResetTask()
    {
        GagspeakEventManager.UnlocksLogger.LogTrace($"Did not keep active for the required time for {Title}. Resetting task.", LoggerType.AchievementInfo);
        StartPoint = DateTime.MinValue;
        _cancellationTokenSource?.Cancel();
    }

    public override AchievementType GetAchievementType() => AchievementType.RequiredTimeConditional;
}




