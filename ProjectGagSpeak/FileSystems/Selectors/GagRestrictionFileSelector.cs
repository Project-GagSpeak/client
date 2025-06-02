using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.FileSystem.Selector;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.CkCommons.Gui;
using GagspeakAPI.Extensions;
using ImGuiNET;

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
    protected override void DrawLeafInner(CkFileSystem<GarblerRestriction>.Leaf leaf, in GagRestrictionState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.InvisibleButton("##leaf", leafSize);
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        // If it was double clicked, open it in the editor.
        if (hovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            _manager.StartEditing(leaf.Value);

        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);

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

        using (ImRaii.Group())
        {
            ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
            ImGui.AlignTextToFramePadding();
            Icons.DrawFavoriteStar(_favorites, leaf.Value.GagType);
            ImGui.SameLine();
            ImGui.Text(leaf.Value.GagType.GagName());
        }
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnGagRestrictionChange(StorageItemChangeType type, GarblerRestriction gagRestriction, string? oldString)
        => SetFilterDirty();

    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFiltersWidth(float width)
        => width - CkGui.IconButtonSize(FAI.FolderPlus).X;

    protected override void DrawCustomFilters()
    {
        DrawFolderButton();
    }
}

