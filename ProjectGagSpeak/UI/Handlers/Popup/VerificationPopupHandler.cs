using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using ImGuiNET;

namespace GagSpeak.Gui.Components;

internal class VerificationPopupHandler : IPopupHandler
{
    private string _verificationCode = string.Empty;

    public VerificationPopupHandler()
    { }

    public Vector2 PopupSize => new(600, 160);
    public bool ShowClosed => false;
    public bool CloseHovered { get; set; } = false;
    public Vector2? WindowPadding => null;
    public float? WindowRounding => null;

    public void DrawContent()
    {
        var width = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
        // push the title for the popup.
        using (UiFontService.UidFont.Push())
        {
            var headerTextSize = ImGui.CalcTextSize("Verification Code for " + MainHub.DisplayName);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - (headerTextSize.X / 2));
            CkGui.TextWrapped("Verification Code for " + MainHub.DisplayName);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGuiHelpers.GlobalScale * 5);
            ImGui.Separator();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGuiHelpers.GlobalScale * 5);

            // canter the text input so that its width is the width of the _verificationCode text + 10px, centerd in the available context.
            ImGui.SetCursorPosX(width / 2 - ((ImGui.CalcTextSize(_verificationCode).X + ImGuiHelpers.GlobalScale * 12) / 2));

            ImGui.TextColored(ImGuiColors.ParsedPink, _verificationCode);
            CkGui.CopyableDisplayText(_verificationCode, "Click to copy verification code to clipboard");
        }
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGuiHelpers.GlobalScale * 10);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGuiHelpers.GlobalScale * 10);
        ImGui.AlignTextToFramePadding();
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X / 2 - ImGui.CalcTextSize("Close Popup window &&& Be sure verification is successful before closing.").X / 2);
        using (ImRaii.Disabled(string.IsNullOrEmpty(_verificationCode)))
        {
            if (CkGui.IconTextButton(FAI.ExclamationTriangle, "Close Popup Window"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.SameLine();
        CkGui.ColorTextWrapped($"Be sure verification is successful before closing.", ImGuiColors.DalamudYellow);

    }

    public void Open(VerificationPopupMessage msg) => _verificationCode = msg.VerificationCode.Code;
}
