using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.Gui.Components;

/// <summary>
/// The base for the draw folder, which is a dropdown section in the list of paired users, and handles the basic draw functionality
/// </summary>
public class DrawCollarRequests : IRequestsFolder
{
    private IEnumerable<DrawCollarRequest> _outgoingItems;
    private IEnumerable<DrawCollarRequest> _incomingItems;
    private bool _viewingOutgoing = false;
    private bool _wasHovered = false;
    private bool _isExpanded = false;

    public string ID { get; init; }
    public int TotalOutgoing => _outgoingItems.Count();
    public int TotalIncoming => _incomingItems.Count();

    public DrawCollarRequests(string tag, IImmutableList<DrawCollarRequest> incRequests,
        IImmutableList<DrawCollarRequest> outRequests)
    {
        ID = tag;
        _outgoingItems = outRequests;
        _incomingItems = incRequests;
        _viewingOutgoing = TotalOutgoing > 0 ? _viewingOutgoing : TotalIncoming > 0;
    }

    public void Draw()
    {
        if (!_outgoingItems.Any() && !_incomingItems.Any())
            return;

        // Begin drawing out the header section for the requests folder dropdown thingy.
        using var id = ImRaii.PushId("folder_" + ID);
        var childSize = new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight());
        using (CkRaii.Child("folder__" + ID, childSize, _wasHovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0))
        {
            CkGui.InlineSpacingInner();
            CkGui.FramedIconText(_isExpanded ? FAI.CaretDown : FAI.CaretRight);

            ImGui.SameLine();
            CkGui.FramedIconText(FAI.Inbox);
            ImGui.SameLine();
            var inboxPos = ImGui.GetCursorPosX();
            DrawViewTypeSelection();

            ImGui.SameLine(inboxPos);
            using (ImRaii.PushFont(UiBuilder.MonoFont))
                ImUtf8.TextFrameAligned($"{(_viewingOutgoing ? "Outgoing" : "Incoming")} Request{((_viewingOutgoing ? TotalOutgoing : TotalIncoming) == 1 ? "" : "s")}");

        }
        _wasHovered = ImGui.IsItemHovered();
        if (ImGui.IsItemClicked())
            _isExpanded = !_isExpanded;

        ImGui.Separator();
        // if the folder is opened, draw the relevant list.
        if (!_isExpanded)
            return;

        _viewingOutgoing = TotalOutgoing > 0 ? _viewingOutgoing : TotalIncoming > 0;
        var requests = _viewingOutgoing ? _outgoingItems : _incomingItems;

        using var indent = ImRaii.PushIndent(CkGui.IconSize(FAI.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);
        foreach (var entry in requests)
            entry.DrawRequestEntry(_viewingOutgoing);

        ImGui.Separator();
    }

    private void DrawViewTypeSelection()
    {
        var icon = _viewingOutgoing ? FAI.PersonArrowUpFromLine : FAI.PersonArrowDownToLine;
        var text = _viewingOutgoing ? $"View Incoming ({TotalIncoming})" : $"View Outgoing ({TotalOutgoing})";
        var toolTip = $"Switch the list to display {(_viewingOutgoing ? "incoming" : "outgoing")} requests";
        var buttonSize = CkGui.IconTextButtonSize(icon, text);
        var spacingX = ImGui.GetStyle().ItemSpacing.X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();

        var disabled = _viewingOutgoing ? TotalIncoming is 0 : TotalOutgoing is 0;
        var rightSideStart = windowEndX - (buttonSize + spacingX);
        ImGui.SameLine(windowEndX - buttonSize);

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            if (CkGui.IconTextButton(icon, text, null, true, disabled))
                _viewingOutgoing = !_viewingOutgoing;
        CkGui.AttachToolTip(disabled ? "There are 0 entries here!" : toolTip);
    }
}
