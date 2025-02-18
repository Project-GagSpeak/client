using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.VibeLobby;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Globalization;
using System.Numerics;

namespace GagSpeak.UI.UiToybox;

public class ToyboxVibeRooms : DisposableMediatorSubscriberBase
{
    private readonly VibeRoomManager _roomManager;
    private readonly UiSharedService _uiShared;
    private readonly PairManager _pairManager;
    private readonly GagspeakConfigService _configService;
    private readonly ServerConfigService _serverConfigs;

    public ToyboxVibeRooms(ILogger<ToyboxVibeRooms> logger,
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

    public void DrawVibeServerPanel()
    {
        UiSharedService.ColorTextCentered("Currently Under Construction!", ImGuiColors.DalamudRed);
    }
}
