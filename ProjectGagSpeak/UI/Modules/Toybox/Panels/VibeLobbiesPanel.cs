using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Network;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Toybox;

public class VibeLobbiesPanel
{
    private readonly ILogger<VibeLobbiesPanel> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly MainHub _hub;
    private readonly IpcCallerIntiface _ipc;
    private readonly VibeLobbyManager _lobbyManager;
    private readonly VibeLobbyDistributionService _lobbyCaller;
    private readonly TutorialService _guides;

    private string _searchQuery = string.Empty;
    private HashSet<string> _searchTags = new();
    private HubFilter _filterType = HubFilter.LobbySize;
    private HubDirection _sortOrder = HubDirection.Ascending;

    public VibeLobbiesPanel(ILogger<VibeLobbiesPanel> logger, GagspeakMediator mediator,
        MainConfig config, MainHub hub, IpcCallerIntiface ipc, VibeLobbyManager lobbyManager, 
        VibeLobbyDistributionService lobbyCaller, TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;
        _hub = hub;
        _ipc = ipc;
        _lobbyManager = lobbyManager;
        _lobbyCaller = lobbyCaller;
        _guides = guides;

        // grab path to the intiface
        if (IntifaceCentral.AppPath == string.Empty)
            IntifaceCentral.GetApplicationPath();
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("VibeLobbiesTL", drawRegions.TopLeft.Size))
            DrawTopLeftSearch(drawRegions.TopLeft, curveSize);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("VibeLobbiesBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            DrawPublicLobbies(drawRegions.BotLeft, curveSize);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("VibeLobbiesTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawLobbyPreview(drawRegions.BotRight, curveSize);
    }

    private void DrawTopLeftSearch(CkHeader.DrawRegion region, float curveSize)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        using var c = ImRaii.PushColor(ImGuiCol.FrameBg, 0);

        var buttonWidth = CkGui.IconButtonSize(FAI.Search).X;
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

        using (CkRaii.Group(CkCol.CurvedHeaderFade.Uint()))
        {
            var groupWidth = region.SizeX - (buttonWidth + spacing);
            ImGui.Dummy(new Vector2(region.SizeX - (buttonWidth + spacing), ImGui.GetFrameHeight()));
            ImGui.SetCursorScreenPos(ImGui.GetItemRectMin());

            ImGui.SetNextItemWidth(groupWidth - (buttonWidth * 3 + spacing * 3));
            if (ImGui.InputTextWithHint("##RoomSearch", "Search Rooms..", ref _searchQuery, 75, ITFlags.EnterReturnsTrue))
            {
                _logger.LogInformation($"SearchBar was modified! Query: {_searchQuery}");
                UiService.SetUITask(async () => await _lobbyCaller.SearchForRooms(new(_searchQuery, _searchTags.ToArray(), _filterType, _sortOrder)));
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _logger.LogInformation($"SearchBar was deactivated after edit, searching for: {_searchQuery}");
                UiService.SetUITask(async () => await _lobbyCaller.SearchForRooms(new(_searchQuery, _searchTags.ToArray(), _filterType, _sortOrder)));
            }

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Search, disabled: UiService.DisableUI, inPopup: true))
                UiService.SetUITask(async () => await _lobbyCaller.SearchForRooms(new(_searchQuery, _searchTags.ToArray(), _filterType, _sortOrder)));

            ImUtf8.SameLineInner();
            if (CkGuiUtils.IconEnumCombo(FAI.Filter, _filterType, out var newFilter, UiService.DisableUI, null, "##FilterType", true, 2))
            {
                _logger.LogInformation($"Filter Type changed to: {newFilter}");
                _filterType = newFilter;
            }

            ImUtf8.SameLineInner();
            var icon = _sortOrder == HubDirection.Ascending ? FAI.SortAmountUp : FAI.SortAmountDown;
            if (CkGui.IconButton(icon, disabled: UiService.DisableUI, inPopup: true))
            {
                // invert the sort order.
                _sortOrder = _sortOrder == HubDirection.Ascending ? HubDirection.Descending : HubDirection.Ascending;
                // sort the results, without needing to reperform a search.
                var itemsSorted = _sortOrder == HubDirection.Ascending
                    ? _lobbyManager.PublicVibeRooms.OrderByDescending(x => x.CurrentParticipants).ToList()
                    : _lobbyManager.PublicVibeRooms.OrderBy(x => x.CurrentParticipants).ToList();
                _lobbyManager.SetPublicVibeRooms(itemsSorted);
                _logger.LogInformation($"Sorted VibeRooms by: {icon} - {_sortOrder}");
            }
        }

        c.Push(ImGuiCol.Button, CkCol.CurvedHeaderFade.Uint());

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FAI.Tags, disabled: UiService.DisableUI))
        {
            // do fancy voodoo here.
        }
    }

    private RoomListing? _selectedListing = null;
    private void DrawPublicLobbies(CkHeader.DrawRegion region, float curveSize)
    {
        using var patternResultChild = CkRaii.Child("##PublicLobbyListings", region.Size, wFlags: WFlags.NoScrollbar);
        // result styles.
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, CkStyle.ListItemRounding());
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

        // do invites first.
        if (_lobbyManager.CurrentInvites.Count > 0)
        {
            foreach (var invite in _lobbyManager.CurrentInvites)
                DrawVibeRoomInvite(invite);
            ImGui.Separator();
        }

        // draw the results if there are any.
        if (_lobbyManager.PublicVibeRooms.Count <= 0)
        {
            ImGui.Spacing();
            ImGuiUtil.Center("Create your room, or search for others!");
            return;
        }
        else
        {
            foreach (var item in _lobbyManager.PublicVibeRooms)
                if(DrawVibeRoomListing(item))
                    _selectedListing = item;
        }

        // cant click these, only can click the accept or reject buttons.
        void DrawVibeRoomInvite(RoomInvite invite)
        {
            using var _ = ImRaii.Group();
            var wdl = ImGui.GetWindowDrawList();

            // cast a dummy the size of what we want it to be.
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() + ImGui.GetTextLineHeightWithSpacing() * 2));
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            var hovered = ImGui.IsItemHovered();
            ImGui.SetCursorScreenPos(min);

            var bgColor = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
            wdl.AddRectFilled(min, max, bgColor, 5);

            // Now we must display the rooms name.
            CkGui.FramedIconText(FAI.Envelope);
            CkGui.TextFrameAlignedInline(invite.RoomName);

            // on the next line, display the attached message.
            CkGui.ColorTextWrapped(invite.AttachedMessage, ImGuiColors.DalamudGrey);
        }

        bool DrawVibeRoomListing(RoomListing vibeRoom)
        {
            var pos = ImGui.GetCursorScreenPos();
            var size = new Vector2(ImGui.GetContentRegionAvail().X, CkStyle.GetFrameRowsHeight(2) + ImGui.GetTextLineHeightWithSpacing());
            var hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            var bgCol = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

            using (var c = CkRaii.FramedChildPaddedW($"##RoomListing-{vibeRoom.Name}", size.X, size.Y, bgCol, 0, CkStyle.ChildRounding(), 1, DFlags.RoundCornersAll))
            {
                CkGui.FramedIconText(FAI.DoorOpen);
                CkGui.TextFrameAlignedInline(vibeRoom.Name);

                // Draw the current rooms participant count bar.
                ImGui.SameLine();
                var remaining = ImGui.GetContentRegionAvail().X;
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(remaining * .5f, ImGui.GetStyle().FramePadding.Y));
                // Draw out a progress bar in the remaining space.
                var progressText = $"{vibeRoom.CurrentParticipants}/{vibeRoom.MaxParticipants}";
                var progress = (float)(vibeRoom.CurrentParticipants / (float)vibeRoom.MaxParticipants);
                CkGuiUtils.DrawProgressBar(new Vector2(remaining * .5f, ImGui.GetTextLineHeight()), progressText, progress, GsCol.VibrantPink.Uint(), CkStyle.ListItemRounding());

                // Finally, draw out the tags.
                using (ImRaii.Group())
                {
                    CkGui.FramedIconText(FAI.UserTag);
                    if (vibeRoom.Tags.Count() <= 0)
                    {
                        CkGui.ColorTextFrameAlignedInline("(No Tags)", ImGuiColors.ParsedGrey);
                        CkGui.AttachToolTip("This VibeRoom has no tags.");
                    }
                    else
                    {
                        // Draw the tags as a comma separated list, but truncate if too long.
                        var leftoverWidth = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.Y;
                        var tagsString = string.Join(", ", vibeRoom.Tags);
                        if (ImGui.CalcTextSize(tagsString).X > leftoverWidth)
                            tagsString = tagsString.Substring(0, (int)(leftoverWidth / ImGui.CalcTextSize("A").X)) + "...";
                        CkGui.ColorTextFrameAlignedInline(tagsString, ImGuiColors.ParsedGrey);
                    }
                }
                CkGui.AttachToolTip("This VibeRoom's tag filters.");

                // Next line, draw out the description, with no icon label.
                if (string.IsNullOrWhiteSpace(vibeRoom.Description))
                    CkGui.ColorText("No Description Provided..", ImGuiColors.DalamudGrey);
                else
                {
                    if (ImGui.CalcTextSize(vibeRoom.Description).X > c.InnerRegion.X)
                        vibeRoom.Description = vibeRoom.Description.Substring(0, (int)(c.InnerRegion.X / ImGui.CalcTextSize("A").X)) + "...";
                    CkGui.ColorText(vibeRoom.Description, ImGuiColors.DalamudGrey);
                }
            }
            return ImGui.IsItemClicked();
        }
    }

    private void DrawLobbyPreview(CkHeader.DrawRegion region, float rounding)
    {
        DrawSelectedLobby(region, rounding);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));

        // Shift down and draw the lower.
        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y * 3);
        ImGui.SetCursorScreenPos(region.Pos + verticalShift);
        DrawCreateOrActiveLobby(region.Size - verticalShift);
        var botLineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var botLineBotRight = botLineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(botLineTopLeft, botLineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedLobby(CkHeader.DrawRegion drawRegion, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var padding = ImGui.GetStyle().WindowPadding.X;
        var region = new Vector2(drawRegion.SizeX, ImGui.GetTextLineHeightWithSpacing() * 4);
        var notSelected = _selectedListing is null;
        var inRoom = _lobbyManager.IsInVibeRoom;
        var tooltipAct = notSelected ? "No VibeRoom selected!" : inRoom ? "Already in VibeRoom!" : "Double Click to join this room!";
        var label = _selectedListing is null ? "No Lobby Selected" : _selectedListing.Name;

        using var c = CkRaii.ChildLabelButton(region, .65f, label, ImGui.GetFrameHeight(), EnterRoom, tooltipAct, ImDrawFlags.RoundCornersRight, LabelFlags.AddPaddingToHeight);

        // Draw the image preview for the selected item if valid.
        var labelSize = ImGui.GetItemRectSize();
        var availWidth = (c.InnerNoLabel.X - labelSize.X);
        var imgHeight = c.InnerRegion.Y - ImGui.GetTextLineHeight() - ImGui.GetStyle().ItemSpacing.Y * 3;
        var imgSize = new Vector2(imgHeight);
        var drawPos = ImGui.GetItemRectMax() + new Vector2(((availWidth - imgHeight) * .5f), -(labelSize.Y - ImGui.GetStyle().ItemSpacing.Y));
        ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.VibeLobby], drawPos, imgSize);

        if (_selectedListing is { } lobby)
        {
            // drop down to the bottom to draw the progress bar, draw the description after.
            var cursorPos = ImGui.GetCursorPos();
            ImGui.SetCursorPos(cursorPos + new Vector2(ImGui.GetFrameHeight()/3, c.InnerNoLabel.Y - ImGui.GetTextLineHeight()));
            var progressText = $"{lobby.CurrentParticipants}/{lobby.MaxParticipants} Participants Inside";
            var progress = (float)(lobby.CurrentParticipants / (float)lobby.MaxParticipants);
            CkGuiUtils.DrawProgressBar(new Vector2(c.InnerRegion.X - ImGui.GetFrameHeight(), ImGui.GetTextLineHeight()), progressText, progress, GsCol.VibrantPink.Uint());

            ImGui.SetCursorPos(cursorPos);
            if (string.IsNullOrWhiteSpace(lobby.Description))
                ImUtf8.TextWrapped("No description was set for this vibe room.", labelSize.X);
            else
            {
                if (ImGui.CalcTextSize(lobby.Description).X > c.InnerRegion.X)
                    lobby.Description = lobby.Description.Substring(0, (int)(c.InnerRegion.X / ImGui.CalcTextSize("A").X)) + "...";
                ImUtf8.TextWrapped(lobby.Description, labelSize.X);
            }
        }

        void EnterRoom(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || _selectedListing is not { } lobbyToEnter || inRoom)
                return;

            // we can try to join.
            _logger.LogDebug($"Attempting to join lobby: {lobbyToEnter.Name}");
            UiService.SetUITask(async () =>
            {
                // password shit later.
                if (await _lobbyCaller.TryRoomJoin(lobbyToEnter.Name))
                    _logger.LogInformation($"Successfully joined lobby: {lobbyToEnter.Name}");
            });
        }
        // Lobby Information Stuff here.

    }

    private void DrawCreateOrActiveLobby(Vector2 region)
    {
        if (_lobbyManager.IsInVibeRoom)
            DrawJoinedLobby(region);
        else
        {
            DrawCreateLobby(region);
        }
    }

    private TagCollection _tagCollection = new();
    private string _newRoomName = string.Empty;
    private string _newRoomDescription = string.Empty;
    private string _newRoomPassword = string.Empty;
    private List<string> _newRoomTags = new();
    private void DrawCreateLobby(Vector2 region)
    {
        var tagsH = CkStyle.GetFrameRowsHeight(2);
        var totalHeight = ImGui.GetTextLineHeightWithSpacing() * 6 + tagsH.AddWinPadY() + ImGui.GetFrameHeightWithSpacing() * 2 + ImGui.GetStyle().ItemSpacing.Y;

        using var c = CkRaii.ChildLabelText(new Vector2(region.X, totalHeight), .45f, "Host A VibeRoom", ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight, LabelFlags.AddPaddingToHeight);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 6f);
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkCol.CurvedHeaderFade.Uint());
        ImGui.SameLine(0, 0);
        DrawRoomJoinButton();
        CkGui.AttachToolTip(string.IsNullOrWhiteSpace(_newRoomName) ? "Must provide a name first." :
            "Create a new VibeRoom." +
            "--SEP--Once a room is made, you can change the password, or the host." +
            "--SEP--Only the host is allowed to control other people in the lobby." +
            "--SEP--Lobbies last a maximum of 12h.");

        CkGui.ColorText("Name", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(c.InnerRegion.X * 0.6f);
        ImGui.InputTextWithHint("##NewRoomName", "name..", ref _newRoomName, 55);
        CkGui.AttachToolTip("The VibeRooms name, will be visible in public listings and invites.");

        CkGui.ColorText("Password", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(c.InnerRegion.X * 0.6f);
        ImGui.InputText("##NewRoomPass", ref _newRoomPassword, 40);
        CkGui.AttachToolTip("Rooms with a password will be private. Leave blank if you want this to be public.");

        CkGui.ColorText("Description", ImGuiColors.ParsedGold);
        ImGui.InputTextMultiline("##RoomDesc", ref _newRoomDescription, 100, new Vector2(c.InnerRegion.X * .75f, ImGui.GetTextLineHeightWithSpacing() * 2));
        // Draw a hint if no text is present.
        if (string.IsNullOrWhiteSpace(_newRoomDescription) && !ImGui.IsItemActive())
            ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.TextDisabled), "your desc..");
        CkGui.AttachToolTip("This VibeRoom description will be visible on public VibeRoom listings, but not invites.");

        // extract the tabs by splitting the string by comma's
        CkGui.ColorText("Tags", ImGuiColors.ParsedGold);
        using (CkRaii.FramedChildPaddedW("NewRoomTags", c.InnerRegion.X * .75f, CkStyle.GetFrameRowsHeight(2), CkCol.CurvedHeaderFade.Uint(), CkCol.CurvedHeaderFade.Uint(), DFlags.RoundCornersAll))
        {
            if (_tagCollection.DrawTagsEditor("##NewRoomTags", _newRoomTags, out var updatedTags, GsCol.VibrantPink.Vec4()))
                _newRoomTags = updatedTags.Take(5).ToList(); // limit to 5 tags
        }
    }

    private void DrawJoinedLobby(Vector2 region)
    {
        using var c = CkRaii.ChildLabelText(region, .7f, "Selected VibeRoom", ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight, LabelFlags.SizeIncludesHeader);

        ImGui.Text("Im this lobby's internal view");
    }

    private void DrawRoomJoinButton()
    {
        var remainingWidth = ImGui.GetContentRegionAvail().X;
        var isPublic = string.IsNullOrWhiteSpace(_newRoomPassword);
        using var col = ImRaii.PushColor(ImGuiCol.Button, 0).Push(ImGuiCol.Text, isPublic ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);

        var text = $" Create {(isPublic ? "Public" : "Private")} Room ";
        var size = ImGuiHelpers.GetButtonSize(text);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (remainingWidth - size.X) / 2);
        using var _ = CkRaii.Group(CkCol.CurvedHeaderFade.Uint(), ImGui.GetFrameHeight(), 2, ImDrawFlags.RoundCornersAll);

        if (ImUtf8.SmallButton(text))
        {
            if (UiService.DisableUI || string.IsNullOrWhiteSpace(_newRoomName))
            {
                _logger.LogWarning("Cannot create a room without a name!");
                return;
            }

            UiService.SetUITask(async () =>
            {
                if (await _lobbyCaller.TryCreateRoom(_newRoomName, _newRoomDescription, _newRoomPassword, _newRoomTags))
                {
                    _logger.LogInformation($"Created new VibeRoom: {_newRoomName}");
                    // it was a success, so clear our fields.
                    _newRoomName = _newRoomDescription = _newRoomPassword = string.Empty;
                    _newRoomTags.Clear();
                }
                else
                {
                    _logger.LogError($"Failed to create room {_newRoomName} due to errors. See Logs!");
                }
            });
        }
    }
}
