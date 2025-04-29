using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.FileSystems;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentHousingPlant;

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

        // For drawing the grey "selected Item" line.
        var style = ImGui.GetStyle();
        var selectedH = ImGui.GetFrameHeight() * 2 + MoodleDrawer.IconSize.Y + style.ItemSpacing.Y * 2 + style.WindowPadding.Y * 2;
        var selectedSize = new Vector2(drawRegions.BotRight.SizeX, selectedH);
        var linePos = drawRegions.BotRight.Pos - new Vector2(style.WindowPadding.X, 0);
        var linePosEnd = linePos + new Vector2(style.WindowPadding.X, selectedSize.Y);
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkColor.FancyHeader.Uint());
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkGui.Color(ImGuiColors.DalamudGrey));

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("GagsBottomRight", drawRegions.BotRight.Size))
        {
            DrawSelectedItemInfo(selectedSize, curveSize);
            DrawActiveItemInfo();
        }
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

    private void DrawSelectedItemInfo(Vector2 region, float rounding)
    {
        var ItemSelected = _selector.Selected is not null;

        var styler = ImGui.GetStyle();
        using (ImRaii.Child("SelectedItemOuter", region))
        {
            var imgSize = new Vector2(region.Y) - styler.WindowPadding*2;
            var imgDrawPos = ImGui.GetCursorScreenPos() + new Vector2(region.X - region.Y, 0) + styler.WindowPadding;
            // Draw the left items.
            if (ItemSelected)
                DrawSelectedInner();

            // move to the cursor position and attempt to draw it.
            ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
            ImGui.SetCursorScreenPos(imgDrawPos);
            if(ItemSelected)
                _activeItemDrawer.DrawImage(_selector.Selected!.GagType, imgSize, rounding);
        }
        // draw the actual design element.
        var minPos = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        var wdl = ImGui.GetWindowDrawList();
        wdl.AddRectFilled(minPos, minPos + size, CkColor.FancyHeader.Uint(), rounding, ImDrawFlags.RoundCornersRight);

        wdl.AddRectFilled(minPos, minPos + new Vector2(size.X * .65f + styler.ItemInnerSpacing.Y, ImGui.GetFrameHeight() + styler.ItemInnerSpacing.Y), CkColor.SideButton.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);

        var pinkSize = new Vector2(size.X * .65f, ImGui.GetFrameHeight());
        var hoveringTitle = ImGui.IsMouseHoveringRect(minPos + new Vector2(ImGui.GetFrameHeightWithSpacing()), minPos + pinkSize);
        var col = hoveringTitle ? CkColor.VibrantPinkHovered.Uint() : CkColor.VibrantPink.Uint();
        wdl.AddRectFilled(minPos, minPos + pinkSize, col, rounding, ImDrawFlags.RoundCornersBottomRight);

        if (hoveringTitle)
        {
            if (ItemSelected && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                _manager.StartEditing(_selector.Selected!);
            CkGui.AttachToolTip("Double Click me to begin editing!", displayAnyways: true);
        }
    }

    private void DrawSelectedInner()
    {
        using var group = ImRaii.Group();
        // Draw out an icon check or x based on the state.
        CkGui.BooleanToColoredIcon(_selector.Selected!.IsEnabled, false);
        var hoveringToggle = ImGui.IsItemHovered();
        if (hoveringToggle && ImGui.IsItemClicked())
            _logger.LogInformation("This will eventually toggle visual states!");

        CkGui.AttachToolTip("Gag Visuals are " + (_selector.Selected!.IsEnabled ? "Enabled." : "Disabled."));
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(_selector.Selected!.GagType.GagName());

        using var innerGroup = ImRaii.Group();
        // Next row we need to draw the Glamour Icon, Mod Icon, and hardcore Traits.
        var hasGlamour = ItemService.NothingItem(_selector.Selected!.Glamour.Slot).Id != _selector.Selected!.Glamour.GameItem.Id;
        CkGui.FramedIconText(FAI.Vest);
        CkGui.AttachToolTip(hasGlamour
            ? $"A --COL--{_selector.Selected!.Glamour.GameItem.Name}--COL-- is attached to this --COL--{_selector.Selected!.GagType.GagName()}--COL--."
            : $"There is no Glamour Item attached to this {_selector.Selected!.GagType.GagName()}.", color: ImGuiColors.ParsedGold);

        ImUtf8.SameLineInner();
        var hasMod = !(_selector.Selected!.Mod.Label.IsNullOrEmpty());
        CkGui.FramedIconText(FAI.FileDownload);
        CkGui.AttachToolTip(hasMod
            ? "Using Preset for Mod: " + _selector.Selected!.Mod.Label
            : "This Gag has no associated Mod Preset.");

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

        // Draw them out.
        var moodleIds = _selector.Selected!.Moodle switch
        {
            MoodlePreset preset => preset.StatusIds,
            Moodle set => new[] { set.Id },
            _ => Array.Empty<Guid>()
        };
        _moodleDrawer.DrawMoodles(_selector.Selected!.Moodle, MoodleDrawer.IconSize);
    }

    private void DrawActiveItemInfo()
    {
        if (_manager.ServerGagData is null)
            return;

        using var _ = ImRaii.Child("ActiveGagItems", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysUseWindowPadding);

        var innerWidth = ImGui.GetContentRegionAvail().X;
        _activeItemDrawer.DisplayGagSlots(innerWidth);
    }
}
