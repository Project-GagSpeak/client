using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.State.Handlers;
using ImGuiNET;
using ImPlotNET;

namespace GagSpeak.Gui.Components;

public class PlaybackDrawer
{
    private readonly PatternHandler _activePattern;
    public PlaybackDrawer(PatternHandler activePattern)
    {
        _activePattern = activePattern;
    }

    public void DrawPlaybackDisplay()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(0, 0)).Push(ImGuiStyleVar.CellPadding, new Vector2(0, 0));
        using var child = ImRaii.Child("##PatternPlaybackChild", new Vector2(ImGui.GetContentRegionAvail().X, 80), true, WFlags.NoScrollbar);
        if (!child) { return; }
        try
        {
            float[] xs;
            float[] ys;

            // If this is false, it means a pattern is playing back data currently.
            if (!_activePattern.CanPlaybackPattern)
            {
                var start = Math.Max(0, _activePattern.ReadBufferIdx - 150);
                var count = Math.Min(150, _activePattern.ReadBufferIdx - start + 1);
                var buffer = 150 - count; // The number of extra values to display at the end

                xs = Enumerable.Range(-buffer, count + buffer).Select(i => (float)i).ToArray();
                ys = _activePattern.PlaybackData.Skip(_activePattern.PlaybackData.Count - buffer).Take(buffer)
                    .Concat(_activePattern.PlaybackData.Skip(start).Take(count))
                    .Select(pos => (float)pos).ToArray();

                // Transform the x-values so that the latest position appears at x=0
                for (var i = 0; i < xs.Length; i++)
                    xs[i] -= _activePattern.ReadBufferIdx;
            }
            else
            {
                xs = new float[0];
                ys = new float[0];
            }
            var latestX = xs.Length > 0 ? xs[xs.Length - 1] : 0; // The latest x-value
                                                                   // Transform the x-values so that the latest position appears at x=0
            for (var i = 0; i < xs.Length; i++)
                xs[i] -= latestX;

            // get the xpos so we can draw it back a bit to span the whole width
            var xPos = ImGui.GetCursorPosX();
            var yPos = ImGui.GetCursorPosY();
            ImGui.SetCursorPos(new Vector2(xPos - ImGuiHelpers.GlobalScale * 10, yPos - ImGuiHelpers.GlobalScale * 10));
            var width = ImGui.GetContentRegionAvail().X + ImGuiHelpers.GlobalScale * 10;
            // set up the color map for our plots.
            ImPlot.PushStyleColor(ImPlotCol.Line, CkColor.LushPinkLine.Uint());
            ImPlot.PushStyleColor(ImPlotCol.PlotBg, CkColor.RemotePlaybackBg.Uint());
            // draw the waveform
            ImPlot.SetNextAxesLimits(-150, 0, -5, 110, ImPlotCond.Always);
            if (ImPlot.BeginPlot("##Waveform", new Vector2(width, 100), ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMenus | ImPlotFlags.NoLegend | ImPlotFlags.NoFrame))
            {
                ImPlot.SetupAxes("X Label", "Y Label",
                    ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoHighlight,
                    ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks);
                
                if (xs.Length > 0 || ys.Length > 0)
                    ImPlot.PlotLine("Recorded Positions", ref xs[0], ref ys[0], xs.Length);

                ImPlot.EndPlot();
            }
            ImPlot.PopStyleColor(2);
        }
        catch (Exception) { /* Consume */ }
    }
}

