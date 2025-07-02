using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Drawing;
using System.Windows.Forms;

namespace GagSpeak.Gui.Wardrobe;
public partial class CursedLootPanel : DisposableMediatorSubscriberBase
{
    private readonly CursedLootFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItemDrawer;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ItemInEditor != null;
    public CursedLootPanel(
        ILogger<CursedLootPanel> logger,
        GagspeakMediator mediator,
        CursedLootFileSelector selector,
        ActiveItemsDrawer activeItemDrawer,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        CursedLootManager manager,
        FavoritesManager favorites,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator)
    {
        _selector = selector;
        _activeItemDrawer = activeItemDrawer;
        _gags = gags;
        _restrictions = restrictions;
        _manager = manager;
        _cosmetics = cosmetics;
        _guides = guides;

        _gagItemCombo = new RestrictionGagCombo(logger, favorites, () => [
            ..gags.Storage.Values.OrderByDescending(p => favorites._favoriteGags.Contains(p.GagType)).ThenBy(p => p.GagType)
            ]);
        _restrictionItemCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
            ]);
    }

    private RestrictionGagCombo _gagItemCombo;
    private RestrictionCombo _restrictionItemCombo;
    private InputTextTimeSpan? LowerBound;
    private InputTextTimeSpan? UpperBound;
    private int Chance = -1;

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("CursedLootTopLeft", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("CursedLootBotLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("CursedLootTopRight", drawRegions.TopRight.Size))
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
        DrawCursedLootPool(drawRegions.BotRight.Size - verticalShift);
    }

    private void DrawSelectedItemInfo(CkHeader.DrawRegion drawRegion, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var height = CkStyle.GetFrameRowsHeight(3);
        var region = new Vector2(drawRegion.Size.X, height.AddWinPadY());
        var disableNotSelected = _selector.Selected is null;
        var IsEditorItem = !disableNotSelected && _selector.Selected?.Identifier == _manager.ItemInEditor?.Identifier;
        var disableIsActive = _manager.Storage.ActiveItems.Any(gi => gi.Identifier == _selector.Selected?.Identifier);
        var tooltipAct = disableNotSelected ? "No item selected!" : disableIsActive ? "Item is Active!" : string.Empty; // default tooltip.
        var tooltip = disableNotSelected ? "No item selected!" : disableIsActive 
            ? "Cursed Item is Active!" : $"Double Click to {(_manager.ItemInEditor is null ? "Edit" : "Save Changes to")} this Alarm.--SEP-- Right Click to cancel and exit Editor.";

        using var inner = CkRaii.LabelChildAction("SelItem", region, .6f, DrawLabel, ImGui.GetFrameHeight(), BeginEdits, tooltipAct, ImDrawFlags.RoundCornersRight);

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
        {
            if (_selector.Selected!.RestrictionRef is GarblerRestriction gagItem)
                _activeItemDrawer.DrawFramedImage(gagItem.GagType, imgSize.Y, rounding, 0);
            else if (_selector.Selected!.RestrictionRef is BlindfoldRestriction blindfoldRestrictItem)
                _activeItemDrawer.DrawRestrictionImage(blindfoldRestrictItem, imgSize.Y, rounding, false);
            else if (_selector.Selected!.RestrictionRef is RestrictionItem normalRestrictItem)
                _activeItemDrawer.DrawRestrictionImage(normalRestrictItem, imgSize.Y, rounding, false);
        }

        bool DrawLabel()
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().WindowPadding.X);
            var label = _selector.Selected is null
                ? "No Item Selected!" : _selector.Selected.Identifier.Equals(_manager.ItemInEditor?.Identifier)
                    ? $"{_selector.Selected.Label} - (Editing)" : _selector.Selected.Label;
            ImUtf8.TextFrameAligned(label);
            return disableNotSelected || disableIsActive;
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (disableNotSelected || disableIsActive)
                return;

            if (b is ImGuiMouseButton.Right && IsEditorItem)
                _manager.StopEditing();

            if (b is ImGuiMouseButton.Left)
            {
                if (IsEditorItem)
                    _manager.SaveChangesAndStopEditing();
                else if (_manager.ItemInEditor is null)
                    _manager.StartEditing(_selector.Selected!);
            }
        }
    }


    private void DrawCursedLootPool(Vector2 region)
    {
        // Draw out the base window for our padding to be contained within.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12);
        using var c = CkRaii.CustomHeaderChild("##CursedLootPool", region, DrawHeader, ImGui.GetFrameHeight() / 2, HeaderFlags.SizeIncludesHeader);

        // Set the cursor screen pos to the end of the group
        var allItemsInPool = _manager.Storage.AllItemsInPoolByActive;
        using (CkRaii.FramedChildPaddedWH("PoolItems", c.InnerRegion, CkColor.FancyHeaderContrast.Uint()))
        {
            if (allItemsInPool.Count <= 0)
                return;

            foreach (var item in allItemsInPool)
                DrawLootPoolItem(item);
        }

        void DrawHeader()
        {
            using var c = CkRaii.ChildPaddedW("##CursedLootHeader", region.X, ImGui.GetFrameHeight());

            ImUtf8.TextFrameAligned("Pool");
            ImGui.SameLine(0, ImGui.GetFrameHeight());
            DrawCursedLootTimeChance(ImGui.GetContentRegionAvail().X);
        }
    }

    private void DrawLootPoolItem(CursedItem item)
    {
        var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());

        using (CkRaii.FramedChild(item.Identifier.ToString(), itemSize, CkColor.FancyHeaderContrast.Uint()))
        {
            var active = item.AppliedTime != DateTimeOffset.MinValue;
            if(active)
            {
                CkGui.FramedIconText(FAI.Stopwatch);
                CkGui.AttachToolTip("Item is currently applied!");
            }
            else
            {
                if (CkGui.IconButton(FAI.ArrowLeft, inPopup: true))
                    _manager.TogglePoolState(item);
                CkGui.AttachToolTip("Remove this Item from the Cursed Loot Pool.");
            }

            // Draw out the text label.
            ImUtf8.SameLineInner();
            ImUtf8.TextFrameAligned(item.Label);

            if(active)
            {
                // Draw out the release time right aligned.
                ImUtf8.SameLineInner();
                var timerText = item.ReleaseTime.ToGsRemainingTimeFancy();
                var offset = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X - ImGui.CalcTextSize(timerText).X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
                CkGui.ColorText(timerText, ImGuiColors.HealerGreen);
            }
        }
    }

    private void DrawActiveItem(CursedItem item)
    {
        var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        using var group = ImRaii.Group();

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(item.Label);
        ImGui.SameLine();
        CkGui.ColorText(item.ReleaseTime.ToGsRemainingTimeFancy(), ImGuiColors.HealerGreen);
    }

    private void DrawCursedLootTimeChance(float width)
    {
        using var group = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 8f);
        var sliderWidth = ImGui.CalcTextSize("100.9%").X;
        var widthForInputs = width - CkGui.IconSize(FAI.HourglassHalf).X - 2;
        var widthForStrInputs = widthForInputs - sliderWidth;
        var inputWidth = widthForStrInputs / 2;

        // Ensure persistent references
        LowerBound ??= new InputTextTimeSpan(() => _manager.LockRangeLower, _manager.SetLowerLimit);
        UpperBound ??= new InputTextTimeSpan(() => _manager.LockRangeUpper, _manager.SetUpperLimit);
        var chance = Chance != -1 ? Chance : _manager.LockChance;

        // Draw UI
        LowerBound.DrawInputTimer("##TimerInputLower", inputWidth, "Ex: 0h2m7s");
        CkGui.AttachToolTip("Min Cursed Lock Time.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.LowerLockTimer, ImGui.GetItemRectMin(), ImGui.GetItemRectSize());

        ImGui.SameLine(0, 1);
        CkGui.IconText(FAI.HourglassHalf, ImGuiColors.ParsedGold);
        ImGui.SameLine(0, 1);

        UpperBound.DrawInputTimer("##TimerInputUpper", inputWidth, "Ex: 0h2m7s");
        CkGui.AttachToolTip("Max Cursed Lock Time.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.UpperLockTimer, ImGui.GetItemRectMin(), ImGui.GetItemRectSize());

        ImGui.SameLine(0, 1);
        ImGui.SetNextItemWidth(sliderWidth);
        if (ImGui.DragInt("##PercentageSlider", ref chance, 0.1f, 0, 100, "%d%%"))
            Chance = chance;
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _manager.SetLockChance(Chance);
            Chance = -1;
        }
        CkGui.AttachToolTip("% Chance of finding Cursed Bondage Loot.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.RollChance, ImGui.GetItemRectMin(), ImGui.GetItemRectSize());
    }
}
