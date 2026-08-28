using CkCommons;
using System.Collections.Immutable;

namespace GagSpeak;

public static class Consts
{
    public const string DDS_All = "All Kinksters";
    public const string DDS_Rendered = "Visible";
    public const string DDS_Online = "Online";
    public const string DDS_Offline = "Offline";
    // Nearby
    public const string NDDS_Paired = "Paired";
    public const string NDDS_Unpaired = "Unpaired";
    // Requests
    public const string RDDS_Incoming = "Incoming Requests";
    public const string RDDS_Pending = "Pending Requests";
}

// Default values for reversions.
public static class GsDefaults
{
    // DTR - Revise this for our personalized DTR
    public static NativeUiColor DtrColorPairs = new(Foreground: 0xFFD7E3FF, Glow: 0xFF006A99);
    public static NativeUiColor DtrColorDisconnected = new(Glow: 0xFF0428FF);
    public static NativeUiColor DtrColorVisibleUsers = new(Glow: 0xFFFFBA47);

    // Chat
    public static uint DefaultMentionColor = 0xFFF4D762;
    public static NativeUiColor DMColorPrefix = new(Foreground: 0xFFE0B8FF);
    public static NativeUiColor DMColorText = new(Foreground: 0xFFE0B8FF);
    public static NativeUiColor GlobalChatColor = new(Foreground: 0xFFFF5AD0, Glow: 0xFF010101);

    // Nameplates
    public static NativeUiColor NameplateColorKinkster = new(Foreground: 0xFFBDD671, Glow: 0xFF37501D);
    public static NativeUiColor NameplateColorNearby = new(Foreground: 0xFFBDD671, Glow: 0xFF37501D);

    // Add some default themes into here later!
}
