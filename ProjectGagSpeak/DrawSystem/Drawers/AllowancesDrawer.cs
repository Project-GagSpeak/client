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
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Utils;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.DrawSystem;

/// <summary>
///     Draws the all folder for the Kinksters in the all kinksters folder. <para />
///     Has its own selection cache and selector while basing itself off the same DrawSystem to stay in sync.
/// </summary>
public sealed class AllowancesDrawer : DynamicDrawer<Kinkster>
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

    private readonly AllowancesConfig _config;
    private readonly FavoritesConfig _favorites;
    private readonly KinksterManager _kinksters;

    private GSModule _selectedModule = GSModule.None;

    // private vars for renaming items.
    private HashSet<IDynamicNode<Kinkster>> _showingUID = new(); // Nodes in here show UID.

    public AllowancesDrawer(AllowancesConfig config, FavoritesConfig favorites,
        KinksterManager kinksters, WhitelistDrawSystem ds)
        : base("##GSAlllowanceDrawer", Svc.Logger.Logger, ds, new KinksterFolderCache(ds))
    {
        _config    = config;
        _favorites = favorites;
        _kinksters = kinksters;
    }

    // Phase out, this should belong in another folder of the respective draw system,
    // or managed via a different Kinkster drawsystem.
    public ImmutableList<Kinkster> AllowedPairs = ImmutableList<Kinkster>.Empty;

    public GSModule CurModule
    {
        get => _selectedModule;
        set
        {
            if (_selectedModule == value)
                return;

            _selectedModule = value;
            RefreshAllowedList();
        }
    }

    public void AllowFiltered()
    {
        var toAdd = FilterCache.VisibleLeaves.Select(x => x.Data.UserData.UID);
        _config.AddAllowance(_selectedModule, toAdd);
    }

    public void AllowSelected()
    {
        var toAdd = Selector.Leaves.Select(x => x.Data.UserData.UID);
        _config.AddAllowance(_selectedModule, toAdd);
    }

    public void AllowFavorites()
    {
        var favUids = FilterCache.VisibleLeaves.Where(k => k.Data.IsFavorite).Select(x => x.Data.UserData.UID);
        _config.AddAllowance(_selectedModule, favUids);
    }

    public void DisallowFiltered()
    {
        var toRemove = FilterCache.VisibleLeaves.Select(x => x.Data.UserData.UID);
        _config.RemoveAllowance(_selectedModule, toRemove);
    }

    public void DisallowSelected()
    {
        var toRemove = Selector.Leaves.Select(x => x.Data.UserData.UID);
        _config.RemoveAllowance(_selectedModule, toRemove);
    }
    public void DisallowFavorites()
    {
        var favUids = FilterCache.VisibleLeaves.Where(k => k.Data.IsFavorite).Select(x => x.Data.UserData.UID);
        _config.RemoveAllowance(_selectedModule, favUids);
    }

    private void RefreshAllowedList()
    {
        var allowedUids = _selectedModule switch
        {
            GSModule.Restraint => _config.Restraints,
            GSModule.Restriction => _config.Restrictions,
            GSModule.Gag => _config.Gags,
            GSModule.Pattern => _config.Patterns,
            GSModule.Trigger => _config.Triggers,
            _ => Enumerable.Empty<string>()
        };
        AllowedPairs = _kinksters.DirectPairs.Where(pair => allowedUids.Contains(pair.UserData.UID)).ToImmutableList();
    }


    public void DrawAllKinkstersFolder(float width, bool onlyChildren, DynamicFlags flags = DynamicFlags.None)
    {
        // Grab the all folder from the draw system.
        if (!DrawSystem.TryGetFolder(Constants.FolderTagAll, out var folder))
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

        // Set the style for the draw logic.
        ImGui.SetScrollX(0);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One)
            .Push(ImGuiStyleVar.IndentSpacing, 14f * ImGuiHelpers.GlobalScale);
        // Draw out the node, for folders only.
        DrawClippedCacheNode(cachedNode, flags);
        PostDraw();
    }

    protected override void DrawSearchBar(float width, int length)
    {
        var tmp = FilterCache.Filter;
        // Could add a filter customizer here like we did in sundouleia for groups and stuff.
        if (FancySearchBar.Draw("Filter", width, ref tmp, "filter..", length))
            FilterCache.Filter = tmp;
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
            HandleClick(folder, flags);
        HandleDetections(folder, flags);

        // Back to the start, then draw.
        ImGui.SameLine(pos.X);
        CkGui.FramedIconText(folder.IsOpen ? FAI.CaretDown : FAI.CaretRight);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        CkGui.IconText(folder.Icon, folder.IconColor);
        CkGui.ColorTextFrameAlignedInline(folder.Name, folder.NameColor);
        // Total Context.
        CkGui.ColorTextFrameAlignedInline(folder.BracketText, ImGuiColors.DalamudGrey2);
        CkGui.AttachToolTip(folder.BracketTooltip);
    }

    #region KinksterLeaf
    // This override intentionally prevents the inner method from being called so that we can call our own inner method.
    protected override void DrawLeaf(IDynamicLeaf<Kinkster> leaf, DynamicFlags flags, bool selected)
    {
        var cursorPos = ImGui.GetCursorPos();
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - cursorPos.X, ImUtf8.FrameHeight);
        var bgCol = selected ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0;
        using (var _ = CkRaii.Child(Label + leaf.Name, size, bgCol, 5f))
        {
            ImUtf8.SameLineInner();
            // Store current position, then draw the right side.
            var posX = ImGui.GetCursorPosX();
            var rightSide = DrawRightButtons(leaf, flags);
            // Bounce back to the start position.
            ImGui.SameLine(posX);
            // If we are editing the name, draw that, otherwise, draw the name area.
            DrawNameDisplay(leaf, new(rightSide - posX, _.InnerRegion.Y), flags);
        };
    }

    private float DrawRightButtons(IDynamicLeaf<Kinkster> leaf, DynamicFlags flags)
    {
        var interactionsSize = CkGui.IconButtonSize(FAI.ChevronRight);
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var currentRightSide = windowEndX - interactionsSize.X;

        ImGui.SameLine(currentRightSide);
        ImGui.AlignTextToFramePadding();
        Icons.DrawFavoriteStar(_favorites, leaf.Data.UserData.UID, true);
        return currentRightSide;
    }

    private void DrawNameDisplay(IDynamicLeaf<Kinkster> leaf, Vector2 region, DynamicFlags flags)
    {
        // For handling Interactions.
        var isDragDrop = flags.HasAny(DynamicFlags.DragDropLeaves);
        var pos = ImGui.GetCursorPos();
        if (ImGui.InvisibleButton($"{leaf.FullPath}-name-area", region))
            HandleClick(leaf, flags);
        HandleDetections(leaf, flags);

        // Then return to the start position and draw out the text.
        ImGui.SameLine(pos.X);

        // Push the monofont if we should show the UID, otherwise dont.
        DrawKinksterName(leaf);
        CkGui.AttachToolTip(isDragDrop ? DragDropTooltip : NormalTooltip, ImGuiColors.DalamudOrange);
    }

    private void DrawKinksterName(IDynamicLeaf<Kinkster> s)
    {
        // Assume we use mono font initially.
        var useMono = true;
        // Get if we are set to show the UID over the name.
        var showUidOverName = _showingUID.Contains(s);
        // obtain the DisplayName (Player || Nick > Alias/UID).
        var dispName = string.Empty;
        // If we should be showing the uid, then set the display name to it.
        if (_showingUID.Contains(s))
        {
            // Mono Font is enabled.
            dispName = s.Data.UserData.AliasOrUID;
        }
        else
        {
            // Set it to the display name.
            dispName = s.Data.GetDisplayName();
            // Update mono to be disabled if the display name is not the alias/uid.
            useMono = s.Data.UserData.AliasOrUID.Equals(dispName, StringComparison.Ordinal);
        }

        // Display the name.
        using (ImRaii.PushFont(UiBuilder.MonoFont, useMono))
            CkGui.TextFrameAligned(dispName);
    }
    #endregion KinksterLeaf
}

