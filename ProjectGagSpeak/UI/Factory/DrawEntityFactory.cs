using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components.UserPairList;
using GagSpeak.UI.Handlers;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.UserPair;
using System.Collections.Immutable;

namespace GagSpeak.UI;

public class DrawEntityFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly ServerConfigurationManager _configs;
    private readonly IdDisplayHandler _nameDisplay;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    public DrawEntityFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator, MainHub hub,
        ServerConfigurationManager configs, IdDisplayHandler nameDisplay, CosmeticService cosmetics,
        UiSharedService uiShared)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _hub = hub;
        _configs = configs;
        _nameDisplay = nameDisplay;
        _cosmetics = cosmetics;
        _uiShared = uiShared;
    }
    public DrawFolderTag CreateDrawTagFolder(string tag, List<Pair> filteredPairs, IImmutableList<Pair> allPairs)
        => new DrawFolderTag(tag, filteredPairs.Select(u => CreateDrawPair(tag, u)).ToImmutableList(), allPairs,
            _configs, _uiShared);

    public DrawUserPair CreateDrawPair(string id, Pair user)
        => new DrawUserPair(_loggerFactory.CreateLogger<DrawUserPair>(), id + user.UserData.UID,
            user, _hub, _nameDisplay, _mediator, _cosmetics, _uiShared);

    public KinksterRequestEntry CreateKinsterRequest(string id, UserPairRequestDto request)
        => new KinksterRequestEntry(id, request, _hub, _cosmetics, _uiShared);
}
