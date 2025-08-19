using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.WebAPI;
using GagspeakAPI.Network;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;
public class DrawKinksterRequest
{
    private readonly MainHub _hub;
    private readonly string _id;

    private KinksterPairRequest _entry;
    private bool _hovered = false;
    public DrawKinksterRequest(string id, KinksterPairRequest requestEntry, MainHub hub)
    {
        _id = id;
        _entry = requestEntry;
        _hub = hub;
    }

    public void DrawRequestEntry(bool isOutgoing)
    {
        using var id = ImRaii.PushId(GetType() + _id);
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight());
        using (CkRaii.Child(GetType() + _id, size, _hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0))
        {
            // draw here the left side icon and the name that follows it.
            ImUtf8.SameLineInner();
            DrawLeftSide();

            using (ImRaii.PushFont(UiBuilder.MonoFont))
                CkGui.TextFrameAlignedInline("Kinkster-" + (isOutgoing
                ? _entry.Target.UID.Substring(_entry.Target.UID.Length - 4)
                : _entry.User.UID.Substring(_entry.User.UID.Length - 4)));

            // draw the right side based on the entry type.
            if (isOutgoing)
                DrawPendingCancel();
            else
                DrawAcceptReject();
        }
        _hovered = ImGui.IsItemHovered();
    }

    private void DrawLeftSide()
    {
        CkGui.FramedIconText(FAI.QuestionCircle, ImGuiColors.DalamudYellow);
        var timeLeft = _entry.TimeLeft();
        var displayText = $"Expires in {timeLeft.Days}d {timeLeft.Hours}h {timeLeft.Minutes}m.";
        if (_entry.Message.Length > 0) displayText += $" --SEP----COL--Message: --COL--{_entry.Message}";
        CkGui.AttachToolTip(displayText, color: ImGuiColors.TankBlue);
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
            if (CkGui.IconTextButton(FAI.PersonCircleCheck, "Accept", null, true))
                _hub.UserAcceptKinksterRequest(new(_entry.User)).ConfigureAwait(false);
        CkGui.AttachToolTip("Accept the Request");

        currentRightSide -= acceptButtonSize + spacingX;
        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
            if (CkGui.IconTextButton(FAI.PersonCircleXmark, "Reject", null, true))
                _hub.UserRejectKinksterRequest(new(_entry.User)).ConfigureAwait(false);
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
            if (CkGui.IconTextButton(FAI.PersonCircleXmark, "Cancel Request", null, true))
                _hub.UserCancelKinksterRequest(new(_entry.Target)).ConfigureAwait(false);
        CkGui.AttachToolTip("Remove the pending request from both yourself and the pending Kinksters list.");
    }
}
