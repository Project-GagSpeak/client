using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Toybox;

public partial class PatternsPanel
{
    private readonly ILogger<PatternsPanel> _logger;
    private readonly PatternFileSelector _selector;
    private readonly PatternManager _manager;
    private readonly UiSharedService _ui;
    private readonly TutorialService _guides;

    public PatternsPanel(
        ILogger<PatternsPanel> logger,
        PatternFileSelector selector,
        PatternManager manager,
        UiSharedService ui,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _ui = ui;
        _guides = guides;
    }

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
        if (_manager.ActivePattern is not { } activeItem)
            return;

        var durationTxt = activeItem.Duration.Hours > 0 ? activeItem.Duration.ToString("hh\\:mm\\:ss") : activeItem.Duration.ToString("mm\\:ss");
        var startpointTxt = activeItem.StartPoint.Hours > 0 ? activeItem.StartPoint.ToString("hh\\:mm\\:ss") : activeItem.StartPoint.ToString("mm\\:ss");
        
        using (ImRaii.Group())
        {
            // display name, then display the downloads and likes on the other side.
            _ui.GagspeakText(activeItem.Label);
            _ui.DrawHelpText("Description:--SEP--" + activeItem.Description);

            // playback button
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
            // Draw the delete button
            ImGui.SameLine();
        }
        // next line:
        using (var group2 = ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            _ui.IconText(FontAwesomeIcon.Clock);
            ImUtf8.SameLineInner();
            UiSharedService.ColorText(durationTxt, ImGuiColors.DalamudGrey);
            UiSharedService.AttachToolTip("Total Length of the Pattern.");

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            _ui.IconText(FontAwesomeIcon.Stopwatch20);
            ImUtf8.SameLineInner();
            UiSharedService.ColorText(startpointTxt, ImGuiColors.DalamudGrey);
            UiSharedService.AttachToolTip("Start Point of the Pattern.");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - _ui.GetIconData(FontAwesomeIcon.Sync).X - ImGui.GetStyle().ItemInnerSpacing.X);
            _ui.IconText(FontAwesomeIcon.Sync, activeItem.ShouldLoop ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey2);
            UiSharedService.AttachToolTip(activeItem.ShouldLoop ? "Pattern is set to loop." : "Pattern does not loop.");
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
}
