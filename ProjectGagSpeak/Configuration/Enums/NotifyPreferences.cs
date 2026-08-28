namespace GagSpeak.PlayerClient;

[Flags]
public enum AlertKind
{
    None    = 0 << 0,
    Bubble  = 1 << 0,
    DtrBar  = 1 << 1,
    Audio   = 1 << 2,
}

[Flags]
public enum OnlineFilter : int
{
    None      = 0 << 0,
    Temporary = 1 << 0,
    Nicknamed = 1 << 1,
    Favorited = 1 << 2,
}

public enum FilterPolicy
{
    MatchAny = 0,
    MatchAll = 1,
}

