using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Common.Math;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Debouncer;
using GagSpeak.Toybox.Services;
using ImGuiNET;
using ImPlotNET;
using System.Timers;

namespace GagSpeak.UI.Components;

/// <summary>
/// Manages playing back patterns to the client user's connected devices.
/// </summary>
public class PatternPlayback : DisposableMediatorSubscriberBase
{
    private readonly ToyboxRemoteService _remoteService;
    private readonly VibratorService _vibeService;

    public Stopwatch PlaybackDuration;
    private UpdateTimer PlaybackUpdateTimer;

    public PatternPlayback(ILogger<PatternPlayback> logger, GagspeakMediator mediator,
        ToyboxRemoteService remoteService, VibratorService vibeService) : base(logger, mediator)
    {
        _remoteService = remoteService;
        _vibeService = vibeService;

        PlaybackDuration = new Stopwatch();
        PlaybackUpdateTimer = new UpdateTimer(20, ReadVibePosFromBuffer);
    }
    // private accessors for the playback only
    private double[] CurrentPositions = new double[2];
    private List<float> volumeLevels = new List<float>();
    private PatternData? PatternToPlayback = null;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // dispose of the timers
        PlaybackUpdateTimer.Dispose();
        PlaybackDuration.Stop();
        PlaybackDuration.Reset();
    }

    public void DrawPlaybackDisplay()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(0, 0)).Push(ImGuiStyleVar.CellPadding, new Vector2(0, 0));
        using var child = ImRaii.Child("##PatternPlaybackChild", new Vector2(ImGui.GetContentRegionAvail().X, 80), true, ImGuiWindowFlags.NoScrollbar);
        if (!child) { return; }
        try
        {
            // Draw the waveform
            float[] xs;  // x-values
            float[] ys;  // y-values
                         // if we are playing back
            if (PatternToPlayback is not null)
            {
                int start = Math.Max(0, ReadBufferIdx - 150);
                int count = Math.Min(150, ReadBufferIdx - start + 1);
                int buffer = 150 - count; // The number of extra values to display at the end


                xs = Enumerable.Range(-buffer, count + buffer).Select(i => (float)i).ToArray();
                ys = PatternToPlayback.PatternByteData.Skip(PatternToPlayback.PatternByteData.Count - buffer).Take(buffer)
                    .Concat(PatternToPlayback.PatternByteData.Skip(start).Take(count))
                    .Select(pos => (float)pos).ToArray();

                // Transform the x-values so that the latest position appears at x=0
                for (int i = 0; i < xs.Length; i++)
                {
                    xs[i] -= ReadBufferIdx;
                }
            }
            else
            {
                xs = new float[0];
                ys = new float[0];
            }
            float latestX = xs.Length > 0 ? xs[xs.Length - 1] : 0; // The latest x-value
                                                                   // Transform the x-values so that the latest position appears at x=0
            for (int i = 0; i < xs.Length; i++)
            {
                xs[i] -= latestX;
            }

            // get the xpos so we can draw it back a bit to span the whole width
            var xPos = ImGui.GetCursorPosX();
            var yPos = ImGui.GetCursorPosY();
            ImGui.SetCursorPos(new Vector2(xPos - ImGuiHelpers.GlobalScale * 10, yPos - ImGuiHelpers.GlobalScale * 10));
            var width = ImGui.GetContentRegionAvail().X + ImGuiHelpers.GlobalScale * 10;
            // set up the color map for our plots.
            ImPlot.PushStyleColor(ImPlotCol.Line, _remoteService.LushPinkLine);
            ImPlot.PushStyleColor(ImPlotCol.PlotBg, _remoteService.LovenseScrollingBG);
            // draw the waveform
            ImPlot.SetNextAxesLimits(-150, 0, -5, 110, ImPlotCond.Always);
            if (ImPlot.BeginPlot("##Waveform", new System.Numerics.Vector2(width, 100), ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMenus
            | ImPlotFlags.NoLegend | ImPlotFlags.NoFrame))
            {
                ImPlot.SetupAxes("X Label", "Y Label",
                    ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoHighlight,
                    ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks);
                if (xs.Length > 0 || ys.Length > 0)
                {
                    ImPlot.PlotLine("Recorded Positions", ref xs[0], ref ys[0], xs.Length);
                }
                ImPlot.EndPlot();
            }
            ImPlot.PopStyleColor(2);
        }
        catch (Exception e)
        {
            Logger.LogError($"{e} Error drawing the toybox workshop subtab");
        }
    }

    public void StartPlayback(PatternData patternToPlay)
    {
        Logger.LogDebug($"Starting playback of pattern {patternToPlay.Name}", LoggerType.ToyboxPatterns);
        // set the playback index to the start
        ReadBufferIdx = 0;

        // iniitalize volume levels if using simulated vibe
        if (_vibeService.UsingSimulatedVibe)
            InitializeVolumeLevels(patternToPlay.PatternByteData);

        // start our timers
        PlaybackDuration.Start();
        PlaybackUpdateTimer.Start();
        // begin playing the pattern to the vibrators
        _vibeService.StartActiveVibes();
        // Finally, set the patternToPlay, so that we can draw the waveform
        PatternToPlayback = patternToPlay;
    }
    public void InitializeVolumeLevels(List<byte> intensityPattern)
    {
        volumeLevels.Clear();
        foreach (var intensity in intensityPattern)
        {
            // Assuming intensity is a value between 0 and 100
            float volume = intensity / 100f;
            volumeLevels.Add(volume);
        }
    }

    public void StopPlayback()
    {
        if (PatternToPlayback is null)
        {
            Logger.LogWarning("Attempted to stop a pattern not present. Likely here on plugin shutdown or reset.");
            return;
        }

        Logger.LogDebug($"Stopping playback of pattern " + PatternToPlayback.Name, LoggerType.ToyboxPatterns);
        // clear the local variables
        ReadBufferIdx = 0;
        // reset the timers
        PlaybackUpdateTimer.Stop();
        PlaybackDuration.Stop();
        PlaybackDuration.Reset();
        // reset vibe to normal levels
        _vibeService.StopActiveVibes();
        // clear the pattern to play
        PatternToPlayback = null;
    }

    private int ReadBufferIdx;  // The current index of the playback
    private void ReadVibePosFromBuffer(object? sender, ElapsedEventArgs e)
    {
        // return if the playback is no longer active.
        if (PatternToPlayback is null)
            return;

        // If the new read buffer idx >= the total length of the list, stop playback, or loop to start.
        if (ReadBufferIdx >= PatternToPlayback.PatternByteData.Count)
        {
            // If we should loop, start it over again.
            if (PatternToPlayback.ShouldLoop)
            {
                ReadBufferIdx = 0;
                PlaybackDuration.Restart();
                return;
            }
            else // otherwise, stop.
            {
                Mediator.Publish(new PlaybackStateToggled(PatternToPlayback.UniqueIdentifier, NewState.Disabled));
                return;
            }
        }
        // Convert the current stored position to a float and store it in currentPos
        CurrentPositions[1] = PatternToPlayback.PatternByteData[ReadBufferIdx];
        // Send the vibration command to the device
        _vibeService.SendNextIntensity(PatternToPlayback.PatternByteData[ReadBufferIdx]);
        // Increment the buffer index
        ReadBufferIdx++;
    }
}

