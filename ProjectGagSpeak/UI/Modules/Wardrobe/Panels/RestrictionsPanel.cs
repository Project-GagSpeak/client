using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Restrictions;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Wardrobe;
public partial class RestrictionsPanel
{
    private readonly ILogger<RestrictionsPanel> _logger;
    private readonly FileDialogManager _fileDialog = new();
    private readonly RestrictionFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly TraitsDrawer _traitsDrawer;
    private readonly RestrictionManager _manager;
    private readonly PairManager _pairs;
    private readonly CosmeticService _textures;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ActiveEditorItem != null;
    public RestrictionsPanel(
        ILogger<RestrictionsPanel> logger,
        RestrictionFileSelector selector,
        ActiveItemsDrawer activeItemDrawer,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        TraitsDrawer traitsDrawer,
        RestrictionManager manager,
        PairManager pairs,
        CosmeticService textures,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _traitsDrawer = traitsDrawer;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _activeItemDrawer = activeItemDrawer;
        _manager = manager;
        _pairs = pairs;
        _textures = textures;
        _guides = guides;
    }

    public void DrawContents(DrawerHelpers.CkHeaderDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.Topleft.Pos);
        using (ImRaii.Child("RestrictionsTopLeft", drawRegions.Topleft.Size))
            _selector.DrawFilterRow(drawRegions.Topleft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestrictionsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestrictionsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // For drawing the grey "selected Item" line.
        var styler = ImGui.GetStyle();
        var selectedH = ImGui.GetFrameHeight() * 3 + styler.ItemSpacing.Y * 2 + styler.WindowPadding.Y * 2;
        var selectedSize = new Vector2(drawRegions.BotRight.SizeX, selectedH);
        var linePos = drawRegions.BotRight.Pos - new Vector2(styler.WindowPadding.X, 0);
        var linePosEnd = linePos + new Vector2(styler.WindowPadding.X, selectedSize.Y);
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkColor.FancyHeader.Uint());
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkGui.Color(ImGuiColors.DalamudGrey));

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("RestrictionsBR", drawRegions.BotRight.Size))
        {
            DrawSelectedItemInfo(selectedSize, curveSize);
            DrawActiveItemInfo();
        }
    }

    public void DrawEditorContents(DrawerHelpers.CkHeaderDrawRegions drawRegions, float curveSize)
    {
        ImGui.SetCursorScreenPos(drawRegions.Topleft.Pos);
        using (ImRaii.Child("RestrictionsTopLeft", drawRegions.Topleft.Size))
            DrawEditorHeaderLeft(drawRegions.Topleft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestrictionsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            DrawEditorLeft(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestrictionsTopRight", drawRegions.TopRight.Size))
            DrawEditorHeaderRight(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("RestrictionsBottomRight", drawRegions.BotRight.Size))
            DrawEditorRight(drawRegions.BotRight.SizeX);
    }


    private void DrawSelectedItemInfo(Vector2 region, float rounding)
    {
        var ItemSelected = _selector.Selected is not null;
        var styler = ImGui.GetStyle();
        var wdl = ImGui.GetWindowDrawList();

        using (ImRaii.Child("SelectedItemOuter", new Vector2(ImGui.GetContentRegionAvail().X, region.Y)))
        {
            var imgSize = new Vector2(region.Y) - styler.WindowPadding * 2;
            var imgDrawPos = ImGui.GetCursorScreenPos() + new Vector2(region.X - region.Y, 0) + styler.WindowPadding;
            // Draw the left items.
            if (ItemSelected)
                DrawSelectedInner(imgSize.X);

            // move to the cursor position and attempt to draw it.
            ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
            ImGui.SetCursorScreenPos(imgDrawPos);
            if (ItemSelected)
                _activeItemDrawer.DrawImage(_selector.Selected!, imgSize, rounding);
        }
        // draw the actual design element.
        var minPos = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        wdl.AddRectFilled(minPos, minPos + size, CkColor.FancyHeader.Uint(), rounding, ImDrawFlags.RoundCornersRight);

        // Draw a secondary rect just like the first but going slightly bigger.
        var secondaryRect = new Vector2(size.X * .65f + styler.ItemInnerSpacing.Y, ImGui.GetFrameHeight() + styler.ItemInnerSpacing.Y);
        wdl.AddRectFilled(minPos, minPos + secondaryRect, CkColor.SideButton.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);

        // Add a rect that spans the top row up to about .67 of the height.
        var pinkSize = new Vector2(size.X * .65f, ImGui.GetFrameHeight());
        var hoveringTitle = ImGui.IsMouseHoveringRect(minPos, minPos + pinkSize);
        var col = hoveringTitle ? CkColor.VibrantPinkHovered.Uint() : CkColor.VibrantPink.Uint();
        wdl.AddRectFilled(minPos, minPos + pinkSize, col, rounding, ImDrawFlags.RoundCornersBottomRight);

        // Draw the type of restriction item as an image path here.
        if(ItemSelected)
        {
            (var image, var tooltip) = _selector.Selected!.Type switch
            {
                RestrictionType.Gag => (_textures.CoreTextures[CoreTexture.Gagged], "This is a Gag Restriction!"),
                RestrictionType.Collar => (_textures.CoreTextures[CoreTexture.Collar], "This is a Collar Restriction!"),
                RestrictionType.Blindfold => (_textures.CoreTextures[CoreTexture.Blindfolded], "This is a Blindfold Restriction!"),
                _ => (_textures.CoreTextures[CoreTexture.Restrained], "This is a generic Restriction.")
            };
            wdl.AddDalamudImage(image, minPos + new Vector2(size.X * .6f - ImGui.GetFrameHeight(), 0), new Vector2(ImGui.GetFrameHeight()), tooltip);
        }


        if (hoveringTitle)
        {
            if (ItemSelected && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                _manager.StartEditing(_selector.Selected!);
            CkGui.AttachToolTip("Double Click me to begin editing!", displayAnyways: true);
        }
    }

    private void DrawSelectedInner(float rightOffset)
    {
        using var group = ImRaii.Group();
        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(_selector.Selected!.Label);

        using var innerGroup = ImRaii.Group();
        // Next row we need to draw the Glamour Icon, Mod Icon, and hardcore Traits.
        var hasGlamour = ItemService.NothingItem(_selector.Selected!.Glamour.Slot).Id != _selector.Selected!.Glamour.GameItem.Id;
        CkGui.FramedIconText(FAI.Vest);
        CkGui.AttachToolTip(hasGlamour
            ? $"A --COL--{_selector.Selected!.Glamour.GameItem.Name}--COL-- is attached to the --COL--{_selector.Selected!.Label}--COL--."
            : $"There is no Glamour Item attached to the {_selector.Selected!.Label}.", color: ImGuiColors.ParsedGold);

        ImUtf8.SameLineInner();
        var hasMod = !(_selector.Selected!.Mod.Label.IsNullOrEmpty());
        CkGui.FramedIconText(FAI.FileDownload);
        CkGui.AttachToolTip(hasMod
            ? "Using Preset for Mod: " + _selector.Selected!.Mod.Label
            : "This Restriction Item has no associated Mod Preset.");

        // go right aligned for the trait previews.
        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - rightOffset - ImGui.GetStyle().ItemInnerSpacing.X);
        _traitsDrawer.DrawTraitPreview(_selector.Selected!.Traits, _selector.Selected!.Stimulation);
        // next row, draw the moodle preview along the lower row, with the height of the frame.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemInnerSpacing.X);
        DrawMoodlePreview();
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
        _moodleDrawer.DrawMoodles(_selector.Selected!.Moodle, MoodlesDisplayer.DefaultSize);
    }

    private void DrawActiveItemInfo()
    {
        if (_manager.ActiveRestrictionsData is null)
            return;

        using var _ = ImRaii.Child("ActiveRestrictionItems", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysUseWindowPadding);

        var innerWidth = ImGui.GetContentRegionAvail().X;
        _activeItemDrawer.DisplayRestrictionSlots(innerWidth);
    }
}
