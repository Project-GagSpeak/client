using MessagePack;

namespace GagspeakAPI.Dto.Connection;

// The datatransfer object that tracks the current number of connected online users on the server.
[MessagePackObject(keyAsPropertyName: true)]
public record SystemInfoDto
{
    public int OnlineUsers { get; set; } // The number of online users
    public int OnlineToyboxUsers { get; set; } // The number of online users using the toybox connections
}