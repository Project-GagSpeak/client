using CkCommons;
using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;
using OtterGuiInternal.Structs;

namespace GagSpeak.FileSystems;

public sealed class BuzzToyFileSelector : CkFileSystemSelector<BuzzToy, BuzzToyFileSelector.ToyState>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly BuzzToyManager _manager;
    public GagspeakMediator Mediator { get; init; }

    private ToyBrandName _newItemName = ToyBrandName.Unknown;

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct ToyState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new BuzzToyFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public BuzzToyFileSelector(GagspeakMediator mediator, FavoritesManager favorites, BuzzToyManager manager,
        BuzzToyFileSystem fileSystem) : base(fileSystem, Svc.Logger.Logger, Svc.KeyState, "##SexToyFS")
    {
        Mediator = mediator;
        _favorites = favorites;
        _manager = manager;

        Mediator.Subscribe<ConfigSexToyChanged>(this, (msg) => OnSexToyChange(msg.Type, msg.Item, msg.OldString));

        // Do not subscribe to the default renamer, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        SubscribeRightClickLeaf(RenameSexToy);
    }

    private void RenameSexToy(BuzzToyFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.LabelName;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename SexToy:");
        if (ImGui.InputText("##RenameSexToy", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed alarm.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
    }

    protected override bool DrawLeafInner(CkFileSystem<BuzzToy>.Leaf leaf, in ToyState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.InvisibleButton("leaf", leafSize);
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();

        var iconSize = CkGui.IconSize(FAI.Trash).X;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var iconSpacing = iconSize + spacing;

        ImRect leftOfFav = new(rectMin, rectMin + new Vector2(spacing, leafSize.Y));
        ImRect rightOfFav = new(rectMin + new Vector2(iconSpacing, 0), rectMin + leafSize - new Vector2(iconSpacing * 3, 0));

        var wasHovered = ImGui.IsMouseHoveringRect(leftOfFav.Min, leftOfFav.Max) || ImGui.IsMouseHoveringRect(rightOfFav.Min, rightOfFav.Max);
        var bgColor = wasHovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
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

        ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGui.GetStyle().ItemSpacing.X });
        ImUtf8.TextFrameAligned(leaf.Value.LabelName);
        // Only draw the deletion if the item is not active or occupied.

        var shiftPressed = KeyMonitor.ShiftPressed();
        var mouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
        var isEditingItem = leaf.Value.Id.Equals(_manager.ItemInEditor?.Id);
        var currentX = leafSize.X - iconSpacing;

        ImGui.SameLine((rectMax.X - rectMin.X) - ImGui.GetFrameHeightWithSpacing());
        var pos = ImGui.GetCursorScreenPos();
        var hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
        var col = (hovering && shiftPressed) ? ImGuiCol.Text : ImGuiCol.TextDisabled;
        CkGui.FramedIconText(FAI.Trash, ImGui.GetColorU32(col));
        if (!isEditingItem && hovering && shiftPressed && mouseReleased)
        {
            Log.Debug($"Deleting {leaf.Value.LabelName} with SHIFT pressed.");
            _manager.RemoveDevice(leaf.Value);
        }
        CkGui.AttachToolTip("Delete this device from storage.--SEP--Must be holding SHIFT to remove.");

        currentX -= iconSpacing;
        ImGui.SameLine(currentX);
        pos = ImGui.GetCursorScreenPos();
        hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
        var interactIcon = leaf.Value.Interactable ? FAI.Handshake : FAI.HandshakeSlash;
        var interactCol = leaf.Value.Interactable ? CkColor.TriStateCheck.Vec4() : CkColor.TriStateCross.Vec4();
        CkGui.FramedIconText(interactIcon, interactCol);
        CkGui.AttachToolTip(leaf.Value.Interactable
            ? "Device interactions are active, and can be used by GagSpeak's Remote."
            : "This device has interactions off, preventing usage via GagSpeak's Remote.");

        if (leaf.Value is IntifaceBuzzToy ibt)
        {
            currentX -= iconSpacing;
            ImGui.SameLine(currentX);
            pos = ImGui.GetCursorScreenPos();
            hovering = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(ImGui.GetFrameHeight()));
            var onlineCol = ibt.DeviceConnected ? CkColor.TriStateCheck.Vec4() : CkColor.TriStateCross.Vec4();
            CkGui.FramedIconText(FAI.Globe, onlineCol);
            CkGui.AttachToolTip(ibt.DeviceConnected
                ? "This device is online and connected to Intiface."
                : "This device is offline or not connected to Intiface.");
        }
        return wasHovered && mouseReleased;
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnSexToyChange(StorageChangeType type, BuzzToy device, string? oldString)
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
            ImGui.OpenPopup("##NewSexToy");
        CkGui.AttachToolTip("Create a new SexToy.");

        ImGui.SameLine(0, 1);
        DrawFolderButton();
    }

    public override void DrawPopups()
        => NewSexToyPopup();

    private void NewSexToyPopup()
    {
        using var popup = ImRaii.Popup("##NewSexToy");
        if (!popup)
            return;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            ImGui.CloseCurrentPopup();

        if (CkGuiUtils.EnumCombo("##newName", 300 * ImGuiHelpers.GlobalScale, _newItemName, out var newVal, (n) => n.ToName(), "Choose Brand Name..", flags: CFlags.None))
        {
            _newItemName = newVal;
            _manager.CreateNew(_newItemName);
            _newName = string.Empty;
            ImGui.CloseCurrentPopup();
        }
    }
}

