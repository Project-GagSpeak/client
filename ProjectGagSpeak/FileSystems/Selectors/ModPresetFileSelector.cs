using CkCommons;
using CkCommons.FileSystem;
using CkCommons.FileSystem.Selector;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using OtterGui.Text;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class ModPresetFileSelector : CkFileSystemSelector<ModPresetContainer, ModPresetFileSelector.PresetState>, IMediatorSubscriber, IDisposable
{
    private readonly ModPresetManager _manager;
    public GagspeakMediator Mediator { get; init; }


    // could maybe add expanded state or something I dunno.
    public record struct PresetState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new ModPresetFileSystem.Leaf? SelectedLeaf
        => base.SelectedLeaf;

    public ModPresetFileSelector(ILogger<ModPresetFileSelector> log, GagspeakMediator mediator,
        ModPresetManager manager, ModPresetFileSystem fs) : base(fs, Svc.Logger.Logger, Svc.KeyState, "##ModPresetFS")
    {
        Mediator = mediator;
        _manager = manager;

        Mediator.Subscribe<ConfigModPresetChanged>(this, (msg) => OnPresetChange(msg.Type, msg.Item, msg.OldDirString));
        // Do not subscribe to the default renamer, we only want to rename the item itself.
        UnsubscribeRightClickLeaf(RenameLeaf);
        // maybe something like a preset selector? idk.
        // SubscribeRightClickLeaf(RenamePattern);
        SelectionChanged += OnSelectionChanged;
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.UnsubscribeAll(this);
        SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(ModPresetContainer? oldContainer, ModPresetContainer? newContainer, in PresetState state)
        => Mediator.Publish(new SelectedModContainerChanged());

    protected override bool DrawLeafInner(CkFileSystem<ModPresetContainer>.Leaf leaf, in PresetState state, bool selected)
    {
        // must be a valid drag-drop source, so use invisible button.
        var leafSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.InvisibleButton(leaf.Value.DirectoryPath, leafSize);
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = selected ? CkCol.LChildBg.Uint() : hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : new Vector4(0.25f, 0.2f, 0.2f, 0.4f).ToUint(); 
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgColor, 5);

        // the border if selected.
        if (selected)
        {
            ImGui.GetWindowDrawList().AddRectFilledMultiColor(
                rectMin,
                rectMin + leafSize,
                CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)), 0, 0, CkGui.Color(new Vector4(0.886f, 0.407f, 0.658f, .3f)));

            ImGui.GetWindowDrawList().AddRectFilled(rectMin, new Vector2(rectMin.X + ImGuiHelpers.GlobalScale * 3, rectMax.Y),
                CkGui.Color(ImGuiColors.ParsedPink), 5);
        }

        using (ImRaii.Group())
        {
            ImGui.SetCursorScreenPos(rectMin);
            CkGui.FramedIconText(FAI.FileArchive);
            ImGui.SameLine();
            ImUtf8.TextFrameAligned(leaf.Value.ModName);
        }

        if (hovered)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            ImGui.Text(leaf.Value.DirectoryPath);
        }
        return hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
    }

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnPresetChange(StorageChangeType type, ModPresetContainer preset, string? oldString)
        => SetFilterDirty();

    public override void DrawPopups()
        => NewPresetPopup();

    private void NewPresetPopup()
    {
        //if (!ImGuiUtil.OpenNameField("##NewPreset", ref _newName))
        //    return;

        //if (Selected is null)
        //{
        //    Log.Error("No Mod Preset selected to create a new preset for.");
        //    return;
        //}

        //_manager.TryCreatePreset(Selected, _newName);
        //_newName = string.Empty;
    }
}

