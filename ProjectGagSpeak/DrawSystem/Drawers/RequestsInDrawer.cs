using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using OtterGui.Text;

namespace GagSpeak.DrawSystem;

// Drawer specifically for handling incoming kinkster requests.
// Holds its own selection cache and individual reply variables from pending requests.
// This allows to cache reply progress when switching between the two.
public class RequestsInDrawer : DynamicDrawer<RequestEntry>
{
    private static readonly string ToolTip =
        "--COL--[L-CLICK]--COL-- Single-Select for bulk responding." +
  "--NL----COL--[SHIFT + L-CLICK]--COL-- Select/Deselect all between current & last selected ";

    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly RequestsManager _manager;
    private readonly KinksterManager _kinksters;
    private readonly SidePanelService _sidePanel;

    private RequestCache _cache => (RequestCache)FilterCache;

    public RequestsInDrawer(ILogger<RequestsInDrawer> logger, MainHub hub, MainConfig config, 
        RequestsManager manager, KinksterManager kinksters, SidePanelService sidePanel,
        RequestsDrawSystem ds) 
        : base("##GSRequestsInDrawer", Svc.Logger.Logger, ds, new RequestCache(ds))
    {
        _hub = hub;
        _config = config;
        _manager = manager;
        _kinksters = kinksters;
        _sidePanel = sidePanel;
    }

    public IReadOnlyList<DynamicLeaf<RequestEntry>> SelectedRequests => Selector.Leaves;
    public bool MultiSelecting => SelectedRequests.Count > 1;

    #region Search
    // Special top area here due to how it displays either essential config or bulk selection options.
    protected override void DrawSearchBar(float width, int length)
    {
        var tmp = FilterCache.Filter;
        var buttonsWidth = CkGui.IconButtonSize(FAI.Wrench).X + CkGui.IconTextButtonSize(FAI.Envelope, "Incoming");
        // Update the search bar if things change, like normal.
        if (FancySearchBar.Draw("Filter", width, ref tmp, "filter..", length, buttonsWidth, DrawButtons))
            FilterCache.Filter = tmp;
        
        // Update the side panel if currently set to none, but drawing incoming.
        if (_sidePanel.DisplayMode is not SidePanelMode.IncomingRequests)
            _sidePanel.ForRequests(SidePanelMode.IncomingRequests, _cache, Selector);
        // Draw the config if it is opened.
        if (_cache.FilterConfigOpen)
            DrawConfig(width);

        void DrawButtons()
        {
            // For swapping which drawer is displayed. (Should also swap what is present in the service if multi-selecting.
            if (CkGui.IconTextButton(FAI.Envelope, "Incoming", null, true, MultiSelecting || _cache.FilterConfigOpen))
            {
                _config.Current.ViewingIncoming = !_config.Current.ViewingIncoming;
                _config.Save();
                // Update the side panel service.
                _sidePanel.ForRequests(SidePanelMode.PendingRequests, _cache, Selector);
            }
            CkGui.AttachToolTip($"Switch to outgoing requests.");

            ImGui.SameLine(0, 0);
            if (CkGui.IconButton(FAI.Wrench, disabled: MultiSelecting, inPopup: !_cache.FilterConfigOpen))
                _cache.FilterConfigOpen = !_cache.FilterConfigOpen;
            CkGui.AttachToolTip("Configure preferences for requests handling.");
        }
    }

    // Draws the grey line around the filtered content when expanded and stuff.
    protected override void PostSearchBar()
    {
        if (_cache.FilterConfigOpen)
            ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.Button), 5f);
    }
    #endregion Search

    // Override a custom method to draw only the specific folder's cached children.
    public void DrawIncomingRequests(float width, DynamicFlags flags = DynamicFlags.None)
    {
        // Obtain the folder first before handling the draw logic.
        if (!DrawSystem.FolderMap.TryGetValue(Constants.FolderTagRequestInc, out var folder))
            return;
        // Draw the singular folder.
        DrawFolder(folder, width, flags);
    }
    
    protected override void DrawFolderBannerInner(IDynamicFolder<RequestEntry> folder, Vector2 region, DynamicFlags flags)
        => DrawIncomingRequests((RequestFolder)folder, region, flags);

    private void DrawIncomingRequests(RequestFolder folder, Vector2 region, DynamicFlags flags)
    {
        // No interactions, keep open.
        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        CkGui.IconText(folder.Icon, folder.IconColor);
        CkGui.ColorTextFrameAlignedInline(folder.Name, folder.NameColor);
        CkGui.ColorTextFrameAlignedInline($"[{folder.TotalChildren}]", ImGuiColors.DalamudGrey2);
        // Draw the right side buttons.
        DrawFolderButtons(folder);
    }

    private float DrawFolderButtons(RequestFolder folder)
    {
        var byWorldSize = CkGui.IconTextButtonSize(FAI.Globe, "In World");
        var byAreaSize = CkGui.IconTextButtonSize(FAI.Map, "In Area");
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - byWorldSize;

        ImGui.SameLine(currentRightSide);
        if (CkGui.IconTextButton(FAI.Globe, "In World", null, true, UiService.DisableUI))
        {
            Selector.SelectMultiple([]);
        }
        CkGui.AttachToolTip("Select requests sent from your current world.");

        currentRightSide -= byAreaSize;
        ImGui.SameLine(currentRightSide);
        if (CkGui.IconTextButton(FAI.Map, "In Area", null, true, UiService.DisableUI))
        {
            Selector.SelectMultiple([]);
        }
        CkGui.AttachToolTip("Select requests sent from your current area.");

        return currentRightSide;
    }

    // Will only ever be incoming in our case.
    protected override void DrawLeaf(IDynamicLeaf<RequestEntry> leaf, DynamicFlags flags, bool selected)
    {
        // If Selector.SelectedLeaf is this leaf, it is the single selection to respond to.
        if (Selector.SelectedLeaf == leaf)
            DrawResponderEntry(leaf, flags, selected);
        else
            DrawEntry(leaf, flags, selected);
    }

    private void DrawResponderEntry(IDynamicLeaf<RequestEntry> leaf, DynamicFlags flags, bool selected)
    {
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), CkStyle.ThreeRowHeight());
        var bgCol = selected ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0;

        using var _ = CkRaii.FramedChild(Label + leaf.Name, size, 0, ImGui.GetColorU32(ImGuiCol.Button), 5f, 1f);

        using (var __ = CkRaii.Child(Label + leaf.Name + "inner", new(_.InnerRegion.X, ImUtf8.FrameHeight), ImGui.GetColorU32(ImGuiCol.FrameBgHovered), 5f))
        {
            ImUtf8.SameLineInner();
            var posX = ImGui.GetCursorPosX();
            using (ImRaii.PushFont(UiBuilder.MonoFont))
                CkGui.TextFrameAlignedInline(leaf.Data.SenderAnonName);
            // store the cursorX
            var rightX = DrawIncomingRightInfo(leaf, flags);
            ImGui.SameLine(posX);
            if (ImGui.InvisibleButton($"request_{leaf.FullPath}", new Vector2(rightX - posX, ImUtf8.FrameHeight)))
                HandleClick(leaf, flags);
            HandleDetections(leaf, flags);
            CkGui.AttachToolTip(ToolTip, ImGuiColors.DalamudOrange);
        }

        // Lower area for responder options.
        CkGui.ColorText("Include selector for groups, and an accept/reject button.", ImGuiColors.ParsedGold);
        // Dummy placeholders.
        if (CkGui.IconTextButton(FAI.PersonCircleCheck, "Accept", null, true, UiService.DisableUI))
            AcceptRequest(leaf.Data);
        CkGui.AttachToolTip("Accept this kinkster request.");
        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.PersonCircleXmark, "Reject", null, true, UiService.DisableUI))
            RejectRequest(leaf.Data);
        CkGui.AttachToolTip("Reject this kinkster request.");
    }

    private void DrawEntry(IDynamicLeaf<RequestEntry> leaf, DynamicFlags flags, bool selected)
    {
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImUtf8.FrameHeight);
        var bgCol = selected ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0;

        using var _ = CkRaii.Child(Label + leaf.Name, size, bgCol, 5f);
        // Inner content.
        ImUtf8.SameLineInner();
        var posX = ImGui.GetCursorPosX();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
            CkGui.TextFrameAlignedInline(leaf.Data.SenderAnonName);

        // store the cursorX
        var rightX = DrawIncomingRightInfo(leaf, flags);

        ImGui.SameLine(posX);
        if (ImGui.InvisibleButton($"request_{leaf.FullPath}", new Vector2(rightX - posX, ImUtf8.FrameHeight)))
            HandleClick(leaf, flags);
        HandleDetections(leaf, flags);
        CkGui.AttachToolTip(ToolTip, ImGuiColors.DalamudOrange);
    }

    private float DrawIncomingRightInfo(IDynamicLeaf<RequestEntry> leaf, DynamicFlags flags)
    {
        var timeLeftText = $"{leaf.Data.TimeToRespond.Days}d {leaf.Data.TimeToRespond.Hours}h {leaf.Data.TimeToRespond.Minutes}m";
        var iconSize = ImUtf8.FrameHeight;
        var timeLeft = ImGui.CalcTextSize(timeLeftText).X;
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - iconSize;

        ImGui.SameLine(currentRightSide);
        CkGui.FramedHoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint());
        ShowRequestDetails(leaf.Data);

        currentRightSide -= timeLeft;
        ImGui.SameLine(currentRightSide);
        CkGui.ColorTextFrameAligned(timeLeftText, ImGuiColors.DalamudViolet);
        CkGui.AttachToolTip("Time left to respond to this request.");
        return currentRightSide;
    }

    private void ShowRequestDetails(RequestEntry request)
    {
        if (!ImGui.IsItemHovered())
            return;

        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 6f)
            .Push(ImGuiStyleVar.WindowRounding, 4f)
            .Push(ImGuiStyleVar.PopupBorderSize, 1f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var _ = ImRaii.Tooltip();
        // Can add later requested nick here.
        CkGui.ColorText("Is Temporary:", ImGuiColors.ParsedGold);
        CkGui.BooleanToColoredIcon(request.IsTemporaryRequest, true);
        // Include message.
        if (request.AttachedMessage.Length > 0)
        {
            CkGui.ColorText("Attached Message:", ImGuiColors.ParsedGold);
            CkGui.TextWrapped(request.AttachedMessage);
        }
    }

    #region Utility
    private void DrawConfig(float width)
    {
        var bgCol = ColorHelpers.Fade(ImGui.GetColorU32(ImGuiCol.FrameBg), 0.4f);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImUtf8.ItemSpacing.Y);
        using var child = CkRaii.ChildPaddedW("IncReqConfig", width, CkStyle.TwoRowHeight(), bgCol, 5f);

        // Maybe move the vars into a config so we can store them between plugin states.
        CkGui.FramedIconText(FAI.PeopleGroup);
        CkGui.TextFrameAlignedInline("Dummy Text");
    }

    // Accepts a single request.
    private void AcceptRequest(RequestEntry request)
    {
        UiService.SetUITask(async () =>
        {
            // Wait for the response.
            var res = await _hub.UserAcceptKinksterRequest(new(new(request.SenderUID))).ConfigureAwait(false);
            
            // If already paired, we should remove the request from the manager.
            if (res.ErrorCode is GagSpeakApiEc.AlreadyPaired)
                _manager.RemoveRequest(request);
            // Otherwise, if successful, proceed with pairing operations.
            else if (res.ErrorCode is GagSpeakApiEc.Success)
            {
                // Remove the request from the manager.
                _manager.RemoveRequest(request);
                // Add the Kinkster to the KinksterManager.
                _kinksters.AddKinkster(res.Value!.Pair);
                // If they are online, mark them online.
                if (res.Value!.OnlineInfo is { } onlineKinkster)
                    _kinksters.MarkKinksterOnline(onlineKinkster);
                
                // TODO: Add them to the groups we wanted to add them to.
                // TODO: Set their nick to the desired nick.
            }
        });
    }

    private void AcceptRequests(IEnumerable<RequestEntry> requests)
    {
        // Process the TO BE ADDED Bulk accept server call, then handle responses accordingly.

        // For now, do nothing.
    }

    private void RejectRequest(RequestEntry request)
    {
        UiService.SetUITask(async () =>
        {
            if (await _hub.UserRejectKinksterRequest(new(new(request.RecipientUID))) is { } res && res.ErrorCode is GagSpeakApiEc.Success)
                _manager.RemoveRequest(request);
        });
    }

    private void RejectRequests(IEnumerable<RequestEntry> requests)
    {
        // Process the TO BE ADDED Bulk reject server call, then handle responses accordingly.
        // For now, do nothing.
    }
    #endregion Utility
}

