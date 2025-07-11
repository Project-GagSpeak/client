using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Handlers;
using GagSpeak.WebAPI;
using ImGuiNET;
using ImPlotNET;

namespace GagSpeak.Gui.Remote;

/// <summary>
///     No longer abstract, but rather a single Remote. Not factory created.
///     Personal, Pattern creator, and vibe rooms all share this window.
///     
///     (change this later, just do personal for now, we can worry about other things later)
/// </summary>
public class BuzzToyRemoteUI : WindowMediatorSubscriberBase
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
    private static readonly double[] PLOT_STATIC_X = Enumerable.Range(0, 1000).Select(i => (double)-i).ToArray();

    private readonly RemoteHandler _handler;
    private readonly RemoteService _service;
    private readonly TutorialService _guides;

    private bool _themePushed = false;
    private DevicePlotState? _selectedDevice = null;
    private MotorDot? _selectedMotor = null;
    private uint _idleTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.475f, .475f, .475f, .475f));
    private uint _hoverTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.65f, .65f, .65f, .7f));

    public BuzzToyRemoteUI(ILogger<BuzzToyRemoteUI> logger, GagspeakMediator mediator,
        RemoteHandler handler, RemoteService service,  TutorialService guides) 
        : base(logger, mediator, "Personal Remote" + REMOTE_IDENTIFIER)
    {
        _handler = handler;
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

            // Get the devices we are handling for the selected kinkster.
            var devices = _service.GetManagedDevicesByMode();

            // Draw the playback display.
            DrawRecordedDisplay(topDispSize, devices);
            var pbMin = ImGui.GetItemRectMin();
            var pbMax = ImGui.GetItemRectMax();
            wdl.AddLine(pbMax with { X = pbMin.X } + lineYOffset, pbMax + lineYOffset, CkColor.RemoteLines.Uint(), lineThickness);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - lineThickness);

            // draw the center bar for recording information and things
            DrawCenterBar(40 * ImGuiHelpers.GlobalScale, devices);
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
        var isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        using var _ = ImRaii.Table("Controls", 2, ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInnerV, ImGui.GetContentRegionAvail());

        if (!_) return;

        ImGui.TableSetupColumn("InteractiveMotors", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("RightSideButtons", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60);

        ImGui.TableNextColumn();
        DrawInteractionPlot();

        // Handle Hotkey operations for the selected motor.
        if (_selectedMotor is { } motor)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && isFocused)
                motor.IsLooping = !motor.IsLooping;
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle) && isFocused)
                motor.IsFloating = !motor.IsFloating;
        }

        ImGui.TableNextColumn();
        DrawSideButtons();
    }

    public void DrawRecordedDisplay(Vector2 size, IEnumerable<DevicePlotState> devices)
    {
        ImPlot.SetNextAxesLimits(-150, 0, -0.05, 1.1, ImPlotCond.Always);
        using var _ = ImRaii.Plot("##PlaybackDisplay", size, PLOT_FLAGS);
        if (!_)
            return;

        var pos = ImGui.GetCursorScreenPos();
        ImPlot.SetupAxes("X Label", "Y Label", PLAYBACK_AXIS_FLAGS, PLAYBACK_AXIS_FLAGS);

        // If the service is not currently processing any remote mode, do not display any details.
        if (_service.CurrentMode is RemoteService.RemoteMode.None)
            return;

        // If the timer is not actively running, do not display any datapoints.        
        if (!_service.RemoteIsActive)
            return;

        foreach (var device in devices.Where(d => d.IsPoweredOn))
        {
            // Draw the Vibe Motors
            for (var i = 0; i < device.VibeDots.Count; i++) 
                ImPlot.PlotLine($"VibeMotor{i}", ref PLOT_STATIC_X[0], ref device.VibeDots[i].PosHistory[0], device.VibeDots[i].PosHistory.Count);

            // Draw the Oscillation Motors
            for (var i = 0; i < device.OscillateDots.Count; i++)
                ImPlot.PlotLine($"OsciMotor{i}", ref PLOT_STATIC_X[0], ref device.OscillateDots[i].PosHistory[0], device.OscillateDots[i].PosHistory.Count);

            // Draw the RotateMotor
            if (device.Device.CanRotate)
                ImPlot.PlotLine("RotMotor", ref PLOT_STATIC_X[0], ref device.RotateDot.PosHistory[0], device.RotateDot.PosHistory.Count);

            // Draw the ConstrictMotor
            if (device.Device.CanConstrict)
                ImPlot.PlotLine("ConstMotor", ref PLOT_STATIC_X[0], ref device.ConstrictDot.PosHistory[0], device.ConstrictDot.PosHistory.Count);

            // Draw the InflateMotor
            if (device.Device.CanInflate)
                ImPlot.PlotLine("InflMotor", ref PLOT_STATIC_X[0], ref device.InflateDot.PosHistory[0], device.InflateDot.PosHistory.Count);
        }

        var lineOffset = new Vector2(0, ImGui.GetStyle().ItemSpacing.Y);
        ImGui.GetWindowDrawList().AddLine(pos + lineOffset, pos + new Vector2(size.X, lineOffset.Y), CkColor.RemoteLines.Uint(), ImGui.GetStyle().ItemSpacing.Y);
    }
    
    /// <summary>
    ///     Used to draw out the connected devices Motors for selection and control interaction. 
    ///     Clicking a motor button makes it's visibility toggle, and toggles the buttons alpha as well.
    /// </summary>
    public void DrawCenterBar(float height, IEnumerable<DevicePlotState> devices)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(4));
        using var inner = CkRaii.ChildPaddedW("###CenterBar", ImGui.GetContentRegionAvail().X, height.RemoveWinPadY(), CkColor.RemoteBg.Uint());

        // If the service is not currently processing any remote mode, do not display any details.
        if (_service.CurrentMode is RemoteService.RemoteMode.None)
            return;

        var imgSize = new Vector2(inner.InnerRegion.Y);
        var validDevices = 0;
        foreach (var deviceState in devices)
        {
            if (deviceState.Device.FactoryName is CoreIntifaceElement.UnknownDevice)
                continue;

            if (CosmeticService.IntifaceTextures.Cache[deviceState.Device.FactoryName] is not { } wrap)
                continue;

            validDevices++;
            // draw the custom image button.
            using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, deviceState.IsPoweredOn ? 1.0f : 0.5f))
            {
                ImGui.Dummy(imgSize);
                var col = deviceState.IsPoweredOn ? uint.MaxValue : ImGui.IsItemHovered() ? _hoverTint : _idleTint;
                ImGui.GetWindowDrawList().AddDalamudImageRounded(wrap, ImGui.GetItemRectMin(), imgSize, 45, col);
                if (ImGui.IsItemClicked())
                    ThrottleSelectedDevice(deviceState);
                if (deviceState.Equals(_selectedDevice))
                {
                    if (deviceState.IsPoweredOn)
                        ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + imgSize / 2, imgSize.X / 2 - 1, 0xFF000000, 32, 2);
                    ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + imgSize / 2, imgSize.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                deviceState.ThrottlePower();

            CkGui.AttachToolTip($"{deviceState.Device.FactoryName} ({deviceState.Device.LabelName})" +
                $"--SEP----COL--Left-Click:--COL-- View Device's Motors" +
                $"--NL----COL--Right-Click:--COL-- Toggle Device power, {(deviceState.IsPoweredOn ? "disabling" : "enabling")} it's Motors", color: ImGuiColors.ParsedGold);
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

        var idleTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.475f, .475f, .475f, .475f));
        var hoverTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.65f, .65f, .65f, .7f));
        var imgSize = new Vector2(availableHeight);
        var imgCache = CosmeticService.IntifaceTextures.Cache;

        // Vibration motors
        if (device.Device.CanVibrate)
            for (var i = 0; i < device.Device.VibeMotorCount; i++)
                DrawMotor(device.VibeDots[i], $"Vibe Motor #{i}", CoreIntifaceElement.MotorVibration);

        // Oscillation motors
        if (device.Device.CanOscillate)
            for (var i = 0; i < device.Device.OscillateMotorCount; i++)
                DrawMotor(device.OscillateDots[i], $"Oscillation Motor #{i}", CoreIntifaceElement.MotorOscillation);

        // Single motors
        if (device.Device.CanRotate)
            DrawMotor(device.RotateDot, "Rotation Motor", CoreIntifaceElement.MotorRotation);

        if (device.Device.CanConstrict)
            DrawMotor(device.ConstrictDot, "Constriction Motor", CoreIntifaceElement.MotorConstriction);

        if (device.Device.CanInflate)
            DrawMotor(device.InflateDot, "Inflation Motor", CoreIntifaceElement.MotorInflation);

        void DrawMotor(MotorDot motor, string label, CoreIntifaceElement textureType)
        {
            if (!imgCache.TryGetValue(textureType, out var wrap))
                return;

            ImGui.Dummy(imgSize);
            var col = motor.Visible ? uint.MaxValue : ImGui.IsItemHovered() ? hoverTint : idleTint;
            ImGui.GetWindowDrawList().AddDalamudImageRounded(wrap, ImGui.GetItemRectMin(), imgSize, 45, col);
            if (ImGui.IsItemClicked()) 
                motor.Visible = !motor.Visible;
            if (motor.Equals(_selectedMotor))
                ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + imgSize / 2, imgSize.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);

            CkGui.AttachToolTip($"{label}--SEP----COL--Left-Click:--COL--Toggle Visibility (still runs)", color: ImGuiColors.ParsedGold);
        }
    }

    private void DrawInteractionPlot()
    {
        ImPlot.SetNextAxesLimits(-50, +50, -0.1, 1.1, ImPlotCond.Always);
        using var plot = ImRaii.Plot("##DataPointRecorderBox", ImGui.GetContentRegionAvail(), PLOT_FLAGS);
        if (!plot)
            return;

        // If the service is not currently processing any remote mode, do not display any details.
        if (_service.CurrentMode is RemoteService.RemoteMode.None)
            return;

        var recordingData = _service.RemoteIsActive;
        var mouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var mouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
        var pdl = ImPlot.GetPlotDrawList();
        ImPlot.SetupAxes("X Label", "Y Label", RECORDER_X_AXIS, RECORDER_Y_AXIS);
        ImPlot.SetupAxisTicks(ImAxis.Y1, ref PLOT_POSITIONS[0], 11, PLOT_LABELS);

        var dragPointIdx = 0;
        if (_selectedDevice is { } device)
        {
            // Process all the motor dots for the device. (for active UI display only, not recording data points (but maybe they are? idk)
            using var disabled = ImRaii.Disabled(!_service.RemoteIsActive);
            if (device.Device.CanVibrate)
                for (var i = 0; i < device.VibeDots.Count(); i++)
                    ProcessMotorDot(device.VibeDots[i], device.IsPoweredOn, $"#{i}");

            if (device.Device.CanOscillate)
                for (var i = 0; i < device.OscillateDots.Count(); i++)
                    ProcessMotorDot(device.OscillateDots[i], device.IsPoweredOn, $"#{i}");

            if (device.Device.CanRotate)
                ProcessMotorDot(device.RotateDot, device.IsPoweredOn);

            if (device.Device.CanConstrict)
                ProcessMotorDot(device.ConstrictDot, device.IsPoweredOn);

            if (device.Device.CanInflate)
                ProcessMotorDot(device.InflateDot, device.IsPoweredOn);
        }
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.ControllableCircle, GetWindowPos(), GetWindowSize());
        _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.DraggableCircle, GetWindowPos(), GetWindowSize());

        // See if we can get away is IsItemHovered here
        void ProcessMotorDot(MotorDot motor, bool deviceOn, string? label = null)
        {
            // if the motor is not visible simply return.
            if (!motor.Visible)
                return;

            using var disabled = ImRaii.Disabled(!deviceOn);
            
            var col = deviceOn && _service.RemoteIsActive ? CkColor.LushPinkButton.Vec4() : CkColor.LushPinkButtonDisabled.Vec4();
            ImPlot.DragPoint(dragPointIdx, ref motor.Position[0], ref motor.Position[1], col, 20f, ImPlotDragToolFlags.NoCursors);
            
            dragPointIdx++;
            var plotCenter = ImPlot.PlotToPixels(motor.Position[0], motor.Position[1]);
            if (!string.IsNullOrEmpty(label))
            {
                var textSize = ImGui.CalcTextSize(label);
                var textPos = plotCenter - textSize * 0.5f;
                pdl.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), label);
            }

            // Handle a drag release.
            if (motor.IsDragging && mouseReleased)
                motor.EndDrag();

            // Handle a drag begin.
            var hovering = Vector2.Distance(ImGui.GetMousePos(), plotCenter) <= 20f;
            if (!motor.IsDragging && hovering && mouseDown)
            {
                motor.BeginDrag();
                _selectedMotor = motor;
            }

            // Clamp the button bounds and account for dropping to the floor.
            if (!motor.IsLooping || mouseDown)
            {
                var newY = motor.Position[1];
                if (!motor.IsFloating && !motor.IsDragging)
                    newY = (motor.Position[1] < 0.01f) ? 0.0f : motor.Position[1] - 0.075f;
                
                motor.Position[0] = Math.Clamp(motor.Position[0], -X_AXIS_LIMIT, X_AXIS_LIMIT);
                motor.Position[1] = Math.Clamp(newY, Y_AXIS_LIMIT_LOWER, Y_AXIS_LIMIT_UPPER);
            }

            // Add to history
            if (recordingData && deviceOn)
                motor.AddPosToHistory();
        }
    }

    public void DrawSideButtons()
    {
        // draw the timer
        var timerText = _service.RemoteIsActive ? $"{_service.ElapsedTime:mm\\:ss}" : "00:00";
        CkGui.CenterTextAligned(timerText);
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.TimerButton, GetWindowPos(), GetWindowSize());

        ImGui.Separator();
        ImGui.Spacing();

        var drawRegion = ImGui.GetContentRegionAvail();
        var imgSize = new Vector2(drawRegion.X * 0.75f);
        var xOffset = (drawRegion.X - imgSize.X) * 0.5f;
        var cursorPos = ImGui.GetCursorPos();
        var powerDrawPos = cursorPos + drawRegion - imgSize - new Vector2(xOffset, ImGui.GetStyle().ItemSpacing.Y * 1.5f);

        // Draw the Loop Button.
        ImGui.SetCursorPosX(ImGui.GetCursorPos().X + xOffset);
        var loopState = _selectedMotor?.IsLooping ?? false;
        var disableLoop = _selectedMotor is null;
        if (CustomImageButton(CosmeticService.CoreTextures.Cache[CoreTexture.ArrowSpin], disableLoop, loopState))
            _selectedMotor!.IsLooping = !_selectedMotor.IsLooping;
        CkGui.AttachToolTip(disableLoop ? "No Motor currently selected!"
            : $"{(loopState ? "Disable" : "Enable")} looping for this motor.--SEP----COL--Right-Click:--COL--Keybind Alternative", color: ImGuiColors.ParsedGold);
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.LoopButton, GetWindowPos(), GetWindowSize());

        CkGui.Separator(0);

        // Process the Float Button.
        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPos().X + xOffset, ImGui.GetCursorPos().Y));
        var floatState = _selectedMotor?.IsFloating ?? false;
        var disableFloat = _selectedMotor is null;
        if (CustomImageButton(CosmeticService.CoreTextures.Cache[CoreTexture.CircleDot], disableFloat, floatState))
            _selectedMotor!.IsFloating = !_selectedMotor.IsFloating;
        CkGui.AttachToolTip(disableFloat ? "No Motor currently selected!"
            : $"{(floatState ? "Disable" : "Enable")} floating for this motor.--SEP----COL--Middle-Click:--COL--Keybind Alternative", color: ImGuiColors.ParsedGold);
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.FloatButton, GetWindowPos(), GetWindowSize());

        // push to the bottom right  minus the button height to draw the last centered button.
        ImGui.SetCursorPos(powerDrawPos);
        if (CustomImageButton(CosmeticService.CoreTextures.Cache[CoreTexture.Power], false, _service.RemoteIsActive))
        {
            // set to variable UID's once we integrate the vibe room and stuff.
            if (_service.RemoteIsActive)
                _service.PowerOnRemote(MainHub.UID);
            else
                _service.PowerOffRemote(MainHub.UID);
        }
        CkGui.AttachToolTip("Start/Stop Recording the SexToy data stream");
        _guides.OpenTutorial(TutorialType.Remote, StepsRemote.PowerButton, GetWindowPos(), GetWindowSize());

        bool CustomImageButton(IDalamudTextureWrap wrap, bool isDisabled, bool isActive)
        {
            using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, isDisabled ? 0.5f : 1.0f);
            ImGui.Dummy(imgSize);
            var col = isDisabled ? (isActive ? CkColor.LushPinkButton.Uint() : _idleTint)
                : isActive ? CkColor.LushPinkButton.Uint() : ImGui.IsItemHovered() ? _hoverTint : _idleTint;
            ImGui.GetWindowDrawList().AddDalamudImageRounded(wrap, ImGui.GetItemRectMin(), imgSize, 45, col);
            return !isDisabled && ImGui.IsItemClicked();
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        // If we are not currently in any valid mode, close the window immidiately.
        if (_service.CurrentMode is RemoteService.RemoteMode.None)
        {
            _logger.LogWarning("Remote UI opened while not in a valid mode, closing immediately.");
            Toggle();
            return;
        }
    }

    public override void OnClose()
    {
        base.OnClose();
        // _handler.SetRemoteMode(RemoteService.RemoteMode.None);
    }
}
