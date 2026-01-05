using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using Dalamud.Bindings.ImGui;
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
    private float DragDropItemHeight => CkStyle.TwoRowHeight().AddWinPadY();
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
        var layerIdx = 0;

        using (ImRaii.Table("##RestraintLayers", 1, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg, region))
        {
            // The .ToList() makes the for-each work on a copy, preventing an exception when removing a layer.
            foreach (var layer in setInEdit.Layers.ToList())
            {
                DrawExistingLayer(layer, layerIdx, region.X);

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
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.Layers, ImGui.GetWindowPos(), ImGui.GetWindowSize());
    }

    private void DrawExistingLayer(IRestraintLayer layer, int idx, float totalWidth) 
    {
        ImGui.TableNextRow();
        using var id = ImRaii.PushId(layer.ID.ToString());
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var rightButtons = new Vector2(CkGui.IconButtonSize(FAI.Eraser).X * 2 + spacing, ImGui.GetFrameHeight());

        // If we are currently holding down our mouse and 'moving' the item, have it fade between a gradient green glow.
        if (_dragLayerId == layer.ID)
        {
            var greenCol = CkColor.TriStateCheck.Vec4();
            var color = CkGui.Color(Gradient.Get(greenCol, greenCol with { W = greenCol.W / 4 }, 500));
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, color);
        }

        // Draw out the side header, which is what we use for our movable drag-drop object.
        ImGui.TableNextColumn();
        var rowPos = ImGui.GetCursorPos();
        DrawSideHeader($"##Move{layer.ID}", $"Layer {idx + 1}", DragDropHeaderWidth);

        // Show the cursor move icon while hovering.
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

        // Define the drag-drop source. Here we store the ID of the restriction layer into the payload.
        // By doing this, its respective accepted payload target can match with the same ID.
        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
        {
            CkGui.SetDragDropPayload("ReorderRestraintLayers", layer.ID);
            _dragLayerId = layer.ID;
            //_logger.LogTrace("DragDropSource = " + _dragLayerId);
            ImGui.EndDragDropSource();
        }
        // If we are NOT dragging, and we have just released, and our drag layer is the same as the current, clear the drag layer.
        else if (_dragLayerId == layer.ID)
            _dragLayerId = Guid.Empty;

        // Define the move index that should be used if the drag-drop target is accepted.
        var moveIndex = idx;
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
        using (var c = CkRaii.ChildPadded($"DDLayer{idx}", new Vector2(ImGui.GetContentRegionAvail().X, DragDropItemHeight).WithoutWinPadding()))
        {
            // Basic layer information.
            CkGui.InlineSpacingInner();
            // Depending on the type we want to draw its respective details. draw out layer info by type
            if (layer is RestrictionLayer rLayer)
                _equipDrawer.DrawRestrictionRef(rLayer, layer.ID.ToString(), c.InnerRegion.X * .5f);
            else if (layer is ModPresetLayer mpLayer)
            {
                var combosW = c.InnerRegion.X * .5f - ImGui.GetFrameHeight();
                _modDrawer.DrawModPresetCombos(mpLayer.ID.ToString(), mpLayer, combosW);
                ImGui.SameLine(0,0);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (c.InnerRegion.Y - ImGui.GetFrameHeight()) / 2);
                CkGui.FramedHoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint());
                if (ImGui.IsItemHovered() && mpLayer.IsValid())
                    _modDrawer.DrawPresetTooltip(mpLayer.Mod);
            }

            ImUtf8.SameLineInner();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - rightButtons.Y) / 2);
            using (var bc = CkRaii.Child($"LayerButtons{idx}", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
            {
                var tmpLabel = layer.Label;
                ImGui.SetNextItemWidth(bc.InnerRegion.X - rightButtons.X - spacing);
                ImGui.InputTextWithHint($"##LayerName_{idx}", "Input Layer Name..", ref tmpLabel, 45);
                if (ImGui.IsItemDeactivatedAfterEdit() && tmpLabel != layer.Label)
                    layer.Label = tmpLabel;
                CkGui.AttachToolTip("The Layer name is seen by other Kinksters when applying Restraint Layers!");

                // Swap the layer type to the other type.
                ImUtf8.SameLineInner();
                if (CkGui.IconButton(FAI.ArrowsLeftRight, inPopup: true, disabled: !KeyMonitor.ShiftPressed()))
                    _manager.ItemInEditor!.Layers[idx] = layer is RestrictionLayer ? new ModPresetLayer() : new RestrictionLayer();
                CkGui.AttachToolTip("Swap layer type to Mod Preset Layer. (Hold Shift)");
                if (idx == 0) // this only needs to attach to the first layer item.
                {
                    _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.LayerTypes, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
                        () => _manager.ItemInEditor!.Layers[idx] = layer is RestrictionLayer ? new ModPresetLayer() : new RestrictionLayer());
                    _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.LayerTypesBuffer, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
                        () => FancyTabBar.SelectTab("RS_EditBar", RestraintsPanel.EditorTabs[3], RestraintsPanel.EditorTabs));
                }
                    ImUtf8.SameLineInner();
                if (CkGui.IconButton(FAI.Eraser, inPopup: true, disabled: !KeyMonitor.ShiftPressed() || idx != _manager.ItemInEditor!.Layers.Count - 1))
                    _manager.ItemInEditor!.Layers.RemoveAt(idx);
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
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AddingLayers, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
                () =>
                {
                    _manager.ItemInEditor!.Layers.Add(new RestrictionLayer());
                    _manager.ItemInEditor!.Layers.Add(new RestrictionLayer());
                });
        }
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementBG.Uint(), DragDropItemRounding, ImDrawFlags.RoundCornersRight);
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
