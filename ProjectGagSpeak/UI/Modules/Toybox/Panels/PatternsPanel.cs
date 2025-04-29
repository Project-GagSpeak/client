using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Toybox;

public partial class PatternsPanel
{
    private readonly ILogger<PatternsPanel> _logger;
    private readonly PatternFileSelector _selector;
    private readonly PatternManager _manager;
    private readonly TutorialService _guides;

    public PatternsPanel(
        ILogger<PatternsPanel> logger,
        PatternFileSelector selector,
        PatternManager manager,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _guides = guides;
    }

    public void DrawPanel(Vector2 remainingRegion, float selectorSize)
    {
        using var group = ImRaii.Group();

        // within this group, if we are editing an item, draw the editor.
        if (_manager.ItemInEditor is not null)
        {
            DrawEditor(remainingRegion);
            return;
        }
        else
        {
            using (ImRaii.Group())
            {
                _selector.DrawFilterRow(selectorSize);
                ImGui.Spacing();
                _selector.DrawList(selectorSize);
            }
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
            CkGui.GagspeakText(activeItem.Label);
            CkGui.HelpText("Description:--SEP--" + activeItem.Description);

            // playback button
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
            // Draw the delete button
            ImGui.SameLine();
        }
        // next line:
        using (var group2 = ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.Clock);
            ImUtf8.SameLineInner();
            CkGui.ColorText(durationTxt, ImGuiColors.DalamudGrey);
            CkGui.AttachToolTip("Total Length of the Pattern.");

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.Stopwatch20);
            ImUtf8.SameLineInner();
            CkGui.ColorText(startpointTxt, ImGuiColors.DalamudGrey);
            CkGui.AttachToolTip("Start Point of the Pattern.");

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.Sync).X - ImGui.GetStyle().ItemInnerSpacing.X);
            CkGui.IconText(FAI.Sync, activeItem.ShouldLoop ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey2);
            CkGui.AttachToolTip(activeItem.ShouldLoop ? "Pattern is set to loop." : "Pattern does not loop.");
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
