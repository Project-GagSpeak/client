using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Network;
using ImGuiNET;
using ImPlotNET;
using NAudio.CoreAudioApi;
using static Penumbra.GameData.Data.GamePaths;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace GagSpeak.Gui.Remote;

/// <summary>
///     No longer abstract, but rather a single Remote. Not factory created.
///     Personal, Pattern creator, and vibe rooms all share this window.
///     
///     (change this later, just do personal for now, we can worry about other things later)
/// </summary>
public class SexToyRemoteUI : WindowMediatorSubscriberBase
{
    private const ImPlotFlags PLOT_FLAGS = ImPlotFlags.NoBoxSelect | ImPlotFlags.NoMenus | ImPlotFlags.NoLegend | ImPlotFlags.NoFrame | ImPlotFlags.NoMouseText;
    private const ImPlotAxisFlags PLAYBACK_AXIS_FLAGS = ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoHighlight;
    private const ImPlotAxisFlags RECORDER_X_AXIS = ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoMenus | ImPlotAxisFlags.NoHighlight;
    private const ImPlotAxisFlags RECORDER_Y_AXIS = ImPlotAxisFlags.NoGridLines | ImPlotAxisFlags.NoMenus | ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoHighlight;
    private const string REMOTE_IDENTIFIER = "###GagSpeakRemote";
    private const float X_AXIS_LIMIT = 40;
    private const float Y_AXIS_LIMIT_LOWER = 0.0f;
    private const float Y_AXIS_LIMIT_UPPER = 1.0f;

    private static readonly double[] PLOT_POSITIONS = { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };
    private static readonly string[] PLOT_LABELS = { "0%", "", "", "", "", "", "", "", "", "", "100%" };
    private static readonly double[] PLOT_STATIC_X = Enumerable.Range(0, 1000).Select(i => (double)(i - 999)).ToArray();

    private readonly BuzzToyHandler _handler;
    private readonly BuzzToyManager _manager;
    private readonly RemoteService _service;
    private readonly TutorialService _guides;

    private bool _themePushed = false;
    private uint _imageButtonIdleTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.475f, .475f, .475f, .475f));
    private uint _imageButtonHoverTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.65f, .65f, .65f, .7f));
    private DevicePlotState? _selectedDevice = null;
    private MotorDot? _selectedMotor = null;

    public SexToyRemoteUI(ILogger<SexToyRemoteUI> logger, GagspeakMediator mediator,
        BuzzToyHandler handler, BuzzToyManager manager, RemoteService service, 
        TutorialService guides) : base(logger, mediator, "Personal Remote" + REMOTE_IDENTIFIER)
    {
        _handler = handler;
        _manager = manager;
        _service = service;
        _guides = guides;
        
        AllowPinning = false;
        AllowClickthrough = false;
        Flags = WFlags.NoScrollbar | WFlags.NoResize;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300f, 430),
            MaximumSize = new Vector2(300f, 430)
        };

        RespectCloseHotkey = false;
        TitleBarButtons = new()
        {
            new TitleBarButton()
            {
                Icon = FAI.QuestionCircle,
                Click = (msg) =>
                {
                    if (_guides.IsTutorialActive(TutorialType.Remote))
                        _guides.SkipTutorial(TutorialType.Remote);
                    else
                        _guides.StartTutorial(TutorialType.Remote);
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

    public Vector2 GetWindowPos() => Position.HasValue ? Position.Value : Vector2.Zero;
    public Vector2 GetWindowSize() => Size.HasValue ? Size.Value : Vector2.Zero;

    private void ThrottleSelectedDevice(DevicePlotState newSelection)
    {
        // if the selection is the same, return it to null.
        if (_selectedDevice is not null && _selectedDevice == newSelection)
        {
            _selectedDevice = null;
            _selectedMotor = null; // reset the selected motor.
            _logger.LogTrace($"Deselected Device: {newSelection.Device.FactoryName} ({newSelection.Device.LabelName})");
            return;
        }
        // Otherwise, update the selection and return.
        _selectedDevice = newSelection;
        _selectedMotor = null; // reset the selected motor.
        _logger.LogTrace($"Selected Device: {newSelection.Device.FactoryName} ({newSelection.Device.LabelName})");
    }

    private void ThrottleSelectedMotor(MotorDot newSelection)
    {
        // if the selection is the same, return it to null.
        if (_selectedMotor is not null && _selectedMotor == newSelection)
        {
            _selectedMotor = null; // reset the selected motor.
            _logger.LogTrace("Deselected Motor from Device.");
            return;
        }
        // Otherwise, update the selection and return.
        _selectedMotor = newSelection;
        _logger.LogTrace($"Selected different / new Motor");
    }

    protected override void PreDrawInternal()
    {
        if (!_themePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 0));

            ImGui.PushStyleColor(ImGuiCol.WindowBg, CkColor.RemoteBgDark.Uint());
            ImGui.PushStyleColor(ImGuiCol.ChildBg, CkColor.RemoteBg.Uint()); // maybe make dark, i kinda like it.
            ImGui.PushStyleColor(ImGuiCol.TitleBg, CkColor.RemoteBgDark.Uint());
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, CkColor.RemoteBgDark.Uint());
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, CkColor.RemoteBg.Uint());

            ImPlot.PushStyleVar(ImPlotStyleVar.MajorTickSize, new Vector2(1.0f));
            ImPlot.PushStyleVar(ImPlotStyleVar.PlotPadding, Vector2.Zero);

            ImPlot.PushStyleColor(ImPlotCol.Line, CkColor.LushPinkLine.Uint());
            ImPlot.PushStyleColor(ImPlotCol.PlotBg, CkColor.RemoteBgDark.Uint());
            ImPlot.PushStyleColor(ImPlotCol.FrameBg, 0x00FFFFFF);

            _themePushed = true;
        }
    }
    protected override void PostDrawInternal()
    {
        if (_themePushed)
        {
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(5);

            ImPlot.PopStyleVar(2);
            ImPlot.PopStyleColor(3);
            _themePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        try
        {
            var isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
            var lineThickness = ImGui.GetStyle().ItemSpacing.Y * 0.5f;
            var lineYOffset = new Vector2(0, lineThickness * 0.5f);

            using var c = CkRaii.Child("##RemoteUI", ImGui.GetContentRegionAvail(), CkColor.RemoteBgDark.Uint(), WFlags.NoDecoration);

            var itemSpacing = ImGui.GetStyle().ItemSpacing;
            var topDispSize = new Vector2(c.InnerRegion.X, 125 * ImGuiHelpers.GlobalScale);

            // Draw the top line before the plot
            var wdl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            wdl.AddLine(pos + lineYOffset, pos + new Vector2(topDispSize.X, lineYOffset.Y), CkColor.RemoteLines.Uint(), lineThickness);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineThickness);

            // Draw the playback display.
            DrawRecordedDisplay(topDispSize);
            var pbMin = ImGui.GetItemRectMin();
            var pbMax = ImGui.GetItemRectMax();
            wdl.AddLine(pbMax with { X = pbMin.X } + lineYOffset, pbMax + lineYOffset, CkColor.RemoteLines.Uint(), lineThickness);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - lineThickness);

            // draw the center bar for recording information and things
            DrawCenterBar(40 * ImGuiHelpers.GlobalScale);
            var barMax = ImGui.GetItemRectMax();
            _guides.OpenTutorial(TutorialType.Remote, StepsRemote.DeviceList, GetWindowPos(), GetWindowSize());
            wdl.AddLine(pbMin with { Y = barMax.Y } + lineYOffset, barMax + lineYOffset, CkColor.RemoteLines.Uint(), lineThickness);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - lineThickness);

            DrawControls();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while drawing the Remote UI.");
            ImGui.TextColored(ImGuiColors.DalamudRed, "An error occurred while drawing the Remote UI. Check the logs for details.");
        }
    }

    private void DrawControls()
    {
        using var _ = ImRaii.Table("Controls", 2, ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInnerV, ImGui.GetContentRegionAvail());

        if (!_) return;

        ImGui.TableSetupColumn("InteractiveMotors", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("RightSideButtons", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60);

        ImGui.TableNextColumn();
        DrawInteractionPlot();

        ImGui.TableNextColumn();
        DrawSideButtons();
    }

    public void DrawRecordedDisplay(Vector2 size)
    {
        // Prepare the Plot for display.
        ImPlot.SetNextAxesLimits(-150, 0, -0.05, 1.1, ImPlotCond.Always);
        // Attempt to draw the plot.
        if (ImPlot.BeginPlot("##PlaybackDisplay", size, PLOT_FLAGS))
        {
            var pos = ImGui.GetCursorScreenPos();
            ImPlot.SetupAxes("X Label", "Y Label", PLAYBACK_AXIS_FLAGS, PLAYBACK_AXIS_FLAGS);
            if (_selectedDevice != null)
            {
                // Draw the Vibe Motors
                for (var i = 0; i < _selectedDevice.VibeDots.Count; i++)
                {
                    var vibeMotor = _selectedDevice.VibeDots[i];
                    ImPlot.PlotLine($"VibeMotor{i}", ref PLOT_STATIC_X[0], ref vibeMotor.RecordedPositions[0], vibeMotor.RecordedPositions.Count);
                }
                // Draw the Oscillation Motors
                for (var i = 0; i < _selectedDevice.OscillateDots.Count; i++)
                {
                    var vibeMotor = _selectedDevice.OscillateDots[i];
                    ImPlot.PlotLine($"VibeMotor{i}", ref PLOT_STATIC_X[0], ref vibeMotor.RecordedPositions[0], vibeMotor.RecordedPositions.Count);
                }
                // Draw the RotateMotor
                if (_selectedDevice.Device.CanRotate)
                    ImPlot.PlotLine("RotateMotor", ref PLOT_STATIC_X[0], ref _selectedDevice.RotateDot.RecordedPositions[0], _selectedDevice.RotateDot.RecordedPositions.Count);
                // Draw the ConstrictMotor
                if (_selectedDevice.Device.CanConstrict)
                    ImPlot.PlotLine("ConstrictMotor", ref PLOT_STATIC_X[0], ref _selectedDevice.ConstrictDot.RecordedPositions[0], _selectedDevice.ConstrictDot.RecordedPositions.Count);
                // Draw the InflateMotor
                if (_selectedDevice.Device.CanInflate)
                    ImPlot.PlotLine("InflateMotor", ref PLOT_STATIC_X[0], ref _selectedDevice.InflateDot.RecordedPositions[0], _selectedDevice.InflateDot.RecordedPositions.Count);
            }

            var lineOffset = new Vector2(0, ImGui.GetStyle().ItemSpacing.Y);
            ImGui.GetWindowDrawList().AddLine(pos + lineOffset, pos + new Vector2(size.X, lineOffset.Y), CkColor.RemoteLines.Uint(), ImGui.GetStyle().ItemSpacing.Y);

            ImPlot.EndPlot();
        }
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.OutputDisplay, GetWindowPos(), GetWindowSize());
    }
    
    /// <summary>
    ///     Used to draw out the connected devices Motors for selection and control interaction. 
    ///     Clicking a motor button makes it's visibility toggle, and toggles the buttons alpha as well.
    /// </summary>
    public void DrawCenterBar(float height)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(4));
        using var inner = CkRaii.ChildPaddedW("###CenterBar", ImGui.GetContentRegionAvail().X, height.RemoveWinPadY(), CkColor.RemoteBg.Uint());

        var imgSize = new Vector2(inner.InnerRegion.Y);

        // Display all Devices.
        int validDevices = 0;
        foreach (var deviceState in _service.ManagedDevices.Values.Where(x => x.Device.ValidForRemotes))
        {
            var textureEnum = GsExtensions.FromFactoryName(deviceState.Device.FactoryName);
            if (textureEnum is CoreIntifaceTexture.MotorVibration)
                continue;

            if (CosmeticService.IntifaceTextures.Cache[textureEnum] is not { } wrap)
                continue;

            validDevices++;
            // draw the custom image button.
            if (CustomImageButton(imgSize, wrap, deviceState.IsPoweredOn))
                ThrottleSelectedDevice(deviceState);
            // draw a highlighted circle around it if selected.
            if (deviceState.Equals(_selectedDevice))
                ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + imgSize / 2, imgSize.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                deviceState.IsPoweredOn = !deviceState.IsPoweredOn;

            CkGui.AttachToolTip($"{deviceState.Device.FactoryName} ({deviceState.Device.LabelName})" +
                $"--SEP----COL--Left-Click:--COL-- Focus this Device" +
                $"--NL----COL--Right-Click:--COL-- Toggle Power State", color: ImGuiColors.ParsedGold);
        }

        if(validDevices > 0)
        {
            CkGui.VerticalSeparator(2);
            // Display the motors for the selected device.
            DisplayDeviceMotors(inner.InnerRegion.Y);
        }
    }

    private void DisplayDeviceMotors(float availableHeight)
    {
        if (_selectedDevice is not { } device)
            return;

        var imgSize = new Vector2(availableHeight);
        var imgCache = CosmeticService.IntifaceTextures.Cache;

        // Vibration motors
        if (device.Device.CanVibrate)
            for (int i = 0; i < device.Device.VibeMotorCount; i++)
                DrawMotor(device.VibeDots[i], $"Vibe Motor #{i}", CoreIntifaceTexture.MotorVibration);

        // Oscillation motors
        if (device.Device.CanOscillate)
            for (int i = 0; i < device.Device.OscillateMotorCount; i++)
                DrawMotor(device.OscillateDots[i], $"Oscillation Motor #{i}", CoreIntifaceTexture.MotorOscillation);

        // Single motors
        if (device.Device.CanRotate)
            DrawMotor(device.RotateDot, "Rotation Motor", CoreIntifaceTexture.MotorRotation);

        if (device.Device.CanConstrict)
            DrawMotor(device.ConstrictDot, "Constriction Motor", CoreIntifaceTexture.MotorConstriction);

        if (device.Device.CanInflate)
            DrawMotor(device.InflateDot, "Inflation Motor", CoreIntifaceTexture.MotorInflation);

        void DrawMotor(MotorDot motor, string label, CoreIntifaceTexture textureType)
        {
            if (!imgCache.TryGetValue(textureType, out var wrap))
                return;

            var isSelected = motor.Equals(_selectedMotor);

            if (CustomImageButton(imgSize, wrap, isSelected))
                ThrottleSelectedMotor(motor);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                motor.Visible = !motor.Visible;

            CkGui.AttachToolTip(
                $"{label}" +
                $"--SEP----COL--Left-Click:--COL-- Focus this Motor" +
                $"--NL----COL--Right-Click:--COL-- Toggle Motor Visibility (Will still run)",
                color: ImGuiColors.ParsedGold
            );
        }
    }

    private void DrawInteractionPlot()
    {
        var mouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var mouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
        // Define the axis limits.
        ImPlot.SetNextAxesLimits(-50, +50, -0.1, 1.1, ImPlotCond.Always);
        // Attempt to draw the plot thing.
        if (ImPlot.BeginPlot("##DataPointRecorderBox", ImGui.GetContentRegionAvail(), PLOT_FLAGS))
        {
            ImPlot.SetupAxes("X Label", "Y Label", RECORDER_X_AXIS, RECORDER_Y_AXIS);
            ImPlot.SetupAxisTicks(ImAxis.Y1, ref PLOT_POSITIONS[0], 11, PLOT_LABELS);

            var dragPointIdx = 0;
            if (_selectedDevice is { } device)
            {
                using var disabled = ImRaii.Disabled(!device.IsPoweredOn);

                ImPlot.DragPoint(dragPointIdx, ref device.VibeDots[0].Position[0], ref device.VibeDots[0].Position[1], CkColor.LushPinkButton.Vec4(), 20, ImPlotDragToolFlags.NoCursors);

                // Process the vibe motors.
                if (_selectedDevice.Device.CanVibrate)
                    foreach (var vibeMotor in _selectedDevice.VibeDots)
                        ProcessMotorDot(vibeMotor, ref dragPointIdx);

                // Process the OscillationMotors
                if (_selectedDevice.Device.CanOscillate)
                    foreach (var oscMotor in _selectedDevice.OscillateDots)
                        ProcessMotorDot(oscMotor, ref dragPointIdx);

                // Process the rotate motor.
                if (_selectedDevice.Device.CanRotate)
                    ProcessMotorDot(_selectedDevice.RotateDot, ref dragPointIdx);

                // Process the constrict motor.
                if (_selectedDevice.Device.CanConstrict)
                    ProcessMotorDot(_selectedDevice.ConstrictDot, ref dragPointIdx);

                // Process the inflate motor.
                if (_selectedDevice.Device.CanInflate)
                    ProcessMotorDot(_selectedDevice.InflateDot, ref dragPointIdx);
            }
            _guides.OpenTutorial(TutorialType.Remote, StepsRemote.ControllableCircle, GetWindowPos(), GetWindowSize());
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.DraggableCircle, GetWindowPos(), GetWindowSize());
            // end the plot
            ImPlot.EndPlot();
        }

        // See if we can get away is IsItemHovered here
        void ProcessMotorDot(MotorDot motor, ref int dragPointIdx)
        {
            ImPlot.DragPoint(dragPointIdx, ref PLOT_STATIC_X[0], ref motor.Position[1], CkColor.LushPinkButton.Vec4(), 20, ImPlotDragToolFlags.NoCursors);
            // Handle a drag release.
            if (motor.IsDragging && ImGui.IsItemHovered() && mouseReleased)
                motor.EndDrag();

            // Handle a drag begin.
            if (!motor.IsDragging && ImGui.IsItemHovered() && mouseDown)
            {
                motor.BeginDrag();
                ThrottleSelectedMotor(motor);
            }

            // Clamp the button bounds and account for dropping to the floor.
            if (!motor.IsLooping || ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                // If the motor is not floating, drop it to the floor.
                if (!motor.IsFloating && !motor.IsDragging)
                    motor.Position[1] = (motor.Position[1] < 0.01) ? 0.0f : motor.Position[1] - 0.075f;

                // Clamp X and Y to their respective axis limits
                motor.Position[0] = Math.Clamp(motor.Position[0], -X_AXIS_LIMIT, X_AXIS_LIMIT);
                motor.Position[1] = Math.Clamp(motor.Position[1], Y_AXIS_LIMIT_LOWER, Y_AXIS_LIMIT_UPPER);
            }
        }
    }

    public void DrawSideButtons()
    {
        // draw the timer
        var timerText = _service.DurationTimer.IsRunning ? $"{_service.DurationTimer.Elapsed:mm\\:ss}" : "00:00";
        CkGui.CenterTextAligned(timerText);
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.TimerButton, GetWindowPos(), GetWindowSize());

        ImGui.Separator();
        ImGui.Spacing();

        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var drawRegion = ImGui.GetContentRegionAvail();
        var imgSize = new Vector2(drawRegion.X * 0.75f);
        var xOffset = (drawRegion.X - imgSize.X) * 0.5f;
        var cursorPos = ImGui.GetCursorPos();
        var minCursorPosX = cursorPos.X;
        var powerDrawPos = cursorPos + drawRegion - imgSize - new Vector2(xOffset, itemSpacing.Y * 1.5f);
        // Draw the Loop Button.
        ImGui.SetCursorPosX(ImGui.GetCursorPos().X + xOffset);
        if (CustomImageButton(imgSize, CosmeticService.CoreTextures.Cache[CoreTexture.ArrowSpin], false, CkColor.LushPinkButton.Uint()))
        {
            if (_selectedMotor is { } motor)
                motor.IsLooping = !motor.IsLooping;
        }
        CkGui.AttachToolTip("Toggle Looping mode for selected Motor");
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.LoopButton, GetWindowPos(), GetWindowSize());

        CkGui.Separator(0);

        // Process the Float Button.
        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPos().X + xOffset, ImGui.GetCursorPos().Y));
        if (CustomImageButton(imgSize, CosmeticService.CoreTextures.Cache[CoreTexture.CircleDot], false, CkColor.LushPinkButton.Uint()))
        {
            if (_selectedMotor is { } motor)
                motor.IsFloating = !motor.IsFloating;
        }
        CkGui.AttachToolTip("Toggle Floating mode for selected Motor");
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.FloatButton, GetWindowPos(), GetWindowSize());

        // push to the bottom right  minus the button height to draw the last centered button.
        ImGui.SetCursorPos(powerDrawPos);
        if (CustomImageButton(imgSize, CosmeticService.CoreTextures.Cache[CoreTexture.Power], _service.DurationTimer.IsRunning, CkColor.LushPinkButton.Uint()))
        {
            if (_service.DurationTimer.IsRunning)
                _service.StopRecording();
            else
                _service.StartRecording();
        }
        CkGui.AttachToolTip("Start/Stop Recording the SexToy data stream");
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.PowerButton, GetWindowPos(), GetWindowSize());
    }

    private bool CustomImageButton(Vector2 size, IDalamudTextureWrap imgWrap, bool selected, uint? activeColorTint = null)
    {
        // Draw the invisible button
        var pressed = ImGui.InvisibleButton($"{imgWrap.ImGuiHandle.ToString()}", size);
        var hovered = ImGui.IsItemHovered();
        var imgTint = selected ? (activeColorTint ?? uint.MaxValue) : (hovered ? _imageButtonHoverTint : _imageButtonIdleTint);
        // Draw the image, over the button.
        ImGui.GetWindowDrawList().AddDalamudImageRounded(imgWrap, ImGui.GetItemRectMin(), size, 45, imgTint);

        if(selected)
            ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + size / 2, size.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);

        return pressed;
    }
}
