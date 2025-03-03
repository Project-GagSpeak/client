using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.FileSystem.Selector;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.Utils;
using ImGuiNET;
using static GagSpeak.Restrictions.RestrictionFileSelector;
using Dalamud.Interface.Utility.Raii;
using GagspeakAPI.Extensions;
using OtterGui.Text;
using OtterGui;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class GagRestrictionFileSelector : CkFileSystemSelector<GarblerRestriction, GagRestrictionFileSelector.GagRestrictionState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly GagRestrictionManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct GagRestrictionState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new GagFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public GagRestrictionFileSelector(ILogger<GagRestrictionFileSelector> log, GagspeakMediator mediator, FavoritesManager favorites,
        GagRestrictionManager manager, GagFileSystem fileSystem, IKeyState keys) : base(fileSystem, log, keys, "##GagsFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigGagRestrictionChanged>(this, (msg) => OnGagRestrictionChange(msg.Type, msg.Item, msg.OldString));

        // we can add, or unsubscribe from buttons here. Remember this down the line, it will become useful.
        UnsubscribeRightClickLeaf(RenameLeaf);
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.Unsubscribe<ConfigGagRestrictionChanged>(this);
    }

    // can override the selector here to mark the last selected set in the config or something somewhere.

    protected override void DrawLeafName(CkFileSystem<GarblerRestriction>.Leaf leaf, in GagRestrictionState state, bool selected)
    {
        using var id = ImRaii.PushId((int)leaf.Identifier);
        using var leafInternalGroup = ImRaii.Group();
        DrawLeafInternal(leaf, state, selected);
    }

    private void DrawLeafInternal(CkFileSystem<GarblerRestriction>.Leaf leaf, in GagRestrictionState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        ImGui.InvisibleButton(leaf.Value.GagType.GagName(), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()));
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = ImGui.IsItemHovered() ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);

        using (ImRaii.Group())
        {
            ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
            ImGui.AlignTextToFramePadding();
            Icons.DrawFavoriteStar(_favorites, leaf.Value.GagType);
            ImGui.SameLine();
            ImGui.Text(leaf.Value.GagType.GagName());
        }

        // the border if selected.
        if (selected)
        {
            ImGui.GetWindowDrawList().AddRectFilled(
                rectMin,
                new Vector2(rectMin.X + ImGuiHelpers.GlobalScale * 3, rectMax.Y),
                CkGui.Color(ImGuiColors.ParsedPink), 5);
        }
    }

    protected override void DrawFolderName(CkFileSystem<GarblerRestriction>.Folder folder, bool selected)
    {
        using var id = ImRaii.PushId((int)folder.Identifier);
        using var group = ImRaii.Group();
        CkGuiUtils.DrawFolderSelectable(folder, FolderLineColor, selected);
    }

    // if desired, can override the colors for expanded, collapsed, and folder line colors.
    // Can also define if the folders are open by default or not.

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnGagRestrictionChange(StorageItemChangeType type, GarblerRestriction gagRestriction, string? oldString)
        => SetFilterDirty();

    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFilters(float width)
    {
        var pos = ImGui.GetCursorPos();
        var remainingWidth = width - CkGui.IconButtonSize(FontAwesomeIcon.FolderPlus).X;

        var buttonsPos = new Vector2(pos.X + remainingWidth, pos.Y);

        ImGui.SetCursorPos(buttonsPos);
        DrawFolderButton();

        ImGui.SetCursorPos(pos);
        return remainingWidth - ImGui.GetStyle().ItemInnerSpacing.X;
    }
}

