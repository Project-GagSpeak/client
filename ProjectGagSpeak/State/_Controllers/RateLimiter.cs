using GagspeakAPI.Extensions;

namespace GagSpeak.PlayerState.Controllers;

public class ActionRateLimiter
{
    // Track each module's action counts and strike status
    private readonly Dictionary<InvokableActionType, ActionStats> _actionStats = new();

    // List of delays (block times) based on strike count
    private readonly IReadOnlyList<TimeSpan> _delays = new List<TimeSpan>
    {
        TimeSpan.FromSeconds(10),  // 1st delay (10 seconds)
        TimeSpan.FromSeconds(30),  // 2nd delay (30 seconds)
        TimeSpan.FromMinutes(5),   // 3rd delay (5 minutes)
        TimeSpan.FromHours(1)      // 4th delay (1 hour)
    };

    private readonly TimeSpan _actionWindow; // The time our stopwatches run for when started.

    public class ActionStats
    {
        public int ActionCapPerWindow { get; init; }
        public int Count { get; set; } = 0; // Number of actions invoked within the time window
        public int StrikeCount { get; set; } = 0; // Track how many strikes this module has accumulated
        public Stopwatch Stopwatch { get; set; } = new Stopwatch(); // Stopwatch to track the time window
        public TimeSpan BlockFor { get; set; } = TimeSpan.Zero; // Time until actions are allowed again
        public DateTime LastStrikeTime { get; set; } = DateTime.MinValue; // Time when last strike occurred

        public ActionStats(int actionCapPerWindow)
        {
            Stopwatch.Start();
            ActionCapPerWindow = actionCapPerWindow;
        }

        public void Reset()
        {
            Count = 0;
            StrikeCount = 0;
            Stopwatch.Restart();
            BlockFor = TimeSpan.Zero;
            LastStrikeTime = DateTime.MinValue;
        }
    }

    public ActionRateLimiter(TimeSpan timeWindow, int restraintCap, int restrictionCap, int gagCap, int moodleCap)
    {
        _actionWindow = timeWindow;
        _actionStats[InvokableActionType.Restraint] = new ActionStats(restraintCap);
        _actionStats[InvokableActionType.Restriction] = new ActionStats(restrictionCap);
        _actionStats[InvokableActionType.Gag] = new ActionStats(gagCap);
        _actionStats[InvokableActionType.Moodle] = new ActionStats(moodleCap);
    }

    public bool CanExecute(InvokableActionType actionType)
    {
        // False if the type is not in the dictionary
        if (!_actionStats.ContainsKey(actionType))
            return false;

        var stats = _actionStats[actionType];

        // If the module is currently blocked (lockout time is active), reject the action
        if (stats.Stopwatch.Elapsed < stats.BlockFor)
            return false;

        // If the time has passed the action window, reset the count and start the stopwatch again
        if (stats.Stopwatch.Elapsed > _actionWindow)
        {
            // Make sure we are not still straining the server the moment the restriction is lifted.
            if (stats.LastStrikeTime + stats.BlockFor + TimeSpan.FromSeconds(3) > DateTime.Now)
            {
                StrikeModule(actionType);
                return false;
            }

            stats.Reset();
        }
        else
        {
            // If within the action window, increment the count and check if it exceeds the cap
            if (stats.Count >= stats.ActionCapPerWindow)
            {
                // Strike the module and set the block time if the action cap is exceeded
                StrikeModule(actionType);
                return false;
            }

            // Otherwise, increment the count and allow the action to proceed
            stats.Count++;
        }

        return true;
    }

    private void StrikeModule(InvokableActionType type)
    {
        var stats = _actionStats[type];

        // Increment the strike count
        stats.StrikeCount++;
        stats.LastStrikeTime = DateTime.Now;

        // Apply the appropriate block time based on strike count
        switch (stats.StrikeCount)
        {
            case 1: stats.BlockFor.Add(TimeSpan.FromSeconds(10)); break;
            case 2: stats.BlockFor.Add(TimeSpan.FromSeconds(30)); break;
            case 3: stats.BlockFor.Add(TimeSpan.FromMinutes(5)); break;
            default: stats.BlockFor.Add(TimeSpan.FromHours(1)); break;
        }

        // Log the strike for debugging purposes
        GagSpeak.StaticLog.Verbose($"[RateLimiter] {type} has been struck at {DateTime.Now}, " +
            $"next available time allowed in: {stats.BlockFor.ToGsRemainingTime()}", LoggerType.Triggers);
    }
}
