namespace GagSpeak.State;

/// <summary>
///     Represents the type of restriction that is being applied.
/// </summary>
[Flags]
public enum HardcoreSetting : byte
{
    None             = 0 << 0,
    LockedFollowing  = 1 << 0,
    LockedEmote      = 1 << 1,
    IndoorConfinement= 1 << 2,
    Imprisoned       = 1 << 3,
    ChatBoxesHidden  = 1 << 4,
    ChatInputHidden  = 1 << 5,
    ChatInputBlocked = 1 << 6,
}
