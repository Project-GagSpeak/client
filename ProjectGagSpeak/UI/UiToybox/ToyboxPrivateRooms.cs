using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Enums;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Toybox;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Globalization;
using System.Numerics;
using GagSpeak.Utils;

namespace GagSpeak.UI.UiToybox;

public class ToyboxPrivateRooms : DisposableMediatorSubscriberBase
{
    private readonly ToyboxHub _apiHubToybox;
    private readonly PrivateRoomManager _roomManager;
    private readonly UiSharedService _uiShared;
    private readonly PairManager _pairManager;
    private readonly GagspeakConfigService _configService;
    private readonly ServerConfigurationManager _serverConfigs;

    public ToyboxPrivateRooms(ILogger<ToyboxPrivateRooms> logger,
        GagspeakMediator mediator, ToyboxHub apiHubToybox,
        PrivateRoomManager privateRoomManager, UiSharedService uiShared,
        PairManager pairManager, GagspeakConfigService mainConfig,
        ServerConfigurationManager serverConfigs) : base(logger, mediator)
    {
        _apiHubToybox = apiHubToybox;
        _roomManager = privateRoomManager;
        _uiShared = uiShared;
        _pairManager = pairManager;
        _configService = mainConfig;
        _serverConfigs = serverConfigs;
    }

    private void ToggleToyboxConnection(bool newState)
    {
        // Obtain the current fullPause state.
        var currentState = _serverConfigs.CurrentServer.ToyboxFullPause;

        // If our new state is the same as the current state, return.
        if (currentState == newState)
            return;

        // If its true, make sure our ServerStatus is Connected, or if its false, make sure our ServerStatus is Disconnected or offline.
        if (!currentState && ToyboxHub.ServerStatus is ServerState.Connected)
        {
            // If we are connected, we want to disconnect.
            _serverConfigs.CurrentServer.ToyboxFullPause = !_serverConfigs.CurrentServer.ToyboxFullPause;
            _serverConfigs.Save();
            _ = _apiHubToybox.Disconnect(ServerState.Disconnected);
        }
        else if (currentState && ToyboxHub.ServerStatus is (ServerState.Disconnected or ServerState.Offline))
        {
            // If we are disconnected, we want to connect.
            _serverConfigs.CurrentServer.ToyboxFullPause = !_serverConfigs.CurrentServer.ToyboxFullPause;
            _serverConfigs.Save();
            _ = _apiHubToybox.Connect();
        }
    }

    // local accessors for the private room creation
    private string NewHostNameRef = string.Empty;
    private string HostChatAlias = string.Empty;
    private string _errorMessage = string.Empty;
    private DateTime _errorTime;

    public bool CreatingNewHostRoom { get; private set; } = false;
    public bool RoomCreatedSuccessful { get; private set; } = false;
    public bool HostPrivateRoomHovered { get; private set; } = false;
    private List<bool> JoinRoomItemsHovered = new List<bool>();

    public void DrawVibeServerPanel()
    {
        // draw the connection interface
        DrawToyboxServerStatus();


        if (!ToyboxHub.IsConnected)
        {
            UiSharedService.ColorText("Must be connected to view private rooms.", ImGuiColors.DalamudRed);
        }
        else
        {
            // Draw the header for creating a host room
            if (CreatingNewHostRoom)
            {
                DrawCreatingHostRoomHeader();
                ImGui.Separator();
                DrawNewHostRoomDisplay();
            }
            else
            {
                DrawCreateHostRoomHeader();
                ImGui.Separator();

                // see if the manager has any rooms at all to display
                if (_roomManager.AllPrivateRooms.Count == 0 || _roomManager.ClientUserUID.IsNullOrEmpty())
                {
                    ImGui.Text("No private rooms available.");
                    return;
                }
                else
                {
                    // before we draw out the private room listings, we will need to update attribute states.
                    UpdateAttributeStates();

                    // Draw the private room menu
                    DrawPrivateRoomMenu();
                }

                // DEBUGGING
                // draw out all details about the current hosted room.
                if (_roomManager.ClientHostingAnyRoom)
                {
                    ImGui.Text("Am I in any rooms?: " + _roomManager.ClientInAnyRoom);
                    ImGui.Text("Hosted Room Details:");
                    ImGui.Text("Room Name: " + _roomManager.ClientHostedRoomName);
                    // draw out the participants
                    var privateRoom = _roomManager.AllPrivateRooms.First(r => r.RoomName == _roomManager.ClientHostedRoomName);
                    // draw out details about this room.
                    ImGui.Text("Host UID: " + privateRoom.HostParticipant.User.UserUID);
                    ImGui.Text("Host Alias: " + privateRoom.HostParticipant.User.ChatAlias);
                    ImGui.Text("InRoom: " + privateRoom.HostParticipant.User.ActiveInRoom);
                    ImGui.Text("Allow Vibes: " + privateRoom.HostParticipant.User.VibeAccess);
                    // draw out the participants
                    ImGui.Indent();
                    foreach (var participant in privateRoom.Participants)
                    {
                        ImGui.Text("User UID: " + participant.User.UserUID);
                        ImGui.Text("User Alias: " + participant.User.ChatAlias);
                        ImGui.Text("InRoom: " + participant.User.ActiveInRoom);
                        ImGui.Text("Allow Vibes: " + participant.User.VibeAccess);
                        ImGui.Separator();
                    }
                    ImGui.Unindent();
                }
            }
        }
    }

    private void UpdateAttributeStates()
    {
        // see if we are currently hosting a room
        bool hostingRoom = _roomManager.ClientHostingAnyRoom;
        // get the size that our hovered items should be
        int SizeHoveredItemsShouldBe = hostingRoom ? _roomManager.AllPrivateRooms.Count - 1 : _roomManager.AllPrivateRooms.Count;
        
        // if the size is not the size that it should be, we need to rescale the items hovered list to the new size.
        if (JoinRoomItemsHovered.Count != SizeHoveredItemsShouldBe)
        {
            JoinRoomItemsHovered = new List<bool>(Enumerable.Repeat(false, SizeHoveredItemsShouldBe));
        }
    }


    private void DrawCreateHostRoomHeader()
    {
        // Use button rounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        var invitesSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Plus, "Invites");
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize("Host Room");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("CreateHostRoomHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + ((startYpos + centerYpos) - startYpos) * 2)))
        {
            // set startYpos to 0
            startYpos = ImGui.GetCursorPosY();
            // Center the button vertically
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // Draw the icon button. If room is created, this will turn into a trash bin for deletion.
            if (_roomManager.ClientHostingAnyRoom)
            {
                using (var disabled = ImRaii.Disabled(!KeyMonitor.ShiftPressed()))
                {
                    if (_uiShared.IconButton(FontAwesomeIcon.Trash))
                    {
                        _apiHubToybox.PrivateRoomRemove(_roomManager.ClientHostedRoomName).ConfigureAwait(false);
                    }
                }
                UiSharedService.AttachToolTip("Delete Hosted Room (Must hold shift)");
            }
            else
            {
                if (_uiShared.IconButton(FontAwesomeIcon.Plus))
                {
                    CreatingNewHostRoom = true;
                }
            }

            // Draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            _uiShared.BigText("Host Room");

            // Draw the "See Invites" button on the right
            ImGui.SameLine((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X)
                - invitesSize - 10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            var pos = ImGui.GetCursorScreenPos();
            // get disabled bool
            bool isDisabled = !_roomManager.ClientHostingAnyRoom;
            if (_uiShared.IconTextButton(FontAwesomeIcon.Envelope, "Invites ", null, false, isDisabled))
            {
                ImGui.SetNextWindowPos(new Vector2(pos.X, pos.Y + ImGui.GetFrameHeight()));
                ImGui.OpenPopup("InviteUsersToRoomPopup");
            }
            // Popup
            if (ImGui.BeginPopup("InviteUsersToRoomPopup"))
            {
                DrawInviteUserPopup();
                ImGui.EndPopup();
            }
        }
    }

    private void DrawCreatingHostRoomHeader()
    {
        // Use button rounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.PowerOff);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize("Setup Room");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("PrivateRoomSetupHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + ((startYpos + centerYpos) - startYpos) * 2)))
        {
            // set startYpos to 0
            startYpos = ImGui.GetCursorPosY();
            // Center the button vertically
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // Draw the icon button
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                // reset the createdAlarm to a new alarm, and set editing alarm to true
                CreatingNewHostRoom = false;
            }
            UiSharedService.AttachToolTip("Exit Private Room Setup");

            // Draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            _uiShared.BigText("Setup Room");

            // Draw the "See Invites" button on the right
            ImGui.SameLine((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X)
                - iconSize.X - 10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            if (_uiShared.IconButton(FontAwesomeIcon.PowerOff))
            {
                // create the room
                try
                {
                    // also log the success of the creation.
                    RoomCreatedSuccessful = _apiHubToybox.PrivateRoomCreate(new RoomCreateDto(NewHostNameRef, HostChatAlias)).Result;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error creating private room.");
                    _errorMessage = ex.Message;
                    _errorTime = DateTime.Now;
                }
                // if the room creation was successful, set the room creation to false.
                if (RoomCreatedSuccessful)
                {
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.VibeRoomCreated);
                    CreatingNewHostRoom = false;
                    RoomCreatedSuccessful = false;
                }
                else
                {
                    // if the room creation was not successful, display the error message.
                    _errorMessage = "Error creating private room.";
                    _errorTime = DateTime.Now;
                }
            }
            UiSharedService.AttachToolTip("Startup your Private Room with the defined settings below");
        }
    }

    private void DrawNewHostRoomDisplay()
    {
        var refString1 = NewHostNameRef;
        ImGui.InputTextWithHint("Room Name (ID)##HostRoomName", "Private Room Name...", ref refString1, 50);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            NewHostNameRef = refString1;
        }

        var refString2 = HostChatAlias;
        ImGui.InputTextWithHint("Your Chat Alias##HostChatAlias", "Chat Alias...", ref refString2, 30);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            HostChatAlias = refString2;
        }
        using (_uiShared.UidFont.Push())
        {
            UiSharedService.ColorText("Hosted Rooms Info:", ImGuiColors.ParsedPink);
        }
        UiSharedService.TextWrapped(" - You may only host ONE private room at a time."
            + Environment.NewLine + " - You can send user-pairs invites to UserPairs online in"
            + " the vibe server by clicking the hosted room after it is created."
            + Environment.NewLine + " - ANY Hosted room made is automatically removed 12 hours later."
            + Environment.NewLine + " - ONLY the host of the room can control other users vibrators."
            + Environment.NewLine + " - You can create another hosted room directly after removing the current one.");

        // if there is an error message, display it
        if (!string.IsNullOrEmpty(_errorMessage) && (DateTime.Now - _errorTime).TotalSeconds < 3)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, _errorMessage);
        }
    }

    private void DrawPrivateRoomMenu()
    {
        // Display error message if it has been less than 3 seconds since the error occurred
        if (!string.IsNullOrEmpty(_errorMessage) && (DateTime.Now - _errorTime).TotalSeconds < 3)
            ImGui.TextColored(ImGuiColors.DalamudRed, _errorMessage);

        // try and grab the hosted room.
        var hostedRoom = _roomManager.AllPrivateRooms.FirstOrDefault(r => r.RoomName == _roomManager.ClientHostedRoomName);
        // If currently hosting a room, draw the hosted room first
        if (_roomManager.ClientHostingAnyRoom && hostedRoom != null)
        {
            // grab the PrivateRoom of the AllPrivateRooms list where the room name == ClientHostedRoomName
            DrawPrivateRoomSelectable(hostedRoom, true);
        }

        // Draw the rest of the rooms, excluding the hosted room
        int idx = 0;
        foreach (var room in _roomManager.AllPrivateRooms.Where(r => r != hostedRoom))
        {
            DrawPrivateRoomSelectable(room, false, idx);
            idx++;
        }
    }

    private void DrawPrivateRoomSelectable(PrivateRoom privateRoomRef, bool isHostedRoom, int idx = -1)
    {
        try
        {
            // define our sizes
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);

            // grab the room name.
            string roomType = isHostedRoom ? "[Hosted]" : "[Joined]";
            string roomName = privateRoomRef.RoomName;
            string participantsCountText = "[" + privateRoomRef.GetActiveParticipants() + "/" + privateRoomRef.Participants.Count + " Active]";

            // draw startposition in Y
            var startYpos = ImGui.GetCursorPosY();
            var joinedState = _uiShared.GetIconButtonSize(privateRoomRef.IsParticipantActiveInRoom(_roomManager.ClientUserUID)
                ? FontAwesomeIcon.DoorOpen : FontAwesomeIcon.DoorClosed);
            var roomTypeTextSize = ImGui.CalcTextSize(roomType);
            var roomNameTextSize = ImGui.CalcTextSize(roomName);
            var totalParticipantsTextSize = ImGui.CalcTextSize(participantsCountText);
            var participantAliasListSize = ImGui.CalcTextSize(privateRoomRef.GetParticipantList());

            // DEBUG Logger.LogTrace("IDX {idx} - RoomName {roomName} - RoomType {roomType} - ClientUID {uid}", idx, roomName, roomType, _roomManager.ClientUserUID);
            // DEBUG Logger.LogTrace("JoinRoomItemsHovered Size {size} - Non-HostedRooms Size {nonHostedSize}", JoinRoomItemsHovered.Count, _roomManager.AllPrivateRooms.Count);
            using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), (isHostedRoom ? HostPrivateRoomHovered : JoinRoomItemsHovered[idx]));
            using (ImRaii.Child($"##PreviewPrivateRoom{roomName}", new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetFrameHeight() * 2)))
            {
                // create a group for the bounding area
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);
                using (var group = ImRaii.Group())
                {
                    // scooch over a bit like 5f
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                    // display the type.
                    UiSharedService.ColorText(roomType, ImGuiColors.DalamudYellow);
                    ImUtf8.SameLineInner();
                    // display the room name
                    ImGui.Text(roomName);
                }

                // now draw the lower section out.
                using (var group = ImRaii.Group())
                {
                    // scooch over a bit like 5f
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                    // display the participants count
                    UiSharedService.ColorText(participantsCountText, ImGuiColors.DalamudGrey2);
                    ImUtf8.SameLineInner();
                    UiSharedService.ColorText(privateRoomRef.GetParticipantList(), ImGuiColors.DalamudGrey3);

                }

                ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()
                    - joinedState.X - ImGui.GetStyle().ItemSpacing.X);

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetFrameHeight() / 2));
                // draw out the icon button
                if (_uiShared.IconButton(privateRoomRef.IsParticipantActiveInRoom(_roomManager.ClientUserUID)
                    ? FontAwesomeIcon.DoorOpen : FontAwesomeIcon.DoorClosed))
                {
                    try
                    {
                        // set the enabled state of the alarm based on its current state so that we toggle it
                        if (privateRoomRef.IsParticipantActiveInRoom(_roomManager.ClientUserUID))
                        {
                            // leave the room
                            _apiHubToybox.PrivateRoomLeave(new RoomParticipantDto
                                (privateRoomRef.GetParticipant(_roomManager.ClientUserUID).User, roomName)).ConfigureAwait(false);

                        }
                        else
                        {
                            // join the room
                            _apiHubToybox.PrivateRoomJoin(new RoomParticipantDto
                                (privateRoomRef.GetParticipant(_roomManager.ClientUserUID).User, roomName)).ConfigureAwait(false);
                        }
                        // toggle the state & early return so we don't access the child clicked button
                        return;
                    }
                    catch (Exception ex)
                    {
                        _errorMessage = ex.Message;
                        _errorTime = DateTime.Now;
                    }
                }
                UiSharedService.AttachToolTip(privateRoomRef.IsParticipantActiveInRoom(_roomManager.ClientUserUID)
                    ? "Leave Room" : "Join Room");

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);
            }
            // Check if the item is hovered and assign the hover state correctly
            if (isHostedRoom)
            {
                HostPrivateRoomHovered = ImGui.IsItemHovered();
            }
            else
            {
                JoinRoomItemsHovered[idx] = ImGui.IsItemHovered();
            }
            // action on clicky.
            if (ImGui.IsItemClicked())
            {
                // if we are currently joined in the private room, we can open the instanced remote.
                if (privateRoomRef.IsParticipantActiveInRoom(_roomManager.ClientUserUID))
                {
                    // open the respective rooms remote.
                    Mediator.Publish(new OpenPrivateRoomRemote(privateRoomRef));
                }
                else
                {
                    // toggle the additional options display.
                    Logger.LogInformation("You must be joined into the room to open the interface.");
                }
            }
            ImGui.Separator();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error drawing private room.");
        }
        
    }

    /* ---------------- Server Status Header Shit --------------------- */
    private void DrawToyboxServerStatus()
    {
        var windowPadding = ImGui.GetStyle().WindowPadding;
        var buttonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Link);
        var userCount = ToyboxHub.ToyboxOnlineUsers.ToString(CultureInfo.InvariantCulture);
        var userSize = ImGui.CalcTextSize(userCount);
        var textSize = ImGui.CalcTextSize("Toybox Users Online");

        string toyboxConnection = $"GagSpeak Toybox Server";

        var serverTextSize = ImGui.CalcTextSize(toyboxConnection);
        var printServer = toyboxConnection != string.Empty;

        // if the server is connected, then we should display the server info
        if (ToyboxHub.IsConnected)
        {
            // fancy math shit for clean display, adjust when moving things around
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) 
                / 2 - (userSize.X + textSize.X) / 2 - ImGui.GetStyle().ItemSpacing.X / 2);
            ImGui.TextColored(ImGuiColors.ParsedGreen, userCount);
            ImGui.SameLine();
            ImGui.TextUnformatted("Toybox Users Online");
        }
        // otherwise, if we are not connected, display that we aren't connected.
        else
        {
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth())
                / 2 - (ImGui.CalcTextSize("Not connected to the toybox server").X) / 2 - ImGui.GetStyle().ItemSpacing.X / 2);
            ImGui.TextColored(ImGuiColors.DalamudRed, "Not connected to the toybox server");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2 - serverTextSize.X / 2);
        ImGui.TextUnformatted(toyboxConnection);
        ImGui.SameLine();

        // now we need to display the connection link button beside it.
        var color = UiSharedService.GetBoolColor(!_serverConfigs.CurrentServer!.ToyboxFullPause);
        var connectedIcon = !_serverConfigs.CurrentServer.ToyboxFullPause ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink;

        ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - buttonSize.X);
        if (printServer)
        {
            // unsure what this is doing but we can find out lol
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ((userSize.Y + textSize.Y) / 2 + serverTextSize.Y) / 2 - ImGui.GetStyle().ItemSpacing.Y + buttonSize.Y / 2);
        }

        // if the server is reconnecting or disconnecting
        if (ToyboxHub.ServerStatus is not (ServerState.Reconnecting or ServerState.Disconnecting))
        {
            // we need to turn the button from the connected link to the disconnected link.
            using (ImRaii.PushColor(ImGuiCol.Text, color))
            {
                // then display it
                if (_uiShared.IconButton(connectedIcon))
                {
                    // and toggle the full pause for the current server, save the config, and recreate the connections,
                    // placing it into a disconnected state due to the full pause being active. (maybe change this later)
                    ToggleToyboxConnection(!_serverConfigs.CurrentServer.ToyboxFullPause);
                }
            }
            // attach the tooltip for the connection / disconnection button)
            UiSharedService.AttachToolTip(!_serverConfigs.CurrentServer.ToyboxFullPause
                ? "Disconnect from Toybox Server" : "Connect to ToyboxServer");

            // go back to the far left, at the same height, and draw another button.
            var invitesOpenIcon = FontAwesomeIcon.Envelope;
            var invitesIconSize = _uiShared.GetIconButtonSize(invitesOpenIcon);

            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + windowPadding.X);
            if (printServer)
            {
                // unsure what this is doing but we can find out lol
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ((userSize.Y + textSize.Y) / 2 + serverTextSize.Y) / 2 - ImGui.GetStyle().ItemSpacing.Y + buttonSize.Y / 2);
            }

            var pos = ImGui.GetCursorScreenPos();
            if (_uiShared.IconButton(invitesOpenIcon, ImGui.GetFrameHeight()))
            {
                ImGui.SetNextWindowPos(new Vector2(pos.X, pos.Y + ImGui.GetFrameHeight()));
                ImGui.OpenPopup("InviteViewPopup");
            }
        }

        // Popup
        if (ImGui.BeginPopup("InviteViewPopup"))
        {
            DrawViewInvitesPopup();
            ImGui.EndPopup();
        }


        // draw out the vertical slider.
        ImGui.Separator();
    }

    private string PreferredChatAlias = string.Empty;
    private void DrawViewInvitesPopup()
    {
        // if we have no invites, simply display that we have no invites.
        if (_roomManager.RoomInvites.Count == 0)
        {
            ImGui.Text("No invites available.");
            return;
        }

        var aliasRef = PreferredChatAlias;
        if (ImGui.InputTextWithHint("##PreferredChatAlias", "Preferred Chat Alias on Join...", ref aliasRef, 24))
        {
            PreferredChatAlias = aliasRef;
        }
        ImGui.Separator();

        // input text field for preferred chat alias
        var size = _uiShared.GetIconButtonSize(FontAwesomeIcon.Check);

        if (ImGui.BeginTable("InvitationsList", 3))
        {
            ImGui.TableSetupColumn("Private Room Name", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Private Room Namemmmmm").X);
            ImGui.TableSetupColumn("Invited By", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Invited Bymmmmm").X);
            ImGui.TableSetupColumn("Join?", ImGuiTableColumnFlags.WidthFixed, size.X * 2);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            // draw out the invites
            foreach (var roomInvite in _roomManager.RoomInvites)
            {
                ImGui.Text(roomInvite.RoomName);
                ImGui.TableNextColumn();
                ImGui.Text(roomInvite.UserInvited.AliasOrUID);
                ImGui.TableNextColumn();
                if (_uiShared.IconButton(FontAwesomeIcon.Check))
                {
                    // compile the new private room user.
                    var newUser = new PrivateRoomUser { UserUID = _roomManager.ClientUserUID, ChatAlias = PreferredChatAlias };
                    // join the room
                    _apiHubToybox.PrivateRoomJoin(new RoomParticipantDto(newUser, roomInvite.RoomName)).ConfigureAwait(false);
                }
                // draw another iconbutton X that will remove the invite listing from the list
                ImGui.SameLine();
                if (_uiShared.IconButton(FontAwesomeIcon.Times))
                {
                    _roomManager.RejectInvite(roomInvite);
                }
            }
            ImGui.EndTable();
        }

    }

    private void DrawInviteUserPopup()
    {
        // if no users are online to invite, display none
        if (_pairManager.GetOnlineToyboxUsers().Count == 0)
        {
            ImGui.Text("No users online to invite.");
            return;
        }

        ImGuiUtil.Center("Invite Users To Room");
        ImGui.Separator();
        // input text field for preferred chat alias
        var size = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.UserPlus, "Invite To Room");

        if (ImGui.BeginTable("InviteUsersToRoom", 2)) // 3 columns for hours, minutes, seconds
        {
            ImGui.TableSetupColumn("Online User", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("MMMMMMMMMMMMMM").X);
            ImGui.TableSetupColumn("Send Invite", ImGuiTableColumnFlags.WidthFixed, size);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            // draw out the list of online toybox users.
            var PairList = _pairManager.GetOnlineToyboxUsers()
                .OrderBy(p => p.GetNickname() ?? p.UserData.AliasOrUID, StringComparer.OrdinalIgnoreCase);

            foreach (Pair pair in PairList)
            {
                ImGui.TextUnformatted(pair.GetNickname() ?? pair.UserData.AliasOrUID);
                ImGui.TableNextColumn();
                if (_uiShared.IconTextButton(FontAwesomeIcon.UserPlus, "Invite To Room"))
                {
                    // invite the user to the room
                    _apiHubToybox.PrivateRoomInviteUser(new RoomInviteDto(pair.UserData, _roomManager.ClientHostedRoomName)).ConfigureAwait(false);
                }
            }
            ImGui.EndTable();
        }
    }
}
