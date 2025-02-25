using Dalamud.Interface.Colors;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.VibeLobby;

namespace GagSpeak.UI.UiToybox;

public class VibeLobbiesPanel : DisposableMediatorSubscriberBase
{
    private readonly VibeRoomManager _roomManager;
    private readonly UiSharedService _uiShared;
    private readonly PairManager _pairManager;
    private readonly GagspeakConfigService _configService;
    private readonly ServerConfigService _serverConfigs;

    public VibeLobbiesPanel(ILogger<VibeLobbiesPanel> logger,
        GagspeakMediator mediator, VibeRoomManager roomManager,
        UiSharedService uiShared, PairManager pairManager,
        GagspeakConfigService mainConfig, ServerConfigService serverConfigs)
        : base(logger, mediator)
    {
        _roomManager = roomManager;
        _uiShared = uiShared;
        _pairManager = pairManager;
        _configService = mainConfig;
        _serverConfigs = serverConfigs;
    }

    public void DrawPanel(Vector2 remainingRegion, float selectorSize)
    {
        UiSharedService.ColorTextCentered("Currently Under Construction!", ImGuiColors.DalamudRed);
    }
}
