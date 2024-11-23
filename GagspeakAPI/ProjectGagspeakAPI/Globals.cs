namespace GagspeakAPI;

/// <summary>
/// Container for all globally used strings across client and server.
/// </summary>
public static class Globals
{
    /// <summary>
    /// Used to denote if an action was applied by the client and not another pair.
    /// </summary>
    public const string SelfApplied = "SelfApplied";

    /// <summary>
    /// Used to denote that the permission can only be unlocked by the applierUID defined to the left of the |
    /// </summary>
    public const string DevotedString = "|pairlocked";
}
