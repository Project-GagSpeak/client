using FFXIVClientStructs.FFXIV.Common.Lua;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.State.Managers;
public class VibeLobbyManager : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;

    private List<RoomInvite> CurrentInvites = new();

    public VibeLobbyManager(ILogger<VibeLobbyManager> logger, GagspeakMediator mediator, MainHub hub)
        : base(logger, mediator)
    {
        _hub = hub;
    }

    public void CreateVibeRoom(string roomName, string roomPassword = "")
    {
        if (UiService.DisableUI)
            return;

        UiService.SetUITask(async () =>
        {
            var result = await _hub.RoomCreate(new RoomCreateRequest(roomName, VibeRoomFlags.None) { Password = roomPassword });
            if (result.ErrorCode is not GagSpeakApiEc.Success)
            {
                Logger.LogWarning($"Failed to create Vibe Lobby. ({result})");
                return;
            }
            else
            {
                Logger.LogInformation("Vibe Room created successfully.", LoggerType.VibeLobbies);
            }
        });
    }
}
