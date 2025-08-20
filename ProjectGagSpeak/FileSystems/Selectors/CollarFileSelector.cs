using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class CollarFileSelector : CkFileSystemSelector<GagSpeakCollar, CollarFileSelector.CollarState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly CollarManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct CollarState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new CollarFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public CollarFileSelector(GagspeakMediator mediator, FavoritesManager favorites, CollarManager manager, 
        CollarFileSystem fileSystem) : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##CollarFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigCollarChanged>(this, (msg) => OnCollarChange(msg.Type, msg.Item, msg.OldString));

        // Do not subscribe to the default rename action, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        // Subscribe to the rename Collar.
        SubscribeRightClickLeaf(RenameCollar);

    }

    private void RenameCollar(CollarFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Collar:");
        if (ImGui.InputText("##RenameCollar", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachToolTip("Enter a new name here to rename the changed collar.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
    }

    protected override void DrawLeafInner(CkFileSystem<GagSpeakCollar>.Leaf leaf, in CollarState state, bool selected)
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
        Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Collar, leaf.Value.Identifier);
        CkGui.TextFrameAlignedInline(leaf.Value.Label);
        // Only draw the deletion if the item is not active or occupied.
        if(!leaf.Value.Identifier.Equals(_manager.ServerCollarData?.Identifier))
        {
            ImGui.SameLine((rectMax.X - rectMin.X) - ImGui.GetFrameHeightWithSpacing());
            var pos = ImGui.GetCursorScreenPos();
            var hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
            var col = (hovering && KeyMonitor.ShiftPressed()) ? ImGuiCol.Text : ImGuiCol.TextDisabled;
            CkGui.FramedIconText(FAI.Trash, ImGui.GetColorU32(col));
            if (hovering && KeyMonitor.ShiftPressed() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                Log.Debug($"Deleting {leaf.Value.Label} with SHIFT pressed.");
                _manager.Delete(leaf.Value);
            }
            CkGui.AttachToolTip("Delete this collar item. This cannot be undone.--SEP--Must be holding SHIFT to remove.");
        }
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnCollarChange(StorageChangeType type, GagSpeakCollar collar, string? oldString)
        => SetFilterDirty();


    /// <summary> Add the state filter combo-button to the right of the filter box. </summary>
    protected override float CustomFiltersWidth(float width)
        => width - CkGui.IconButtonSize(FAI.Plus).X - ImGui.GetStyle().ItemInnerSpacing.X;

    protected override void DrawCustomFilters()
    {
        if (CkGui.IconButton(FAI.Plus, inPopup: true))
            ImGui.OpenPopup("##NewCollar");
        CkGui.AttachToolTip("Create a new Collar Item.");
    }

    public override void DrawPopups()
        => NewCollarPopup();

    private void NewCollarPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewCollar", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

