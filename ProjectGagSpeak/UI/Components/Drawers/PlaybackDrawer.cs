using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using CkCommons;
using GagSpeak.State.Handlers;
using ImGuiNET;
using ImPlotNET;

namespace GagSpeak.Gui.Components;

public class PlaybackDrawer
{
    private const ImPlotFlags PLAYBACK_PLOT_FLAGS = ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMenus | ImPlotFlags.NoLegend | ImPlotFlags.NoFrame;
    private const ImPlotAxisFlags PLAYBACK_X_AXIS = ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoHighlight;
    private const ImPlotAxisFlags PLAYBACK_Y_AXIS = ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks;
    private static readonly double[] PLOT_STATIC_X = Enumerable.Range(0, 1000).Select(i => (double)(i - 999)).ToArray();

    private readonly PatternHandler _activePattern;
    public PlaybackDrawer(PatternHandler activePattern)
    {
        _activePattern = activePattern;
    }

    public void DrawPlaybackDisplay()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(0, 0))
            .Push(ImGuiStyleVar.CellPadding, new Vector2(0, 0));
        using var col = ImRaii.PushColor(ImPlotCol.PlotBg, CkColor.RemoteBgDark.Uint())
            .Push(ImPlotCol.Line, CkColor.LushPinkLine.Uint());

        using var _ = ImRaii.Child("##PatternPlaybackChild", new Vector2(ImGui.GetContentRegionAvail().X, 80), true, WFlags.NoScrollbar);
        if (!_) 
            return;

        var ys = new double[0];

        // If this is false, it means a pattern is playing back data currently.
        if (!_activePattern.CanPlaybackPattern)
        {
            var start = Math.Max(0, _activePattern.ReadBufferIdx - 150);
            var count = Math.Min(150, _activePattern.ReadBufferIdx - start + 1);
            var buffer = 150 - count; // The number of extra values to display at the end

            ys = _activePattern.PlaybackData.Skip(_activePattern.PlaybackData.Count - buffer).Take(buffer)
                .Concat(_activePattern.PlaybackData.Skip(start).Take(count))
                .Select(pos => (double)pos).ToArray();
        }

        // get the xpos so we can draw it back a bit to span the whole width
        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - ImGuiHelpers.GlobalScale * 10, ImGui.GetCursorPosY() - ImGuiHelpers.GlobalScale * 10));
        var width = ImGui.GetContentRegionAvail().X + ImGuiHelpers.GlobalScale * 10;

        // Setup the plot information.
        ImPlot.SetNextAxesLimits(-150, 0, -5, 110, ImPlotCond.Always);
        // Attempt to draw out the plot.
        if (ImPlot.BeginPlot("##Waveform", new Vector2(width, 100), PLAYBACK_PLOT_FLAGS))
        {
            ImPlot.SetupAxes("X Label", "Y Label", PLAYBACK_X_AXIS, PLAYBACK_Y_AXIS);

            // Draw the line if we should.
            if (ys.Length > 0)
                ImPlot.PlotLine("Recorded Positions", ref PLOT_STATIC_X[0], ref ys[0], ys.Length);

            ImPlot.EndPlot();
        }
    }
}

