namespace GagSpeak;
/// <summary>
///     Helps ensure that nodes are not interacted with too frequently, 
///     but just enough to make people feel helpless.
/// </summary>
public static class NodeThrottler
{
    internal static StringThrottler Throttler = new();
    public static IReadOnlyCollection<string> ThrottleNames => Throttler.ThrottleNames;

    public static bool Throttle(string name, int milliseconds = 1000, bool resetOnAssign = false)
        => Throttler.ThrottleString(name, milliseconds, resetOnAssign);

    public static bool Reset(string name)
        => Throttler.ResetThrottle(name);

    public static bool Check(string name)
        => Throttler.CheckThrottle(name);

    public static long GetTimeLeft(string name, bool timeCanGoBelowZero = false)
        => Throttler.GetThrottleTimeLeft(name, timeCanGoBelowZero);


    /// <summary>
    ///     Internal string throttler to help mitigate the frequency of interactions with nodes in the game.
    /// </summary>
    public class StringThrottler
    {
        // the strings to throttle, and their total milliseconds to wait before allowing another interaction.
        private Dictionary<string, long> StringThrottles = [];
        public IReadOnlyCollection<string> ThrottleNames => StringThrottles.Keys;
        public bool ThrottleString(string name, int milliseconds = 1000, bool resetOnAssign = false)
        {
            // add it if it does not yet exist, throttled on the enviormental clock, seperate from the game's framework thread. (avoid overhead!)
            if (!StringThrottles.ContainsKey(name))
            {
                StringThrottles[name] = Environment.TickCount64 + milliseconds;
                return true;
            }
            // otherwise, check if the current tick count is greater than the stored value, requiring a reset.
            if (Environment.TickCount64 >= StringThrottles[name])
            {
                StringThrottles[name] = Environment.TickCount64 + milliseconds;
                return true;
            }
            else
            {
                // we need to reset only if we desire to, but return false as it is still throttled.
                if (resetOnAssign)
                    StringThrottles[name] = Environment.TickCount64 + milliseconds;
                return false;
            }
        }

        // Remove a throttler by its name.
        public bool ResetThrottle(string name)
            => StringThrottles.Remove(name);

        // Check if a throttler can be used or not.
        public bool CheckThrottle(string name)
        {
            // if it does not exist, we can assume it is not throttled.
            if (!StringThrottles.ContainsKey(name))
                return true;
            return Environment.TickCount64 > StringThrottles[name];
        }

        public long GetThrottleTimeLeft(string name, bool timeCanGoBelowZero = false)
        {
            if (!StringThrottles.ContainsKey(name)) 
                return timeCanGoBelowZero ? -Environment.TickCount64 : 0;
            // obtain the value minus the current tick count.
            var timeLeft = StringThrottles[name] - Environment.TickCount64;
            if (timeCanGoBelowZero)
                return timeLeft;
            else
                return timeLeft > 0 ? timeLeft : 0;
        }
    }
}
