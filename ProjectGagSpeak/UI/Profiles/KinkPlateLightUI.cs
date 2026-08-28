using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.User;

namespace GagSpeak.Gui.Profile;

public class KinkPlateLightUI : WindowMediatorSubscriberBase
{
    private readonly KinkPlateLight _lightUI;
    private readonly KinkPlateService _kinkplates;
    private readonly KinksterManager _pairManager;
    private bool _showFullUID;

    public KinkPlateLightUI(ILogger<KinkPlateLightUI> logger, GagspeakMediator mediator,
        KinkPlateLight plateLightUi, KinkPlateService KinkPlateManager,
        KinksterManager pairManager, UserData user) : base(logger, mediator, "###KinkPlateLight" + user.UID)
    {
        _lightUI = plateLightUi;
        _kinkplates = KinkPlateManager;
        _pairManager = pairManager;


        Flags = WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoTitleBar;
        Size = new(288, 576);
        IsOpen = true;
        ForceMainWindow = true;

        _showFullUID = _pairManager.DirectPairs.Any(x => x.User.UID == user.UID) || user.UID == MainHub.UID;
        UserDataToDisplay = user;
    }

    public UserData UserDataToDisplay { get; init; }
    private bool HoveringCloseButton = false;
    private bool HoveringReportButton = false;

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 35f * ImGuiHelpers.GlobalScale);
        base.PreDraw();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        base.PostDraw();
    }

    protected override void DrawInternal()
    {
        // do not display if pair is null.
        if (UserDataToDisplay is null)
            return;

        // obtain the profile for this userPair.
        var KinkPlate = _kinkplates.GetUserProfile(UserDataToDisplay);

        var DisplayName = _showFullUID
            ? UserDataToDisplay.AliasOrUID
            : "Kinkster-" + UserDataToDisplay.UID.Substring(UserDataToDisplay.UID.Length - 4);

        #if DEBUG
        DisplayName = UserDataToDisplay.UID;
        #endif

        var drawList = ImGui.GetWindowDrawList();
        // clip based on the region of our draw space.
        _lightUI.RectMin = drawList.GetClipRectMin();
        _lightUI.RectMax = drawList.GetClipRectMax();

        // draw the plate.
        HoveringReportButton = _lightUI.DrawKinkPlateLight(drawList, KinkPlate, DisplayName, UserDataToDisplay, _showFullUID, HoveringReportButton);
        
        // Draw the close button.
        CloseButton(drawList, DisplayName);
        CkGui.AttachToolTipRect(_lightUI.CloseButtonPos, _lightUI.CloseButtonSize, "Close " + DisplayName + "'s KinkPlate™");
    }

    private void CloseButton(ImDrawListPtr drawList, string displayName)
    {
        var btnPos = _lightUI.CloseButtonPos;
        var btnSize = _lightUI.CloseButtonSize;

        var closeButtonColor = HoveringCloseButton ? ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)) : ImGui.GetColorU32(ImGuiColors.ParsedPink);

        drawList.AddLine(btnPos, btnPos + btnSize, closeButtonColor, 3 * ImGuiHelpers.GlobalScale);
        drawList.AddLine(new Vector2(btnPos.X + btnSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + btnSize.Y), closeButtonColor, 3 * ImGuiHelpers.GlobalScale);

        ImGui.SetCursorScreenPos(btnPos);
        if (ImGui.InvisibleButton($"CloseButton##KinkPlateClose" + displayName, btnSize))
            this.IsOpen = false;

        HoveringCloseButton = ImGui.IsItemHovered();
    }

    public override void OnClose()
    {
        // remove profile on close if not in our direct pairs.
        if (_showFullUID is false)
            Mediator.Publish(new ClearUserProfileMessage(UserDataToDisplay));
        // destroy the window.        
        Mediator.Publish(new RemoveCreatedWindowMessage(this));
    }
}
