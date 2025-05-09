using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using OtterGui.Text;
using System.Drawing;

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

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("PatternsTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("PatternsBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PatternsTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawPatternInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawPatternInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedPattern(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedPattern(CkHeader.DrawRegion region)
    {
        if (_selector.Selected is null)
            return;

        using (CkRaii.LabelChildText(region.Size, new Vector2(region.SizeX/2, ImGui.GetFrameHeight()), "Testing Label", ImGui.GetFrameHeight(), 2f))
        {
            ImGui.Text("What");
        }
    }

    private void DrawPatternPlayback(Vector2 region)
    {
        using var _ = CkRaii.Group(CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), 0);
        ImGui.Text("Hi Im pattern Playback!");
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
