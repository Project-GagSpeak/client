using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI.Handlers;
using System.Collections.Immutable;
using GagSpeak.UI;
using OtterGui.Text;
using GagSpeak.PlayerData.Data;
using Dalamud.Interface.Utility;
using OtterGui;
using GagspeakAPI.Dto.UserPair;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using Penumbra.GameData.Gui.Debug;

namespace GagSpeak.UI.Components.UserPairList;

/// <summary>
/// The base for the draw folder, which is a dropdown section in the list of paired users, and handles the basic draw functionality
/// </summary>
public class DrawRequests : IRequestsFolder
{
    private readonly MainHub _apiHubMain;
    private readonly ClientData _clientData;
    private readonly DrawEntityFactory _pairRequestFactory;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    private float _menuWidth = -1;
    private bool _wasHovered = false;
    private bool _isRequestFolderOpen = false;
    private DrawRequestsType _viewingMode = DrawRequestsType.Outgoing;
    public string ID => "Kinkster_Requests";

    // The Kinkster Requests currently present.
    private HashSet<KinksterRequestEntry> _allOutgoingRequests;
    private HashSet<KinksterRequestEntry> _allIncomingRequests;

    public bool HasRequests => _clientData.CurrentRequests.Any();
    public int TotalOutgoing => _clientData.OutgoingRequests.Count();
    public int TotalIncoming => _clientData.IncomingRequests.Count();

    public DrawRequests(MainHub mainHub, ClientData clientData, 
        DrawEntityFactory pairRequestFactory, CosmeticService cosmetics, 
        UiSharedService uiSharedService)
    {
        _apiHubMain = mainHub;
        _clientData = clientData;
        _pairRequestFactory = pairRequestFactory;
        _cosmetics = cosmetics;
        _uiShared = uiSharedService;
    }

    public void Draw()
    {
/*        if (HasRequests is false) 
            return;*/

        // Begin drawing out the header section for the requests folder dropdown thingy.
        using var id = ImRaii.PushId("folder_" + ID);
        var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), _wasHovered);
        using (ImRaii.Child("folder__" + ID, new System.Numerics.Vector2(UiSharedService.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight())))
        {
            // draw opener
            var icon = _isRequestFolderOpen ? FontAwesomeIcon.CaretDown : FontAwesomeIcon.CaretRight;
            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();

            // toggle folder state on click
            _uiShared.IconText(icon);
            if (ImGui.IsItemClicked()) _isRequestFolderOpen = !_isRequestFolderOpen;

            ImGui.SameLine();
            var folderIconEndPos = DrawFolderIcon();
            ImGui.SameLine();
            DrawViewTypeSelection();

            // draw name
            ImGui.SameLine(folderIconEndPos);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Kinkster Requests");
        }
        _wasHovered = ImGui.IsItemHovered();
        if(ImGui.IsItemClicked()) _isRequestFolderOpen = !_isRequestFolderOpen;


        color.Dispose();
        ImGui.Separator();
        // if the folder is opened, draw the relevant list.
        if (_isRequestFolderOpen)
        {
            using var indent = ImRaii.PushIndent(_uiShared.GetIconData(FontAwesomeIcon.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);
            // draw the entries based on the type selected.
            if (_viewingMode is DrawRequestsType.Outgoing)
            {
                foreach(var entry in _allOutgoingRequests)
                    entry.DrawRequestEntry();
            }
            else
            {
                foreach(var entry in _allIncomingRequests)
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
            .Select(request => _pairRequestFactory.CreateKinsterRequest("outgoing-" + request.User.UID, request))
            .ToHashSet();
        _allIncomingRequests = _clientData.IncomingRequests
            .Select(request => _pairRequestFactory.CreateKinsterRequest("incoming-" + request.User.UID, request))
            .ToHashSet();
    }


    private float DrawFolderIcon()
    {
        ImGui.AlignTextToFramePadding();
        _uiShared.IconText(FontAwesomeIcon.Inbox);

        if (HasRequests)
        {
            // set the spacing for this parameter that is drawn to the right of the icon.
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = ImGui.GetStyle().ItemSpacing.X / 2f }))
            {
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("[" + (TotalOutgoing + TotalIncoming) + "]");
            }
            UiSharedService.AttachToolTip("You have " + TotalOutgoing + " requests pending to Kinksters." +
                "--SEP--You have " + TotalIncoming + " incoming requests from other Kinksters.");
        }
        // ensure sameline and return current X position.
        ImGui.SameLine();
        return ImGui.GetCursorPosX();
    }
    private float DrawViewTypeSelection()
    {
        var outgoingButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.PersonArrowUpFromLine, "Outgoing");
        var incomingButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.PersonArrowDownToLine, "Incoming");
        var spacingX = ImGui.GetStyle().ItemSpacing.X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth();
        
        // Flyout Menu
        var rightSideStart = windowEndX - (outgoingButtonSize + incomingButtonSize + (spacingX * 2));

        ImGui.SameLine(windowEndX - outgoingButtonSize - incomingButtonSize - spacingX);
        if (_uiShared.IconTextButton(FontAwesomeIcon.PersonArrowUpFromLine, "Outgoing", isInPopup: true, disabled: _viewingMode == DrawRequestsType.Outgoing))
            _viewingMode = DrawRequestsType.Outgoing;
        // same line, draw incoming.
        ImGui.SameLine();
        if (_uiShared.IconTextButton(FontAwesomeIcon.PersonArrowDownToLine, "Incoming", isInPopup: true, disabled: _viewingMode == DrawRequestsType.Incoming))
            _viewingMode = DrawRequestsType.Incoming;

        return rightSideStart;
    }
}
