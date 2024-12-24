using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.Profile;

public class PopoutKinkPlateUi : WindowMediatorSubscriberBase
{
    private readonly KinkPlateLight _lightUI;
    private readonly KinkPlateService _KinkPlateManager;
    private readonly PairManager _pairManager;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly UiSharedService _uiShared;
    private UserData? _userDataToDisplay;
    private bool _showFullUID;

    private bool ThemePushed = false;

    public PopoutKinkPlateUi(ILogger<PopoutKinkPlateUi> logger, GagspeakMediator mediator,
        UiSharedService uiBuilder, ServerConfigurationManager serverManager,
        GagspeakConfigService gagspeakConfigService, KinkPlateLight plateLightUi,
        KinkPlateService KinkPlateManager, PairManager pairManager)
        : base(logger, mediator, "###GagSpeakPopoutProfileUI")
    {
        _lightUI = plateLightUi;
        _uiShared = uiBuilder;
        _serverConfigs = serverManager;
        _KinkPlateManager = KinkPlateManager;
        _pairManager = pairManager;
        Flags = ImGuiWindowFlags.NoDecoration;

        Mediator.Subscribe<ProfilePopoutToggle>(this, (msg) =>
        {
            IsOpen = msg.PairUserData != null; // only open if the pair sent is not null
            _showFullUID = _pairManager.DirectPairs.Any(x => x.UserData.UID == msg.PairUserData?.UID);
            _userDataToDisplay = msg.PairUserData; // set the pair to display the popout profile for.
        });

        IsOpen = false;
    }

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 35f);

            ThemePushed = true;
        }

        var position = _uiShared.LastMainUIWindowPosition;
        position.X -= 288;
        ImGui.SetNextWindowPos(position);

        Flags |= ImGuiWindowFlags.NoMove;

        var size = new Vector2(288, 576);

        ImGui.SetNextWindowSize(size);
    }
    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        // do not display if pair is null.
        if (_userDataToDisplay is null)
            return;

        // obtain the profile for this userPair.
        var KinkPlate = _KinkPlateManager.GetKinkPlate(_userDataToDisplay);

        string DisplayName = _userDataToDisplay.AliasOrUID;

        var drawList = ImGui.GetWindowDrawList();
        _lightUI.RectMin = drawList.GetClipRectMin();
        _lightUI.RectMax = drawList.GetClipRectMax();
        _lightUI.DrawKinkPlateLight(drawList, KinkPlate, DisplayName, _userDataToDisplay, true, false);
    }
}
