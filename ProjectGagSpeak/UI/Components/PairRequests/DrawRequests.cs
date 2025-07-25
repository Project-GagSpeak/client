using CkCommons.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

/// <summary>
/// The base for the draw folder, which is a dropdown section in the list of paired users, and handles the basic draw functionality
/// </summary>
public class DrawRequests : IRequestsFolder
{
    private readonly MainHub _hub;
    private readonly KinksterRequests _clientData;
    private readonly DrawEntityFactory _pairRequestFactory;
    private readonly CosmeticService _cosmetics;

    private bool _wasHovered = false;
    private bool _isRequestFolderOpen = false;
    private DrawRequestsType _viewingMode = DrawRequestsType.Outgoing;
    public string ID => "Kinkster_Requests";

    // The Kinkster Requests currently present.
    private HashSet<KinksterPairRequest> _allOutgoingRequests;
    private HashSet<KinksterPairRequest> _allIncomingRequests;

    public bool HasRequests => _clientData.CurrentRequests.Any();
    public int TotalOutgoing => _clientData.OutgoingRequests.Count();
    public int TotalIncoming => _clientData.IncomingRequests.Count();

    public DrawRequests(MainHub mainHub, KinksterRequests clientData,
        DrawEntityFactory pairRequestFactory, CosmeticService cosmetics)
    {
        _hub = mainHub;
        _clientData = clientData;
        _pairRequestFactory = pairRequestFactory;
        _cosmetics = cosmetics;

    }

    public void Draw()
    {
        if (HasRequests is false)
            return;

        // Begin drawing out the header section for the requests folder dropdown thingy.
        using var id = ImRaii.PushId("folder_" + ID);
        var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), _wasHovered);
        using (ImRaii.Child("folder__" + ID, new System.Numerics.Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight())))
        {
            // draw opener
            var icon = _isRequestFolderOpen ? FAI.CaretDown : FAI.CaretRight;
            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();

            // toggle folder state on click
            CkGui.IconText(icon);

            ImGui.SameLine();
            var folderIconEndPos = DrawFolderIcon();
            ImGui.SameLine();
            DrawViewTypeSelection();

            // draw name
            ImGui.SameLine(folderIconEndPos);
            ImGui.AlignTextToFramePadding();
            var text = _viewingMode is DrawRequestsType.Outgoing
                ? (TotalOutgoing != 1 ? TotalOutgoing + " Outgoing Requests" : TotalOutgoing + " Outgoing Request")
                : (TotalIncoming != 1 ? TotalIncoming + " Incoming Requests" : TotalIncoming + " Incoming Request");
            using (ImRaii.PushFont(UiBuilder.MonoFont)) ImGui.TextUnformatted(text);
        }
        _wasHovered = ImGui.IsItemHovered();
        if (ImGui.IsItemClicked()) _isRequestFolderOpen = !_isRequestFolderOpen;


        color.Dispose();
        ImGui.Separator();
        // if the folder is opened, draw the relevant list.
        if (_isRequestFolderOpen)
        {
            // if there are no requests to display, show a message and return.
            if (TotalOutgoing is 0 && TotalIncoming is 0)
            {
                // close the folder if there are no requests to display.
                _isRequestFolderOpen = false;
                return;
            }

            using var indent = ImRaii.PushIndent(CkGui.IconSize(FAI.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);
            // draw the entries based on the type selected.
            if (_viewingMode is DrawRequestsType.Outgoing)
            {
                // switch the tab to incoming if there are no outgoing requests.
                if (TotalOutgoing is 0) _viewingMode = DrawRequestsType.Incoming;

                foreach (var entry in _allOutgoingRequests)
                    entry.DrawRequestEntry();
            }
            else
            {
                // switch the tab to outgoing if there are no incoming requests.
                if (TotalIncoming is 0) _viewingMode = DrawRequestsType.Outgoing;

                foreach (var entry in _allIncomingRequests)
                    entry.DrawRequestEntry();
            }
            ImGui.Separator();
        }
    }

    /// <summary>
    /// Grabs the latest kinkster request entries on each pair list update or request update recieved.
    /// </summary>
    public void UpdateKinksterRequests()
    {
        // I think this is all i need? Idk i guess we will find out or something lol.
        _allOutgoingRequests = _clientData.OutgoingRequests
            .Select(request => _pairRequestFactory.CreateKinsterRequest("outgoing-" + request.Target.UID, request))
            .ToHashSet();
        _allIncomingRequests = _clientData.IncomingRequests
            .Select(request => _pairRequestFactory.CreateKinsterRequest("incoming-" + request.User.UID, request))
            .ToHashSet();

        // if there are no outgoing, and we are on outgoing, switch it to incoming.
        if (TotalOutgoing is 0 && _viewingMode is DrawRequestsType.Outgoing)
            _viewingMode = DrawRequestsType.Incoming;

        // if there are no incoming, and we are on incoming, switch it to outgoing.
        if (TotalIncoming is 0 && _viewingMode is DrawRequestsType.Incoming)
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
