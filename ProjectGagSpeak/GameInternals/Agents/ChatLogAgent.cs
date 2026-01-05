using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Immutable;

namespace GagSpeak.GameInternals.Agents;

public static class ChatLogAgent
{
    // this is the agent that handles the chatlog
    // private static unsafe AgentChatLog* ChatlogAgent => (AgentChatLog*)Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.ChatLog);

    public unsafe static InputChannel CurrentChannel() => (InputChannel)AgentChatLog.Instance()->CurrentChannel;
    public unsafe static Utf8String CurrentChannelLabel() => AgentChatLog.Instance()->ChannelLabel;
    public unsafe static Utf8String TellPlayerName() => AgentChatLog.Instance()->TellPlayerName;
    public unsafe static ushort TellPlayerWorld() => AgentChatLog.Instance()->TellWorldId;

    // Can add localization here later.
    public static IEnumerable<(string, InputChannel[])> SortedChannels =
    [
        ("General Channels",
        [
            InputChannel.Tell, InputChannel.Say, InputChannel.Party, InputChannel.Alliance,
            InputChannel.Yell, InputChannel.Shout, InputChannel.FreeCompany, InputChannel.Echo,
        ]),

        ("Linkshells",
        [
            InputChannel.LS1, InputChannel.LS2, InputChannel.LS3, InputChannel.LS4,
            InputChannel.LS5, InputChannel.LS6, InputChannel.LS7, InputChannel.LS8
        ]),

        ("Cross World Linkshells",
        [
            InputChannel.CWL1, InputChannel.CWL2, InputChannel.CWL3, InputChannel.CWL4,
            InputChannel.CWL5, InputChannel.CWL6, InputChannel.CWL7, InputChannel.CWL8
        ]),
    ];

    /// <summary>
    ///     Personal Lookup Table.
    /// </summary>
    public static readonly ImmutableDictionary<string, InputChannel> PrefixToChannel = ImmutableDictionary<string, InputChannel>.Empty
        .Add("/t", InputChannel.Tell)
        .Add("/tell", InputChannel.Tell)
        .Add("/s", InputChannel.Say)
        .Add("/say", InputChannel.Say)
        .Add("/p", InputChannel.Party)
        .Add("/party", InputChannel.Party)
        .Add("/a", InputChannel.Alliance)
        .Add("/alliance", InputChannel.Alliance)
        .Add("/y", InputChannel.Yell)
        .Add("/yell", InputChannel.Yell)
        .Add("/sh", InputChannel.Shout)
        .Add("/shout", InputChannel.Shout)
        .Add("/fc", InputChannel.FreeCompany)
        .Add("/freecompany", InputChannel.FreeCompany)
        .Add("/echo", InputChannel.Echo)
        .Add("/cwl1", InputChannel.CWL1)
        .Add("/cwl2", InputChannel.CWL2)
        .Add("/cwl3", InputChannel.CWL3)
        .Add("/cwl4", InputChannel.CWL4)
        .Add("/cwl5", InputChannel.CWL5)
        .Add("/cwl6", InputChannel.CWL6)
        .Add("/cwl7", InputChannel.CWL7)
        .Add("/cwl8", InputChannel.CWL8)
        .Add("/l1", InputChannel.LS1)
        .Add("/linkshell1", InputChannel.LS1)
        .Add("/l2", InputChannel.LS2)
        .Add("/linkshell2", InputChannel.LS2)
        .Add("/l3", InputChannel.LS3)
        .Add("/linkshell3", InputChannel.LS3)
        .Add("/l4", InputChannel.LS4)
        .Add("/linkshell4", InputChannel.LS4)
        .Add("/l5", InputChannel.LS5)
        .Add("/linkshell5", InputChannel.LS5)
        .Add("/l6", InputChannel.LS6)
        .Add("/linkshell6", InputChannel.LS6)
        .Add("/l7", InputChannel.LS7)
        .Add("/linkshell7", InputChannel.LS7)
        .Add("/l8", InputChannel.LS8)
        .Add("/linkshell8", InputChannel.LS8);

    /// <summary>
    ///     Get the chat channel type from the XIVChatType
    /// </summary>
    public static InputChannel? FromXivChatType(XivChatType type)
    {
        return type switch
        {
            XivChatType.TellIncoming => InputChannel.Tell,
            XivChatType.TellOutgoing => InputChannel.Tell,
            XivChatType.Say => InputChannel.Say,
            XivChatType.Party => InputChannel.Party,
            XivChatType.Alliance => InputChannel.Alliance,
            XivChatType.Yell => InputChannel.Yell,
            XivChatType.Shout => InputChannel.Shout,
            XivChatType.FreeCompany => InputChannel.FreeCompany,
            XivChatType.NoviceNetwork => InputChannel.NoviceNetwork,
            XivChatType.Ls1 => InputChannel.LS1,
            XivChatType.Ls2 => InputChannel.LS2,
            XivChatType.Ls3 => InputChannel.LS3,
            XivChatType.Ls4 => InputChannel.LS4,
            XivChatType.Ls5 => InputChannel.LS5,
            XivChatType.Ls6 => InputChannel.LS6,
            XivChatType.Ls7 => InputChannel.LS7,
            XivChatType.Ls8 => InputChannel.LS8,
            XivChatType.CrossLinkShell1 => InputChannel.CWL1,
            XivChatType.CrossLinkShell2 => InputChannel.CWL2,
            XivChatType.CrossLinkShell3 => InputChannel.CWL3,
            XivChatType.CrossLinkShell4 => InputChannel.CWL4,
            XivChatType.CrossLinkShell5 => InputChannel.CWL5,
            XivChatType.CrossLinkShell6 => InputChannel.CWL6,
            XivChatType.CrossLinkShell7 => InputChannel.CWL7,
            XivChatType.CrossLinkShell8 => InputChannel.CWL8,
            XivChatType.Echo => InputChannel.Echo,
            _ => null
        };
    }

    public static bool IsPrefixForGsChannel(string message, out string prefix, out InputChannel channel)
    {
        var spaceIdx = message.IndexOf(' ');
        var firstWord = spaceIdx == -1 ? message : message[..spaceIdx];
        if (PrefixToChannel.TryGetValue(firstWord, out channel))
        {
            prefix = firstWord;
            return true;
        }

        prefix = string.Empty;
        channel = 0;
        return false;
    }
}
