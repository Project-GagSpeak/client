using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox.Debouncer;
using GagSpeak.Toybox.Services;
using ImGuiNET;
using ImPlotNET;
using OtterGui;
using System.Numerics;
using System.Timers;

namespace GagSpeak.UI.UiRemote;

/// <summary>
/// I Blame ImPlot for its messiness as a result for this abyssmal display of code here.
/// </summary>
public class RemotePatternMaker : RemoteBase
{
    // the class includes are shared however (i think), so dont worry about that.
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    private readonly VibratorService _vibeService; // these SHOULD all be shared. but if not put into Service.
    private readonly ToyboxRemoteService _remoteService;
    private readonly string _windowName;
    public RemotePatternMaker(ILogger<RemotePatternMaker> logger, GagspeakMediator mediator,
        CosmeticService cosmetics, UiSharedService uiShared, VibratorService vibeService,
        ToyboxRemoteService remoteService, TutorialService guides, string windowName = "Pattern Creator") 
        : base(logger, mediator, uiShared, vibeService, remoteService, guides, windowName)
    {
        // grab the shared services
        _cosmetics = cosmetics;
        _uiShared = uiShared;
        _vibeService = vibeService;
        _remoteService = remoteService;
        _windowName = windowName;
    }

    // The storage buffer of all recorded vibration data in byte format. Eventually stored into a pattern.
    public List<byte> StoredVibrationData = new List<byte>();

    // If we are currently recording data to be stored as a pattern 
    public bool IsRecording { get; protected set; } = false;

    // If we have finished recording the data to be stored as a pattern
    public bool FinishedRecording { get; protected set; } = false;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // anything else we should add here we can add here.
    }


    /// <summary>
    /// Will display personal devices, their motors and additional options. </para>
    /// </summary>
    public override void DrawCenterBar(ref float xPos, ref float yPos, ref float width)
    {
        // grab the content region of the current section
        var CurrentRegion = ImGui.GetContentRegionAvail();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGuiHelpers.GlobalScale * 5);
        using (var color = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.2f, 0.2f, 0.2f, 0.930f)))
        {
            // create a child for the center bar
            using (var canterBar = ImRaii.Child($"###CenterBarDrawPersonal", new Vector2(CurrentRegion.X, 40f), false))
            {
                // Dummy bar.
            }
        }
    }


    /// <summary>
    /// This method is also an overrided function, as depending on the use.
    /// We may also implement unique buttons here on the side that execute different functionalities.
    /// </summary>
    /// <param name="region"> The region of the side button section of the UI </param>
    public override void DrawSideButtonsTable(Vector2 region)
    {
        // push our styles
        using var styleColor = ImRaii.PushColor(ImGuiCol.Button, new Vector4(.2f, .2f, .2f, .2f))
            .Push(ImGuiCol.ButtonHovered, new Vector4(.3f, .3f, .3f, .4f))
            .Push(ImGuiCol.ButtonActive, _remoteService.LushPinkButton);
        using var styleVar = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 40);

        // grab the content region of the current section
        var CurrentRegion = ImGui.GetContentRegionAvail();
        var yPos2 = ImGui.GetCursorPosY();

        // setup a child for the table cell space
        using (var leftChild = ImRaii.Child($"###ButtonsList", CurrentRegion with { Y = region.Y }, false, ImGuiWindowFlags.NoDecoration))
        {
            var InitPos = ImGui.GetCursorPosY();
            if (IsRecording)
            {
                ImGui.AlignTextToFramePadding();
                ImGuiUtil.Center($"{DurationStopwatch.Elapsed.ToString(@"mm\:ss")}");
            }
            else
            {
                ImGui.AlignTextToFramePadding();
                ImGuiUtil.Center("00:00");
            }
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.RecordedDuration, CurrentPos, CurrentSize);

            // move our yposition down to the top of the frame height times a .3f scale of the current region
            ImGui.SetCursorPosY(InitPos + CurrentRegion.Y * .1f);
            ImGui.Separator();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 7f);

            // attempt to obtain an image wrap for it
            var spinArrow = _cosmetics.GetImageFromDirectoryFile("RequiredImages\\arrowspin.png");
            if (spinArrow is { } wrap)
            {
                Vector4 buttonColor = IsLooping ? _remoteService.LushPinkButton : _remoteService.SideButton;
                // aligns the image in the center like we want.
                if (_uiShared.DrawScaledCenterButtonImage("LoopButton"+ _windowName, new Vector2(50, 50),
                    buttonColor, new Vector2(40, 40), wrap))
                {
                    IsLooping = !IsLooping;
                    if (IsFloating) { IsFloating = false; }
                }
            }
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.LoopButton, CurrentPos, CurrentSize);


            // move it down from current position by another .2f scale
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + CurrentRegion.Y * .05f);

            var circlesDot = _cosmetics.GetImageFromDirectoryFile("RequiredImages\\circledot.png");
            if (circlesDot is { } wrap2)
            {
                Vector4 buttonColor2 = IsFloating ? _remoteService.LushPinkButton : _remoteService.SideButton;
                // aligns the image in the center like we want.
                if (_uiShared.DrawScaledCenterButtonImage("FloatButton" + _windowName, new Vector2(50, 50),
                    buttonColor2, new Vector2(40, 40), wrap2))
                {
                    IsFloating = !IsFloating;
                    if (IsLooping) { IsLooping = false; }
                }
            }
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.FloatButton, CurrentPos, CurrentSize);


            ImGui.SetCursorPosY(CurrentRegion.Y * .775f);
            Vector4 buttonColor3 = IsRecording ? _remoteService.LushPinkButton : _remoteService.SideButton;
            // display the stop or play icon depending on if we are recording or not.
            if (!IsRecording)
            {
                var play = _cosmetics.GetImageFromDirectoryFile("RequiredImages\\play.png");
                if (play is { } wrap3)
                {
                    if (_uiShared.DrawScaledCenterButtonImage("RecordStartButton" + _windowName, new Vector2(50, 50),
                        buttonColor3, new Vector2(40, 40), wrap3))
                    {
                        _logger.LogTrace("Starting Recording!");
                        StartVibrating();
                    }
                }
                _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.RecordingButton, CurrentPos, CurrentSize);
            }
            // we are recording so display stop
            else
            {
                var stop = _cosmetics.GetImageFromDirectoryFile("RequiredImages\\stop.png");
                if (stop is { } wrap4)
                {
                    if (_uiShared.DrawScaledCenterButtonImage("RecordStopButton" + _windowName, new Vector2(50, 50),
                        buttonColor3, new Vector2(40, 40), wrap4))
                    {
                        _logger.LogTrace("Stopping Recording!");
                        StopVibrating();
                    }
                }
                _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.StoppingRecording, CurrentPos, CurrentSize);
            }
        }
        // pop what we appended
        styleColor.Pop(3);
        styleVar.Pop();
    }

    public override void StartVibrating()
    {
        _logger.LogDebug($"Started Recording on parent class {_windowName}!");
        // call the base start
        base.StartVibrating();
        // reset our pattern data and begin recording
        StoredVibrationData.Clear();
        IsRecording = true;
    }

    public override void StopVibrating()
    {
        // before we stop the stopwatch from the base call, we must make sure that we extract it here
        var Duration = DurationStopwatch.Elapsed;
        _logger.LogDebug($"Stopping Recording on parent class {_windowName}!");
        // call the base stop
        base.StopVibrating();
        // stop recording and set that we have finished
        IsRecording = false;
        FinishedRecording = true;
        // compile together and send off the popup handler for compiling a newly created pattern
        Mediator.Publish(new PatternSavePromptMessage(StoredVibrationData, Duration));
        Mediator.Publish(new UiToggleMessage(typeof(RemotePatternMaker)));

    }


    /// <summary>
    /// Override method for the recording data.
    /// It is here that we decide how our class handles the recordData function for our personal remote.
    /// </summary>
    public override void RecordData(object? sender, ElapsedEventArgs e)
    {
        // add to recorded storage for the pattern
        AddIntensityToByteStorage();

        // handle playing to our personal vibrator configured devices.
        PlayIntensityToDevices();
    }

    private void AddIntensityToByteStorage()
    {
        if (IsLooping && !IsDragging && StoredLoopDataBlock.Count > 0)
        {
            //_logger.LogTrace($"Looping & not Dragging: {(byte)Math.Round(StoredLoopDataBlock[BufferLoopIndex])}");
            // If looping, but not dragging, and have stored LoopData, add the stored data to the vibration data.
            StoredVibrationData.Add((byte)Math.Round(StoredLoopDataBlock[BufferLoopIndex]));
        }
        else
        {
            //_logger.LogTrace($"Injecting new data: {(byte)Math.Round(CirclePosition[1])}");
            // Otherwise, add the current circle position to the vibration data.
            StoredVibrationData.Add((byte)Math.Round(CirclePosition[1]));
        }
        // if we reached passed our "capped limit", (its like 3 hours) stop recording.
        if (StoredVibrationData.Count > 270000)
        {
            //_logger.LogWarning("Capped the stored data, stopping recording!");
            StopVibrating();
        }
    }

    private void PlayIntensityToDevices()
    {
        // if any devices are currently connected, and our intiface client is connected,
        if (_vibeService.ConnectedToyActive)
        {
            //_logger.LogTrace("Sending Vibration Data to Devices!");
            // send the vibration data to all connected devices
            if (IsLooping && !IsDragging && StoredLoopDataBlock.Count > 0)
            {
                //_logger.LogTrace($"{(byte)Math.Round(StoredLoopDataBlock[BufferLoopIndex])}");
                _vibeService.SendNextIntensity((byte)Math.Round(StoredLoopDataBlock[BufferLoopIndex]));
            }
            else
            {
                //_logger.LogTrace($"{(byte)Math.Round(CirclePosition[1])}");
                _vibeService.SendNextIntensity((byte)Math.Round(CirclePosition[1]));
            }
        }
    }
}
