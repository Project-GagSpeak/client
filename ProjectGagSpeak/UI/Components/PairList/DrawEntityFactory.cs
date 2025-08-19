using GagSpeak.Gui.Components;
using GagSpeak.Gui.Handlers;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Network;
using System.Collections.Immutable;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;

namespace GagSpeak.Gui;

public class DrawEntityFactory
{
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly ClientData _clientData;
    private readonly ServerConfigManager _configs;
    private readonly IdDisplayHandler _nameDisplay;

    public DrawEntityFactory(GagspeakMediator mediator, MainHub hub, ClientData clientData, 
        ServerConfigManager configs, IdDisplayHandler nameDisplay)
    {
        _mediator = mediator;
        _hub = hub;
        _clientData = clientData;
        _configs = configs;
        _nameDisplay = nameDisplay;
    }

    public DrawFolderTag CreateDrawTagFolder(string tag, List<Kinkster> filteredPairs, IImmutableList<Kinkster> allPairs)
        => new DrawFolderTag(tag, filteredPairs.Select(u => CreateDrawPair(tag, u)).ToImmutableList(), allPairs, _configs);

    public DrawKinksterRequests CreatePairRequestFolder(string tag)
        => new DrawKinksterRequests(tag,
            _clientData.ReqPairIncoming.Select(r => CreateDrawPairRequest(tag, r)).ToImmutableList(),
            _clientData.ReqPairOutgoing.Select(r => CreateDrawPairRequest(tag, r)).ToImmutableList());

    public DrawCollarRequests CreateCollarRequestFolder(string tag)
        => new DrawCollarRequests(tag,
            _clientData.ReqCollarIncoming.Select(r => CreateDrawCollarRequest(tag, r)).ToImmutableList(),
            _clientData.ReqCollarOutgoing.Select(r => CreateDrawCollarRequest(tag, r)).ToImmutableList());

    public DrawUserPair CreateDrawPair(string id, Kinkster kinkster)
        => new DrawUserPair(id + kinkster.UserData.UID, kinkster, _mediator, _hub, _nameDisplay);

    public DrawKinksterRequest CreateDrawPairRequest(string id, KinksterPairRequest request)
        => new DrawKinksterRequest(id, request, _hub);

    public DrawCollarRequest CreateDrawCollarRequest(string id, CollarOwnershipRequest request)
        => new DrawCollarRequest(id, request, _hub);
}
