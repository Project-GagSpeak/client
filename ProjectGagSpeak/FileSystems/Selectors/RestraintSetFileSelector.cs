using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.FileSystem.Selector;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.RestraintSets;

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
    private RestraintSet? _clonedRestraintSet; // This will be done via the right click menu later so it will go away probably.
    private string _newName = string.Empty;


    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new RestraintSetFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public RestraintSetFileSelector(ILogger<RestraintSetFileSelector> log, GagspeakMediator mediator, FavoritesManager favorites,
        RestraintManager manager, RestraintSetFileSystem fileSystem, IKeyState keys) : base(fileSystem, log, keys, "##RestraintSetFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigRestraintSetChanged>(this, (msg) => OnRestraintSetChange(msg.Type, msg.Item, msg.OldString));

        // we can add, or unsubscribe from buttons here. Remember this down the line, it will become useful.
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

    // can override the selector here to mark the last selected set in the config or something somewhere.

    protected override void DrawLeafName(CkFileSystem<RestraintSet>.Leaf leaf, in RestraintSetState state, bool selected)
    {
        using var id = ImRaii.PushId((int)leaf.Identifier);
        using var leafInternalGroup = ImRaii.Group();
        DrawLeafInternal(leaf, state, selected);
    }

    private void DrawLeafInternal(CkFileSystem<RestraintSet>.Leaf leaf, in RestraintSetState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        ImGui.InvisibleButton(leaf.Value.Identifier.ToString(), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * 2));
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = ImGui.IsItemHovered() ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);

        using (ImRaii.Group())
        {
            ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, leaf.Value.Identifier);
                ImGui.SameLine();
                ImGui.Text(leaf.Value.Label);
                // below, in a darker text, draw out the description, up to 100 characters.
                if(leaf.Value.Description.IsNullOrWhitespace())
                {
                    CkGui.ColorText("No Description Provided...", ImGuiColors.ParsedGrey);
                }
                else
                {
                    var desc = leaf.Value.Description.Length > 100 ? leaf.Value.Description.Substring(0, 60) : leaf.Value.Description;
                    CkGui.ColorText(desc, ImGuiColors.ParsedGrey);
                }
            }
            // Optimize later.
            ImGui.SameLine((rectMax.X - rectMin.X) - CkGui.IconSize(FontAwesomeIcon.Trash).X - ImGui.GetStyle().ItemSpacing.X);
            var centerHeight = (ImGui.GetItemRectSize().Y - CkGui.IconSize(FontAwesomeIcon.Trash).Y) / 2;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerHeight);
            if (CkGui.IconButton(FontAwesomeIcon.Trash, inPopup: true, disabled: !KeyMonitor.ShiftPressed()))
                _manager.Delete(leaf.Value);
            CkGui.AttachToolTip("Delete this restraint set. This cannot be undone.--SEP--Must be holding SHIFT to remove.");
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

    protected override void DrawFolderName(CkFileSystem<RestraintSet>.Folder folder, bool selected)
    {
        using var id = ImRaii.PushId((int)folder.Identifier);
        using var group = ImRaii.Group();
        CkGuiUtils.DrawFolderSelectable(folder, FolderLineColor, selected);
    }

    // if desired, can override the colors for expanded, collapsed, and folder line colors.
    // Can also define if the folders are open by default or not.

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnRestraintSetChange(StorageItemChangeType type, RestraintSet restraintSet, string? oldString)
        => SetFilterDirty();

    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFilters(float width)
    {
        var pos = ImGui.GetCursorPos();
        var remainingWidth = width
            - CkGui.IconButtonSize(FontAwesomeIcon.Plus).X
            - CkGui.IconButtonSize(FontAwesomeIcon.FolderPlus).X
            - ImGui.GetStyle().ItemInnerSpacing.X;

        var buttonsPos = new Vector2(pos.X + remainingWidth, pos.Y);

        ImGui.SetCursorPos(buttonsPos);
        if (CkGui.IconButton(FontAwesomeIcon.Plus))
            ImGui.OpenPopup("##NewRestraintSet");
        CkGui.AttachToolTip("Create a new restraint set.");

        ImUtf8.SameLineInner();
        DrawFolderButton();

        ImGui.SetCursorPos(pos);
        return remainingWidth - ImGui.GetStyle().ItemInnerSpacing.X;
    }

    protected override void DrawPopups()
    {
        NewRestraintPopup();
    }

    private void NewRestraintPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewRestraintSet", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

