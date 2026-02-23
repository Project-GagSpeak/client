using GagspeakAPI.Extensions;
using System.Security.Principal;

namespace GagSpeak.Utils;

internal sealed class UsageTracker
{
    /// <summary>
    ///     The current score for this usage tracker.
    /// </summary>
    public double BaseScore;

    /// <summary>
    ///     Increases each time the threshold is passed. A punishment multiplier.
    /// </summary>
    public int StackCount = 0;

    /// <summary>
    ///     The last tick this was updated. (Calculates decay and timeout.)
    /// </summary>
    public long LastTick;

    /// <summary>
    ///     The tick when we last passed the threshold. 0 if not yet passed.
    /// </summary>
    public long LastThresholdTick = 0;
}

/// <summary>
///     Makeshift rate limiter that allows for actions to be performed in burst, but decay
///     overtime and prevent excessive use.
/// </summary>
internal sealed class RateLimiter<T> where T : notnull
{
    /// <summary>
    ///     How much to remove from the score per second.
    /// </summary>
    internal double DecayRate { get; }

    internal double LogDecay { get; }

    /// <summary>
    ///     The threshold to decline actions after.
    /// </summary>
    internal double Threshold { get; }

    /// <summary>
    ///     If no action occurs for the length of the reset time, reset to 0.
    /// </summary>
    internal double ScoreTimeout { get; } = 30;

    /// <summary>
    ///     The multiplier to apply to the score when the threshold is passed.
    /// </summary>
    internal double PunishMultiplier { get; } = 1.5;

    /// <summary>
    ///     How many seconds after the last threshold pass tick that 
    ///     we will increase the pass count over resetting it.
    /// </summary>
    internal double PunishWindow { get; } = 15;

    private readonly ConcurrentDictionary<T, UsageTracker> _trackers = new();

    public RateLimiter(double decayRate, double threshold)
    {
        DecayRate = decayRate;
        LogDecay = Math.Log(decayRate);
        Threshold = threshold;
    }

    public RateLimiter(double decayRate, double threshold, double punishMultiplier, double punishWindow, double scoreTimeout = 30)
        : this(decayRate, threshold)
    {
        ScoreTimeout = scoreTimeout;
        PunishMultiplier = punishMultiplier;
        PunishWindow = punishWindow;
    }

    public UsageTracker GetOrCreateTracker(T key)
        => _trackers.GetOrAdd(key, _ => new UsageTracker { LastTick = Stopwatch.GetTimestamp() });

    public bool CanRecord(T key)
    {
        var t = GetOrCreateTracker(key);
        var projected = t.BaseScore * Math.Exp(LogDecay * (Stopwatch.GetTimestamp() - t.LastTick) / Stopwatch.Frequency) + t.StackCount * PunishMultiplier;
        //Svc.Logger.Information($"Projected score: {projected:F2}. Threshold: {Threshold}. StackCount: {t.StackCount}, Multiplier {PunishMultiplier}");
        return projected < Threshold;
    }

    /// <summary>
    ///     Records usage, returning if we went over the threshold or not.
    /// </summary>
    public bool RecordUse(T key)
    {
        var t = GetOrCreateTracker(key);
        lock (t)
        {
            ApplyUse(t);
            return t.BaseScore < Threshold;
        }
    }

    public TimeSpan GetPenaltyLength(T key)
    {
        var t = GetOrCreateTracker(key);
        lock (t)
        {
            var projected = t.BaseScore * Math.Exp(LogDecay * (Stopwatch.GetTimestamp() - t.LastTick) / Stopwatch.Frequency);
            var effective = projected + t.StackCount * PunishMultiplier;
            if (effective < Threshold) return TimeSpan.Zero;
            return TimeSpan.FromSeconds(Math.Max(0, Math.Log(Threshold / effective) / LogDecay));
        }
    }

    /// <summary>
    ///     Applies usage, updating the score and stack count as necessary. <para />
    ///     <b>Ensure this is called within a LOCK</b>
    /// </summary>
    private void ApplyUse(UsageTracker t)
    {
        var now = Stopwatch.GetTimestamp();
        var elapsed = (now - t.LastTick) / (double)Stopwatch.Frequency;

        t.BaseScore *= Math.Exp(LogDecay * elapsed);
        t.BaseScore += 1.0;

        if (t.BaseScore >= Threshold)
        {
            var stackElapsed = t.LastThresholdTick == 0
                ? double.MaxValue
                : (now - t.LastThresholdTick) / (double)Stopwatch.Frequency;
            t.StackCount = stackElapsed > PunishWindow ? 1 : t.StackCount + 1;
            t.LastThresholdTick = now;
            t.BaseScore += t.StackCount * PunishMultiplier;
        }
        else if (elapsed >= ScoreTimeout)
        {
            t.StackCount = 0;
            t.LastThresholdTick = 0;
        }

        t.LastTick = now;
    }
}
