using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;

namespace GagSpeak.Gui.Wardrobe;
public class CursedLootUI : WindowMediatorSubscriberBase
{
    private readonly CursedLootFileSelector _selector;
    private readonly CursedLootManager _manager;
    private readonly TutorialService _guides;
    
    private bool ThemePushed = false;

    public CursedLootUI(ILogger<CursedLootUI> logger, GagspeakMediator mediator,
        CursedLootFileSelector selector, CursedLootManager manager, TutorialService guides, 
        LootItemsTab itemsTab, LootPoolTab itemPoolTab, LootAppliedTab appliedTab) 
        : base(logger, mediator, "Cursed Loot UI")
    {
        _selector = selector;
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

    private InputTextTimeSpan? LowerBound;
    private InputTextTimeSpan? UpperBound;
    private int Chance = -1;

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .403f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.428f));
            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        var frameH = ImGui.GetFrameHeight();
        var regions = CkHeader.FlatWithBends(CkColor.FancyHeader.Uint(), frameH, frameH, frameH);
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
        // should be dependant on the tab selected.
        _selector.DrawFilterRow(region.X);
    }

    private void DrawTopRight(Vector2 region)
    {
        ImGui.Text("Min and Max Time, with Percent Changes");
    }

    private void DrawTabBarContent()
    {
        using var _ = CkRaii.TabBarChild("LootContents", CkColor.VibrantPink.Uint(), CkColor.VibrantPinkHovered.Uint(), CkColor.FancyHeader.Uint(),
                LabelFlags.PadInnerChild | LabelFlags.SizeIncludesHeader, out var selected, CursedLootTabs);
        // Draw the selected tab's contents.
        selected?.DrawContents(_.InnerRegion.X);
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

    // migrate to new header format stuff
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
        // _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.LowerLockTimer, ImGui.GetItemRectMin(), ImGui.GetItemRectSize());

        ImGui.SameLine(0, 1);
        CkGui.IconText(FAI.HourglassHalf, ImGuiColors.ParsedGold);
        ImGui.SameLine(0, 1);

        UpperBound.DrawInputTimer("##TimerInputUpper", inputWidth, "Ex: 0h2m7s");
        CkGui.AttachToolTip("Max Cursed Lock Time.");
        // _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.UpperLockTimer, ImGui.GetItemRectMin(), ImGui.GetItemRectSize());

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
        // _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.RollChance, ImGui.GetItemRectMin(), ImGui.GetItemRectSize());
    }
}
