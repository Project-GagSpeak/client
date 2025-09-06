using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using OtterGui.Text;
using static FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.DynamicEvent.Delegates;

namespace GagSpeak.Gui.Wardrobe;

// it might be wise to move the selector draw into the panel so we have more control over the editor covering both halves.
public class RestraintsPanel : DisposableMediatorSubscriberBase
{
    private readonly RestraintSetFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly AttributeDrawer _attributeDrawer;
    private readonly RestraintManager _manager;
    private readonly UiThumbnailService _thumbnails;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ItemInEditor != null;
    public RestraintsPanel(
        ILogger<RestraintsPanel> logger, 
        GagspeakMediator mediator,
        RestraintSetFileSelector selector,
        ActiveItemsDrawer activeDrawer,
        MoodleDrawer moodleDrawer,
        AttributeDrawer attributeDrawer,
        RestraintManager manager,
        RestraintEditorInfo editorInfo,
        RestraintEditorLayers editorLayers,
        RestraintEditorEquipment editorEquipment,
        RestraintEditorModsMoodles editorModsMoodles,
        UiThumbnailService thumbnails,
        TutorialService guides) : base(logger, mediator)
    {
        _selector = selector;
        _thumbnails = thumbnails;
        _activeItemDrawer = activeDrawer;
        _moodleDrawer = moodleDrawer;
        _attributeDrawer = attributeDrawer;
        _manager = manager;
        _guides = guides;

        // The editor tab windows.
        EditorTabs = [editorInfo, editorEquipment, editorLayers, editorModsMoodles];
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
            if (msg.Folder is not ImageDataType.Restraints)
                return;

            if (manager.Storage.TryGetRestraint(msg.SourceId, out var match))
            {
                _selector.SelectByValue(match);
                manager.UpdateThumbnail(match, msg.FileName);
            }
        });
    }

    private static TriStateBoolCheckbox TriCheckbox = new();

    public static IFancyTab[] EditorTabs;

    /// <summary> All Content in here is grouped. Can draw either editor or overview left panel. </summary>
    public void DrawEditorContents(CkHeader.QuadDrawRegions regions)
    {
        ImGui.SetCursorScreenPos(regions.TopLeft.Pos);
        using (ImRaii.Child("RestraintEditTop", regions.TopSize))
            DrawEditorHeader();

        ImGui.SetCursorScreenPos(regions.BotLeft.Pos);
        using (ImRaii.Child("RestraintEditBot", regions.BotSize, false, WFlags.AlwaysUseWindowPadding))
        {
            // Draw out the tab bar, and the items respective contents.
            using (var _ = CkRaii.TabBarChild("RS_EditBar", CkColor.VibrantPink.Uint(), CkColor.VibrantPinkHovered.Uint(), CkColor.FancyHeader.Uint(),
                LabelFlags.PadInnerChild | LabelFlags.SizeIncludesHeader, out var selected, EditorTabs))
                selected?.DrawContents(_.InnerRegion.X);
        }
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("RestraintsTopLeft", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.Searching, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestraintsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.RestraintList, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () =>
            {
                if (_selector.tutorialSet is not null)
                    _selector.SelectByValue(_selector.tutorialSet);
            });

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestraintsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // Draw the selected item.
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawSelectedItemInfo(drawRegions.BotRight, curveSize);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SelectedRestraint, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        // Shift down and draw the Active items
        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y * 3);
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos + verticalShift);
        DrawActiveItemInfo(drawRegions.BotRight.Size - verticalShift);
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ActiveRestraint, ImGui.GetWindowPos(), ImGui.GetWindowSize());
    }

    private void DrawSelectedItemInfo(CkHeader.DrawRegion drawRegion, float rounding)
    {
        var height = ImGui.GetFrameHeightWithSpacing() * 2 + MoodleDrawer.IconSize.Y;
        var region = new Vector2(drawRegion.Size.X, height);
        var notSelected = _selector.Selected is null;
        var isActive = !notSelected && _selector.Selected!.Equals(_manager.AppliedRestraint!);
        var tooltip = notSelected ? "No item selected!" : isActive ? "Restraint Set is Active!" : "Double Click to edit this Restraint Set.";

        var label = notSelected ? "Nothing Selected!" : _selector.Selected!.Label;
        using var inner = CkRaii.ChildLabelButton(region, .6f, label, ImGui.GetFrameHeight(), BeginEdits, tooltip, DFlags.RoundCornersRight, LabelFlags.AddPaddingToHeight);
        if (_selector.Selected is not null)
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.EnteringEditor, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => _manager.StartEditing(_selector.Selected));

        var pos = ImGui.GetItemRectMin();
        var imgSize = new Vector2(inner.InnerRegion.Y / 1.2f, inner.InnerRegion.Y);
        var imgDrawPos = pos with { X = pos.X + inner.InnerRegion.X - imgSize.X };

        // Left side content
        if (_selector.Selected is not null)
        {
            // 1st row.
            using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
            {
                CkGui.BooleanToColoredIcon(_selector.Selected!.IsEnabled, false);
                CkGui.TextFrameAlignedInline($"Visuals  ");
            }
            if (!isActive && ImGui.IsItemHovered() && ImGui.IsItemClicked())
                _manager.ToggleVisibility(_selector.Selected!.Identifier);
            CkGui.AttachToolTip($"Visuals {(_selector.Selected!.IsEnabled ? "will" : "will not")} be applied.");

            if (_selector.Selected!.Traits > 0)
            {
                ImUtf8.SameLineInner();
                _attributeDrawer.DrawTraitPreview(_selector.Selected!.Traits);
            }
            // 2nd row
            DrawAttributeRow();
            // 3rd row
            var maxWidth = drawRegion.Size.X - imgSize.X - ImGui.GetStyle().WindowPadding.X * 2;
            _moodleDrawer.ShowStatusIcons(_selector.Selected!.GetAllMoodles(), maxWidth, MoodleDrawer.IconSize, 1);
        }

        // Right side image
        ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
        ImGui.SetCursorScreenPos(imgDrawPos);
        if (_selector.Selected is not null)
        {
            _activeItemDrawer.DrawRestraintImage(_selector.Selected!, imgSize, rounding);
            if (!isActive && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                _thumbnails.SetThumbnailSource(_selector.Selected!.Identifier, new Vector2(120, 120f * 1.2f), ImageDataType.Restraints);
            CkGui.AttachToolTip("The Thumbnail for this Restraint Set.--SEP--Double Click to change the image.");

            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SelectingThumbnails, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
                () => _thumbnails.SetThumbnailSource(_selector.Selected!.Identifier, new Vector2(120, 120f * 1.2f), ImageDataType.Restraints));
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is ImGuiMouseButton.Left && !notSelected && !isActive)
                _manager.StartEditing(_selector.Selected!);
        }
    }

    private void DrawAttributeRow()
    {
        using var _ = ImRaii.Group();
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        var sel = _selector.Selected!;
        var trueCol = 0xFFFFFFFF;
        var falseCol = CkColor.FancyHeaderContrast.Uint();

        var attrs = new (FAI Icon, bool Condition, string Tooltip)[]
{
            (FAI.LayerGroup,      sel.Layers.Count > 0,                         "This Restraint has layers"),
            (FAI.FileDownload,    sel.RestraintMods.Count > 0,                  "This Set has attached Mods"),
            (FAI.TheaterMasks,    sel.RestraintMoodles.Count > 0,               "This Set has attached Moodles"),
            (FAI.Glasses,         !sel.MetaStates.Equals(MetaDataStruct.Empty), "This Set is forcing Metadata states."),
            (FAI.Repeat,          sel.DoRedraw,                                 "This Set redraws the player upon application / removal."),
            (FAI.PersonRays,      sel.Traits != 0,                              "This Set applies Hardcore Traits when set by allowed kinksters."),
            (FAI.Heartbeat,       sel.Arousal != 0,                             "This Set increases arousal levels."),
};

        foreach (var (icon, condition, tooltip) in attrs)
            DrawAttrIcon(icon, condition, tooltip);

        // Helper func.
        void DrawAttrIcon(FAI icon, bool condition, string tooltip)
        {
            CkGui.FramedIconText(icon, condition ? trueCol : falseCol);
            CkGui.AttachToolTip(condition ? tooltip : string.Empty);
            ImUtf8.SameLineInner();
        }
    }

    private float GetActiveItemHeight()
        => _manager.ServerData?.Identifier == Guid.Empty ? ImGui.GetFrameHeight() : CkStyle.GetFrameRowsHeight(8);

    private void DrawActiveItemInfo(Vector2 region)
    {
        var appliedSet = _manager.AppliedRestraint;
        var title = appliedSet is not null ? $"Active Set - {appliedSet.Label}" : "Active Restraint Set";
        using var c = CkRaii.HeaderChild(title, new Vector2(region.X, GetActiveItemHeight()), HeaderFlags.AddPaddingToHeight);

        if (_manager.ServerData is not { } data)
            return;

        // if no item is selected, display the unique 'Applier' group.
        if (data.Identifier == Guid.Empty)
        {
            _activeItemDrawer.ApplyItemGroup(data);
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SelectingRestraint, ImGui.GetWindowPos(), ImGui.GetWindowSize());
            return;
        }

        // Otherwise, if the item is sucessfully applied, display the locked states, based on what is active.
        if (_manager.AppliedRestraint is { } item)
        {
            if (data.IsLocked())
                _activeItemDrawer.UnlockItemGroup(data, item);
            else
                _activeItemDrawer.LockItemGroup(data, item);
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
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.CancelingChanges, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        // Create a child that spans the remaining region.
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2 - CkGui.IconButtonSize(FAI.ArrowLeft).X - ImGui.GetStyle().ItemInnerSpacing.X);
        var curLabel = setInEdit.Label;
        if (ImGui.InputTextWithHint("##EditorNameField", "Enter Name...", ref curLabel, 48))
            setInEdit.Label = curLabel;
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.EditName, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X);
        var remainingWidth = ImGui.GetContentRegionAvail().X;

        // now we must draw out the right side.
        var childGroupSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());
        var itemSpacing = (remainingWidth - CkGui.IconButtonSize(FAI.Save).X - (childGroupSize.X * 4)) / 6;

        // Cast a child group for the drawer.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + itemSpacing);
        using (ImRaii.Group())
        {
            using (ImRaii.Child("HelmetMetaGroup", childGroupSize))
            {
                ImGui.AlignTextToFramePadding();
                if (TriCheckbox.Draw("##MetaHelmet", setInEdit.MetaStates.Headgear, out var newHelmValue))
                    setInEdit.MetaStates = setInEdit.MetaStates.WithMetaIfDifferent(MetaIndex.HatState, newHelmValue);

                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.HatCowboySide);
                CkGui.AttachToolTip("The locked helmet state while active.--SEP--Note: conflicts prioritize ON over OFF.");
            }

            ImGui.SameLine(0, itemSpacing);
            using (ImRaii.Child("VisorMetaGroup", childGroupSize))
            {
                if (TriCheckbox.Draw("##MetaVisor", setInEdit.MetaStates.Visor, out var newVisorValue))
                    setInEdit.MetaStates = setInEdit.MetaStates.WithMetaIfDifferent(MetaIndex.VisorState, newVisorValue);

                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.Glasses);
                CkGui.AttachToolTip("The locked visor state while active.--SEP--Note: conflicts prioritize ON over OFF.");
            }

            ImGui.SameLine(0, itemSpacing);
            using (ImRaii.Child("WeaponMetaGroup", childGroupSize))
            {
                if (TriCheckbox.Draw("##MetaWeapon", setInEdit.MetaStates.Weapon, out var newWeaponValue))
                    setInEdit.MetaStates = setInEdit.MetaStates.WithMetaIfDifferent(MetaIndex.WeaponState, newWeaponValue);

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
        }
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.EditMeta, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        // beside this, enhances the font scale to 1.5x, draw the save icon, then restore the font scale.
        ImGui.SameLine(0, itemSpacing);
        if (CkGui.IconButton(FAI.Save))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Save Changes to this Restraint Set.");
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SavingChanges, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () => _manager.SaveChangesAndStopEditing());
    }
}
