using Dalamud.Bindings.ImGui;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;

namespace GagSpeak.Gui.Profile;

public class PopoutKinkPlateUi : WindowMediatorSubscriberBase
{
    private bool ThemePushed = false;

    private readonly KinkPlateLight _lightUI;
    private readonly KinkPlateService _service;

    private UserData? User = null;

    public PopoutKinkPlateUi(ILogger<PopoutKinkPlateUi> logger, GagspeakMediator mediator,
        KinkPlateLight plateLightUi, KinkPlateService service) 
        : base(logger, mediator, "###GSPopoutProfileUI")
    {
        _lightUI = plateLightUi;
        _service = service;

        Flags = WFlags.NoDecoration;

        Mediator.Subscribe<OpenKinkPlatePopout>(this, (msg) =>
        {
            IsOpen = true;
            User = msg.UserData;
        });
        Mediator.Subscribe<CloseKinkPlatePopout>(this, (msg) =>
        {
            IsOpen = false;
            User = null;
        });
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
        if (User is null)
            return;
        // obtain the profile for this userPair.
        var toDraw = _service.GetKinkPlate(User);
        var dispName = User.AliasOrUID;

        var wdl = ImGui.GetWindowDrawList();
        _lightUI.RectMin = wdl.GetClipRectMin();
        _lightUI.RectMax = wdl.GetClipRectMax();
        _lightUI.DrawKinkPlateLight(wdl, toDraw, dispName, User, true, false);
    }
}
