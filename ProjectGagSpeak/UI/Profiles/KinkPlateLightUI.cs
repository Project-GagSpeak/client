using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using ImGuiNET;

namespace GagSpeak.Gui.Profile;

public class KinkPlateLightUI : WindowMediatorSubscriberBase
{
    private readonly KinkPlateLight _lightUI;
    private readonly KinkPlateService _KinkPlateManager;
    private readonly KinksterManager _pairManager;
    private bool _showFullUID;

    private bool ThemePushed = false;

    public KinkPlateLightUI(ILogger<KinkPlateLightUI> logger, GagspeakMediator mediator,
        KinkPlateLight plateLightUi, KinkPlateService KinkPlateManager,
        KinksterManager pairManager, UserData pairUserData) : base(logger, mediator, "###KinkPlateLight" + pairUserData.UID)
    {
        _lightUI = plateLightUi;
        _KinkPlateManager = KinkPlateManager;
        _pairManager = pairManager;


        Flags = WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoTitleBar;
        Size = new(288, 576);
        IsOpen = true;
        ForceMainWindow = true;

        _showFullUID = _pairManager.DirectPairs.Any(x => x.UserData.UID == pairUserData.UID) || pairUserData.UID == MainHub.UID;
        UserDataToDisplay = pairUserData;
    }

    public UserData UserDataToDisplay { get; init; }
    private bool HoveringCloseButton = false;
    private bool HoveringReportButton = false;

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 35f * ImGuiHelpers.GlobalScale);

            ThemePushed = true;
        }
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
        if (UserDataToDisplay is null)
            return;

        // obtain the profile for this userPair.
        var KinkPlate = _KinkPlateManager.GetKinkPlate(UserDataToDisplay);

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
            Mediator.Publish(new ClearProfileDataMessage(UserDataToDisplay));
        // destroy the window.        
        Mediator.Publish(new RemoveWindowMessage(this));
    }
}
