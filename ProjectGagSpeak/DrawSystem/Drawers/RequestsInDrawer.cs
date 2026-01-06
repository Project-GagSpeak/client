using CkCommons;
using CkCommons.Classes;
using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using OtterGui.Text;
using TerraFX.Interop.Windows;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;
using static System.ComponentModel.Design.ObjectSelectorEditor;

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

    private IDynamicNode? _hoveredReplyNode;     // From last frame.
    private IDynamicNode? _newHoveredReplyNode;  // Tracked each frame.
    private DateTime? _hoverExpiry;            // time until we should hide the hovered reply node.

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

    protected override void UpdateHoverNode()
    {
        // if we are hovering something new, accept immidiately
        if (_newHoveredReplyNode != null)
        {
            _hoveredReplyNode = _newHoveredReplyNode;
            _hoverExpiry = null;
        }
        else if (_hoveredReplyNode != null)
        {
            _hoverExpiry ??= DateTime.Now.AddMilliseconds(350);
            // Check expiry every frame while still hovered, then gracefully clear.
            if (DateTime.Now >= _hoverExpiry)
            {
                _hoveredReplyNode = null;
                _hoverExpiry = null;
            }
        }

        _newHoveredReplyNode = null;
        base.UpdateHoverNode();
    }

    // Custom draw method spesifically for our incoming folder.
    public void DrawRequests(float width, DynamicFlags flags = DynamicFlags.None)
    {
        // Obtain the folder first before handling the draw logic.
        if (!DrawSystem.FolderMap.TryGetValue(Constants.FolderTagRequestInc, out var folder))
            return;

        // Ensure the child is at least draw to satisfy the expected drawn content region.
        using var _ = ImRaii.Child(Label, new Vector2(width, -1), false, WFlags.NoScrollbar);
        if (!_) return;

        // Handle any main context interactions such as right-click menus and the like.
        HandleMainContextActions();
        // Update the cache to its latest state.
        FilterCache.UpdateCache();

        if (!FilterCache.CacheMap.TryGetValue(folder, out var cachedNode))
            return;

        if (cachedNode is not DynamicFolderCache<RequestEntry> incRequests)
            return;

        // Set the style for the draw logic.
        ImGui.SetScrollX(0);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One)
            .Push(ImGuiStyleVar.IndentSpacing, 14f * ImGuiHelpers.GlobalScale);

        // Do not include any indentation.
        DrawIncomingRequests(incRequests, flags);
        PostDraw();
    }

    // We dont need to overprotect against ourselves when we know what we're drawing.
    // The only thing that should ever be drawn here is the incoming folder.
    // As such, create our own override for this drawer. 
    private void DrawIncomingRequests(DynamicFolderCache<RequestEntry> cf, DynamicFlags flags)
    {
        using var id = ImRaii.PushId($"DDS_{Label}_{cf.Folder.ID}");
        
        DrawFolderBanner(cf.Folder, flags);
        // The below, the request entries.
        DrawFolderLeaves(cf, flags);
    }

    private void DrawFolderBanner(IDynamicFolder<RequestEntry> f, DynamicFlags flags)
    {
        var width = CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX();
        // Display a framed child with stylizations based on the folders preferences.
        using var _ = CkRaii.FramedChildPaddedW($"df_{Label}_{f.ID}", width, ImUtf8.FrameHeight, f.BgColor, f.BorderColor, 5f, 1f);

        // No interactions, keep open.
        ImUtf8.SameLineInner();
        CkGui.IconTextAligned(f.Icon, f.IconColor);
        CkGui.ColorTextFrameAlignedInline(f.Name, f.NameColor);
        CkGui.ColorTextFrameAlignedInline($"[{f.TotalChildren}]", ImGuiColors.DalamudGrey2);

        // Could draw more stuff to the right if we want.
        if (Selector.Leaves.Count > 0 && f is RequestFolder folder)
            DrawFolderButtons(folder);        
    }

    private void DrawFolderButtons(RequestFolder folder)
    {
        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var rejectAllSize = CkGui.IconTextButtonSize(FAI.TimesCircle, "Reject All");
        var acceptAllSize = CkGui.IconTextButtonSize(FAI.CheckCircle, "Accept All");
        endX -= rejectAllSize + acceptAllSize + CkGui.GetSeparatorVWidth(inner: true);

        ImGui.SameLine(endX);
        using (ImRaii.PushColor(ImGuiCol.Text, CkColor.TriStateCheck.Uint()))
            if (CkGui.IconTextButton(FAI.CheckCircle, "Accept All", null, true, UiService.DisableUI))
                Log.Information("Accepting all incoming kinkster requests.");
        CkGui.AttachToolTip("Accept all incoming kinkster requests.");

        CkGui.FrameSeparatorV(inner: true);

        using (ImRaii.PushColor(ImGuiCol.Text, CkColor.TriStateCross.Uint()))
            if (CkGui.IconTextButton(FAI.TimesCircle, "Reject All", null, true, UiService.DisableUI))
                Log.Information("Rejecting all incoming kinkster requests.");
        CkGui.AttachToolTip("Reject all incoming kinkster requests.");
    }


    // Override each drawn leaf for its unique display in the request folder.
    protected override void DrawLeafInner(IDynamicLeaf<RequestEntry> leaf, Vector2 region, DynamicFlags flags)
    {
        DrawLeftSide(leaf.Data, flags);
        ImUtf8.SameLineInner();

        // Store the pos at the point we draw out the name area.
        var posX = ImGui.GetCursorPosX();

        // Draw out the responce area, and get where it ends.
        var rightSide = DrawRightSide(leaf, region.Y, flags);

        // Bounce back to the name area.
        ImGui.SameLine(posX);
        // Draw out the invisible button over the area to draw in.
        if (ImGui.InvisibleButton($"{leaf.FullPath}-hoverspace", new Vector2(rightSide - posX, region.Y)))
            HandleClick(leaf, flags);
        HandleDetections(leaf, flags);
        CkGui.AttachToolTip(ToolTip, ImGuiColors.DalamudOrange);

        // Bounce back and draw out the name.
        ImGui.SameLine(posX);
        using var _ = ImRaii.PushFont(UiBuilder.MonoFont);
        CkGui.TextFrameAligned(leaf.Data.SenderAnonName);
    }

    private void DrawLeftSide(RequestEntry entry, DynamicFlags flags)
    {
        // If there was an attached message then we should show it.
        if (entry.HasMessage)
            CkGui.FramedHoverIconText(FAI.CommentDots, ImGuiColors.TankBlue.ToUint());
        else
            CkGui.FramedIconText(FAI.CommentDots, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        CkGui.AttachToolTip($"--COL--Attached Message:--COL----SEP--{entry.Message}", !entry.HasMessage, ImGuiColors.ParsedGold);
    }

    // Draw out the responder entry.
    private float DrawRightSide(IDynamicLeaf<RequestEntry> leaf, float height, DynamicFlags flags)
    {
        // Get if this leaf if currently in a responding state.
        var replying = _hoveredReplyNode == leaf;

        // Grab the end of the selectable region.
        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var timeTxt = leaf.Data.GetRemainingTimeString();
        var buttonSize = CkGui.IconButtonSize(FAI.Times);
        var timeTxtWidth = ImGui.CalcTextSize(timeTxt).X;
        var spacing = ImUtf8.ItemInnerSpacing.X;

        var childWidth = replying 
            ? ImUtf8.FrameHeight + (buttonSize.X + spacing) * 2
            : ImUtf8.FrameHeight;

        endX -= childWidth;
        ImGui.SameLine(endX);
        using (CkRaii.Child("reply", new Vector2(childWidth, height), ImGui.GetColorU32(ImGuiCol.FrameBg), 12f))
        {
            if (replying)
            {
                using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f))
                {
                    // Draw out the initial frame with a small outer boarder.
                    if (CkGui.IconButtonColored(FAI.Check, CkColor.TriStateCheck.Uint(), UiService.DisableUI))
                        AcceptRequest(leaf.Data);
                    CkGui.AttachToolTip("Accept this kinkster request.");
                    ImUtf8.SameLineInner();
                    if (CkGui.IconButtonColored(FAI.Times, CkColor.TriStateCross.Uint(), UiService.DisableUI))
                        RejectRequest(leaf.Data);
                    CkGui.AttachToolTip("Reject this kinkster request.");
                    ImUtf8.SameLineInner();
                }
            }

            CkGui.FramedHoverIconText(FAI.Reply, uint.MaxValue);
            CkGui.AttachToolTip("Hover me to open single-request responder.");
        }
        // Should be if we hover anywhere in the area.
        if (ImGui.IsItemHovered())
            _newHoveredReplyNode = leaf;

        // Now the time.
        if (!replying)
        {
            endX -= (timeTxtWidth + spacing);
            ImGui.SameLine(endX);
            CkGui.ColorTextFrameAligned(timeTxt, ImGuiColors.ParsedGrey);
            CkGui.AttachToolTip("Time left to respond to this request.");
        }
        
        return endX;
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
            Log.Information($"Accepting kinkster request from {request.SenderAnonName} ({request.SenderUID})");
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
            }
            else
            {
                Log.Warning($"Failed to accept kinkster request from {request.SenderAnonName} ({request.SenderUID}): {res.ErrorCode}");
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
            var res = await _hub.UserRejectKinksterRequest(new(new(request.RecipientUID))).ConfigureAwait(false);
            if (res.ErrorCode is GagSpeakApiEc.Success)
                _manager.RemoveRequest(request);
            else
                Log.Warning($"Failed to reject kinkster request to {request.RecipientAnonName} ({request.RecipientUID}): {res.ErrorCode}");
        });
    }

    private void RejectRequests(IEnumerable<RequestEntry> requests)
    {
        // Process the TO BE ADDED Bulk reject server call, then handle responses accordingly.
        // For now, do nothing.
    }
    #endregion Utility
}

