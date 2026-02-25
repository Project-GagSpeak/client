using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class CursedLootFileSelector : CkFileSystemSelector<CursedItem, CursedLootFileSelector.CursedItemState>, IMediatorSubscriber, IDisposable
{
    private readonly ActiveItemsDrawer _drawer;
    private readonly FavoritesConfig _favorites;
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

    public CursedLootFileSelector(GagspeakMediator mediator, ActiveItemsDrawer drawer, FavoritesConfig favorites, 
        CursedLootManager manager, CursedLootFileSystem fileSystem) 
        : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##CursedLootFS", true)
    {
        Mediator = mediator;
        _drawer = drawer;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigCursedItemChanged>(this, (msg) => OnCursedItemChange(msg.Type, msg.Item, msg.OldString));

        // Do not subscribe to the default renamer, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        SubscribeRightClickLeaf(DissolveLeafOption);
        SubscribeRightClickLeaf(PoolOptions);
        SubscribeRightClickLeaf(RenameCursedItem);
    }

    public override ISortMode<CursedItem> SortMode => new CursedLootSorter();

    private void DissolveLeafOption(CursedLootFileSystem.Leaf leaf)
    {
        // Some logic here to remove a leaf from its current folders to the root folder.
    }

    private void PoolOptions(CursedLootFileSystem.Leaf leaf)
    {
        if (ImGui.MenuItem($"{(leaf.Value.InPool ? "Remove item from" : "Add item to")} the Loot Pool"))
        {
            _manager.TogglePoolState(leaf.Value);
            ImGui.CloseCurrentPopup();
        }
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
        CkGui.AttachToolTip("Enter a new name here to rename the changed cursedItem.");
    }



    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
    }

    protected override bool DrawLeafInner(CkFileSystem<CursedItem>.Leaf leaf, in CursedItemState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        var eraserSize = CkGui.IconSize(FAI.Eraser);
        var rounding = ImGui.GetStyle().FrameRounding;
        var spacing = ImGui.GetStyle().ItemSpacing;
        var padding = new Vector2(spacing.X / 2);
        var iconSpacing = eraserSize.X + spacing.X;
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2 + spacing.X);
        ImGui.InvisibleButton("button-leaf", leafSize);
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var wdl = ImGui.GetWindowDrawList();
        var leafHovered = ImGui.IsItemHovered();
        var keyElementHovered = false;

        wdl.ChannelsSplit(2);
        wdl.ChannelsSetCurrent(1);

        ImGui.SetCursorScreenPos(rectMin + padding);
        var pos = ImGui.GetCursorScreenPos();
        var iconSize = new Vector2(leafSize.Y - spacing.X);
        // Draw out the actual contents based on the type.
        wdl.AddRectFilled(pos, pos + iconSize, ImGui.GetColorU32(ImGuiCol.FrameBg), rounding);
        // Begin drawing out the contents.
        if (leaf.Value is CursedGagItem gag)
        {
            _drawer.DrawFramedImage(gag.RefItem.GagType, iconSize.Y, rounding, 0);
            ImUtf8.SameLineInner();
            using (ImRaii.Group())
            {
                Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.CursedLoot, leaf.Value.Identifier, false);
                keyElementHovered |= ImGui.IsItemHovered();
                CkGui.TextInline(leaf.Value.Label);

                ImGui.SameLine();
                ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new Vector2(ImGui.GetTextLineHeight()));
                CkGui.AttachToolTip("Cursed Loot type is [Gag].");

                ImGui.SameLine();
                CkGui.TagLabelText(gag.Precedence.ToName(), gag.Precedence.ToColor(), 3 * ImGuiHelpers.GlobalScale);

                // Next Line, draw out the name of the ref item.
                CkGui.ColorText($"Applies: [{gag.RefLabel}]", ImGuiColors.DalamudGrey3);
            }
        }
        else if (leaf.Value is CursedRestrictionItem item)
        {
            _drawer.DrawRestrictionImage(item.RefItem, iconSize.Y, rounding, false);
            ImUtf8.SameLineInner();
            using (ImRaii.Group())
            {
                if (Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.CursedLoot, leaf.Value.Identifier, false))
                    SetFilterDirty();
                keyElementHovered = ImGui.IsItemHovered();
                CkGui.TextInline(leaf.Value.Label);

                ImGui.SameLine();
                ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Weighty].Handle, new Vector2(ImGui.GetTextLineHeight()));

                ImGui.SameLine();
                CkGui.TagLabelText(item.Precedence.ToName(), item.Precedence.ToColor(), 3 * ImGuiHelpers.GlobalScale);

                // Next Line, draw out the name of the ref item.
                CkGui.ColorText($"Applies: [{item.RefLabel}]", ImGuiColors.DalamudGrey3);
            }
        }
        else
            ImGui.Text("<Invalid Item Type>");

        var centerHeight = (ImGui.GetItemRectSize().Y - eraserSize.Y) / 2;
        var shiftPressed = KeyMonitor.ShiftPressed();
        ImGui.SameLine(leafSize.X - iconSpacing);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerHeight);
        var eraserPos = ImGui.GetCursorScreenPos();
        var overErase = ImGui.IsMouseHoveringRect(eraserPos, eraserPos + eraserSize);
        var allowErase = !leaf.Value.InPool && overErase && shiftPressed && !leaf.Value.Identifier.Equals(_manager.ItemInEditor?.Identifier);
        var col = allowErase ? ImGuiCol.Text : ImGuiCol.TextDisabled;
        CkGui.IconText(FAI.Eraser, ImGui.GetColorU32(col));
        keyElementHovered |= overErase;
        if (allowErase && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            _manager.Delete(leaf.Value);
        CkGui.AttachToolTip(leaf.Value.InPool ? "Cannot delete while in loot pool" : "Delete this cursed Item. This cannot be undone.--SEP--Must be holding SHIFT to remove.");


        wdl.ChannelsSetCurrent(0);
        // Draw the bg, and the selected items.
        var wasHovered = leafHovered && !keyElementHovered;
        var bgColor = wasHovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        wdl.AddRectFilled(rectMin, rectMax, bgColor, 7 * ImGuiHelpers.GlobalScale);
        // If Selected, draw out the gradient.
        if (selected)
        {
            wdl.AddRectFilledMultiColor(rectMin, rectMin + leafSize, CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)), 0, 0, CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)));
            wdl.AddRectFilled(rectMin, new Vector2(rectMin.X + ImGuiHelpers.GlobalScale * 3, rectMax.Y), CkGui.Color(ImGuiColors.ParsedPink), 7 * ImGuiHelpers.GlobalScale);
        }

        wdl.ChannelsMerge();
        return leafHovered && !keyElementHovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnCursedItemChange(StorageChangeType type, CursedItem cursedItem, string? oldString)
    {
        // Set the filter dirty regardless of type? (This will also close all folders and unselect).
        SetFilterDirty();
    }

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

    // Placeholder until we Integrate the DynamicSorter
    private struct CursedLootSorter : ISortMode<CursedItem>
    {
        public string Name
            => "Cursed Loot Sorter";

        public string Description
            => "Sort all cursed loot by their name, with favorites first.";

        public IEnumerable<CkFileSystem<CursedItem>.IPath> GetChildren(CkFileSystem<CursedItem>.Folder folder)
            => folder.GetSubFolders().Cast<CkFileSystem<CursedItem>.IPath>()
                .Concat(folder.GetLeaves().OrderByDescending(l => FavoritesConfig.CursedLoot.Contains(l.Value.Identifier)));
    }
}

