using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;
public class CursedLootUI : WindowMediatorSubscriberBase
{
    // Revamp this later.
    private static bool THEME_PUSHED = false;

    private readonly CursedLootManager _manager;
    private readonly TutorialService _guides;
   
    public CursedLootUI(ILogger<CursedLootUI> logger, GagspeakMediator mediator,
        CursedLootManager manager, TutorialService guides, 
        LootItemsTab itemsTab, LootPoolTab itemPoolTab, LootAppliedTab appliedTab)
        : base(logger, mediator, "Cursed Loot UI")
    {
        _manager = manager;
        _guides = guides;

        CursedLootTabs = [itemsTab, itemPoolTab, appliedTab];

        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(600, 490), ImGui.GetIO().DisplaySize);
        TitleBarButtons = new TitleBarButtonBuilder()
            .AddTutorial(guides, TutorialType.CursedLoot)
            .Build();
    }

    public static IFancyTab[] CursedLootTabs;

    private string? _lowerBoundStr = null;
    private string? _upperBoundStr = null;
    private int? _chance = null;

    protected override void PreDrawInternal()
    {
        if (!THEME_PUSHED)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .403f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.428f));
            THEME_PUSHED = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (THEME_PUSHED)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
            THEME_PUSHED = false;
        }
    }

    protected override void DrawInternal()
    {
        var frameH = ImGui.GetFrameHeight();
        var regions = CkHeader.FlatWithBends(CkCol.CurvedHeader.Uint(), frameH, ImUtf8.ItemSpacing.X, frameH);
        // draw out the header information, and then the tab contents.

        ImGui.SetCursorScreenPos(regions.TopLeft.Pos);
        using (ImRaii.Child("CursedLootSearch", regions.TopLeft.Size))
            DrawTopLeft(regions.TopLeft.Size);

        ImGui.SetCursorScreenPos(regions.TopRight.Pos);
        using (ImRaii.Child("CursedLootSettings", regions.TopRight.Size))
            DrawTopRight(regions.TopRight.Size);

        // then the tab bar contents.
        ImGui.SetCursorScreenPos(regions.BotLeft.Pos);
        using (ImRaii.Child("CursedLootContent", regions.BotSize, false, WFlags.AlwaysUseWindowPadding))
            DrawTabBarContent();
    }

    private void DrawTopLeft(Vector2 region)
    {
        // To Be Determined.
    }

    private void DrawTopRight(Vector2 region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 9f);
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkCol.CurvedHeaderFade.Uint());
        var frameH = ImUtf8.FrameHeight;
        var spacing = ImUtf8.ItemSpacing.X;
        var innerSpacing = ImUtf8.ItemInnerSpacing.X;
        var changeLength = frameH * 3 + innerSpacing;
        var inputTimeWidth = region.X - changeLength - spacing;

        using (ImRaii.Child("Timers", new Vector2(inputTimeWidth, region.Y)))
        {
            // now that we have the inner area for this, we can determine the midpoint and our input text lengths.
            var inputTextLength = (inputTimeWidth - ImUtf8.FrameHeight) / 2f;

            // Set the bounds and chance if null.
            _lowerBoundStr ??= _manager.LockRangeLower.ToGsRemainingTime();
            _upperBoundStr ??= _manager.LockRangeUpper.ToGsRemainingTime();
            _chance ??= _manager.LockChance;

            ImGui.SetNextItemWidth(inputTextLength);
            ImGui.InputTextWithHint("##MinTime", "Ex: 5m20s", ref _lowerBoundStr, 32);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                // Ensure that it has a valid parsed time. Only update value if it passed.
                if (RegexEx.TryParseTimeSpan(_lowerBoundStr, out var newSpan))
                    _manager.SetLowerLimit(newSpan);
                // Revert to null so we can update it with the latest value in the manager.
                _lowerBoundStr = null;
            }
            CkGui.AttachToolTip("The lower Mimic Lock Time.");

            ImGui.SameLine(0, 0);
            CkGui.FramedIconText(FAI.HourglassHalf);

            ImGui.SameLine(0, 0);
            ImGui.SetNextItemWidth(inputTextLength);
            ImGui.InputTextWithHint("##MaxTime", "Ex: 1h10m", ref _upperBoundStr, 32);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                // Ensure that it has a valid parsed time. Only update value if it passed.
                if (RegexEx.TryParseTimeSpan(_upperBoundStr, out var newSpan))
                    _manager.SetUpperLimit(newSpan);
                // Revert to null so we can update it with the latest value in the manager.
                _upperBoundStr = null;
            }
            CkGui.AttachToolTip("The upper Mimic Lock Time.");
        }

        ImGui.SameLine();
        using (CkRaii.Child("Chance", new Vector2(changeLength, region.Y), CkCol.CurvedHeaderFade.Uint(), 9f * ImGuiHelpers.GlobalScale))
        {
            ImGui.SetNextItemWidth(changeLength - frameH - innerSpacing);
            var chance = _chance ?? _manager.LockChance;
            if (ImGui.DragInt("##ChanceSel", ref chance, 0.1f, 0, 100, "%d%%"))
                _chance = chance;
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _manager.SetLockChance(chance);
                _chance = null;
            }
            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Percentage);
        }
        CkGui.AttachToolTip("How likely you are to encounter cursed loot.");
    }

    private void DrawTabBarContent()
    {
        using var _ = CkRaii.TabBarChild("LootContents", GsCol.VibrantPink.Uint(), GsCol.VibrantPinkHovered.Uint(), CkCol.CurvedHeader.Uint(),
                LabelFlags.PadInnerChild | LabelFlags.SizeIncludesHeader, out var selected, CursedLootTabs);
        // Draw the selected tab's contents.
        selected?.DrawContents(_.InnerRegion.X);
    }
}
