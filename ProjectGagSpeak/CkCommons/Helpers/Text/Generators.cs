namespace GagSpeak.CkCommons.Helpers;

public static class Generators
{
    public static string GetRandomCharaString(int length)
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var random = new Random();
        return new string(Enumerable.Range(0, length).Select(_ => validChars[random.Next(validChars.Length)]).ToArray());
    }

    public static string GetRandomIntString(int length)
    {
        const string validChars = "0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length).Select(_ => validChars[random.Next(validChars.Length)]).ToArray());
    }

    public static TimeSpan GetRandomTimeSpan(TimeSpan min, TimeSpan max)
    {
        // if the min is greater than the max, make the timespan 1 second and return.
        if (min > max) return TimeSpan.FromSeconds(5);

        var random = new Random();
        var minSeconds = min.TotalSeconds;
        var maxSeconds = max.TotalSeconds;
        var randomSeconds = random.NextDouble() * (maxSeconds - minSeconds) + minSeconds;
        return TimeSpan.FromSeconds(randomSeconds);
    }
}

