using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Timers;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Wardrobe;
public partial class CursedLootPanel : DisposableMediatorSubscriberBase
{
    private readonly FileDialogManager _fileDialog = new();
    private readonly CursedLootFileSelector _selector;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly CursedLootManager _manager;
    private readonly UiSharedService _ui;
    private readonly TutorialService _guides;

    public CursedLootPanel(
        ILogger<CursedLootPanel> logger,
        GagspeakMediator mediator,
        CursedLootFileSelector selector,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        CursedLootManager manager,
        UiSharedService ui,
        TutorialService guides) : base(logger, mediator)
    {
        _selector = selector;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _manager = manager;
        _ui = ui;
        _guides = guides;
    }

    private TimeSpanTextEditor? LowerBound;
    private TimeSpanTextEditor? UpperBound;
    private int Chance = -1;

    public void DrawPanel(Vector2 remainingRegion, float selectorSize)
    {
        // This panel in particular is special. We want to draw the selector on the left,
        // and we want to draw the active pool on the right.
        //
        // There should in no case ever be a point where the right half is not shown.
        // 
        // To Accomodate for this, we should replace the selector with the editor section.
        // unless we can find a way to make it drop down into the editor like it used to.
        using var group = ImRaii.Group();

        // draw the editor or the selector.
        if (_manager.ActiveEditorItem is not null)
        {
            DrawEditor(new Vector2(selectorSize, remainingRegion.Y));
        }
        else
        {
            _selector.Draw(selectorSize);
        }
        ImGui.SameLine();
        using (ImRaii.Group())
        {
            DrawSelectedItemInfo();
            // Draw the chance configurator.
            DrawCursedLootTimeChance(ImGui.GetContentRegionAvail().X);
            // Draw the active items in the pool.
            DrawActiveItemsInfo(ImGui.GetContentRegionAvail().X);
        }
    }

    private void DrawSelectedItemInfo()
    {
        // Draws additional information about the selected item. Uses the Selector for reference.
        if (_selector.Selected is null)
            return;

        ImGui.Text("Selected Item:" + _selector.Selected.Label);

        if (ImGui.Button("Begin Editing"))
            _manager.StartEditing(_selector.Selected);
    }

    private void DrawActiveItemsInfo(float width)
    {
        using (ImRaii.Group())
        {
            _ui.BigText("Enabled Pool");
            ImGui.Separator();
            // Draw all items in the pool that are active, in order of their application, (Longest timer is top)
            DrawActiveCursedItems();
            ImGui.Separator();
            DrawInactiveItemsInPool();
            _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.RemovingFromEnabledPool, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        }
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.TheEnabledPool, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
    }

    private void DrawActiveCursedItems()
    {
        if (_manager.Storage.ActiveItemsDecending.Count <= 0)
            return;

        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.HealerGreen);
        using var bgCol = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        var size = new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeightWithSpacing());

        using var group = ImRaii.Group();

        foreach (var item in _manager.Storage.ActiveItemsDecending)
            ActiveAppliedItemBox(item, size);
    }

    private void DrawInactiveItemsInPool()
    {
        if (_manager.Storage.ActiveItemsDecending.Count <= 0)
            return;

        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var bgCol = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        var size = new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeightWithSpacing());

        using var group = ImRaii.Group();

        foreach (var item in _manager.Storage.InactiveItemsInPool)
            InactiveItemInPool(item, size);
    }

    /// <summary> Everything in here is confined into a group. </summary>
    private void ActiveAppliedItemBox(CursedItem item, Vector2 size)
    {
        // Find alternative for this, maybe a group?
        using var child = ImRaii.Child($"##EnabledSelectable" + item.Identifier, size, true);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(item.Label);
        ImGui.SameLine();
        UiSharedService.ColorText(item.ReleaseTime.ToGsRemainingTimeFancy(), ImGuiColors.HealerGreen);
    }

    private bool InactiveItemInPool(CursedItem item, Vector2 size)
    {
        using var child = ImRaii.Child($"##InactiveSelectable" + item.Identifier, size, true);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(item.Label);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - _ui.GetIconButtonSize(FontAwesomeIcon.ArrowLeft).X);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGold))
        {
            if (_ui.IconButton(FontAwesomeIcon.ArrowLeft, inPopup: true))
                return true;
            UiSharedService.AttachToolTip("Remove this Item to the Cursed Loot Pool.");
        }
        return false;
    }

    private void DrawCursedLootTimeChance(float width)
    {
        var inputWidth = (width - _ui.GetIconData(FontAwesomeIcon.HourglassHalf).X - ImGui.GetStyle().ItemInnerSpacing.X * 2 - ImGui.CalcTextSize("100.9%  ").X) / 2;

        // Ensure persistent references
        LowerBound ??= new TimeSpanTextEditor(() => _manager.LockRangeLower, _manager.SetLowerLimit);
        UpperBound ??= new TimeSpanTextEditor(() => _manager.LockRangeUpper, _manager.SetUpperLimit);
        var chance = Chance != -1 ? Chance : _manager.LockChance;

        // Draw UI
        LowerBound.DrawInputTimer("##TimerInputLower", inputWidth, "Ex: 0h2m7s");
        UiSharedService.AttachToolTip("Min Cursed Lock Time.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.LowerLockTimer, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        ImUtf8.SameLineInner();
        _ui.IconText(FontAwesomeIcon.HourglassHalf, ImGuiColors.ParsedGold);
        ImUtf8.SameLineInner();

        UpperBound.DrawInputTimer("##TimerInputUpper", inputWidth, "Ex: 0h2m7s");
        UiSharedService.AttachToolTip("Max Cursed Lock Time.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.UpperLockTimer, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.DragInt("##PercentageSlider", ref chance, 0.1f, 0, 100, "%d%%"))
            Chance = chance;
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _manager.SetLockChance(Chance);
            Chance = -1;
        }
        UiSharedService.AttachToolTip("The % Chance that opening Dungeon Loot will contain Cursed Bondage Loot.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.RollChance, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
    }
}
