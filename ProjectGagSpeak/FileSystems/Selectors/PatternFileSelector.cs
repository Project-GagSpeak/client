using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class PatternFileSelector : CkFileSystemSelector<Pattern, PatternFileSelector.PatternState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly PatternManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct PatternState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new PatternFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public PatternFileSelector(ILogger<PatternFileSelector> log, GagspeakMediator mediator,
        FavoritesManager favorites, PatternManager manager, PatternFileSystem fileSystem) 
        : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##PatternsFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigPatternChanged>(this, (msg) => OnPatternChange(msg.Type, msg.Item, msg.OldString));
        // Do not subscribe to the default renamer, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        SubscribeRightClickLeaf(RenamePattern);
    }

    private void RenamePattern(PatternFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Pattern:");
        if (ImGui.InputText("##RenamePattern", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed pattern.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
    }

    protected override void DrawLeafInner(CkFileSystem<Pattern>.Leaf leaf, in PatternState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
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
        Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restriction, leaf.Value.Identifier);
        CkGui.TextFrameAlignedInline(leaf.Value.Label);
        // Only draw the deletion if the item is not active or occupied.
        if (!leaf.Value.Identifier.Equals(_manager.ActivePattern?.Identifier))
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
            CkGui.AttachToolTip("Delete this Pattern. This cannot be undone.--SEP--Must be holding SHIFT to remove.");
        }

        //using (ImRaii.Group())
        //{
        //    // display name, then display the downloads and likes on the other side.
        //    CkGui.GagspeakText(activeItem.Label);
        //    CkGui.HelpText("Description:--SEP--" + activeItem.Description);

        //    // playback button
        //    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        //    // Draw the delete button
        //    ImGui.SameLine();
        //}
        //// next line:
        //using (var group2 = ImRaii.Group())
        //{
        //    ImGui.AlignTextToFramePadding();
        //    CkGui.IconText(FAI.Clock);
        //    ImUtf8.SameLineInner();
        //    CkGui.ColorText(durationTxt, ImGuiColors.DalamudGrey);
        //    CkGui.AttachToolTip("Total Length of the Pattern.");

        //    ImGui.SameLine();
        //    ImGui.AlignTextToFramePadding();
        //    CkGui.IconText(FAI.Stopwatch20);
        //    ImUtf8.SameLineInner();
        //    CkGui.ColorText(startpointTxt, ImGuiColors.DalamudGrey);
        //    CkGui.AttachToolTip("Start Point of the Pattern.");

        //    ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.Sync).X - ImGui.GetStyle().ItemInnerSpacing.X);
        //    CkGui.IconText(FAI.Sync, activeItem.ShouldLoop ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey2);
        //    CkGui.AttachToolTip(activeItem.ShouldLoop ? "Pattern is set to loop." : "Pattern does not loop.");
        //}
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnPatternChange(StorageChangeType type, Pattern pattern, string? oldString)
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
            ImGui.OpenPopup("##NewPattern");
        CkGui.AttachToolTip("Create a new Pattern.");

        ImGui.SameLine(0, 1);
        DrawFolderButton();
    }

    protected override void DrawPopups()
        => NewPatternPopup();

    private void NewPatternPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewPattern", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

