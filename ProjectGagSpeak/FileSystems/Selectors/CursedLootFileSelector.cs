using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using OtterGuiInternal.Structs;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class CursedLootFileSelector : CkFileSystemSelector<CursedItem, CursedLootFileSelector.CursedItemState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly CursedLootManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct CursedItemState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new CursedLootFileSystem.Leaf? SelectedLeaf
        => base.SelectedLeaf;

    public CursedLootFileSelector(GagspeakMediator mediator, FavoritesManager favorites, CursedLootManager manager,
        CursedLootFileSystem fileSystem) : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##CursedLootFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigCursedItemChanged>(this, (msg) => OnCursedItemChange(msg.Type, msg.Item, msg.OldString));

        // Do not subscribe to the default renamer, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        SubscribeRightClickLeaf(RenameCursedItem);
    }

    private void RenameCursedItem(CursedLootFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename CursedItem:");
        if (ImGui.InputText("##RenameCursedItem", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed cursedItem.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
    }

    protected override void DrawLeafInner(CkFileSystem<CursedItem>.Leaf leaf, in CursedItemState state, bool selected)
    {
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.InvisibleButton("##leaf", leafSize);
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();

        var iconSize = CkGui.IconSize(FAI.Trash).X;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var iconSpacing = iconSize + spacing;

        ImRect leftOfFav = new(rectMin, rectMin + new Vector2(spacing, leafSize.Y));
        ImRect rightOfFav = new(rectMin + new Vector2(iconSpacing, 0), rectMin + leafSize - new Vector2(iconSpacing * 2, 0));

        var wasHovered = ImGui.IsMouseHoveringRect(leftOfFav.Min, leftOfFav.Max) || ImGui.IsMouseHoveringRect(rightOfFav.Min, rightOfFav.Max);
        // Draw the base frame, colored.
        var bgColor = wasHovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgColor, 5);

        if (selected)
        {
            ImGui.GetWindowDrawList().AddRectFilledMultiColor(
                rectMin,
                rectMin + leafSize,
                CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)), 0, 0, CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)));

            ImGui.GetWindowDrawList().AddRectFilled(
                rectMin,
                new Vector2(rectMin.X + ImGuiHelpers.GlobalScale * 3, rectMax.Y),
                CkGui.Color(ImGuiColors.ParsedPink), 5);
        }

        // Contents.
        ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
        Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.CursedLoot, leaf.Value.Identifier);
        CkGui.TextFrameAlignedInline(leaf.Value.Label);
        // Only draw the deletion if the item is not active or occupied.
        var isInPool = leaf.Value.InPool;
        var shiftPressed = KeyMonitor.ShiftPressed();
        var mouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

        var currentX = leafSize.X - iconSpacing;
        ImGui.SameLine(currentX);
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
        var col = (!isInPool && hovering && shiftPressed) ? ImGuiCol.Text : ImGuiCol.TextDisabled;
        CkGui.FramedIconText(FAI.Trash, ImGui.GetColorU32(col));
        if (hovering && shiftPressed && mouseReleased)
        {
            Log.Debug($"Deleting {leaf.Value.Label} with SHIFT pressed.");
            _manager.Delete(leaf.Value);
        }
        CkGui.AttachToolTip("Delete this cursed Item. This cannot be undone.--SEP--Must be holding SHIFT to remove.");

        currentX -= iconSpacing;
        ImGui.SameLine(currentX);
        pos = ImGui.GetCursorScreenPos();
        hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
        col = (!isInPool && hovering) ? ImGuiCol.Text : ImGuiCol.TextDisabled;
        CkGui.FramedIconText(FAI.ArrowRight, ImGui.GetColorU32(col));
        if (hovering && !isInPool && mouseReleased)
        {
            Log.Debug($"Adding {leaf.Value.Label} to the Cursed Loot Pool.");
            _manager.TogglePoolState(leaf.Value);
        }
        CkGui.AttachToolTip("Put this Item in the Cursed Loot Pool.");
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnCursedItemChange(StorageChangeType type, CursedItem cursedItem, string? oldString)
        => SetFilterDirty();

    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFiltersWidth(float width)
    {
        return width
            - CkGui.IconButtonSize(FAI.Plus).X
            - CkGui.IconButtonSize(FAI.FolderPlus).X
            - ImGui.GetStyle().ItemInnerSpacing.X;
    }

    protected override void DrawCustomFilters()
    {
        if (CkGui.IconButton(FAI.Plus, inPopup: true))
            ImGui.OpenPopup("##NewCursedItem");
        CkGui.AttachToolTip("Create a new Cursed Item.");

        ImGui.SameLine(0, 1);
        DrawFolderButton();
    }

    public override void DrawPopups()
        => NewCursedItemPopup();

    private void NewCursedItemPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewCursedItem", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

