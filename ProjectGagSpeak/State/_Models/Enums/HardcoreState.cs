namespace GagSpeak.PlayerState.Visual;

[Flags]
public enum HardcoreState : byte
{
    None             = 0x00,
    ForceFollow      = 0x01,
    ForceEmote       = 0x02,
    ForceStay        = 0x04,
    ChatBoxHidden    = 0x08,
    ChatInputHidden  = 0x10,
    ChatInputBlocked = 0x20,
}
