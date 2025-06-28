using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Localization;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;
using static CkCommons.Widgets.CkHeader;

namespace GagSpeak.Gui.Wardrobe;

// it might be wise to move the selector draw into the panel so we have more control over the editor covering both halves.
public partial class RestraintsPanel : DisposableMediatorSubscriberBase
{
    private readonly RestraintSetFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly AttributeDrawer _attributeDrawer;
    private readonly RestraintManager _manager;
    private readonly KinksterManager _pairs;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ItemInEditor != null;
    public RestraintsPanel(
        ILogger<RestraintsPanel> logger, 
        GagspeakMediator mediator,
        RestraintSetFileSelector selector,
        ActiveItemsDrawer activeDrawer,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        AttributeDrawer attributeDrawer,
        RestraintManager manager,
        RestraintEditorInfo editorInfo,
        RestraintEditorLayers editorLayers,
        RestraintEditorEquipment editorEquipment,
        RestraintEditorModsMoodles editorModsMoodles,
        KinksterManager pairs,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator)
    {
        _selector = selector;
        _activeItemDrawer = activeDrawer;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _attributeDrawer = attributeDrawer;
        _manager = manager;
        _pairs = pairs;
        _cosmetics = cosmetics;
        _guides = guides;

        // create some dummy tabs to see if it even works.
        EditorTabs = new IFancyTab[]
        {
            editorInfo,
            editorEquipment,
            editorLayers,
            editorModsMoodles,
        };

        Mediator.Subscribe<TooltipSetItemToEditorMessage>(this, (msg) =>
        {
            if (_manager.ItemInEditor?.RestraintSlots[msg.Slot] is RestraintSlotBasic basicSlot)
            {
                basicSlot.Glamour.GameItem = msg.Item;
                Logger.LogDebug($"Set [" + msg.Slot + "] to [" + msg.Item.Name + "] on edited set " + "[" + _manager.ItemInEditor.Label + "]", LoggerType.Restraints);
            }
        });

        Mediator.Subscribe<ThumbnailImageSelected>(this, (msg) =>
        {
            if (msg.MetaData.Kind is not ImageDataType.Restraints)
                return;

            if (manager.Storage.TryGetRestraint(msg.MetaData.SourceId, out var match))
            {
                _selector.SelectByValue(match);
                manager.UpdateThumbnail(match, msg.Name);
            }
        });
    }

    private static TriStateBoolCheckbox HelmetCheckbox = new();
    private static TriStateBoolCheckbox VisorCheckbox = new();
    private static TriStateBoolCheckbox WeaponCheckbox = new();
    public IFancyTab[] EditorTabs;

    /// <summary> All Content in here is grouped. Can draw either editor or overview left panel. </summary>
    public void DrawEditorContents(CkHeader.DrawRegion topRegion, CkHeader.DrawRegion botRegion)
    {
        ImGui.SetCursorScreenPos(topRegion.Pos);
        using (ImRaii.Child("RestraintEditorTop", topRegion.Size))
            DrawEditorHeader();

        ImGui.SetCursorScreenPos(botRegion.Pos);
        using (ImRaii.Child("RestraintEditorBot", botRegion.Size, false, WFlags.AlwaysUseWindowPadding))
        {
            // Draw out the tab bar, and the items respective contents.
            using (CkRaii.TabBarChild("AllowanceTabBars", WFlags.AlwaysUseWindowPadding, out var selected, EditorTabs))
                selected?.DrawContents(ImGui.GetContentRegionAvail().X);
        }
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("RestraintsTopLeft", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestraintsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestraintsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // Draw the selected item.
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawSelectedItemInfo(drawRegions.BotRight, curveSize);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));

        // Shift down and draw the Active items
        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y * 3);
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos + verticalShift);
        DrawActiveItemInfo(drawRegions.BotRight.Size - verticalShift);
    }

    private void DrawSelectedItemInfo(CkHeader.DrawRegion drawRegion, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var region = new Vector2(drawRegion.Size.X, WardrobeUI.SelectedRestraintH().AddWinPadY());
        var disabled = _selector.Selected is null || _selector.Selected.Identifier.Equals(_manager.AppliedRestraint?.Identifier);
        var tooltipAct = "Double Click me to begin editing!";

        // Draw the inner label child action item.
        using var inner = CkRaii.LabelChildAction("SelItem", region, DrawLabel, ImGui.GetFrameHeight(), BeginEdits, tooltipAct, dFlag: ImDrawFlags.RoundCornersRight);

        var pos = ImGui.GetItemRectMin();
        var imgSize = new Vector2(inner.InnerRegion.Y / 1.2f, inner.InnerRegion.Y);
        var imgDrawPos = pos with { X = pos.X + inner.InnerRegion.X - imgSize.X };
        // Draw the left item stuff.
        if (_selector.Selected is not null)
            DrawSelectedInner(drawRegion, rounding, imgSize.X);

        // Draw the right image item.
        ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
        ImGui.SetCursorScreenPos(imgDrawPos);
        if (_selector.Selected is not null)
        {
            _activeItemDrawer.DrawRestraintImage(_selector.Selected!, imgSize, rounding, true);
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                var metaData = new ImageMetadataGS(ImageDataType.Restraints, new Vector2(120, 120f * 1.2f), _selector.Selected!.Identifier);
                Mediator.Publish(new OpenThumbnailBrowser(metaData));
            }
            CkGui.AttachToolTip("The Thumbnail for this Restraint Set.--SEP--Double Click to change the image.");
        }

        void DrawLabel()
        {
            using var _ = ImRaii.Child("LabelChild", new Vector2(region.X * .6f, ImGui.GetFrameHeight()));
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().WindowPadding.X);
            ImUtf8.TextFrameAligned(_selector.Selected?.Label ?? "No Item Selected!");
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || disabled)
                return;

            if (_selector.Selected is not null)
                _manager.StartEditing(_selector.Selected);
        }
    }

    private void DrawSelectedInner(CkHeader.DrawRegion drawRegion, float rounding, float rightOffset)
    {
        using var innerGroup = ImRaii.Group();
        float maxWidth = drawRegion.Size.X - rightOffset - ImGui.GetStyle().WindowPadding.X * 2;

        DrawAttributeRow();
        DrawLayerRow();

        _attributeDrawer.DrawTraitPreview(_selector.Selected!.Traits);

        // Draw out the moodles row to finalize it off, only up to 7.
        _moodleDrawer.ShowStatusIcons(_selector.Selected!.GetMoodles(), maxWidth, MoodleDrawer.IconSize, 1);
    }

    private void DrawAttributeRow()
    {
        using var _ = ImRaii.Group();
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        var sel = _selector.Selected!;
        var trueCol = 0xFFFFFFFF;
        var falseCol = CkColor.FancyHeaderContrast.Uint();

        var layers = sel.Layers.Count > 0;
        var mods = sel.RestraintMods.Count > 0;
        var moodles = sel.RestraintMoodles.Count > 0;
        var meta = sel.HeadgearState != TriStateBool.Null || sel.VisorState != TriStateBool.Null || sel.WeaponState != TriStateBool.Null;
        var redraws = sel.DoRedraw;
        var traits = sel.Traits != 0;
        var arousal = sel.Arousal != 0;

        CkGui.FramedIconText(FAI.LayerGroup, layers ? trueCol : falseCol);
        CkGui.AttachToolTip(layers ? "This Restraint has layers" : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.FileDownload, mods ? trueCol : falseCol);
        CkGui.AttachToolTip(mods ? "This Set has attached Mods" : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.TheaterMasks, moodles ? trueCol : falseCol);
        CkGui.AttachToolTip(moodles ? "This Set has attached Moodles" : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.Glasses, meta ? trueCol : falseCol);
        CkGui.AttachToolTip(meta ? "This Set is forcing Metadata states." : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.Repeat, redraws ? trueCol : falseCol);
        CkGui.AttachToolTip(redraws ? "This Set redraws the player upon application / removal." : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.PersonRays, traits ? trueCol : falseCol);
        CkGui.AttachToolTip(traits ? "This Set applies Hardcore Traits when set by allowed kinksters." : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.Heartbeat, arousal ? trueCol : falseCol);
        CkGui.AttachToolTip(arousal ? "This Set increases arousal levels." : string.Empty);
    }

    private void DrawLayerRow()
    {
        using var _ = ImRaii.Group();
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        var sel = _selector.Selected!;
        var trueCol = 0xFFFFFFFF;
        var falseCol = CkColor.FancyHeaderContrast.Uint();

        var layerCount = sel.Layers.Count;

        CkGui.FramedIconText(FAI.DiceOne, layerCount > 0 ? trueCol : falseCol);
        CkGui.AttachToolTip(layerCount > 0 ? "Layer 1 Exists" : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.DiceTwo, layerCount > 1 ? trueCol : falseCol);
        CkGui.AttachToolTip(layerCount > 1 ? "Layer 2 Exists" : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.DiceThree, layerCount > 2 ? trueCol : falseCol);
        CkGui.AttachToolTip(layerCount > 2 ? "Layer 3 Exists" : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.DiceFour, layerCount > 3 ? trueCol : falseCol);
        CkGui.AttachToolTip(layerCount > 3 ? "Layer 4 Exists" : string.Empty);

        ImUtf8.SameLineInner();
        CkGui.FramedIconText(FAI.DiceFive, layerCount > 4 ? trueCol : falseCol);
        CkGui.AttachToolTip(layerCount > 4 ? "Layer 5 Exists" : string.Empty);
    }

    private void DrawActiveItemInfo(Vector2 regiun)
    {
        var labelSize = new Vector2(regiun.X * .7f, ImGui.GetTextLineHeightWithSpacing());

        using (var c = CkRaii.LabelChildText(regiun, labelSize, "Active Restraint Set", ImGui.GetStyle().ItemSpacing.X, ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight))
        {
            if (_manager.ServerRestraintData is not { } activeSet)
                return;
            
            
            _activeItemDrawer.ApplyRestraintSetGroup(activeSet);
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                Mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Removed, new CharaActiveRestraint()));
            }
        }
    }

    private void DrawEditorHeader()
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } setInEdit)
            return;

        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var c = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.ChildBg, CkColor.FancyHeaderContrast.Uint());

        if (CkGui.IconButton(FAI.ArrowLeft))
            _manager.StopEditing();

        // Create a child that spans the remaining region.
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2 - CkGui.IconButtonSize(FAI.ArrowLeft).X - ImGui.GetStyle().ItemInnerSpacing.X);
        var curLabel = setInEdit.Label;
        if (ImGui.InputTextWithHint("##EditorNameField", "Enter Name...", ref curLabel, 48))
            setInEdit.Label = curLabel;

        ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X);
        var remainingWidth = ImGui.GetContentRegionAvail().X;

        // now we must draw out the right side.
        var childGroupSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());
        var itemSpacing = (remainingWidth - CkGui.IconButtonSize(FAI.Save).X - (childGroupSize.X * 4)) / 6;

        // Cast a child group for the drawer.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + itemSpacing);
        using (ImRaii.Child("HelmetMetaGroup", childGroupSize))
        {
            ImGui.AlignTextToFramePadding();
            if (HelmetCheckbox.Draw("##MetaHelmet", setInEdit.HeadgearState, out var newHelmValue))
                setInEdit.HeadgearState = newHelmValue;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.HatCowboySide);
            CkGui.AttachToolTip("The locked helmet state while active.--SEP--Note: conflicts prioritize ON over OFF.");
        }

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("VisorMetaGroup", childGroupSize))
        {
            if (VisorCheckbox.Draw("##MetaVisor", setInEdit.VisorState, out var newVisorValue))
                setInEdit.VisorState = newVisorValue;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Glasses);
            CkGui.AttachToolTip("The locked visor state while active.--SEP--Note: conflicts prioritize ON over OFF.");
        }

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("WeaponMetaGroup", childGroupSize))
        {
            if (WeaponCheckbox.Draw("##MetaWeapon", setInEdit.WeaponState, out var newWeaponValue))
                setInEdit.WeaponState = newWeaponValue;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Explosion);
            CkGui.AttachToolTip("The locked weapon state while active.--SEP--Note: conflicts prioritize ON over OFF.");
        }

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("RedrawMetaGroup", childGroupSize))
        {
            var doRedraw = setInEdit.DoRedraw;
            if (ImGui.Checkbox("##MetaRedraw", ref doRedraw))
                setInEdit.DoRedraw = doRedraw;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Repeat);
            CkGui.AttachToolTip("If you redraw after application.");
        }

        // beside this, enhances the font scale to 1.5x, draw the save icon, then restore the font scale.
        ImGui.SameLine(0, itemSpacing);
        if (CkGui.IconButton(FAI.Save))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Save Changes to this Restraint Set.");

    }
}
