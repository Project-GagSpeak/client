using CkCommons.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using CkCommons.Raii;

namespace GagSpeak.Gui.Components;

/// <summary>
/// The base for the draw folder, which is a dropdown section in the list of paired users, and handles the basic draw functionality
/// </summary>
public class DrawCollarRequests : IRequestsFolder
{
    private readonly ClientData _clientData;
    private readonly DrawEntityFactory _requestFactory;

    private bool _wasHovered = false;
    private bool _isRequestFolderOpen = false;
    private DrawRequestsType _viewingMode = DrawRequestsType.Outgoing;
    public string ID => "Kinkster_Requests";

    // The Kinkster Requests currently present.
    private IEnumerable<CollarRequestItem> _allOutgoingRequests;
    private IEnumerable<CollarRequestItem> _allIncomingRequests;

    public int TotalOutgoing => _clientData.CollarRequestsOutgoing.Count();
    public int TotalIncoming => _clientData.CollarRequestsIncoming.Count();

    public DrawCollarRequests(ClientData clientData, DrawEntityFactory factory)
    {
        _clientData = clientData;
        _requestFactory = factory;
    }

    public void Draw()
    {
        if (_clientData.HasCollarRequests is false)
            return;

        // Begin drawing out the header section for the requests folder dropdown thingy.
        using var id = ImRaii.PushId("folder_" + ID);
        var childSize = new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight());
        using (CkRaii.Child("folder__" + ID, childSize, _wasHovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0))
        {
            CkGui.InlineSpacingInner();
            CkGui.FramedIconText(_isRequestFolderOpen ? FAI.CaretDown : FAI.CaretRight);

            ImGui.SameLine();
            var folderIconEndPos = DrawFolderIcon();
            ImGui.SameLine();
            DrawViewTypeSelection();

            // draw name
            ImGui.SameLine(folderIconEndPos);
            using (ImRaii.PushFont(UiBuilder.MonoFont)) 
                ImUtf8.TextFrameAligned(_viewingMode is DrawRequestsType.Outgoing
                    ? $"Outgoing Request{(TotalOutgoing > 1 ? "Requests" : "Request")}"
                    : $"Incoming Request{(TotalIncoming > 1 ? "Requests" : "Request")}");
        }
        _wasHovered = ImGui.IsItemHovered();
        if (ImGui.IsItemClicked()) 
            _isRequestFolderOpen = !_isRequestFolderOpen;

        ImGui.Separator();
        // if the folder is opened, draw the relevant list.
        if (!_isRequestFolderOpen)
            return;

        // ensure correct tabbo is opened.
        if (_viewingMode is DrawRequestsType.Outgoing && TotalOutgoing is 0)
            _viewingMode = DrawRequestsType.Incoming;
        else if (_viewingMode is DrawRequestsType.Incoming && TotalIncoming is 0)
            _viewingMode = DrawRequestsType.Outgoing;

        var requests = _viewingMode is DrawRequestsType.Outgoing ? _allOutgoingRequests : _allIncomingRequests;

        using var indent = ImRaii.PushIndent(CkGui.IconSize(FAI.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);
        foreach (var entry in _allIncomingRequests)
            entry.DrawRequestEntry();

        ImGui.Separator();
    }

    /// <summary>
    /// Grabs the latest kinkster request entries on each pair list update or request update recieved.
    /// </summary>
    public void UpdateKinksterRequests()
    {
        // I think this is all i need? Idk i guess we will find out or something lol.
        _allOutgoingRequests = _clientData.CollarRequestsOutgoing
            .Select(request => _requestFactory.CreateDrawCollarRequest($"outgoing-{request.Target.UID}", request));
        _allIncomingRequests = _clientData.CollarRequestsIncoming
            .Select(request => _requestFactory.CreateDrawCollarRequest($"incoming-{request.User.UID}", request));

        // if there are no outgoing, and we are on outgoing, switch it to incoming.
        if (TotalOutgoing is 0 && _viewingMode is DrawRequestsType.Outgoing)
            _viewingMode = DrawRequestsType.Incoming;
        // if there are no incoming, and we are on incoming, switch it to outgoing.
        else if (TotalIncoming is 0 && _viewingMode is DrawRequestsType.Incoming)
            _viewingMode = DrawRequestsType.Outgoing;
    }


    private float DrawFolderIcon()
    {
        ImGui.AlignTextToFramePadding();
        CkGui.IconText(FAI.Inbox);
        ImGui.SameLine();
        return ImGui.GetCursorPosX();
    }
    private void DrawViewTypeSelection()
    {
        var viewingOutgoing = _viewingMode is DrawRequestsType.Outgoing;
        var icon = viewingOutgoing ? FAI.PersonArrowUpFromLine : FAI.PersonArrowDownToLine;
        var text = viewingOutgoing ? "View Incoming (" + TotalIncoming + ")" : "View Outgoing (" + TotalOutgoing + ")";
        var toolTip = viewingOutgoing ? "Switch the list to display Incoming Requests" : "Switch the list to display Outgoing Requests";
        var buttonSize = CkGui.IconTextButtonSize(icon, text);
        var spacingX = ImGui.GetStyle().ItemSpacing.X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();

        var disabled = viewingOutgoing ? TotalIncoming is 0 : TotalOutgoing is 0;
        var rightSideStart = windowEndX - (buttonSize + spacingX);
        ImGui.SameLine(windowEndX - buttonSize);

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
        {
            if (CkGui.IconTextButton(icon, text, null, true, disabled))
                _viewingMode = viewingOutgoing ? DrawRequestsType.Incoming : DrawRequestsType.Outgoing;
        }
        CkGui.AttachToolTip(disabled ? "There are 0 entries here!" : toolTip);
    }
}
