using Timer = System.Timers.Timer;

namespace OtterGui.Classes;

public static class DayChangeTracker
{
    private static readonly Timer Timer = new(GetSleepTime());

    public static event Action<int, int, int>? DayChanged;

    static DayChangeTracker()
    {
        Timer.Elapsed += (s, e) =>
        {
            var now = DateTime.Now;
            DayChanged?.Invoke(now.Day, now.Month, now.Year);
            Timer.Interval = GetSleepTime();
        };
        Timer.Start();
    }

    private static double GetSleepTime()
    {
        var midnightTonight          = DateTime.Today.AddDays(1);
        var differenceInMilliseconds = (midnightTonight - DateTime.Now).TotalMilliseconds;
        return differenceInMilliseconds;
    }

    private static void OnSystemTimeChanged(object sender, EventArgs e)
    {
        Timer.Interval = GetSleepTime();
    }
}
