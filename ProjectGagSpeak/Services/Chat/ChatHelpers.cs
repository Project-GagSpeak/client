using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using GagspeakAPI.Chat;
using GagspeakAPI.User;

namespace GagSpeak.Services;

// Defined by FFXIVClientStructs as uint, but could be wrong?
public enum InputChannel : int
{
    None = -2,

    Tell_In = 0,
    Say = 1,
    Party = 2,
    Alliance = 3,
    Yell = 4,
    Shout = 5,
    FreeCompany = 6,
    PvpTeam = 7,
    NoviceNetwork = 8,

    CWL1 = 9,
    CWL2 = 10,
    CWL3 = 11,
    CWL4 = 12,
    CWL5 = 13,
    CWL6 = 14,
    CWL7 = 15,
    CWL8 = 16,

    Tell = 17, // Special channel for received tells and such, 18 is Unused seemingly.

    LS1 = 19,
    LS2 = 20,
    LS3 = 21,
    LS4 = 22,
    LS5 = 23,
    LS6 = 24,
    LS7 = 25,
    LS8 = 26,

    Echo = 56,

    // Custom channels can be defined past 1000, (1000-1008 are used by chat2 for extrachat?)

    Invalid = 9999,
}

public unsafe static class ChatHelpers
{
    public const byte START_BYTE = 0x02;
    public const byte CHUNK_TYPE_INTERACTABLE = 0x27;
    public const byte LOG_INFO_TYPE = 0x65; // GagSpeak-> 6ag5peak (Custom type)
    public const byte END_BYTE = 0x03;

    public static RawPayload CreateGsChatIdPayload(string logId)
    {
        var chunkLen =
            1 + // LOG_INFO_TYPE
            1 + // UNK 0xFF (For string)
            1 + // logId.Length byte
            logId.Length;

        var bytes = new List<byte>() { START_BYTE, CHUNK_TYPE_INTERACTABLE, (byte)chunkLen, LOG_INFO_TYPE };

        bytes.AddRange([0xFF, (byte)logId.Length]);
        bytes.AddRange(Encoding.UTF8.GetBytes(logId));

        bytes.Add(END_BYTE);
        return new RawPayload(bytes.ToArray());
    }

    public static string ExtractGsChatlogId(SeString source)
    {
        if (source.Payloads.Count > 0 && source.Payloads[0] is RawPayload raw)
        {
            var data = raw.Data;
            try
            {
                if (data[1] == CHUNK_TYPE_INTERACTABLE && data[3] == LOG_INFO_TYPE)
                    return Encoding.UTF8.GetString(data[6..^1]);
            }
            catch (ArgumentException ex)
            {
                Svc.Logger.Error(ex, "Failed to parse custom log ID");
                Svc.Logger.Error($"Byte Array: {string.Join(", ", data[6..^1])}");
                return string.Empty;
            }
        }
        return string.Empty;
    }

    public static ChatlogId GetDirectMessageChatId(UserData sender, UserData recipient)
        => new(GsChatKind.Direct, string.CompareOrdinal(sender.UID, recipient.UID) < 0 ? $"{sender.UID}-{recipient.UID}" : $"{recipient.UID}-{sender.UID}");

    internal static bool ValidAnyLinkshell(this InputChannel channel)
    {
        var idx = channel.LinkshellIdx();
        if (idx == uint.MaxValue) // Another way to validate a custom ReplyChannel!!! (Because we shouldnt be setting custom ones anyways?!?
            return true;
        // If a valid linkshell, true.
        if (channel.IsLinkshell() && ValidLinkshell(idx))
            return true;
        // If a valid crosslinkshell, true.
        if (channel.IsCrossLinkshell() && ValidCrossLinkshell(idx))
            return true;
        // Fail otherwise.
        return false;
    }

    internal static bool ValidLinkshell(uint idx)
        => idx <= 7 && InfoProxyLinkshell.Instance()->LinkShells[(int)idx].Id != 0;

    internal static bool ValidCrossLinkshell(uint idx)
        => idx <= 7 && InfoProxyCrossWorldLinkshell.Instance()->CrossWorldLinkshells[(int)idx].Name.Length > 0;

    internal static uint LinkshellIdx(this InputChannel channel) => channel switch
    {
        InputChannel.LS1 => 0,
        InputChannel.LS2 => 1,
        InputChannel.LS3 => 2,
        InputChannel.LS4 => 3,
        InputChannel.LS5 => 4,
        InputChannel.LS6 => 5,
        InputChannel.LS7 => 6,
        InputChannel.LS8 => 7,
        InputChannel.CWL1 => 0,
        InputChannel.CWL2 => 1,
        InputChannel.CWL3 => 2,
        InputChannel.CWL4 => 3,
        InputChannel.CWL5 => 4,
        InputChannel.CWL6 => 5,
        InputChannel.CWL7 => 6,
        InputChannel.CWL8 => 7,
        _ => uint.MaxValue,
    };

    // Could do ranges for this.
    internal static bool IsLinkshell(this InputChannel channel) => channel switch
    {
        InputChannel.LS1 => true,
        InputChannel.LS2 => true,
        InputChannel.LS3 => true,
        InputChannel.LS4 => true,
        InputChannel.LS5 => true,
        InputChannel.LS6 => true,
        InputChannel.LS7 => true,
        InputChannel.LS8 => true,
        _ => false,
    };

    // Could do ranges for this.
    internal static bool IsCrossLinkshell(this InputChannel channel) => channel switch
    {
        InputChannel.CWL1 => true,
        InputChannel.CWL2 => true,
        InputChannel.CWL3 => true,
        InputChannel.CWL4 => true,
        InputChannel.CWL5 => true,
        InputChannel.CWL6 => true,
        InputChannel.CWL7 => true,
        InputChannel.CWL8 => true,
        _ => false,
    };
}
