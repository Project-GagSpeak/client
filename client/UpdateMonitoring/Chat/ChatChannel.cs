using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace GagSpeak.ChatMessages;

public static class ChatChannel
{
    // this is the agent that handles the chatlog
    private static unsafe AgentChatLog* ChatlogAgent = (AgentChatLog*)Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.ChatLog);

    // this is the enum that handles the chat channels
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class EnumOrderAttribute : Attribute
    {
        public int Order { get; }
        public EnumOrderAttribute(int order)
        {
            Order = order;
        }
    }

    /// <summary> This enum is used to handle the chat channels. </summary>
    public enum Channels
    {
        [EnumOrder(0)]
        Tell_In = 0,

        [EnumOrder(1)]
        Tell = 17,

        [EnumOrder(2)]
        Say = 1,

        [EnumOrder(3)]
        Party = 2,

        [EnumOrder(4)]
        Alliance = 3,

        [EnumOrder(5)]
        Yell = 4,

        [EnumOrder(6)]
        Shout = 5,

        [EnumOrder(7)]
        FreeCompany = 6,

        [EnumOrder(8)]
        NoviceNetwork = 8,

        [EnumOrder(9)]
        CWL1 = 9,

        [EnumOrder(10)]
        CWL2 = 10,

        [EnumOrder(11)]
        CWL3 = 11,

        [EnumOrder(12)]
        CWL4 = 12,

        [EnumOrder(13)]
        CWL5 = 13,

        [EnumOrder(14)]
        CWL6 = 14,

        [EnumOrder(15)]
        CWL7 = 15,

        [EnumOrder(16)]
        CWL8 = 16,

        [EnumOrder(17)]
        LS1 = 19,

        [EnumOrder(18)]
        LS2 = 20,

        [EnumOrder(19)]
        LS3 = 21,

        [EnumOrder(20)]
        LS4 = 22,

        [EnumOrder(21)]
        LS5 = 23,

        [EnumOrder(22)]
        LS6 = 24,

        [EnumOrder(23)]
        LS7 = 25,

        [EnumOrder(24)]
        LS8 = 26,
    }


    /// <summary> This method is used to get the current chat channel. </summary>
    public static Channels GetChatChannel()
    {
        // this is the channel that we are going to return
        Channels channel;
        // this is unsafe code, so we need to use unsafe
        unsafe
        {
            channel = (Channels)ChatlogAgent->CurrentChannel;
        }
        //return the channel now using
        return channel;
    }

    /// <summary> This method is used to get the ordered list of channels. </summary>
    public static IEnumerable<Channels> GetOrderedChannels()
    {
        return Enum.GetValues(typeof(Channels))
                .Cast<Channels>()
                .Where(e => e != Channels.Tell_In && e != Channels.NoviceNetwork)
                .OrderBy(e => GetOrder(e));
    }

    // Match Channel types with command aliases for them
    public static string[] GetChannelAlias(this Channels channel) => channel switch
    {
        Channels.Tell => new[] { "/t", "/tell" },
        Channels.Say => new[] { "/s", "/say" },
        Channels.Party => new[] { "/p", "/party" },
        Channels.Alliance => new[] { "/a", "/alliance" },
        Channels.Yell => new[] { "/y", "/yell" },
        Channels.Shout => new[] { "/sh", "/shout" },
        Channels.FreeCompany => new[] { "/fc", "/freecompany" },
        Channels.NoviceNetwork => new[] { "/n", "/novice" },
        Channels.CWL1 => new[] { "/cwl1", "/cwlinkshell1" },
        Channels.CWL2 => new[] { "/cwl2", "/cwlinkshell2" },
        Channels.CWL3 => new[] { "/cwl3", "/cwlinkshell3" },
        Channels.CWL4 => new[] { "/cwl4", "/cwlinkshell4" },
        Channels.CWL5 => new[] { "/cwl5", "/cwlinkshell5" },
        Channels.CWL6 => new[] { "/cwl6", "/cwlinkshell6" },
        Channels.CWL7 => new[] { "/cwl7", "/cwlinkshell7" },
        Channels.CWL8 => new[] { "/cwl8", "/cwlinkshell8" },
        Channels.LS1 => new[] { "/l1", "/linkshell1" },
        Channels.LS2 => new[] { "/l2", "/linkshell2" },
        Channels.LS3 => new[] { "/l3", "/linkshell3" },
        Channels.LS4 => new[] { "/l4", "/linkshell4" },
        Channels.LS5 => new[] { "/l5", "/linkshell5" },
        Channels.LS6 => new[] { "/l6", "/linkshell6" },
        Channels.LS7 => new[] { "/l7", "/linkshell7" },
        Channels.LS8 => new[] { "/l8", "/linkshell8" },
        _ => Array.Empty<string>(),
    };

    // Get a commands list for given channelList(config) and add extra space for matching to avoid matching emotes.
    public static List<string> GetChatChannelsListAliases(this IEnumerable<Channels> chatChannelsList)
    {
        var result = new List<string>();
        foreach (Channels chatChannel in chatChannelsList)
        {
            result.AddRange(chatChannel.GetChannelAlias().Select(str => str + " "));
        }
        return result;
    }

    // see if the passed in alias is present as an alias in any of our existing channels
    public static bool IsAliasForAnyActiveChannel(this IEnumerable<Channels> enabledChannels, string alias)
    {
        return enabledChannels.Any(channel => channel.GetChannelAlias().Contains(alias));
    }

    // get the chat channel type from the XIVChatType
    public static Channels? GetChatChannelFromXivChatType(XivChatType type)
    {
        return type switch
        {
            XivChatType.TellIncoming => Channels.Tell,
            XivChatType.TellOutgoing => Channels.Tell,
            XivChatType.Say => Channels.Say,
            XivChatType.Party => Channels.Party,
            XivChatType.Alliance => Channels.Alliance,
            XivChatType.Yell => Channels.Yell,
            XivChatType.Shout => Channels.Shout,
            XivChatType.FreeCompany => Channels.FreeCompany,
            XivChatType.NoviceNetwork => Channels.NoviceNetwork,
            XivChatType.Ls1 => Channels.LS1,
            XivChatType.Ls2 => Channels.LS2,
            XivChatType.Ls3 => Channels.LS3,
            XivChatType.Ls4 => Channels.LS4,
            XivChatType.Ls5 => Channels.LS5,
            XivChatType.Ls6 => Channels.LS6,
            XivChatType.Ls7 => Channels.LS7,
            XivChatType.Ls8 => Channels.LS8,
            XivChatType.CrossLinkShell1 => Channels.CWL1,
            XivChatType.CrossLinkShell2 => Channels.CWL2,
            XivChatType.CrossLinkShell3 => Channels.CWL3,
            XivChatType.CrossLinkShell4 => Channels.CWL4,
            XivChatType.CrossLinkShell5 => Channels.CWL5,
            XivChatType.CrossLinkShell6 => Channels.CWL6,
            XivChatType.CrossLinkShell7 => Channels.CWL7,
            XivChatType.CrossLinkShell8 => Channels.CWL8,
            _ => null
        };
    }

    /// <summary> This method is used to get the order of the enum, which is then given to getOrderedChannels. </summary>
    private static int GetOrder(Channels channel)
    {
        // get the attribute of the channel
        var attribute = channel.GetType()
            .GetField(channel.ToString())
            ?.GetCustomAttributes(typeof(EnumOrderAttribute), false)
            .FirstOrDefault() as EnumOrderAttribute;
        // return the order of the channel, or if it doesnt have one, return the max value
        return attribute?.Order ?? int.MaxValue;
    }
}
