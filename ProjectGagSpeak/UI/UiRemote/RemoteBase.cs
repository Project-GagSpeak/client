using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Common.Lua;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox.Debouncer;
using GagSpeak.Toybox.Services;
using ImGuiNET;
using ImPlotNET;
using Lumina.Excel.Sheets;
using OtterGui;
using System.Numerics;
using System.Timers;

namespace GagSpeak.UI.UiRemote;

/// <summary>
/// RemoteBase is created with unique distinct variables for each instance that it is made with.
/// Any attributes or methods that should share common functionality should be managed via the 
/// toybox remote service.
/// </summary>
public abstract class RemoteBase : WindowMediatorSubscriberBase
{
    // the class includes are shared however (i think), so dont worry about that.
    private readonly UiSharedService _uiShared;
    private readonly VibratorService _vibeService;
    private readonly ToyboxRemoteService _remoteService;
    protected readonly TutorialService _guides;

    public RemoteBase(ILogger logger, GagspeakMediator mediator, 
        UiSharedService uiShared, VibratorService vibeService, 
        ToyboxRemoteService remoteService, TutorialService guides, 
        string windowName): base(logger, mediator, windowName + " Remote")
    {
        _uiShared = uiShared;
        _vibeService = vibeService;
        _remoteService = remoteService;
        _guides = guides;

        AllowPinning = false;
        AllowClickthrough = false;
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(WindowWidthMin, 430),
            MaximumSize = new Vector2(WindowWidthMin, 430)
        };

        RespectCloseHotkey = false;

        if(windowName == "Personal")
        {
            TitleBarButtons = new()
            {
                new TitleBarButton()
                {
                    Icon = FontAwesomeIcon.QuestionCircle,
                    Click = (msg) =>
                    {
                        if (_guides.IsTutorialActive(TutorialType.Remote))
                        {
                            _guides.SkipTutorial(TutorialType.Remote);
                            _logger.LogInformation("Skipping Remote Tutorial");
                        }
                        else
                        {
                            _guides.StartTutorial(TutorialType.Remote);
                            _logger.LogInformation("Starting Remote Tutorial");
                        }
                    },
                    IconOffset = new(2, 1),
                    ShowTooltip = () =>
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Start/Stop Remote Tutorial");
                        ImGui.EndTooltip();
                    }
                }
            };
        }

        // label the window with the identifier name
        WindowBaseName = $"GagSpeak Remote UI " + windowName;

        // create a new timer stopwatch, unique to this base class instance.
        DurationStopwatch = new Stopwatch();

        // A timer for recording graph display updates. Nessisary for smooth lines, and Unique to this base class instance
        DisplayUpdateTimer = new UpdateTimer(10, AddCirclePositionToBuffer);

        // A timer for recording intensity data. Nessisary for all types, but must remain distinct for each. (base class)
        IntensityUpdateTimer = new UpdateTimer(20, RecordData);
    }

    protected string WindowBaseName;
    protected static float WindowWidthMin = 300f;
    protected static float WindowWidthMax = 550f;
    public bool IsExpanded { get; private set; } = false;

    protected Vector2 CurrentPos = Vector2.Zero;
    protected Vector2 CurrentSize = Vector2.Zero;

    // Stores the duration of the recording. Must be distinct for all (base class)
    protected Stopwatch DurationStopwatch;

    // Manages the recording of the circle's Y position. Must be distinct for all (base class)
    protected UpdateTimer DisplayUpdateTimer;

    // Manages the interval at which we append handle new vibration intensity data. Must be distinct for all (base class)
    protected UpdateTimer IntensityUpdateTimer;

    // the Recorded Y positions from the circle. Must be distinct for all to ensure draws dont share (base class)
    protected List<double> RecordedPositions = new List<double>();

    // the Recorded Y positions during the latest loop segment. Must be distinct for all to ensure draws dont share (base class)
    protected List<double> StoredLoopDataBlock = new List<double>();

    // the current circle position, stores the X and Y double. Unique to each instance (Base Class)
    protected double[] CirclePosition = new double[2];

    // Must be unique for each base class so that we ensure themes are not overlapped
    private bool ThemePushed = false;

    // the buffer index telling us where in the bufferloop we are reading data from we are at. (base class)
    public int BufferLoopIndex { get; protected set; } = 0;

    // if the remote is powered on (base class)
    public bool RemoteOnline { get; protected set; } = false;

    // if we are dragging the pink circle. (base class)
    public bool IsDragging { get; protected set; } = false;

    // if we have the loop button toggled. (base class)
    public bool IsLooping { get; protected set; }

    // if we have the float button toggled. (base class)
    public bool IsFloating { get; protected set; }

    // Graph data. Does not need to be distinct, but is shared across all types. (base class)
    public const float XAxisLimit = 40;
    public const float YAxisLimitLower = 0;
    public const float YAxisLimitUpper = 100;
    public double[] Positions = { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }; 
    public string[] Labels = { "0%", "", "", "", "", "", "", "", "", "", "100%" };

    // Toggles the size constraints.
    protected void UpdateSizeConstraints(bool shouldExpand)
    {
        // set the should expand
        IsExpanded = shouldExpand;
        // set the new width
        float width = IsExpanded ? WindowWidthMax : WindowWidthMin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(width, 430),
            MaximumSize = new Vector2(width, 430)
        };
        // set the new size
        ImGui.SetWindowSize(new Vector2(width, 430));
    }

    // the disposal for the base class
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // dispose of the timer recorders & stop the stopwatch
        DurationStopwatch.Stop();

        DisplayUpdateTimer.Dispose();
        IntensityUpdateTimer.Dispose();
    }

    protected override void PreDrawInternal()
    {
        // include our personalized theme for this window here if we have themes enabled.
        // no config option yet, so it will always be active. When one is added, append "&& !_configOption.useTheme" to the if statement.
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 0));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }
    protected override void PostDrawInternal()
    {
        // include our personalized theme for this window here if we have themes enabled.
        if (ThemePushed)
        {
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
            ThemePushed = false;
        }
    }
    protected override void DrawInternal()
    {
        //_logger.LogInformation(ImGui.GetWindowSize().ToString());
        var isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        CurrentPos = ImGui.GetWindowPos();
        CurrentSize = ImGui.GetWindowSize();
        using (var child = ImRaii.Child($"##RemoteUIChild{WindowBaseName}", new Vector2(ImGui.GetContentRegionAvail().X, -1), true, ImGuiWindowFlags.NoDecoration))
        {

            if (!child) { return; }

            // get the xpos so we can draw it back a bit to span the whole width of the window.
            float xPos = ImGui.GetCursorPosX();
            float yPos = ImGui.GetCursorPosY();

            // Create a table with one or two columns based on the Expanded property
            int columnCount = IsExpanded ? 2 : 1;
            using (var table = ImRaii.Table("##RemoteUITable", columnCount, ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersV))
            {
                ImGui.TableSetupColumn("##RemoteUIContent", ImGuiTableColumnFlags.WidthFixed, WindowWidthMin);
                if (IsExpanded)
                {
                    ImGui.TableSetupColumn("##RemoteUIExtraContent", ImGuiTableColumnFlags.WidthStretch);
                }

                ImGui.TableNextColumn();

                using (var childBg = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.1f, 0.930f)))
                {
                    // draw the playback display, this particular playback display should have its width increased by 20, and moved up 10f and left 10f
                    ImGui.SetCursorPos(new Vector2(xPos - ImGuiHelpers.GlobalScale * 10f, yPos - ImGuiHelpers.GlobalScale * 10f));
                    float width = WindowWidthMin + ImGuiHelpers.GlobalScale * 20f;
                    DrawRecordedDisplay(ref xPos, ref yPos, ref width);

                    // draw the center bar for recording information and things
                    DrawCenterBar(ref xPos, ref yPos, ref width);

                    // grab the remaining height left
                    yPos = ImGui.GetCursorPosY();
                    ImGui.SetCursorPosY(yPos - ImGuiHelpers.GlobalScale * 5f);
                    // draw the core of the vibe remote
                    DrawVibrationRecorder(ref xPos, ref yPos, ref width);
                }

                // Draw any extra tab details if any, Draws nothing by default.
                if (IsExpanded)
                {
                    ImGui.TableNextColumn();
                    DrawExtraDetails();
                }

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && isFocused)
                {
                    ProcessLoopToggle();
                }
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle) && isFocused)
                {
                    ProcessFloatToggle();
                }

                // move to the next column if we should.

            }
        }
    }

    public void ProcessLoopToggle() => IsLooping = !IsLooping;
    public void ProcessFloatToggle() => IsFloating = !IsFloating;

    public void DrawRecordedDisplay(ref float xPos, ref float yPos, ref float width)
    {
        try
        {
            // Setup the waveform bounding box.
            float[] xs = Enumerable.Range(0, RecordedPositions.Count).Select(i => (float)i).ToArray();  // x-values
            float[] ys = RecordedPositions.Select(pos => (float)pos).ToArray();  // y-values
            float latestX = xs.Length > 0 ? xs[xs.Length - 1] : 0; // The latest x-value

            // Transform the x-values so that the latest position appears at x=0 (ensure it doesn't start smack dab in the middle)
            for (int i = 0; i < xs.Length; i++)
                xs[i] -= latestX;

            // set up the color map for our plots.
            ImPlot.PushStyleColor(ImPlotCol.Line, _remoteService.LushPinkLine);
            ImPlot.PushStyleColor(ImPlotCol.PlotBg, _remoteService.LovenseScrollingBG);

            // setup and draw the waveform graph axis
            ImPlot.SetNextAxesLimits(-150, 0, -5, 110, ImPlotCond.Always);

            if (ImPlot.BeginPlot("##Waveform", new Vector2(width, 125), ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMenus | ImPlotFlags.NoLegend | ImPlotFlags.NoFrame))
            {
                //_logger.LogInformation(ImPlot.GetPlotSize().ToString());
                ImPlot.SetupAxes("X Label", "Y Label",
                    ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoHighlight,
                    ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoHighlight);
                // Draw the recorded positions line.
                if (xs.Length > 0 || ys.Length > 0)
                {
                    ImPlot.PlotLine("Recorded Positions", ref xs[0], ref ys[0], xs.Length);
                }
                ImPlot.EndPlot();
            }
            _guides.OpenTutorial(TutorialType.Remote, StepsRemote.OutputDisplay, CurrentPos, CurrentSize);


            // clear the styles
            ImPlot.PopStyleColor(2);

            // shift up again
            yPos = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(yPos - ImGuiHelpers.GlobalScale * 13);

            // add a seperator to join us up with the middle section.
            ImGui.Separator();
            yPos = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(yPos - ImGuiHelpers.GlobalScale);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error drawing the waveform graph.");
        }
    }

    /// <summary>
    /// This abstract method for the center bar will change how it is drawn based on the remote type.
    /// <para> Personal will display personal devices, their motors and additional options. </para>
    /// <para> Patterns will display the device the pattern is being recorded for, which motor and type we are recording for.</para>
    /// <para> Pattern will display </para>
    /// </summary>
    public abstract void DrawCenterBar(ref float xPos, ref float yPos, ref float width);

    public void DrawVibrationRecorder(ref float xPos, ref float yPos, ref float width)
    {

        // grab the content region
        var region = ImGui.GetContentRegionAvail();
        using (var table2 = ImRaii.Table("ThePatternCreationTable", 2, ImGuiTableFlags.NoPadInnerX
            | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInnerV, region))
        {
            if (!table2) { return; } // make sure our table was made
            ImGui.TableSetupColumn("InteractivePatternDrawer", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("InteractionButtons", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60);
            ImGui.TableNextColumn();

            // create styles for the next plot
            ImPlot.PushStyleColor(ImPlotCol.PlotBg, _remoteService.LovenseDragButtonBG);
            DrawCircleButtonGraph(ref width, ref yPos);

            // end of using disabled & first column
            ImGui.TableNextColumn();
            // create another table inside here
            DrawSideButtonsTable(region);
        }
        // end of table here.
    }

    /// <summary>
    /// A virtual void method to allow for optional additional draw details.
    /// By Default, this draws nothing.
    /// </summary>
    public virtual void DrawExtraDetails() { }

    private void DrawCircleButtonGraph(ref float width, ref float yPos)
    {
        using var color = ImRaii.PushColor(ImPlotCol.PlotBg, _remoteService.LovenseDragButtonBG);
        // Draw a thin line with a timer to show the current position of the circle
        width = ImGui.GetContentRegionAvail().X;
        var height = ImGui.GetContentRegionAvail().Y + ImGui.GetTextLineHeight();

        // go to the next line and draw the grid we can move out thing in
        yPos = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(yPos - ImGui.GetTextLineHeight());
        ImPlot.SetNextAxesLimits(-50, +50, -10, 110, ImPlotCond.Always);
        var PreviousPos = CirclePosition[1]; // store the Y position

        // now draw the actual damn thing
        if (ImPlot.BeginPlot("##Box", new Vector2(width + ImGui.GetTextLineHeight(), height), ImPlotFlags.NoBoxSelect | ImPlotFlags.NoLegend | ImPlotFlags.NoMenus | ImPlotFlags.NoFrame))
        {
            // setup Axis's
            ImPlot.SetupAxes("X Label", "Y Label",
                ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoMenus | ImPlotAxisFlags.NoHighlight,
                ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoMenus | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoHighlight);
            ImPlot.SetupAxisTicks(ImAxis.Y1, ref Positions[0], 11, Labels);

            // setup the drag point circle
            using (ImRaii.Disabled(!RemoteOnline))
            {
                ImPlot.DragPoint(0, ref CirclePosition[0], ref CirclePosition[1], _remoteService.LushPinkButton, 20, ImPlotDragToolFlags.NoCursors);
            }
            _guides.OpenTutorial(TutorialType.Remote, StepsRemote.ControllableCircle, CurrentPos, CurrentSize);
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.DraggableCircle, CurrentPos, CurrentSize);

            // if the mouse button is released, while we are looping and dragging turn dragging off
            if (IsDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                IsDragging = false;
                BufferLoopIndex = 0; // reset the index
                _logger.LogTrace("Dragging Period Ended!");
            }

            // if our mouse is down...
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                // if we are not yet marked as dragging, and the positions are different, then mark that we are dragging
                if (!IsDragging && PreviousPos != CirclePosition[1])
                {
                    IsDragging = true;
                    StoredLoopDataBlock = new List<double>(); // start a new list
                    BufferLoopIndex = 0; // reset the index
                    _logger.LogTrace("Dragging Period Started!");
                }
            }

            // If looping on & Left Mouse isn't down, then we're reading off the loop buffer, so skip float accounting.
            if (!(IsLooping && !ImGui.IsMouseDown(ImGuiMouseButton.Left)))
            {
                AccountForFloating();
            }
            // end the plot
            ImPlot.EndPlot();
        }
    }

    public abstract void DrawSideButtonsTable(Vector2 region);
    

    // When active, the circle will not fall back to the 0 coordinate on the Y axis of the plot, and remain where it is
    public void AccountForFloating()
    {
        // Check if the circle's position is beyond the axis limit and set it to the limit if it is
        if (CirclePosition[0] > XAxisLimit) { CirclePosition[0] = XAxisLimit; }
        if (CirclePosition[0] < -XAxisLimit) { CirclePosition[0] = -XAxisLimit; }
        if (CirclePosition[1] > YAxisLimitUpper) { CirclePosition[1] = YAxisLimitUpper; }
        if (CirclePosition[1] < YAxisLimitLower) { CirclePosition[1] = YAxisLimitLower; }
        // if the isfloating is not active and we have let go of the circle, drop it.
        if (IsFloating == false && IsDragging == false)
        {
            // drop the circle by 10
            if (CirclePosition[1] < 10)
            {
                CirclePosition[1] = 0;
            }
            else
            {
                CirclePosition[1] -= 10;
            }
        }
    }

    public virtual void StartVibrating()
    {
        _logger.LogInformation($"Starting Recording on {WindowBaseName}!");
        // start up our timers and stopwatch
        DisplayUpdateTimer.Start();
        IntensityUpdateTimer.Start();
        DurationStopwatch.Start();
        RemoteOnline = true;
        // start up the simulated vibrator if active.
        _vibeService.StartActiveVibes();
    }

    public virtual void StopVibrating()
    {
        _logger.LogInformation($"Stopping Recording on {WindowBaseName}!");
        // halt our recorders and stopwatch
        DisplayUpdateTimer.Stop();
        IntensityUpdateTimer.Stop();
        DurationStopwatch.Stop();
        RemoteOnline = false;
        // clear our stored data (not the byte intensity block)
        RecordedPositions.Clear();
        StoredLoopDataBlock.Clear();
        // Reset vibrations on motors prior to recording back to original state.
        _vibeService.StopActiveVibes();
    }

    /// <summary>
    /// Because this function simply appends information to the DisplayPositionBuffer, it can be called in the base class.
    /// </summary>
    private void AddCirclePositionToBuffer(object? sender, ElapsedEventArgs e)
    {
        
        // Limit recorded position doubles to 1000. 
        // Once limit reached, reset to new list with last 200 entries. (ensures display isnt fucked up)
        if (RecordedPositions.Count > 1000)
        {
            RecordedPositions = RecordedPositions.GetRange(RecordedPositions.Count - 200, 200);
        }

        // If not looping, add to default buffer
        if (!IsLooping)
        {
            //_logger.LogTrace("Not Looping!");
            RecordedPositions.Add(CirclePosition[1]);
            return;
        }

        // If we are looping but not yet recording loop data, add to default buffer.
        if (IsLooping && !IsDragging && StoredLoopDataBlock.Count == 0)
        {
            //_logger.LogTrace("Looping but not dragging!");
            RecordedPositions.Add(CirclePosition[1]);
            return;
        }


        // If we are recording a looped buffer, add to storedLoopBlock and Recorded Positions.
        // (We add to recorded positions still because what we record for the loop is still part of recording)
        if (IsLooping && IsDragging)
        {
            //_logger.LogTrace("Looping and dragging! (Storing Loop Data)");
            RecordedPositions.Add(CirclePosition[1]);
            StoredLoopDataBlock.Add(CirclePosition[1]);
            return;
        }

        // If looping, but not dragging, & storedLoopBlock has data, add the stored data to the recorded positions.
        if (IsLooping && !IsDragging && StoredLoopDataBlock.Count > 0)
        {
            //_logger.LogTrace("Looping but not dragging! (Reading Loop Data)");
            RecordedPositions.Add(StoredLoopDataBlock[BufferLoopIndex]);
            // inc buffer index so we read the next byte properly
            BufferLoopIndex++;
            // when we reach the end of the buffer index, cycle back over and start again.
            if (BufferLoopIndex >= StoredLoopDataBlock.Count)
            {
                BufferLoopIndex = 0;
            }
            return;
        }
    }

    /// <summary>
    /// This is an abstract class, as this function determines what happens to the data after it is recorded.
    /// <para> This is unique to each kind of device calling it, so we will handle that based on who is calling it.</para>
    /// </summary>
    public abstract void RecordData(object? sender, ElapsedEventArgs e);
}
