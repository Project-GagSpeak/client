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
public class RequestsOutDrawer : DynamicDrawer<RequestEntry>
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

    public RequestsOutDrawer(MainHub hub, MainConfig config, RequestsManager manager, 
        KinksterManager kinksters, SidePanelService sidePanel, RequestsDrawSystem ds) 
        : base("##GSRequestsOutDrawer", Svc.Logger.Logger, ds, new RequestCache(ds))
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
    protected override void DrawSearchBar(float width, int length)
    {
        var tmp = FilterCache.Filter;
        var buttonsWidth = CkGui.IconButtonSize(FAI.Wrench).X + CkGui.IconTextButtonSize(FAI.Stopwatch, "Outgoing");
        // Update the search bar if things change, like normal.
        if (FancySearchBar.Draw("Filter", width, ref tmp, "filter..", length, buttonsWidth, DrawButtons))
            FilterCache.Filter = tmp;

        // Update the side panel if currently set to none, but drawing incoming.
        if (_sidePanel.DisplayMode is not SidePanelMode.PendingRequests)
            _sidePanel.ForRequests(SidePanelMode.PendingRequests, _cache, Selector);
        // Draw the config if open.
        if (_cache.FilterConfigOpen)
            DrawConfig(width);

        void DrawButtons()
        {
            if (CkGui.IconTextButton(FAI.Stopwatch, "Outgoing", null, true, MultiSelecting || _cache.FilterConfigOpen))
            {
                _config.Current.ViewingIncoming = !_config.Current.ViewingIncoming;
                _config.Save();
                // Update the side panel service.
                _sidePanel.ForRequests(SidePanelMode.IncomingRequests, _cache, Selector);
            }
            CkGui.AttachToolTip($"Switch to incoming requests.");

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
    public void DrawPendingRequests(float width, DynamicFlags flags = DynamicFlags.None)
    {
        // Obtain the folder first before handling the draw logic.
        if (!DrawSystem.FolderMap.TryGetValue(Constants.FolderTagRequestPending, out var folder))
            return;
        // Draw the singular folder.
        DrawFolder(folder, width, flags);
    }
    protected override void DrawFolderBannerInner(IDynamicFolder<RequestEntry> folder, Vector2 region, DynamicFlags flags)
        => DrawPendingRequests((RequestFolder)folder, region, flags);

    // Split between an incoming and outgoing folder maybe.
    private void DrawPendingRequests(RequestFolder folder, Vector2 region, DynamicFlags flags)
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
        CkGui.AttachToolTip("Select requests sent from your current world. (No Function)");

        currentRightSide -= byAreaSize;
        ImGui.SameLine(currentRightSide);
        if (CkGui.IconTextButton(FAI.Map, "In Area", null, true, UiService.DisableUI))
        {
            Selector.SelectMultiple([]);
        }
        CkGui.AttachToolTip("Select requests sent from your current area. (No Function)");

        return currentRightSide;
    }

    // Will only ever be incoming in our case.
    protected override void DrawLeaf(IDynamicLeaf<RequestEntry> leaf, DynamicFlags flags, bool selected)
    {
        var timeLeftText = $"{leaf.Data.TimeToRespond.Days}d {leaf.Data.TimeToRespond.Hours}h {leaf.Data.TimeToRespond.Minutes}m";
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImUtf8.FrameHeight);
        var bgCol = selected ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0;

        using var _ = CkRaii.Child(Label + leaf.Name, size, bgCol, 5f);
        // Inner content, first row.
        ImUtf8.SameLineInner();
        var posX = ImGui.GetCursorPosX();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
            CkGui.TextFrameAlignedInline(leaf.Data.SenderAnonName);
               
        var rightX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        rightX -= CkGui.IconButtonSize(FAI.Times).X;
        ImGui.SameLine(rightX);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
            if (CkGui.IconButton(FAI.Times, disabled: UiService.DisableUI, inPopup: true))
                CancelRequest(leaf.Data);
        CkGui.AttachToolTip("Cancel this pending request.");

        rightX -= ImUtf8.FrameHeight;
        ImGui.SameLine(rightX);
        CkGui.FramedHoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint());
        CkGui.AttachToolTip($"--COL--[Requested Nick]:--COL-- <UNKNOWN>" +
            $"--NL----COL--[Message]:--COL----NL--{leaf.Data.AttachedMessage}", ImGuiColors.DalamudOrange);
        
        rightX -= ImGui.CalcTextSize(timeLeftText).X;
        ImGui.SameLine(rightX);
        CkGui.ColorTextFrameAligned(timeLeftText, ImGuiColors.ParsedGold);

        // Shift back and draw the button over the area.
        ImGui.SameLine(posX);
        if (ImGui.InvisibleButton($"request_{leaf.FullPath}", new Vector2(rightX - posX, ImUtf8.FrameHeight)))
            HandleClick(leaf, flags);
        HandleDetections(leaf, flags);
        CkGui.AttachToolTip(ToolTip, ImGuiColors.DalamudOrange);
    }

    protected override void HandleClick(IDynamicLeaf<RequestEntry> node, DynamicFlags flags)
    {
        // Handle Selection.
        if (flags.HasAny(DynamicFlags.SelectableLeaves))
        {
            Selector.SelectItem(node, flags.HasFlag(DynamicFlags.MultiSelect), flags.HasFlag(DynamicFlags.RangeSelect));
            // If we have more than 
        }
    }

    private void DrawConfig(float width)
    {
        var bgCol = ColorHelpers.Fade(ImGui.GetColorU32(ImGuiCol.FrameBg), 0.4f);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImUtf8.ItemSpacing.Y);
        using var child = CkRaii.ChildPaddedW("IncReqConfig", width, CkStyle.TwoRowHeight(), bgCol, 5f);

        CkGui.FramedIconText(FAI.Question);
        CkGui.TextFrameAlignedInline("Do we even need this? Or could it all be one??");
    }

    private void CancelRequest(RequestEntry request)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _hub.UserCancelKinksterRequest(new(new(request.RecipientUID)));
            if (res.ErrorCode is GagSpeakApiEc.Success)
                _manager.RemoveRequest(request);
        });
    }

    private void CancelRequests(IEnumerable<RequestEntry> requests)
    {
        // Process the TO BE ADDED Bulk cancel server call, then handle responses accordingly.
        // For now, do nothing.
    }
}

