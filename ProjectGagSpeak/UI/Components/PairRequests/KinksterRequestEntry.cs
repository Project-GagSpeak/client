using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.UserPair;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.UserPairList;

/// <summary>
/// Class handling the draw function for a singular user pair that the client has. (one row)
/// </summary>
public class KinksterRequestEntry
{
    private readonly string _id;
    private UserPairRequestDto _requestEntry;
    private readonly MainHub _apiHubMain;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    private bool IsHovered = false;
    public KinksterRequestEntry(string id, UserPairRequestDto requestEntry, 
        MainHub apiHubMain, CosmeticService cosmetics, UiSharedService uiShared)
    {
        _id = id;
        _requestEntry = requestEntry;
        _apiHubMain = apiHubMain;
        _cosmetics = cosmetics;
        _uiShared = uiShared;

        _viewingMode = requestEntry.User.UID == MainHub.UID ? DrawRequestsType.Outgoing : DrawRequestsType.Incoming;
    }

    private DrawRequestsType _viewingMode = DrawRequestsType.Outgoing;
    public UserPairRequestDto Request => _requestEntry;
    private TimeSpan TimeLeft => TimeSpan.FromDays(3) - (DateTime.UtcNow - _requestEntry.CreationTime);
    public void DrawRequestEntry()
    {
        using var id = ImRaii.PushId(GetType() + _id);
        using (ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), IsHovered))
        {
            // Draw the main component of the request entry
            using (ImRaii.Child(GetType() + _id, new Vector2(UiSharedService.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight())))
            {
                // draw here the left side icon and the name that follows it.
                ImUtf8.SameLineInner();
                DrawLeftSide();
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();

                var kinksterIdTag = _viewingMode is DrawRequestsType.Outgoing
                    ? _requestEntry.RecipientUser.UID.Substring(_requestEntry.RecipientUser.UID.Length - 3)
                    : _requestEntry.User.UID.Substring(_requestEntry.User.UID.Length - 3);

                using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted("Kinkster-" + kinksterIdTag);

                // draw the right side based on the entry type.
                if (_viewingMode == DrawRequestsType.Outgoing)
                    DrawPendingCancel();
                else
                    DrawAcceptReject();
            }
            // if the panel was hovered, show it as hovered.
            IsHovered = ImGui.IsItemHovered();
        }
    }

    private void DrawLeftSide()
    {
        ImGui.AlignTextToFramePadding();
        _uiShared.IconText(FontAwesomeIcon.QuestionCircle, ImGuiColors.DalamudYellow);
        var displayText = "Request Expires in " + TimeLeft.Days + "d " + TimeLeft.Hours + "h " + TimeLeft.Minutes + "m.";
        if(!_requestEntry.AttachedMessage.IsNullOrWhitespace()) displayText += "--SEP----COL--Message: --COL--" + _requestEntry.AttachedMessage;
        UiSharedService.AttachToolTip(displayText, color: ImGuiColors.TankBlue);
        ImGui.SameLine();
    }

    private void DrawAcceptReject()
    {
        var acceptButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.PersonCircleCheck, "Accept");
        var rejectButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.PersonCircleXmark, "Reject");
        var spacingX = ImGui.GetStyle().ItemSpacing.X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - acceptButtonSize;

        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen))
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.PersonCircleCheck, "Accept", null, true))
                _apiHubMain.UserAcceptIncPairRequest(new(_requestEntry.User)).ConfigureAwait(false);
        }
        UiSharedService.AttachToolTip("Accept the Request");

        currentRightSide -= acceptButtonSize + spacingX;
        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.PersonCircleXmark, "Reject", null, true))
                _apiHubMain.UserRejectIncPairRequest(new(_requestEntry.User)).ConfigureAwait(false);
        }
        UiSharedService.AttachToolTip("Reject the Request");
    }

    private void DrawPendingCancel()
    {
        var cancelButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.PersonCircleXmark, "Cancel Request");
        var spacingX = ImGui.GetStyle().ItemSpacing.X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - cancelButtonSize;

        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.PersonCircleXmark, "Cancel Request", null, true))
                _apiHubMain.UserCancelPairRequest(new(_requestEntry.RecipientUser)).ConfigureAwait(false);
        }
        UiSharedService.AttachToolTip("Remove the pending request from both yourself and the pending Kinksters list.");
    }
}
