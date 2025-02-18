using GagSpeak.PlayerData.Pairs;
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
    private readonly MainHub _hub;
    private readonly GagspeakMediator _mediator;
    private readonly IdDisplayHandler _uidDisplayHandler;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    public DrawEntityFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator, MainHub hub,
        IdDisplayHandler uidDisplayHandler, CosmeticService cosmetics, UiSharedService uiShared)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _hub = hub;
        _uidDisplayHandler = uidDisplayHandler;
        _cosmetics = cosmetics;
        _uiShared = uiShared;
    }
    public DrawFolderTag CreateDrawTagFolder(string tag, List<Pair> filteredPairs, IImmutableList<Pair> allPairs)
        => new DrawFolderTag(tag, filteredPairs.Select(u => CreateDrawPair(tag, u)).ToImmutableList(), allPairs, _uiShared);

    public DrawUserPair CreateDrawPair(string id, Pair user)
        => new DrawUserPair(_loggerFactory.CreateLogger<DrawUserPair>(), id + user.UserData.UID,
            user, _hub, _uidDisplayHandler, _mediator, _cosmetics, _uiShared);

    public KinksterRequestEntry CreateKinsterRequest(string id, UserPairRequestDto request)
        => new KinksterRequestEntry(id, request, _hub, _cosmetics, _uiShared);
}
