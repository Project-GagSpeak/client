namespace GagSpeak.State;

/// <summary>
///     Represents the type of restriction that is being applied.
/// </summary>
[Flags]
public enum HardcoreSetting : byte
{
    None             = 0 << 0,
    ForcedFollow     = 1 << 0,
    ForcedEmote      = 1 << 1,
    ForcedStay       = 1 << 2,
    ChatBoxesHidden  = 1 << 3,
    ChatInputHidden  = 1 << 4,
    ChatInputBlocked = 1 << 5,
}
