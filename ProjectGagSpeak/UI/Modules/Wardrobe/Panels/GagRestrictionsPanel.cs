using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagspeakAPI.Util;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;
public partial class GagRestrictionsPanel
{
    private readonly ILogger<GagRestrictionsPanel> _logger;
    private readonly GagRestrictionFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly AttributeDrawer _attributeDrawer;
    private readonly GagRestrictionManager _manager;
    private readonly KinksterManager _pairs;
    private readonly CosmeticService _textures;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ItemInEditor != null;
    public GagRestrictionsPanel(
        ILogger<GagRestrictionsPanel> logger,
        GagspeakMediator mediator,
        GagRestrictionFileSelector selector,
        ActiveItemsDrawer activeItemDrawer,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        AttributeDrawer attributeDrawer,
        GagRestrictionManager manager,
        KinksterManager pairs,
        CosmeticService textures,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _activeItemDrawer = activeItemDrawer;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _attributeDrawer = attributeDrawer;
        _manager = manager;
        _pairs = pairs;
        _textures = textures;
        _guides = guides;
        _profileCombo = new CustomizeProfileCombo(logger, mediator);
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
        var height = ImGui.GetFrameHeightWithSpacing() + MoodleDrawer.IconSize.Y;
        var region = new Vector2(drawRegion.Size.X, height);
        var notSelected = _selector.Selected is null;
        var isActive = _manager.ActiveItems.Values.Any(gi => gi.GagType == _selector.Selected?.GagType);
        var tooltipAct = notSelected ? "No item selected!" : isActive ? "Item is Active!" : "Double Click to begin editing!";

        using var c = CkRaii.ChildLabelCustomButton("SelItem", region, ImGui.GetFrameHeight(), LabelButton, BeginEdits, tooltipAct, ImDrawFlags.RoundCornersRight, LabelFlags.AddPaddingToHeight);

        var pos = ImGui.GetItemRectMin();
        var imgSize = new Vector2(c.InnerRegion.Y);
        var imgDrawPos = pos with { X = pos.X + c.InnerRegion.X - imgSize.X };
        // Draw the left items.
        if (_selector.Selected is not null)
            DrawSelectedInner(imgSize.X, isActive);

        // Draw the right image item.
        ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
        ImGui.SetCursorScreenPos(imgDrawPos);
        if (_selector.Selected is not null)
            _activeItemDrawer.DrawFramedImage(_selector.Selected!.GagType, imgSize.Y, rounding, 0);

        void LabelButton()
        {
            using var c = CkRaii.Child("##SelItemLabel", new Vector2(region.X * .6f, ImGui.GetFrameHeight()));
            var imgSize = new Vector2(c.InnerRegion.Y);
            var imgPos = ImGui.GetItemRectMax() - imgSize;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().WindowPadding.X);
            ImUtf8.TextFrameAligned(_selector.Selected?.GagType.GagName() ?? "No Item Selected!");
            if (_selector.Selected is not null)
            {
                (var image, var tooltip) = (CosmeticService.CoreTextures.Cache[CoreTexture.Gagged], "This is a Gag Restriction!");
                ImGui.GetWindowDrawList().AddDalamudImage(image, imgPos, imgSize, tooltip);
            }
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is ImGuiMouseButton.Left && !notSelected && !isActive)
                _manager.StartEditing(_selector.Selected!);
        }
    }

    private void DrawSelectedInner(float rightOffset, bool isActive)
    {
        using var innerGroup = ImRaii.Group();

        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
        {
            CkGui.BooleanToColoredIcon(_selector.Selected!.IsEnabled, false);
            CkGui.TextFrameAlignedInline($"Visuals  ");
        }
        if (!isActive && ImGui.IsItemHovered() && ImGui.IsItemClicked())
            _manager.ToggleVisibility(_selector.Selected!.GagType);
        CkGui.AttachToolTip($"Visuals {(_selector.Selected!.IsEnabled ? "will" : "will not")} be applied.");

        if (ItemSvc.NothingItem(_selector.Selected!.Glamour.Slot).Id != _selector.Selected!.Glamour.GameItem.Id)
        {
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Vest);
            CkGui.AttachToolTip($"A --COL--{_selector.Selected!.Glamour.GameItem.Name}--COL-- is attached to the " +
                $"--COL--{_selector.Selected!.GagType.GagName()}--COL--.", color: ImGuiColors.ParsedGold);
        }
        if (_selector.Selected!.Mod.HasData)
        {
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.FileDownload);
            CkGui.AttachToolTip($"Mod Preset ({_selector.Selected.Mod.Label}) is applied." +
                $"--SEP--Source Mod: {_selector.Selected!.Mod.Container.ModName}");
        }
        if (_selector.Selected!.Traits > 0)
        {
            ImUtf8.SameLineInner();
            _attributeDrawer.DrawTraitPreview(_selector.Selected!.Traits);
        }

        _moodleDrawer.ShowStatusIcons(_selector.Selected!.Moodle, ImGui.GetContentRegionAvail().X);
    }

    private void DrawActiveItemInfo(Vector2 region)
    {
        using var child = CkRaii.Child("ActiveItems", region, wFlags: WFlags.NoScrollbar | WFlags.AlwaysUseWindowPadding);

        if (_manager.ServerGagData is not { } activeGagData)
            return;

        var height = ImGui.GetContentRegionAvail().Y;
        var groupH = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
        var groupSpacing = (height - 3 * groupH) / 4;

        // Draw the Gag Slots.
        foreach (var (gagData, index) in activeGagData.GagSlots.WithIndex())
        {
            // Spacing.
            if (index > 0) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + groupSpacing);

            // Slot Display.
            if (gagData.GagItem is GagType.None)
                _activeItemDrawer.ApplyItemGroup(index, gagData);
            else
            {
                if (gagData.IsLocked())
                    _activeItemDrawer.UnlockItemGroup(index, gagData);
                else
                    _activeItemDrawer.LockItemGroup(index, gagData);
            }
        }
    }
}
