using Dalamud.Interface.Colors;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Kinksters.Pairs;
using GagSpeak.State.Listeners;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Text.Widget.Editors;

namespace GagSpeak.CkCommons.Gui.Wardrobe;
public partial class GagRestrictionsPanel
{
    private readonly ILogger<GagRestrictionsPanel> _logger;
    private readonly GagRestrictionFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly TraitsDrawer _traitsDrawer;
    private readonly GagRestrictionManager _manager;
    private readonly PairManager _pairs;
    private readonly CosmeticService _textures;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ItemInEditor != null;
    public GagRestrictionsPanel(
        ILogger<GagRestrictionsPanel> logger,
        GagRestrictionFileSelector selector,
        ActiveItemsDrawer activeItemDrawer,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        TraitsDrawer traitsDrawer,
        GagRestrictionManager manager,
        PairManager pairs,
        CosmeticService textures,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _activeItemDrawer = activeItemDrawer;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _traitsDrawer = traitsDrawer;
        _manager = manager;
        _pairs = pairs;
        _textures = textures;
        _guides = guides;
        _profileCombo = new CustomizeProfileCombo(logger);
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("GagsTopLeft", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("GagsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("GagsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // Draw the selected Item
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

    public void DrawEditorContents(CkHeader.QuadDrawRegions drawRegions, float curveSize)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("RestraintsTopLeft", drawRegions.TopLeft.Size))
            DrawEditorHeaderLeft(drawRegions.TopLeft.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestraintsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            DrawEditorLeft(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestraintsTopRightTR", drawRegions.TopRight.Size))
            DrawEditorHeaderRight(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("RestraintsBR", drawRegions.BotRight.Size))
            DrawEditorRight(drawRegions.BotRight.SizeX);
    }

    private void DrawSelectedItemInfo(CkHeader.DrawRegion drawRegion, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var height = ImGui.GetFrameHeight() * 2 + MoodleDrawer.IconSize.Y + ImGui.GetStyle().ItemSpacing.Y * 2;
        var region = new Vector2(drawRegion.Size.X, height.AddWinPadY());
        var tooltipAct = "Double Click me to begin editing!";

        using var inner = CkRaii.LabelChildAction("SelItem", region, DrawLabel, ImGui.GetFrameHeight(), BeginEdits, tt: tooltipAct, dFlag: ImDrawFlags.RoundCornersRight);

        var pos = ImGui.GetItemRectMin();
        var imgSize = new Vector2(inner.InnerRegion.Y);
        var imgDrawPos = pos with { X = pos.X + inner.InnerRegion.X - imgSize.X };
        // Draw the left items.
        if (_selector.Selected is not null)
            DrawSelectedInner(imgSize.X);

        // Draw the right image item.
        ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
        ImGui.SetCursorScreenPos(imgDrawPos);
        if (_selector.Selected is not null)
            _activeItemDrawer.DrawFramedImage(_selector.Selected!.GagType, imgSize.Y, rounding, true);

        void DrawLabel()
        {
            using var _ = ImRaii.Child("LabelChild", new Vector2(region.X * .6f, ImGui.GetFrameHeight()));
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().WindowPadding.X);
            ImUtf8.TextFrameAligned(_selector.Selected?.GagType.GagName() ?? "No Item Selected!");
            ImGui.SameLine(region.WithoutWinPadding().X * .6f - ImGui.GetFrameHeightWithSpacing());
            var imgPos = ImGui.GetCursorScreenPos();

            // Draw the type of restriction item as an image path here.
            if (_selector.Selected is not null)
            {
                (var image, var tooltip) = (_textures.CoreTextures[CoreTexture.Gagged], "This is a Gag Restriction!");
                ImGui.GetWindowDrawList().AddDalamudImage(image, imgPos, new Vector2(ImGui.GetFrameHeight()), tooltip);
            }
        }

        void BeginEdits() { if (_selector.Selected is not null) _manager.StartEditing(_selector.Selected!); }
    }

    private void DrawSelectedInner(float rightOffset)
    {
        using var innerGroup = ImRaii.Group();

        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
        {
            CkGui.BooleanToColoredIcon(_selector.Selected!.IsEnabled, false);
            CkGui.TextFrameAlignedInline($"Visuals  ");
        }
        if (ImGui.IsItemHovered() && ImGui.IsItemClicked())
            _manager.ToggleEnabledState(_selector.Selected!.GagType);
        CkGui.AttachToolTip("Visual Alterations " + (_selector.Selected!.IsEnabled ? "will" : "will not") + " be applied with this Gag.");

        ImUtf8.SameLineInner();
        var hasGlamour = ItemService.NothingItem(_selector.Selected!.Glamour.Slot).Id != _selector.Selected!.Glamour.GameItem.Id;
        CkGui.FramedIconText(FAI.Vest);
        CkGui.AttachToolTip(hasGlamour
            ? $"A --COL--{_selector.Selected!.Glamour.GameItem.Name}--COL-- is attached to the --COL--{_selector.Selected!.GagType.GagName()}--COL--."
            : $"There is no Glamour Item attached to the {_selector.Selected!.GagType.GagName()}.", color: ImGuiColors.ParsedGold);

        ImUtf8.SameLineInner();
        var hasMod = !(_selector.Selected!.Mod.Label.IsNullOrEmpty());
        CkGui.FramedIconText(FAI.FileDownload);
        CkGui.AttachToolTip(hasMod
            ? "Using Preset for Mod: " + _selector.Selected!.Mod.Label
            : "This Restriction Item has no associated Mod Preset.");
        
        DrawTraitPreview();
        DrawMoodlePreview();
    }
    
    private void DrawTraitPreview()
    {
        if ((_selector.Selected!.Traits & (Traits.Gagged | Traits.Blindfolded)) == 0)
            return;

        // Draw them out.
        var endX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        var currentX = endX;

        if ((_selector.Selected!.Traits & Traits.Gagged) != 0)
        {
            currentX -= ImGui.GetFrameHeight();
            ImGui.Image(_textures.CoreTextures[CoreTexture.Gagged].ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
        }
        else if ((_selector.Selected!.Traits & Traits.Blindfolded) != 0)
        {
            currentX -= (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SameLine(currentX);
            ImGui.Image(_textures.CoreTextures[CoreTexture.Blindfolded].ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
        }
    }
    
    private void DrawMoodlePreview()
    {
        if (_selector.Selected!.Moodle.Id.IsEmptyGuid())
            return;

        _moodleDrawer.DrawMoodles(_selector.Selected!.Moodle, MoodleDrawer.IconSize);
    }

    private void DrawActiveItemInfo(Vector2 region)
    {
        using var child = CkRaii.Child("ActiveItems", region, WFlags.NoScrollbar | WFlags.AlwaysUseWindowPadding);

        if (_manager.ServerGagData is not { } activeGagData)
            return;

        // get the current content height.
        var height = ImGui.GetContentRegionAvail().Y;
        var groupH = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var groupSpacing = (height - 3 * groupH) / 4;

        // Draw the Gag Slots.
        foreach (var (gagData, index) in activeGagData.GagSlots.WithIndex())
        {
            // Spacing.
            if (index > 0) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + groupSpacing);

            // Lock Display.
            if (gagData.GagItem is GagType.None)
                _activeItemDrawer.ApplyItemGroup(groupH, index, gagData);
            else
            {
                if (gagData.IsLocked())
                    _activeItemDrawer.UnlockItemGroup(groupH, index, gagData);
                else
                    _activeItemDrawer.LockItemGroup(groupH, index, gagData);
            }
        }
    }
}
