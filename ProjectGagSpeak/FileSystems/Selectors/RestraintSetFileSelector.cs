using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using GagSpeak.Gui;
using CkCommons.Widgets;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using CkCommons.Gui;
using CkCommons.Helpers;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class RestraintSetFileSelector : CkFileSystemSelector<RestraintSet, RestraintSetFileSelector.RestraintSetState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly RestraintManager _manager;

    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct RestraintSetState(uint Color) { }


    // Helper operations used for creating new items and cloning them.
    // private RestraintSet? _clonedRestraintSet; // This will be done via the right click menu later so it will go away probably.


    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new RestraintSetFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public RestraintSetFileSelector(ILogger<RestraintSetFileSelector> log, GagspeakMediator mediator,
        FavoritesManager favorites, RestraintManager manager, RestraintSetFileSystem fileSystem)
        : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##RestraintSetFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigRestraintSetChanged>(this, (msg) => OnRestraintSetChange(msg.Type, msg.Item, msg.OldString));
    }

    private void RenameLeafRestraintSet(RestraintSetFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        RenameLeaf(leaf);
    }

    private void RenameRestraintSet(RestraintSetFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Restraint Set:");
        if (ImGui.InputText("##RenameRestraintSet", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed restraintSet.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.Unsubscribe<ConfigRestraintSetChanged>(this);
    }

    protected override void DrawLeafInner(CkFileSystem<RestraintSet>.Leaf leaf, in RestraintSetState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * 2);
        ImGui.InvisibleButton("button-leaf", leafSize);
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
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
            using (ImRaii.Group())
            {
                Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, leaf.Value.Identifier);
                CkGui.TextFrameAlignedInline(leaf.Name, false);
                // below, in a darker text, draw out the description, up to 100 characters.
                if(leaf.Value.Description.IsNullOrWhitespace())
                {
                    CkGui.ColorText("No Description Provided...", ImGuiColors.DalamudGrey);
                }
                else
                {
                    var desc = leaf.Value.Description.Length > 40 ? leaf.Value.Description.Substring(0, 40) : leaf.Value.Description;
                    CkGui.ColorText(desc + "..", ImGuiColors.DalamudGrey);
                }
            }
            // Optimize later.
            ImGui.SameLine((rectMax.X - rectMin.X) - CkGui.IconSize(FAI.Trash).X - ImGui.GetStyle().ItemSpacing.X);
            var centerHeight = (ImGui.GetItemRectSize().Y - CkGui.IconSize(FAI.Trash).Y) / 2;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerHeight);
            var pos = ImGui.GetCursorScreenPos();
            var hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
            var col = (hovering && KeyMonitor.ShiftPressed()) ? ImGuiCol.Text : ImGuiCol.TextDisabled;
            CkGui.FramedIconText(FAI.Trash, ImGui.GetColorU32(col));
            if (hovering && KeyMonitor.ShiftPressed() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Log.Debug($"Deleting {leaf.Value.Label} with SHIFT pressed.");
                _manager.Delete(leaf.Value);
            }
            CkGui.AttachToolTip("Delete this restraint set. This cannot be undone.--SEP--Must be holding SHIFT to remove.");
        }
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnRestraintSetChange(StorageChangeType type, RestraintSet restraintSet, string? oldString)
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
            ImGui.OpenPopup("##NewRestraintSet");
        CkGui.AttachToolTip("Create a new restraint set.");

        ImGui.SameLine(0, 1);
        DrawFolderButton();
    }

    public override void DrawPopups()
        => NewRestraintPopup();

    private void NewRestraintPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewRestraintSet", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

