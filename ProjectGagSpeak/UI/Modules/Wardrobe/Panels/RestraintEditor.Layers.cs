using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.FileSystems;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class RestraintEditorLayers : IFancyTab
{
    private readonly ILogger<RestraintEditorLayers> _logger;
    private readonly RestraintSetFileSelector _selector;
    private readonly RestraintManager _manager;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly AttributeDrawer _traitDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public RestraintEditorLayers(ILogger<RestraintEditorLayers> logger,
        RestraintSetFileSelector selector, EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer, MoodleDrawer moodleDrawer, AttributeDrawer traitsDrawer,
        RestraintManager manager, CosmeticService cosmetics, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _traitDrawer = traitsDrawer;
        _manager = manager;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    public string   Label       => "Layers";
    public string   Tooltip     => "Define the layers that can be applied to the restraint set." +
        "--SEP--Restraint Layers can be toggled while a restraint set is locked.";
    public bool     Disabled    => false;

    private float DragDropItemRounding => ImGui.GetStyle().FrameRounding * 2f;
    private float DragDropItemHeight => ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
    private float DragDropHeaderWidth => ImGui.CalcTextSize("Layer XX").X + ImGui.GetStyle().ItemSpacing.X * 2;
    private List<(Vector2 RowPos, Action AcceptDraw)> _moveCommands = [];
    private Guid _dragLayerId { get; set; } = Guid.Empty;

    public void DrawContents(float width)
    {
        if (_manager.ItemInEditor is not { } setInEdit)
            return;

        if (_moveCommands.Count > 0)
            _moveCommands.Clear();

        var region = ImGui.GetContentRegionAvail();
        var buttonSize = CkGui.IconButtonSize(FAI.Eraser);
        var rightButtons = new Vector2(buttonSize.X * 2 + ImGui.GetStyle().ItemSpacing.X, buttonSize.Y);
        var detailsWidth = region.X - rightButtons.X;
        var layerIdx = 0;

        using (ImRaii.Table("##RestraintLayers", 1, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg, region))
        {
            // The .ToList() makes the for-each work on a copy, preventing an exception when removing a layer.
            foreach (var layer in setInEdit.Layers.ToList())
            {
                DrawExistingLayer(layerIdx, region.X, rightButtons);
                layerIdx++;
            }

            if(layerIdx <= Constants.MaxRestraintLayers - 1)
                DrawNewLayerRow(layerIdx);

            // Draw regions for drag-drop targets.
            foreach (var x in _moveCommands)
            {
                ImGui.SetCursorPos(x.RowPos);
                ImGui.Dummy(new Vector2(DragDropHeaderWidth, DragDropItemHeight));
                x.AcceptDraw();
            }
        }
    }

    private void DrawExistingLayer(int curLayerIdx, float totalWidth, Vector2 rightButtons) 
    {
        using var id = ImRaii.PushId(_manager.ItemInEditor!.Layers[curLayerIdx].ID.ToString());
        ImGui.TableNextRow();

        // If we are currently holding down our mouse and 'moving' the item, have it fade between a gradient green glow.
        if (_dragLayerId == _manager.ItemInEditor!.Layers[curLayerIdx].ID)
        {
            var greenCol = CkColor.TriStateCheck.Vec4();
            var color = CkGui.Color(Gradient.Get(greenCol, greenCol with { W = greenCol.W / 4 }, 500));
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, color);
        }

        // Draw out the side header, which is what we use for our movable drag-drop object.
        ImGui.TableNextColumn();
        var rowPos = ImGui.GetCursorPos();
        DrawSideHeader($"##Move{_manager.ItemInEditor!.Layers[curLayerIdx].ID}", $"Layer {curLayerIdx + 1}", DragDropHeaderWidth);

        // Show the cursor move icon while hovering.
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

        // Define the drag-drop source. Here we store the ID of the restriction layer into the payload.
        // By doing this, its respective accepted payload target can match with the same ID.
        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
        {
            CkGui.SetDragDropPayload("ReorderRestraintLayers", _manager.ItemInEditor!.Layers[curLayerIdx].ID);
            _dragLayerId = _manager.ItemInEditor!.Layers[curLayerIdx].ID;
            //_logger.LogTrace("DragDropSource = " + _dragLayerId);
            ImGui.EndDragDropSource();
        }
        // If we are NOT dragging, and we have just released, and our drag layer is the same as the current, clear the drag layer.
        else if (_dragLayerId == _manager.ItemInEditor!.Layers[curLayerIdx].ID)
            _dragLayerId = Guid.Empty;

        // Define the move index that should be used if the drag-drop target is accepted.
        var moveIndex = curLayerIdx;
        _moveCommands.Add((rowPos, () =>
        {
            if (ImGui.BeginDragDropTarget())
            {
                // Swap the dragged source with this layers target if the payloads correspond.
                if (CkGui.AcceptDragDropPayload("ReorderRestraintLayers", out Guid payloadID, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    MoveItemToPosition(_manager.ItemInEditor!.Layers, x => x.ID == payloadID, moveIndex);

                ImGui.EndDragDropTarget();
            }
        }));

        // Draw out the restraint set layer child window information off to the right.
        ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0));
        using (ImRaii.Child("DragDropLayer" + curLayerIdx, new Vector2(ImGui.GetContentRegionAvail().X, DragDropItemHeight), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
        {
            // Basic layer information.
            DrawLayerDetails(curLayerIdx);

            // Buttons for swapping & erasing.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - rightButtons.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - rightButtons.Y) / 2);
            using (ImRaii.Child("LayerButtons-" + curLayerIdx, rightButtons))
            {
                if (CkGui.IconButton(FAI.ArrowsLeftRight, inPopup: true, disabled: !KeyMonitor.ShiftPressed()))
                {
                    // Swap the layer type to the other type.
                    var currentLayer = _manager.ItemInEditor!.Layers[curLayerIdx];
                    _manager.ItemInEditor.Layers[curLayerIdx] = currentLayer switch
                    {
                        RestrictionLayer => new ModPresetLayer(),
                        ModPresetLayer => new RestrictionLayer(),
                        _ => throw new ArgumentOutOfRangeException(nameof(currentLayer), "Unknown Layer Type"),
                    };
                }
                CkGui.AttachToolTip("Swap layer type to Mod Preset Layer. (Hold Shift)");

                ImUtf8.SameLineInner();
                if (CkGui.IconButton(FAI.Eraser, inPopup: true, disabled: !KeyMonitor.ShiftPressed() || curLayerIdx != _manager.ItemInEditor.Layers.Count - 1))
                    _manager.ItemInEditor!.Layers.RemoveAt(curLayerIdx);
                CkGui.AttachToolTip("Delete this layer. (Hold Shift)--SEP--Only the highest layer can be removed.");
            }
        }

        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementBG.Uint(), DragDropItemRounding, ImDrawFlags.RoundCornersRight);
    }

    private void DrawNewLayerRow(int layerIdx)
    {
        using var id = ImRaii.PushId(layerIdx);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        DrawSideHeader($"##Move{layerIdx}", $"Layer {layerIdx + 1}", DragDropHeaderWidth);

        // Draw out the remaining child.
        ImGui.SameLine();
        ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0));
        using (ImRaii.Child("CreateNewLayer" + layerIdx, new Vector2(ImGui.GetContentRegionAvail().X, DragDropItemHeight), false, ImGuiWindowFlags.AlwaysUseWindowPadding))
        {
            var buttonSize = CkGui.IconTextButtonSize(FAI.Plus, "Add New Layer");
            ImGui.SetCursorPosY((DragDropItemHeight - ImGui.GetFrameHeight()) / 2);
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - buttonSize - ImGui.GetStyle().ItemSpacing.X);
            if (CkGui.IconTextButton(FAI.Plus, "New Layer", isInPopup: true))
                _manager.ItemInEditor!.Layers.Add(new RestrictionLayer());
        }
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementBG.Uint(), DragDropItemRounding, ImDrawFlags.RoundCornersRight);
    }

    private void DrawLayerDetails(int layerIdx)
    {
        // Depending on the type we want to draw its respective details.
        var buttonSize = CkGui.IconButtonSize(FAI.Eraser);
        var rightButtons = buttonSize.X * 2 + ImGui.GetStyle().ItemSpacing.X;
        var detailsWidth = ImGui.GetContentRegionAvail().X - rightButtons;

        if (_manager.ItemInEditor!.Layers[layerIdx] is RestrictionLayer restrictionLayer)
            DrawRestrictionLayer(restrictionLayer, detailsWidth);
        else if (_manager.ItemInEditor.Layers[layerIdx] is ModPresetLayer modPresetLayer)
            DrawModPresetLayer(modPresetLayer, detailsWidth);
    }

    private void DrawRestrictionLayer(RestrictionLayer layer, float width)
    {
        // Get the total layer width we want first.
        ImGui.SameLine(ImGui.GetStyle().ItemSpacing.X);
        using (ImRaii.Group())
        {
            // Begin by printing out the restriction layer information.
            _equipDrawer.DrawRestrictionRef(layer, layer.ID.ToString(), width / 2);
            ImGui.SameLine();
            // Draw a group for the moodles and hardcore traits.
            using (ImRaii.Group())
            {
                // Draw moodles, if present.
                if (layer.Ref?.Moodle is { } refMoodle)
                    _moodleDrawer.ShowStatusIcons(refMoodle, ImGui.GetContentRegionAvail().X, MoodleDrawer.IconSizeFramed);

                // Draw hardcore Traits, if present.
                var refTraits = layer.Ref?.Traits ?? Traits.None;
                _traitDrawer.DrawTraitPreview(refTraits);
            }
        }
    }

    private void DrawModPresetLayer(ModPresetLayer modPresetLayer, float width)
    {
        ImGui.SameLine(ImGui.GetStyle().ItemSpacing.X);
        using (ImRaii.Group())
        {
            _modDrawer.DrawModPresetCombos(modPresetLayer.ID.ToString(), modPresetLayer, width / 2);
            ImGui.SameLine();
            var hoverBoxRegion = ImGui.CalcTextSize("Hover To See Settings") + ImGui.GetStyle().FramePadding;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - hoverBoxRegion.Y) / 2);
            using (CkRaii.FrameChildPadded($"ModPresetPreview{modPresetLayer.ID}", hoverBoxRegion, CkColor.FancyHeaderContrast.Uint()))
            {
                ImGuiUtil.Center("Hover To See Settings");

                if (ImGui.IsItemHovered() && modPresetLayer.Mod.HasData)
                    _modDrawer.DrawPresetTooltip(modPresetLayer.Mod);
            }
        }
    }

    /// <summary> Places a header left-aligned beside a child window. </summary>
    private void DrawSideHeader(string id, string text, float width)
    {
        ImGui.InvisibleButton(id, new Vector2(width, DragDropItemHeight));
        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var linePos = min + new Vector2(width, 0);

        // Draw the child background with the element header color.
        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), DragDropItemRounding, ImDrawFlags.RoundCornersLeft);
        // Draw the line off to the left.
        wdl.AddLine(linePos, linePos with { Y = max.Y }, CkColor.ElementSplit.Uint(), 2);
        var textStart = new Vector2((width - ImGui.CalcTextSize(text).X) / 2, (DragDropItemHeight - ImGui.GetTextLineHeight()) / 2);
        wdl.AddText(min + textStart, ImGui.GetColorU32(ImGuiCol.Text), text);
    }

    public void MoveItemToPosition<T>(IList<T> list, Func<T, bool> sourceItemSelector, int targetedIndex)
    {
        var sourceIndex = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (sourceItemSelector(list[i]))
            {
                sourceIndex = i;
                break;
            }
        }
        if (sourceIndex == targetedIndex) return;
        var item = list[sourceIndex];
        list.RemoveAt(sourceIndex);
        list.Insert(targetedIndex, item);
    }
}
