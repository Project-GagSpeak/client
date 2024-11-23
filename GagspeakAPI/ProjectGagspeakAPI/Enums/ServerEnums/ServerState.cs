namespace GagspeakAPI.Enums;


public enum ServerState
{
    Offline,
    // Connection States.
    Disconnected,
    Disconnecting,
    Reconnecting,
    Connecting,
    Connected,
    // Error Messages
    Unauthorized,
    VersionMisMatch,
    NoSecretKey,
    // Occurs from Discord
    ForcedReconnect,
}
