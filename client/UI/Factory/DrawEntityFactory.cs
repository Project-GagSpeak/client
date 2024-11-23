using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components.UserPairList;
using GagSpeak.UI.Handlers;
using GagSpeak.UI.Permissions;
using GagSpeak.WebAPI;
using System.Collections.Immutable;

namespace GagSpeak.UI;

public class DrawEntityFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MainHub _apiHubMain;
    private readonly GagspeakMediator _mediator;
    private readonly SelectPairForTagUi _pairsForTag;
    private readonly TagHandler _tagHandler;
    private readonly IdDisplayHandler _uidDisplayHandler;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    public DrawEntityFactory(ILoggerFactory loggerFactory, MainHub apiHubMain, 
        GagspeakMediator mediator, SelectPairForTagUi pairsForTag, TagHandler tagHandler, 
        IdDisplayHandler uidDisplayHandler, CosmeticService cosmetics, UiSharedService uiShared)
    {
        _loggerFactory = loggerFactory;
        _apiHubMain = apiHubMain;
        _mediator = mediator;
        _pairsForTag = pairsForTag;
        _tagHandler = tagHandler;
        _uidDisplayHandler = uidDisplayHandler;
        _cosmetics = cosmetics;
        _uiShared = uiShared;
    }
    public DrawFolderTag CreateDrawTagFolder(string tag, List<Pair> filteredPairs, IImmutableList<Pair> allPairs)
    {
        return new(tag, filteredPairs.Select(u => CreateDrawPair(tag, u)).ToImmutableList(), allPairs,
            _tagHandler, _apiHubMain, _pairsForTag, _uiShared);
    }

    public DrawUserPair CreateDrawPair(string id, Pair user)
    {
        return new DrawUserPair(_loggerFactory.CreateLogger<DrawUserPair>(), id + user.UserData.UID,
            user, _apiHubMain, _uidDisplayHandler, _mediator, _cosmetics, _uiShared);
    }
}
