using CkCommons.DrawSystem;
using CkCommons.DrawSystem.Selector;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using OtterGui.Text;
using static CkCommons.DrawSystem.DynamicSorterEx;

namespace GagSpeak.DrawSystem;

public sealed class PuppeteersDrawer : DynamicDrawer<Kinkster>
{
    private static readonly string Tooltip = "--COL--[M-CLICK]--COL-- Open Profile";

    private readonly GagspeakMediator _mediator;
    private readonly FavoritesConfig _favorites;

    private HashSet<string> _incognitoFolders = new(StringComparer.Ordinal);

    public PuppeteersDrawer(GagspeakMediator mediator, FavoritesConfig favorites, PuppeteersDrawSystem ds)
        : base("##GSPuppeteers", Svc.Logger.Logger, ds, new KinksterFolderCache(ds))
    {
        _mediator = mediator;
        _favorites = favorites;
    }

    public Kinkster? Selected => Selector.SelectedLeaf?.Data;

    protected override void DrawFolderBannerInner(IDynamicFolder<Kinkster> folder, Vector2 region, DynamicFlags flags)
        => DrawFolderInner((PairFolder)folder, region, flags);

    private void DrawFolderInner(PairFolder folder, Vector2 region, DynamicFlags flags)
    {
        var pos = ImGui.GetCursorPos();
        CkGui.FramedIconText(folder.IsOpen ? FAI.CaretDown : FAI.CaretRight);
        CkGui.ColorTextFrameAlignedInline(folder.Name, folder.NameColor);
        CkGui.ColorTextFrameAlignedInline($"[{folder.TotalChildren}]", ImGuiColors.DalamudGrey2);
        CkGui.AttachToolTip(folder.BracketTooltip);

        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        endX -= CkGui.IconButtonSize(FAI.EyeSlash).X;
        ImGui.SameLine(endX);
        if (CkGui.IconButton(_incognitoFolders.Contains(folder.Name) ? FAI.Eye : FAI.EyeSlash, inPopup: true))
        {
            if (_incognitoFolders.Contains(folder.Name))
                _incognitoFolders.Remove(folder.Name);
            else
                _incognitoFolders.Add(folder.Name);
        }
        CkGui.AttachToolTip("Toggles Anonymous View");

        ImGui.SameLine(pos.X);
        if (ImGui.InvisibleButton($"{Label}_node_{folder.ID}", new(endX - pos.X, region.Y)))
            HandleLeftClick(folder, flags);
        HandleDetections(folder, flags);
    }

    // This override intentionally prevents the inner method from being called so that we can call our own inner method.
    protected override void DrawLeaf(IDynamicLeaf<Kinkster> leaf, DynamicFlags flags, bool selected)
    {
        var cursorPos = ImGui.GetCursorPos();
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - cursorPos.X, ImUtf8.FrameHeight);
        var bgCol = selected ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0;
        using (var _ = CkRaii.Child(Label + leaf.Name, size, bgCol, 5f))
        {
            DrawPuppeteerLeaf(leaf, _.InnerRegion, flags);
            HandleContext(leaf);
        }

        // Draw out the supporter icon after if needed.
        if (leaf.Data.UserData.Tier is not CkSupporterTier.NoRole)
        {
            var Image = CosmeticService.GetSupporterInfo(leaf.Data.UserData);
            if (Image.SupporterWrap is { } wrap)
            {
                ImGui.SameLine(cursorPos.X);
                ImGui.SetCursorPosX(cursorPos.X - ImUtf8.FrameHeight - ImUtf8.ItemInnerSpacing.X);
                ImGui.Image(wrap.Handle, new Vector2(ImUtf8.FrameHeight));
                CkGui.AttachToolTip(Image.Tooltip);
            }
        }
    }

    // Inner leaf called by the above drawfunction, serving as a replacement for the default DrawLeafInner.
    private void DrawPuppeteerLeaf(IDynamicLeaf<Kinkster> leaf, Vector2 region, DynamicFlags flags)
    {
        ImUtf8.SameLineInner();
        // Store current position, then draw the right side.
        var posX = ImGui.GetCursorPosX();
        var rightSide = DrawRightButtons(leaf, flags);
        // Bounce back to the start position.
        ImGui.SameLine(posX);
        if (ImGui.InvisibleButton($"{leaf.FullPath}-name-area", new(rightSide - posX, region.Y)))
            HandleLeftClick(leaf, flags);
        HandleDetections(leaf, flags);

        // Then return to the start position and draw out the text.
        ImGui.SameLine(posX);
        if (_incognitoFolders.Contains(leaf.Parent.Name))
        {
            using (ImRaii.PushFont(UiBuilder.MonoFont))
                CkGui.TextFrameAligned(leaf.Data.UserData.AnonName);
        }
        else
        {
            // obtain the DisplayName (Player || Nick > Alias/UID).
            var dispName = leaf.Data.GetDisplayName();
            // If we should be showing the uid, then set the display name to it.
            var useMono = leaf.Data.UserData.AliasOrUID.Equals(dispName, StringComparison.Ordinal);
            // Display the name.
            using (ImRaii.PushFont(UiBuilder.MonoFont, useMono))
                CkGui.TextFrameAligned(dispName);
        }
        CkGui.AttachToolTip(Tooltip, ImGuiColors.DalamudOrange);
    }

    private float DrawRightButtons(IDynamicLeaf<Kinkster> leaf, DynamicFlags flags)
    {
        var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        endX -= ImUtf8.FrameHeight;

        ImGui.SameLine(endX);
        Icons.DrawFavoriteStar(_favorites, leaf.Data.UserData.UID, true);
        return endX;
    }

    protected override void HandleDetections(IDynamicLeaf<Kinkster> node, DynamicFlags flags)
    {
        if (ImGui.IsItemHovered())
            _newHoveredNode = node;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Middle))
            _mediator.Publish(new KinkPlateCreateOpenMessage(node.Data));

        // Handle Context Menus. (Maybe make a flag later. Would save on some drawtime.)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(node.FullPath);
    }

    // For Sending names and stuff
    protected override void DrawContextMenu(IDynamicLeaf<Kinkster> node)
    {
        base.DrawContextMenu(node);
    }
}

