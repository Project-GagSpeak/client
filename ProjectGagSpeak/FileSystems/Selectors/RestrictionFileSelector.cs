using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.FileSystem.Selector;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerClient;
using GagSpeak.State;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class RestrictionFileSelector : CkFileSystemSelector<RestrictionItem, RestrictionFileSelector.RestrictionState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly RestrictionManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct RestrictionState(uint Color) { }

    // Helper operations used for creating new items and cloning them.
    private RestrictionType _newType;
    // private RestrictionItem? _clonedRestrictionItem;

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new RestrictionFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public RestrictionFileSelector(ILogger<RestrictionFileSelector> log, GagspeakMediator mediator, FavoritesManager favorites,
        RestrictionManager manager, RestrictionFileSystem fileSystem, IKeyState keys) : base(fileSystem, log, keys, "##RestrictionFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigRestrictionChanged>(this, (msg) => OnRestrictionChange(msg.Type, msg.Item, msg.OldString));
    }

    private void RenameLeafRestriction(RestrictionFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        RenameLeaf(leaf);
    }

    private void RenameRestriction(RestrictionFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Restriction:");
        if (ImGui.InputText("##RenameRestriction", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed restriction.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.Unsubscribe<ConfigRestrictionChanged>(this);
    }

    protected override void DrawLeafInner(CkFileSystem<RestrictionItem>.Leaf leaf, in RestrictionState state, bool selected)
    {
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.InvisibleButton("leaf", leafSize);
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);

        // the border if selected.
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

        ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
        ImGui.AlignTextToFramePadding();
        Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, leaf.Value.Identifier);
        ImGui.SameLine();
        ImGui.Text(leaf.Value.Label);
        ImGui.SameLine((rectMax.X - rectMin.X) - CkGui.IconSize(FAI.Trash).X - ImGui.GetStyle().ItemSpacing.X);
        if (CkGui.IconButton(FAI.Trash, inPopup: true, disabled: !KeyMonitor.ShiftPressed()))
        {
            Log.LogDebug($"Deleting {leaf.Value.Label}");
            _manager.Delete(leaf.Value);
        }
        CkGui.AttachToolTip("Delete this restriction item. This cannot be undone.--SEP--Must be holding SHIFT to remove.");

    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnRestrictionChange(StorageChangeType type, RestrictionItem restriction, string? oldString)
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
            ImGui.OpenPopup("##NewRestriction");
        CkGui.AttachToolTip("Create a new Restriction Item.");

        ImUtf8.SameLineInner();
        DrawFolderButton();
    }

    protected override void DrawPopups()
        => NewRestrictionPopup();

    private void NewRestrictionPopup()
    {
        if (!OpenRestrictionNameField("##NewRestriction", ref _newName))
            return;

        _manager.CreateNew(_newName, _newType);
        _newName = string.Empty;
        _newType = RestrictionType.Normal;
    }

    private bool OpenRestrictionNameField(string popupName, ref string newName)
    {
        using var popup = ImRaii.Popup(popupName);
        if (!popup)
            return false;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            ImGui.CloseCurrentPopup();

        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();
        var enterPressed = ImGui.InputTextWithHint("##newName", "Enter New Name...", ref newName, 512, ImGuiInputTextFlags.EnterReturnsTrue);

        if (CkGuiUtils.EnumCombo("Restriction Kind", 100, _newType, out var newType,
            Enum.GetValues<RestrictionType>().SkipLast(1), defaultText: "Select Type.."))
        {
            _newType = newType;
        }

        if (!enterPressed)
            return false;

        ImGui.CloseCurrentPopup();
        return true;
    }
}

