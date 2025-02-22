using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.UI.Wardrobe;

// it might be wise to move the selector draw into the panel so we have more control over the editor covering both halves.
public partial class RestraintsPanel : DisposableMediatorSubscriberBase
{
    private readonly ILogger<RestraintsPanel> _logger;
    private readonly FileDialogManager _fileDialog = new();
    private readonly RestraintSetFileSelector _selector;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly RestraintManager _manager;
    private readonly PairManager _pairs; // For help displaying the nick or alias names of enablers and assigners.
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;

    public RestraintsPanel(
        ILogger<RestraintsPanel> logger, 
        GagspeakMediator mediator,
        RestraintSetFileSelector selector,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        RestraintManager manager,
        PairManager pairs,
        UiSharedService ui,
        TutorialService guides) : base(logger, mediator)
    {
        _logger = logger;
        _selector = selector;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _manager = manager;
        _pairs = pairs;
        _uiShared = ui;
        _guides = guides;

        Mediator.Subscribe<TooltipSetItemToEditorMessage>(this, (msg) =>
        {
            if (_manager.ActiveEditorItem != null && _manager.ActiveEditorItem.RestraintSlots[msg.Slot] is RestraintSlotBasic basicSlot)
            {
                basicSlot.Glamour.GameItem = msg.Item;
                Logger.LogDebug($"Set [" + msg.Slot + "] to [" + msg.Item.Name + "] on edited set " + "[" + _manager.ActiveEditorItem.Label + "]", LoggerType.Restraints);
            }
        });
    }

    // Handles drawing the Padlock interface for client restrictions. (handle this later)
    // private PadlockRestraintsClient _restraintPadlock;

    public void DrawPanel(Vector2 remainingRegion, float selectorSize)
    {
        using var group = ImRaii.Group();

        // within this group, if we are editing an item, draw the editor.
        if (_manager.ActiveEditorItem is not null)
        {
            DrawEditor(remainingRegion);
            return;
        }
        else
        {
            _selector.Draw(selectorSize);
            ImGui.SameLine();
            using (ImRaii.Group())
            {
                DrawActiveItemInfo();
                DrawSelectedItemInfo();
            }
        }
    }

    private void DrawActiveItemInfo()
    {
        if(_manager.ActiveRestraintData is not { } activeData)
            return;

        if (_manager.EnabledSet is not { } activeSet)
            return;

        // The below is a placeholder UI torn from the old UI.
        using (ImRaii.Group())
        {
            var originalCursorPos = ImGui.GetCursorPos();
            // Move the Y pos down a bit, only for drawing this text
            ImGui.SetCursorPosY(originalCursorPos.Y + 2.5f);
            // Draw the text with the desired color
            UiSharedService.ColorText(activeSet.Label, ImGuiColors.DalamudWhite2);
        }
        if (activeData.IsLocked())
        {
            using (ImRaii.Group())
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2.5f);
                UiSharedService.ColorText("Locked By:", ImGuiColors.DalamudGrey2);
                ImGui.SameLine();
                if (_pairs.TryGetNickAliasOrUid(activeData.PadlockAssigner, out var nick))
                    UiSharedService.ColorText(nick, ImGuiColors.DalamudGrey3);
                else UiSharedService.ColorText(activeData.PadlockAssigner, ImGuiColors.DalamudGrey3);
            }
        }
        // draw the padlock dropdown
        //_restraintPadlock.DrawPadlockComboSection(regionSize.X, string.Empty, "Lock/Unlock this restraint.");

        // beside draw the remaining time.
        if (activeData.Padlock.IsTimerLock())
        {
            UiSharedService.ColorText("Time Remaining:", ImGuiColors.DalamudGrey2);
            ImGui.SameLine();
            UiSharedService.ColorText(activeData.Timer.ToGsRemainingTimeFancy(), ImGuiColors.ParsedPink);
        }
        else
        {
            // Supposedly should not be changing anything.
            if (ImGuiUtil.DrawDisabledButton("Disable Set", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()), string.Empty, activeData.IsLocked()))
                Mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Removed, activeData));
        }

        ImGui.Separator();
        var activePreview = ImGui.GetContentRegionAvail() - ImGui.GetStyle().WindowPadding;
        //_itemPreview.DrawRestraintSetPreviewCentered(activeSet, activePreview);
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

    /// <summary> Get this to be an override for the selector at some point (with revisions). </summary>
    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
/*        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - spacingX);
        string filter = RestraintSetSearchString;
        if (ImGui.InputTextWithHint("##RestraintFilter", "Search for Restraint Set", ref filter, 255))
        {
            RestraintSetSearchString = filter;
            LastHoveredIndex = -1;
        }
        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(RestraintSetSearchString));
        if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
        {
            RestraintSetSearchString = string.Empty;
            LastHoveredIndex = -1;
        }*/
    }
}
