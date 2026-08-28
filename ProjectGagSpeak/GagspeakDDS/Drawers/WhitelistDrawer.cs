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
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagspeakAPI.User;
using OtterGui.Text;

namespace GagSpeak.DrawSystem;

public sealed class WhitelistDrawer : DynamicDrawer<Kinkster>
{
    // Static tooltips for leaves.
    private static readonly string DragDropTooltip =
        "--COL--[L-CLICK & DRAG]--COL-- Drag-Drop this User to another Folder." +
        "--NL----COL--[CTRL + L-CLICK]--COL-- Single-Select this item for multi-select Drag-Drop" +
        "--NL----COL--[SHIFT + L-CLICK]--COL-- Select/Deselect all from last selected";
    private static readonly string NormalTooltip =
        "--COL--[L-CLICK]--COL-- Swap Between Name/Nick/Alias & UID." +
        "--NL----COL--[M-CLICK]--COL-- Open Profile" +
        "--NL----COL--[SHIFT + R-CLICK]--COL-- Edit Nickname";

    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly FavoritesConfig _favorites;
    private readonly NicksConfig _nicks;
    private readonly KinksterManager _kinksters;
    private readonly WhitelistDrawSystem _drawSystem;
    private readonly SidePanelService _stickyService;

    private WhitelistCache _cache => (WhitelistCache)FilterCache;

    // Popout Tracking.
    private IDynamicNode? _hoveredTextNode;     // From last frame.
    private IDynamicNode? _newHoveredTextNode;  // Tracked each frame.
    private bool          _profileShown = false;// If currently displaying a popout profile.
    private DateTime?     _lastHoverTime;       // time until we should show the profile.

    public WhitelistDrawer(GagspeakMediator mediator, MainConfig config, FavoritesConfig favorites,
        NicksConfig nicks, KinksterManager kinksters, SidePanelService stickyService, WhitelistDrawSystem ds)
        : base("##GSWhitelistDrawer", Svc.Logger.Logger, ds, new WhitelistCache(ds))
    {
        _mediator = mediator;
        _config = config;
        _favorites = favorites;
        _nicks = nicks;
        _kinksters = kinksters;
        _drawSystem = ds;
        _stickyService = stickyService;
    }

    #region Search
    protected override void DrawSearchBar(float width, int length)
    {
        var tmp = FilterCache.Filter;
        // Update the search bar if things change, like normal.
        if (FancySearchBar.DrawWithButtons("Filter", "filter..", width, ref tmp, length, CkGui.IconTextButtonSize(FAI.Cog, "Settings"), DrawButtons))
            FilterCache.Filter = tmp;

        // If the config is expanded, draw that.
        if (_cache.FilterConfigOpen)
            DrawFilterConfig(width);

        void DrawButtons()
        {
            if (CkGui.IconTextButton(FAI.Cog, "Settings", isInPopup: !_cache.FilterConfigOpen))
                _cache.FilterConfigOpen = !_cache.FilterConfigOpen;
            CkGui.AttachTooltip("Configure preferences for folders.");
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
        // Before we update the nodes we should run a comparison to see if they changed.
        // If they did we should close any popup if opened.
        if (_hoveredTextNode != _newHoveredTextNode)
        {
            if (!_profileShown)
                _lastHoverTime = _newHoveredTextNode is null ? null : DateTime.UtcNow.AddSeconds(_config.Data.ProfileDelay);
            else
            {
                _lastHoverTime = null;
                _profileShown = false;
                _mediator.Publish(new CloseProfilePopout());
            }
        }

        // Update the hovered text node stuff.
        _hoveredTextNode = _newHoveredTextNode;
        _newHoveredTextNode = null;

        // Now properly update the hovered node.
        _hoveredNode = _newHoveredNode;
        _newHoveredNode = null;
    }

    // Look further into luna for how to cache the runtime type to remove any nessisary casting.
    // AKA Creation of "CachedNodes" of defined types.
    // For now this will do.
    protected override void DrawFolderBannerInner(IDynamicFolder<Kinkster> folder, Vector2 region, DynamicFlags flags)
        => DrawFolderInner((PairFolder)folder, region, flags);

    private void DrawFolderInner(PairFolder folder, Vector2 region, DynamicFlags flags)
    {
        var pos = ImGui.GetCursorPos();
        if (ImGui.InvisibleButton($"{Label}_node_{folder.ID}", region))
            HandleLeftClick(folder, flags);
        HandleDetections(folder, flags);

        // Back to the start, then draw.
        ImGui.SameLine(pos.X);
        CkGui.FramedIconText(folder.Expanded ? FAI.CaretDown : FAI.CaretRight);
        ImGui.SameLine();
        CkGui.IconTextAligned(folder.Icon, folder.IconColor);
        CkGui.ColorTextFrameAlignedInline(folder.Name, folder.NameColor);
        // Total Context.
        CkGui.ColorTextFrameAlignedInline(folder.BracketText, ImGuiColors.DalamudGrey2);
        CkGui.AttachTooltip(folder.BracketTooltip);
    }

    #region KinksterLeaf
    // This override intentionally prevents the inner method from being called so that we can call our own inner method.
    protected override void DrawLeaf(IDynamicLeaf<Kinkster> leaf, DynamicFlags flags, bool selected)
    {
        var cursorPos = ImGui.GetCursorPos();
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - cursorPos.X, ImUtf8.FrameHeight);
        var editing = _cache.RenamingNode == leaf;
        var bgCol = (!editing && selected) ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0;
        using (var _ = CkRaii.Child(Label + leaf.Name, size, bgCol, 5f))
            DrawLeafInner(leaf, _.InnerRegion, flags, editing);

        // Draw out the supporter icon after if needed.
        if (leaf.Data.User.Tier is not CkVanityTier.NoRole)
        {
            var Image = CosmeticService.GetSupporterInfo(leaf.Data.User);
            if (Image.SupporterWrap is { } wrap)
            {
                ImGui.SameLine(cursorPos.X);
                ImGui.SetCursorPosX(cursorPos.X - ImUtf8.FrameHeight - ImUtf8.ItemInnerSpacing.X);
                ImGui.Image(wrap.Handle, new Vector2(ImUtf8.FrameHeight));
                CkGui.AttachTooltip(Image.Tooltip);
            }
        }
    }

    // Inner leaf called by the above drawfunction, serving as a replacement for the default DrawLeafInner.
    private void DrawLeafInner(IDynamicLeaf<Kinkster> leaf, Vector2 region, DynamicFlags flags, bool editing)
    {
        ImUtf8.SameLineInner();
        DrawLeftSide(leaf.Data, flags);
        ImGui.SameLine();

        // Store current position, then draw the right side.
        var posX = ImGui.GetCursorPosX();
        var rightSide = DrawRightButtons(leaf, flags);
        // Bounce back to the start position.
        ImGui.SameLine(posX);
        // If we are editing the name, draw that, otherwise, draw the name area.
        if (editing)
            DrawNameEditor(leaf, region.X);
        else
            DrawNameDisplay(leaf, new(rightSide - posX, region.Y), flags);
    }

    private void DrawLeftSide(Kinkster s, DynamicFlags flags)
    {
        var icon = s.IsRendered ? FAI.Eye : FAI.User;
        var color = s.IsOnline ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
        CkGui.IconTextAligned(icon, color);
        CkGui.AttachTooltip(TooltipText(s));
        if (!flags.HasAny(DynamicFlags.DragDropLeaves) && s.IsRendered && ImGui.IsItemClicked())
            _mediator.Publish(new TargetKinksterMessage(s));
    }

    private float DrawRightButtons(IDynamicLeaf<Kinkster> leaf, DynamicFlags flags)
    {
        var interactionsSize = CkGui.IconButtonSize(FAI.ChevronRight);
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - interactionsSize.X;

        ImGui.SameLine(currentRightSide);
        if (!flags.HasAny(DynamicFlags.DragDrop))
        {
            if (CkGui.IconButton(FAI.ChevronRight, inPopup: true))
                _stickyService.ForInteractions(leaf.Data);

            currentRightSide -= interactionsSize.X;
            ImGui.SameLine(currentRightSide);
        }

        ImGui.AlignTextToFramePadding();
        // Would need to refresh all folders they are a part of since they could be in any of them.
        // Could probably remove this after setting up a mediator call for favorite changes.
        if (Icons.DrawFavoriteStar(_favorites, leaf.Data.User.UID, true))
            _cache.MarkCacheDirty();
        return currentRightSide;
    }

    private void DrawNameEditor(IDynamicLeaf<Kinkster> leaf, float width)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.InputTextWithHint($"##{leaf.FullPath}-nick", "Give a nickname..", ref _cache.NameEditStr, 45, ITFlags.EnterReturnsTrue))
        {
            if (!_cache.NameEditStr.IsNullOrWhitespace())
                _nicks.SetNickname(leaf.Data.User.UID, _cache.NameEditStr);
            else
                _nicks.ClearNickname(leaf.Data.User.UID);
            _cache.RenamingNode = null;
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            _cache.RenamingNode = null;
        // Helper tooltip.
        CkGui.AttachTooltip("--COL--[ENTER]--COL-- To save" +
            "--NL----COL--[R-CLICK]--COL-- Cancel edits.", ImGuiColors.DalamudOrange);
    }

    private void DrawNameDisplay(IDynamicLeaf<Kinkster> leaf, Vector2 region, DynamicFlags flags)
    {
        // For handling Interactions.
        var isDragDrop = flags.HasAny(DynamicFlags.DragDropLeaves);
        var pos = ImGui.GetCursorPos();
        if (ImGui.InvisibleButton($"{leaf.FullPath}-name-area", region))
            HandleLeftClick(leaf, flags);
        HandleDetections(leaf, flags);

        // Then return to the start position and draw out the text.
        ImGui.SameLine(pos.X);

        // Push the monofont if we should show the UID, otherwise dont.
        DrawKinksterName(leaf);
        CkGui.AttachTooltip(isDragDrop ? DragDropTooltip : NormalTooltip, ImGuiColors.DalamudOrange);
        if (isDragDrop)
            return;
        // Handle hover state.
        if (ImGui.IsItemHovered())
        {
            _newHoveredTextNode = leaf;

            // If we should show it, and it is not already shown, show it.
            if (!_profileShown && _lastHoverTime < DateTime.UtcNow && _config.Data.ShowProfiles)
            {
                _profileShown = true;
                _mediator.Publish(new OpenProfilePopout(leaf.Data.User));
            }
        }
    }

    private void DrawKinksterName(IDynamicLeaf<Kinkster> s)
    {
        // Assume we use mono font initially.
        var useMono = true;
        // Get if we are set to show the UID over the name.
        var showUidOverName = _cache.ShowingUID.Contains(s);
        // obtain the DisplayName (Player || Nick > Alias/UID).
        var dispName = string.Empty;
        // If we should be showing the uid, then set the display name to it.
        if (_cache.ShowingUID.Contains(s))
        {
            // Mono Font is enabled.
            dispName = s.Data.User.AliasOrUID;
        }
        else
        {
            // Set it to the display name.
            dispName = s.Data.GetDisplayName();
            // Update mono to be disabled if the display name is not the alias/uid.
            useMono = s.Data.User.AliasOrUID.Equals(dispName, StringComparison.Ordinal);
        }

        // Display the name.
        using (ImRaii.PushFont(UiBuilder.MonoFont, useMono))
            CkGui.TextFrameAligned(dispName);
    }

    protected override void HandleDetections(IDynamicLeaf<Kinkster> node, DynamicFlags flags)
    {
        if (ImGui.IsItemHovered())
            _newHoveredNode = node;

        // Handle Drag and Drop.
        if (flags.HasAny(DynamicFlags.DragDropLeaves))
        {
            AsDragDropSource(node);
            AsDragDropTarget(node);
        }
        else
        {
            // Additional, KinksterLeaf-Specific interaction handles.
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                if (!_cache.ShowingUID.Remove(node))
                    _cache.ShowingUID.Add(node);
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Middle))
            {
                _mediator.Publish(new OpenUserProfileMessage(node.Data));
            }
            if (ImGui.GetIO().KeyShift && ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _cache.RenamingNode = node;
                _cache.NameEditStr = node.Data.GetNickname() ?? string.Empty;
            }
        }
    }

    private string TooltipText(Kinkster s)
    {
        var str = $"{s.GetNickAliasOrUid()} is ";
        if (s.IsRendered) str += $"visible ({s.PlayerName})--SEP--Click to target this player";
        else if (s.IsOnline) str += "online";
        else str += "offline";
        return str;
    }

    #endregion KinksterLeaf

    #region Utility
    private void DrawFilterConfig(float width)
    {
        var bgCol = _cache.FilterConfigOpen ? ColorHelpers.Fade(ImGui.GetColorU32(ImGuiCol.FrameBg), 0.4f) : 0;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImUtf8.ItemSpacing.Y);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, ImGui.GetStyle().CellPadding with { Y = 0 });
        using var child = CkRaii.ChildPaddedW("BasicExpandedChild", width, CkStyle.ThreeRowHeight(), bgCol, 5f);
        using var _ = ImRaii.Table("BasicExpandedTable", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV);
        if (!_)
            return;

        ImGui.TableSetupColumn("Displays");
        ImGui.TableSetupColumn("Preferences");
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var showVisible = _config.Data.VisibleFolder;
        if (ImGui.Checkbox(GSLoc.Settings.DDSPrefs.ShowVisibleSeparateLabel, ref showVisible))
        {
            _config.Data.VisibleFolder = showVisible;
            _config.Save();
            Log.Information("Regenerating Basic Folders due to Visible Folder setting change.");
            // Update the folder structure to reflect this change.
            _drawSystem.UpdateVisibleFolderState(showVisible);
        }
        CkGui.AttachTooltip(GSLoc.Settings.DDSPrefs.ShowVisibleSeparateTT);

        var showOffline = _config.Data.OfflineFolder;
        if (ImGui.Checkbox(GSLoc.Settings.DDSPrefs.ShowOfflineSeparateLabel, ref showOffline))
        {
            _config.Data.OfflineFolder = showOffline;
            _config.Save();
            _drawSystem.UpdateOfflineFolderState(showOffline);
        }
        CkGui.AttachTooltip(GSLoc.Settings.DDSPrefs.ShowOfflineSeparateTT);

        var useFocusTarget = _config.Data.UseFocusTargetOnUsers;
        if (ImGui.Checkbox(GSLoc.Settings.DDSPrefs.FocusTargetLabel, ref useFocusTarget))
        {
            _config.Data.UseFocusTargetOnUsers = useFocusTarget;
            _config.Save();
        }
        CkGui.AttachTooltip(GSLoc.Settings.DDSPrefs.FocusTargetTT);

        ImGui.TableNextColumn();
        var nickOverName = _config.Data.UseNicksOverPlayerNames;
        if (ImGui.Checkbox(GSLoc.Settings.DDSPrefs.PreferNicknamesLabel, ref nickOverName))
        {
            _config.Data.UseNicksOverPlayerNames = nickOverName;
            _config.Save();
        }
        CkGui.AttachTooltip(GSLoc.Settings.DDSPrefs.PreferNicknamesTT);
    }
    #endregion Utility
}
