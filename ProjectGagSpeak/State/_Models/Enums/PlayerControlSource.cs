namespace GagSpeak.State;

/// <summary>
///     Represents the sources causing a block
///     to a player's control of some type.
/// </summary>
[Flags]
public enum PlayerControlSource : ushort
{
    None             = 0 << 0,
    LockedFollowing  = 1 << 0,
    LockedEmote      = 1 << 1,
    IndoorConfinement= 1 << 2,
    ChatBoxesHidden  = 1 << 3,
    ChatInputHidden  = 1 << 4,
    ChatInputBlocked = 1 << 5,
    Immobile         = 1 << 6,
    Weighty          = 1 << 7,
    LifestreamTask   = 1 << 8,
}
