using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Kinksters;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.Gui.Profile;

public class PopoutKinkPlateUi : WindowMediatorSubscriberBase
{
    private readonly KinkPlateLight _lightUI;
    private readonly KinkPlateService _KinkPlateManager;
    private readonly KinksterManager _pairManager;
    private UserData? _userDataToDisplay;
    private bool ThemePushed = false;

    public PopoutKinkPlateUi(ILogger<PopoutKinkPlateUi> logger, GagspeakMediator mediator,
        KinkPlateLight plateLightUi, KinkPlateService manager, KinksterManager pairs) 
        : base(logger, mediator, "###GagSpeakPopoutProfileUI")
    {
        _lightUI = plateLightUi;
        _KinkPlateManager = manager;
        _pairManager = pairs;
        Flags = WFlags.NoDecoration;

        Mediator.Subscribe<ProfilePopoutToggle>(this, (msg) =>
        {
            IsOpen = msg.PairUserData != null; // only open if the pair sent is not null
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

        var position = MainUI.LastPos;
        position.X -= 288;
        ImGui.SetNextWindowPos(position);

        Flags |= WFlags.NoMove;

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

        var DisplayName = _userDataToDisplay.AliasOrUID;

        var drawList = ImGui.GetWindowDrawList();
        _lightUI.RectMin = drawList.GetClipRectMin();
        _lightUI.RectMax = drawList.GetClipRectMax();
        _lightUI.DrawKinkPlateLight(drawList, KinkPlate, DisplayName, _userDataToDisplay, true, false);
    }
}
