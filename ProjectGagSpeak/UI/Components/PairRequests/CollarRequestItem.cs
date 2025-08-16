using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.WebAPI;
using GagspeakAPI.Network;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

/// <summary>
/// Class handling the draw function for a singular user pair that the client has. (one row)
/// </summary>
public class CollarRequestItem
{
    private readonly string _id;
    private CollarRequest _entry;
    private readonly MainHub _hub;

    private bool IsHovered = false;
    public CollarRequestItem(string id, CollarRequest entry, MainHub hub)
    {
        _id = id;
        _entry = entry;
        _hub = hub;

        _viewingMode = _entry.User.UID.Equals(MainHub.UID) ? DrawRequestsType.Outgoing : DrawRequestsType.Incoming;
    }

    private DrawRequestsType _viewingMode = DrawRequestsType.Outgoing;
    public CollarRequest Request => _entry;
    private TimeSpan TimeLeft => TimeSpan.FromDays(3) - (DateTime.UtcNow - _entry.CreationTime);
    public void DrawRequestEntry()
    {
        using var id = ImRaii.PushId(GetType() + _id);
        using (ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), IsHovered))
        {
            // Draw the main component of the request entry
            using (ImRaii.Child(GetType() + _id, new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight())))
            {
                // draw here the left side icon and the name that follows it.
                ImUtf8.SameLineInner();
                DrawLeftSide();
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();

                var kinksterIdTag = _viewingMode is DrawRequestsType.Outgoing
                    ? _entry.Target.UID.Substring(_entry.Target.UID.Length - 4)
                    : _entry.User.UID.Substring(_entry.User.UID.Length - 4);

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
        CkGui.FramedIconText(FAI.QuestionCircle, ImGuiColors.DalamudYellow);
        CkGui.AttachToolTip($"Expires in {TimeLeft.Days}d {TimeLeft.Hours}h {TimeLeft.Minutes}m.", color: ImGuiColors.TankBlue);
        ImGui.SameLine();
    }

    private void DrawAcceptReject()
    {
        var acceptButtonSize = CkGui.IconTextButtonSize(FAI.PersonCircleCheck, "Accept");
        var rejectButtonSize = CkGui.IconTextButtonSize(FAI.PersonCircleXmark, "Reject");
        var spacingX = ImGui.GetStyle().ItemSpacing.X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - acceptButtonSize;

        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen))
        {
            if (CkGui.IconTextButton(FAI.PersonCircleCheck, "Accept", null, true))
                _hub.UserAcceptKinksterRequest(new(_entry.User)).ConfigureAwait(false);
        }
        CkGui.AttachToolTip("Accept the Request");

        currentRightSide -= acceptButtonSize + spacingX;
        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
        {
            if (CkGui.IconTextButton(FAI.PersonCircleXmark, "Reject", null, true))
                _hub.UserRejectKinksterRequest(new(_entry.User)).ConfigureAwait(false);
        }
        CkGui.AttachToolTip("Reject the Request");
    }

    private void DrawPendingCancel()
    {
        var cancelButtonSize = CkGui.IconTextButtonSize(FAI.PersonCircleXmark, "Cancel Request");
        var spacingX = ImGui.GetStyle().ItemSpacing.X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - cancelButtonSize;

        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
        {
            if (CkGui.IconTextButton(FAI.PersonCircleXmark, "Cancel Request", null, true))
                _hub.UserCancelKinksterRequest(new(_entry.Target)).ConfigureAwait(false);
        }
        CkGui.AttachToolTip("Remove the pending request from both yourself and the pending Kinksters list.");
    }
}
