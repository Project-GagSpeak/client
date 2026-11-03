using GagSpeak.PlayerClient;
using System.Globalization;

namespace GagSpeak.PlayerControl;

/// <summary>
///   A Locked Follow where the follower is free within <see cref="Range"/> of the leader and only pulled back past it.
///   Stored in the LockedFollowing string as "UID|leash:5[,walk][|pairlocked]", so the server needs no changes.
/// </summary>
public readonly record struct Leash(float Range, bool ForceWalk)
{
    private const string Tag = "leash:";

    /// <summary> Our own Locked Follow's leash, or null if it is a regular Locked Follow (or none at all). </summary>
    public static Leash? Current => Parse(ClientData.Hardcore?.LockedFollowing);

    /// <summary> Debug: set while the solo debug leash runs, so its mount block applies. Cleared by StopLeash. </summary>
    public static bool DebugActive;

    public string ToTag()
        => $"{Tag}{Range.ToString("0.#", CultureInfo.InvariantCulture)}{(ForceWalk ? ",walk" : string.Empty)}";

    public static Leash? Parse(string? followString)
    {
        if (string.IsNullOrEmpty(followString))
            return null;

        foreach (var part in followString.Split('|'))
        {
            if (!part.StartsWith(Tag))
                continue;

            var values = part[Tag.Length..].Split(',');
            if (float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var range))
                return new(range, values.Contains("walk"));
        }
        return null;
    }
}
