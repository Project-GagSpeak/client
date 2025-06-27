using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using GagSpeak.Gui;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using Dalamud.Interface.Utility.Raii;
using OtterGui.Text;
using CkCommons.Widgets;
using GagSpeak.PlayerClient;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using CkCommons.Gui;
using CkCommons.Helpers;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class TriggerFileSelector : CkFileSystemSelector<Trigger, TriggerFileSelector.TriggerState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly TriggerManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct TriggerState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new TriggerFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public TriggerFileSelector(ILogger<TriggerFileSelector> log, GagspeakMediator mediator,
        FavoritesManager favorites, TriggerManager manager, TriggerFileSystem fileSystem)
        : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##TriggerFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigTriggerChanged>(this, (msg) => OnTriggerChange(msg.Type, msg.Item, msg.OldString));

        // we can add, or unsubscribe from buttons here. Remember this down the line, it will become useful.
    }

    private void RenameLeafTrigger(TriggerFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        RenameLeaf(leaf);
    }

    private void RenameTrigger(TriggerFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Trigger:");
        if (ImGui.InputText("##RenameTrigger", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed trigger.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.Unsubscribe<ConfigTriggerChanged>(this);
    }

    protected override void DrawLeafInner(CkFileSystem<Trigger>.Leaf leaf, in TriggerState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        ImGui.InvisibleButton("##leaf", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()));
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);

        using (ImRaii.Group())
        {
            ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
            ImGui.AlignTextToFramePadding();
            Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, leaf.Value.Identifier);
            ImGui.SameLine();
            ImGui.Text(leaf.Value.Label);
            ImGui.SameLine((rectMax.X - rectMin.X) - CkGui.IconSize(FAI.Trash).X - ImGui.GetStyle().ItemSpacing.X);
            if (CkGui.IconButton(FAI.Trash, inPopup: true, disabled: !KeyMonitor.ShiftPressed()))
                _manager.Delete(leaf.Value);
            CkGui.AttachToolTip("Delete this trigger set. This cannot be undone.--SEP--Must be holding SHIFT to remove.");
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

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnTriggerChange(StorageChangeType type, Trigger trigger, string? oldString)
        => SetFilterDirty();


    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFiltersWidth(float width)
    {
        var pos = ImGui.GetCursorPos();
        var remainingWidth = width
            - CkGui.IconButtonSize(FAI.Plus).X
            - CkGui.IconButtonSize(FAI.FolderPlus).X
            - ImGui.GetStyle().ItemInnerSpacing.X;

        var buttonsPos = new Vector2(pos.X + remainingWidth, pos.Y);

        ImGui.SetCursorPos(buttonsPos);
        if (CkGui.IconButton(FAI.Plus))
            ImGui.OpenPopup("##NewTrigger");
        CkGui.AttachToolTip("Create a new Trigger.");

        ImUtf8.SameLineInner();
        DrawFolderButton();

        ImGui.SetCursorPos(pos);
        return remainingWidth - ImGui.GetStyle().ItemInnerSpacing.X;
    }

    protected override void DrawPopups()
    {
        NewTriggerPopup();
    }

    private void NewTriggerPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewTrigger", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

