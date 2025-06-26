using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

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
    private TimeSpanTextEditor? LowerBound;
    private TimeSpanTextEditor? UpperBound;
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

        // For drawing the grey "selected Item" line.
        var styler = ImGui.GetStyle();
        var selectedH = ImGui.GetFrameHeight() * 3 + styler.ItemSpacing.Y * 2 + styler.WindowPadding.Y * 2;
        var selectedSize = new Vector2(drawRegions.BotRight.SizeX, selectedH);
        var linePos = drawRegions.BotRight.Pos - new Vector2(styler.WindowPadding.X, 0);
        var linePosEnd = linePos + new Vector2(styler.WindowPadding.X, selectedSize.Y);
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkColor.FancyHeader.Uint());
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkGui.Color(ImGuiColors.DalamudGrey));

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("CursedLootBottomRight", drawRegions.BotRight.Size))
        {
            DrawSelectedItemInfo(selectedSize, curveSize);
            DrawCursedLootPool();
        }
    }

    private void DrawCursedLootPool()
    {
        // Draw out the base window for our padding to be contained within.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12);
        using var _ = ImRaii.Child("CursedLootPoolFrame", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysUseWindowPadding);
        DrawItemPoolInternal();
    }

    public void DrawItemPoolInternal()
    {
        var wdl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var height = ImGui.GetFrameHeight() + style.FramePadding.Y * 3;
        var padding = style.WindowPadding;
        var region = new Vector2(ImGui.GetContentRegionAvail().X, height);

        // Draw the header frame and line
        wdl.AddRectFilled(pos, pos + new Vector2(region.X, height), CkColor.ElementHeader.Uint(), CkStyle.HeaderRounding(), ImDrawFlags.RoundCornersTop);
        wdl.AddLine(pos + new Vector2(0, height - 2), pos + new Vector2(region.X, height - 2), CkColor.ElementSplit.Uint(), 2);

        // Position content within the header
        ImGui.SetCursorScreenPos(pos + new Vector2(padding.X * 2, style.FramePadding.Y));
        using (ImRaii.Group())
        {
            ImUtf8.TextFrameAligned("Pool");
            ImGui.SameLine(0, ImGui.GetFrameHeight());
            DrawCursedLootTimeChance(ImGui.GetContentRegionAvail().X - padding.X); // Adjusted available width calculation
        }

        // Set the cursor screen pos to the end of the group
        ImGui.SetCursorScreenPos(pos + new Vector2(0, height));
        using (CkRaii.ChildPadded("LootpoolFrame", ImGui.GetContentRegionAvail().WithoutWinPadding(), CkColor.ElementBG.Uint(), dFlags: ImDrawFlags.RoundCornersBottom))
        {
            var allItemsInPool = _manager.Storage.AllItemsInPoolByActive;
            using (CkRaii.FramedChildPaddedWH("PoolItems", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint()))
            {
                if (allItemsInPool.Count <= 0)
                    return;

                foreach (var item in allItemsInPool)
                    DrawLootPoolItem(item, wdl);
            }
        }
    }

    private void DrawLootPoolItem(CursedItem item, ImDrawListPtr wdl)
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

    private void DrawActiveItem(CursedItem item, ImDrawListPtr wdl)
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
        LowerBound ??= new TimeSpanTextEditor(() => _manager.LockRangeLower, _manager.SetLowerLimit);
        UpperBound ??= new TimeSpanTextEditor(() => _manager.LockRangeUpper, _manager.SetUpperLimit);
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
