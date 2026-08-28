using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagspeakAPI.Chat;
using GagspeakAPI.User;

namespace GagSpeak.Services;

// Factory for SanctionChatLogs
public class ChatFactory
{
    private readonly ILoggerFactory _logFactory;
    private readonly ChatConfig _chatConfig;
    private readonly ChatColors _chatColors;
    private readonly KinksterManager _kinksters;
    private readonly BlockService _blockService;
    private readonly PairService _pairService;

    public ChatFactory(ILoggerFactory loggerFactory, ChatConfig chatConfig,
        ChatColors chatColors, KinksterManager kinksters, BlockService blockService,
        PairService pairService)
    {
        _logFactory = loggerFactory;
        _chatConfig = chatConfig;
        _chatColors = chatColors;
        _kinksters = kinksters;
        _blockService = blockService;
        _pairService = pairService;
    }

    public DMChatLog CreateDMChat(UserData otherUser, ChatlogId id, int capacity)
        => new(otherUser, id, capacity, _logFactory.CreateLogger<DMChatLog>(), _chatConfig,
            _chatColors, _kinksters, _blockService, _pairService);
}
