using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using OtterGui;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class AliasesFileSelector : CkFileSystemSelector<AliasTrigger, AliasesFileSelector.AliasItemState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesConfig _favorites;
    private readonly PuppeteerManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct AliasItemState(uint Color)
    { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new AliasesFileSystem.Leaf? SelectedLeaf
        => base.SelectedLeaf;

    public AliasesFileSelector(GagspeakMediator mediator, FavoritesConfig favorites, PuppeteerManager manager, AliasesFileSystem fs) 
        : base(fs, Svc.Logger.Logger, Svc.KeyState, "##AliasesFS", true)
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigAliasItemChanged>(this, (msg) => OnAliasChanged(msg.Type, msg.Item, msg.OldString));

        // Do not subscribe to the default renamer, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        SubscribeRightClickLeaf(DissolveLeafOption);
        SubscribeRightClickLeaf(RenameAlias);
    }

    private void DissolveLeafOption(AliasesFileSystem.Leaf leaf)
    {
        // Some logic here to remove a leaf from its current folders to the root folder.
    }

    private void RenameAlias(AliasesFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Alias:");
        if (ImGui.InputText("##RenameAlias", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            // Rename later and stuff idk.
            // _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachToolTip("Enter a new name here to rename the alias.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
    }

    protected override bool DrawLeafInner(CkFileSystem<AliasTrigger>.Leaf leaf, in AliasItemState state, bool selected)
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
        // Begin drawing out the contents.
        using (ImRaii.Group())
        {
            Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Alias, leaf.Value.Identifier, false);
            keyElementHovered |= ImGui.IsItemHovered();
            CkGui.TextInline(leaf.Value.Label);

            ImGui.SameLine();
            if (leaf.Value.WhitelistedUIDs.Count is 0)
                CkGui.TagLabelText("Global", ImGuiColors.TankBlue, 3 * ImGuiHelpers.GlobalScale);
            else
                CkGui.TagLabelText("Whitelisted", ImGuiColors.ParsedGold.Darken(.35f), 3 * ImGuiHelpers.GlobalScale);
            // Next Line, draw out the name of the ref item.
            CkGui.ColorText($"Detects \"{leaf.Value.InputCommand}\"", ImGuiColors.DalamudGrey3);
        }

        var centerHeight = (ImGui.GetItemRectSize().Y - eraserSize.Y) / 2;
        ImGui.SameLine(leafSize.X - iconSpacing);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerHeight);
        var dotPos = ImGui.GetCursorScreenPos();
        var col = leaf.Value.Enabled ? CkCol.TriStateCheck.Uint() : CkCol.TriStateCross.Uint();
        CkGui.IconText(FAI.Circle, col);
        CkGui.AttachToolTip($"This Alias is {(leaf.Value.Enabled ? "Enabled" : "Disabled")}");

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
    private void OnAliasChanged(StorageChangeType type, AliasTrigger alias, string? oldString)
    {
        // Set the filter dirty regardless of type? (This will also close all folders and unselect).
        SetFilterDirty();
    }

    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFiltersWidth(float width)
    {
        return width
            - CkGui.IconButtonSize(FAI.Trash).X
            - CkGui.IconButtonSize(FAI.Plus).X
            - CkGui.IconButtonSize(FAI.FolderPlus).X
            - ImGui.GetStyle().ItemInnerSpacing.X;
    }

    protected override void DrawCustomFilters()
    {
        if (CkGui.IconButton(FAI.Trash, disabled: !ImGui.GetIO().KeyShift, inPopup: true))
        {
            if (SelectedLeaf is { } singleLeaf)
            {
                _manager.Delete(singleLeaf.Value);
            }
            else
            {
                var toDelete = SelectedPaths.OfType<CkFileSystem<AliasTrigger>.Leaf>();
                foreach (var leaf in toDelete)
                    _manager.Delete(leaf.Value);
            }
        }
        CkGui.AttachToolTip("Deletes this aliases selected.");

        ImGui.SameLine(0, 1);
        if (CkGui.IconButton(FAI.Plus, inPopup: true))
            ImGui.OpenPopup("##NewAlias");
        CkGui.AttachToolTip("Create a new Cursed Item.");

        ImGui.SameLine(0, 1);
        DrawFolderButton();
    }

    public override void DrawPopups()
        => NewAliasPopup();

    private void NewAliasPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewAlias", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

