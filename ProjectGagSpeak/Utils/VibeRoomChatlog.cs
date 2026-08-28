using CkCommons.RichText;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using CkCommons.RichChat;

namespace GagSpeak.Utils;
public class VibeRoomChatlog : RichChatLog<NewGsChatMessage>, IMediatorSubscriber, IDisposable
{
    private static RichTextFilter AllowedTypes = RichTextFilter.All & ~RichTextFilter.Images;

    private readonly ILogger<VibeRoomChatlog> _logger;
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly ClientData _clientData;
    private readonly GagRestrictionManager _gags;
    private readonly VibeLobbyManager _lobbyManager;
    private readonly MufflerService _garbler;

    public VibeRoomChatlog(ILogger<VibeRoomChatlog> logger, GagspeakMediator mediator,
        MainHub hub, MainConfig config, ClientData clientData, GagRestrictionManager gags,
        VibeLobbyManager lobbyManager, MufflerService garbler) 
        : base("VibeRoom Chat", 1000)
    {
        _logger = logger;
        Mediator = mediator;
        _hub = hub;
        _config = config;
        _clientData = clientData;
        _gags = gags;
        _lobbyManager = lobbyManager;
        _garbler = garbler;

        Mediator.Subscribe<VibeRoomChatMessage>(this, AddVibeRoomMessage);
    }

    public GagspeakMediator Mediator { get; }

    void IDisposable.Dispose()
    {
        Mediator.UnsubscribeAll(this);
        GC.SuppressFinalize(this);
    }

    //public void SetAutoScroll (bool newState)
    //    => DoAutoScroll = newState;

    //// Do not reveal extra info, respect privacy!!!
    //protected override string ToTooltip(GagSpeakChatMessage message)
    //    => $"Sent @ {message.Timestamp.ToString("T", CultureInfo.CurrentCulture)}";

    // Add what we put in here soon.
    public void AddVibeRoomMessage(VibeRoomChatMessage message)
    {
        //// get the display name by polling from the current vibe lobby participants.
        //// If the user is not found do not send the message.
        //var dispName = "UNKNOWN";
        //if (message.Kinkster.Tier is CkVanityTier.KinkporiumMistress)
        //    dispName = $"Mistress Cordy";
        //// construct the chat message struct to add, and append it.
        //AddMessage(new GagSpeakChatMessage(message.Kinkster, dispName, message.Message));
    }
}
