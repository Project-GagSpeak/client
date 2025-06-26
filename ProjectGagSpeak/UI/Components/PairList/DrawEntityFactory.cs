using GagSpeak.Gui.Components;
using GagSpeak.Gui.Handlers;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Network;
using System.Collections.Immutable;
using GagSpeak.Kinksters;

namespace GagSpeak.Gui;

public class DrawEntityFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly ServerConfigManager _configs;
    private readonly IdDisplayHandler _nameDisplay;
    private readonly CosmeticService _cosmetics;

    public DrawEntityFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator, MainHub hub,
        ServerConfigManager configs, IdDisplayHandler nameDisplay, CosmeticService cosmetics)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _hub = hub;
        _configs = configs;
        _nameDisplay = nameDisplay;
        _cosmetics = cosmetics;
    }

    public DrawFolderTag CreateDrawTagFolder(string tag, List<Kinkster> filteredPairs, IImmutableList<Kinkster> allPairs)
        => new DrawFolderTag(tag, filteredPairs.Select(u => CreateDrawPair(tag, u)).ToImmutableList(), allPairs, _configs);

    public DrawUserPair CreateDrawPair(string id, Kinkster kinkster)
        => new DrawUserPair(_loggerFactory.CreateLogger<DrawUserPair>(), id + kinkster.UserData.UID,
            kinkster, _hub, _nameDisplay, _mediator, _cosmetics);

    public KinksterPairRequest CreateKinsterRequest(string id, KinksterRequestEntry request)
        => new KinksterPairRequest(id, request, _hub, _cosmetics);
}
