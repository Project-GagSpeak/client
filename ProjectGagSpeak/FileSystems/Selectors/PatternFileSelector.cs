using CkCommons;
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
using GagSpeak.WebAPI;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;
using GagSpeak.Utils;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class PatternFileSelector : CkFileSystemSelector<Pattern, PatternFileSelector.PatternState>, IMediatorSubscriber, IDisposable
{
    private readonly MainHub _hub;
    private readonly FavoritesConfig _favorites;
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

    public PatternFileSelector(ILogger<PatternFileSelector> log, GagspeakMediator mediator, MainHub hub,
        FavoritesConfig favorites, PatternManager manager, PatternFileSystem fileSystem) 
        : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##PatternsFS")
    {
        Mediator = mediator;
        _hub = hub;
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

    protected override bool DrawLeafInner(CkFileSystem<Pattern>.Leaf leaf, in PatternState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.InvisibleButton("leaf", leafSize);
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();

        var iconSize = CkGui.IconSize(FAI.Trash).X;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var iconSpacing = iconSize + spacing;

        ImRect leftOfFav = new(rectMin, rectMin + new Vector2(spacing, leafSize.Y));
        ImRect rightOfFav = new(rectMin + new Vector2(iconSpacing, 0), rectMin + leafSize - new Vector2(iconSpacing * 2, 0));

        var wasHovered = ImGui.IsMouseHoveringRect(leftOfFav.Min, leftOfFav.Max) || ImGui.IsMouseHoveringRect(rightOfFav.Min, rightOfFav.Max);
        // Draw the base frame, colored.
        var bgColor = wasHovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgColor, 5);

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
        Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Pattern, leaf.Value.Identifier);
        CkGui.TextFrameAlignedInline(leaf.Value.Label);
        if(leaf.Value.ShouldLoop)
        {
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Sync, ImGuiColors.ParsedPink);
            CkGui.AttachToolTip("This Pattern will loop indefinitely until stopped.");
        }

        var shiftPressed = KeyMonitor.ShiftPressed();
        var mouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
        var isActiveItem = leaf.Value.Identifier.Equals(_manager.ActivePatternId);
        var currentX = leafSize.X - iconSpacing;

        ImGui.SameLine((rectMax.X - rectMin.X) - ImGui.GetFrameHeightWithSpacing());
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
        var col = (hovering && shiftPressed) ? ImGuiCol.Text : ImGuiCol.TextDisabled;
        CkGui.FramedIconText(FAI.Trash, ImGui.GetColorU32(col));
        if (!isActiveItem && hovering && shiftPressed && mouseReleased)
        {
            if (leaf.Value.Identifier.Equals(_manager.ItemInEditor?.Identifier))
                return false;

            Log.Debug($"Deleting {leaf.Value.Label} with SHIFT pressed.");
            _manager.Delete(leaf.Value);
        }
        CkGui.AttachToolTip("Delete this Pattern from storage.--SEP--Must be holding SHIFT to remove.");

        currentX -= iconSpacing;
        ImGui.SameLine(currentX);
        pos = ImGui.GetCursorScreenPos();
        hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
        CkGui.FramedIconText(FAI.QuestionCircle, hovering ? ImGui.GetColorU32(ImGuiColors.TankBlue) : ImGui.GetColorU32(ImGuiCol.TextDisabled));
        CkGui.AttachToolTip($"Total Length: --COL--{leaf.Value.Duration.ToString("mm\\:ss")}--COL--" +
            $"--NL--Start Time: --COL--{leaf.Value.StartPoint.ToString("mm\\:ss")}--COL--" +
            $"--NL--Playback Time: --COL--{leaf.Value.PlaybackDuration.ToString("mm\\:ss")}--COL--", color: GsCol.VibrantPink.Vec4());
        return wasHovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
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
        if (CkGui.IconButton(FAI.Plus, disabled: !_manager.CanRecordPattern, inPopup: true))
            _manager.OpenRemoteForRecording();
        CkGui.AttachToolTip(_manager.CanRecordPattern ? "Create a new Pattern." : "Cannot be in a VibeRoom, or playing a pattern!");

        ImGui.SameLine(0, 1);
        DrawFolderButton();
    }

    public override void DrawPopups()
        => NewPatternPopup();

    private void NewPatternPopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewPattern", ref _newName))
            return;

        _manager.CreateNew(_newName);
        _newName = string.Empty;
    }
}

