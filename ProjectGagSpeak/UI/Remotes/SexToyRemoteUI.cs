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
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using ImGuiNET;
using ImPlotNET;
using OtterGui.Text;
using IPAFlags = ImPlotNET.ImPlotAxisFlags;
using IPFlags = ImPlotNET.ImPlotFlags;
using TFlags = ImGuiNET.ImGuiTableFlags;

namespace GagSpeak.Gui.Remote;

/// <summary>
///     Im not insane, i promise.
/// </summary>
public class BuzzToyRemoteUI : WindowMediatorSubscriberBase
{
    private const string    REMOTE_IDENTIFIER   = "###BuzzToyRemote";

    private const IPFlags   PLOT_FLAGS          = IPFlags.NoBoxSelect | IPFlags.NoMenus | IPFlags.NoLegend | IPFlags.NoFrame | IPFlags.NoMouseText;
    private const IPAFlags  PLAYBACK_AXIS_FLAGS = IPAFlags.NoGridLines | IPAFlags.NoLabel | IPAFlags.NoTickLabels | IPAFlags.NoTickMarks | IPAFlags.NoHighlight;
    private const IPAFlags  RECORDER_X_AXIS     = IPAFlags.NoGridLines | IPAFlags.NoLabel | IPAFlags.NoTickLabels | IPAFlags.NoTickMarks | IPAFlags.NoMenus | IPAFlags.NoHighlight;
    private const IPAFlags  RECORDER_Y_AXIS     = IPAFlags.NoGridLines | IPAFlags.NoMenus | IPAFlags.NoLabel | IPAFlags.NoHighlight;
    
    private const float     X_AXIS_BOUND    = 40;
    private const float     Y_AXIS_LOWER    = 0.0f;
    private const float     Y_AXIS_UPPER    = 1.0f;

    private const float     WIN_H       = 430f;
    private const float     REMOTE_W    = 300f;
    private const float     VIBEROOM_W  = 550f;
    private const float     PLAYBACK_H  = 125f;
    private const float     DEVICEBAR_H = 40f;

    private static readonly double[] _yPosList      = { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };
    private static readonly string[] _yPosNames     = { "0%", "", "", "", "", "", "", "", "", "", "100%" };
    private static readonly double[] _xPosList      = Enumerable.Range(0, 1000).Select(i => (double)-i).ToArray();
    private static readonly Vector2 _remoteSize     = new(REMOTE_W, WIN_H);
    private static readonly Vector2 _vibeRoomSize   = new(VIBEROOM_W, WIN_H);

    // May be moved as ckColors is finalized and configured. Temporary placement.
    private bool _themePushed = false;
    private uint _idleTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.475f, .475f, .475f, .475f));
    private uint _hoverTint = ColorHelpers.RgbaVector4ToUint(new Vector4(.65f, .65f, .65f, .7f));
    private DeviceDot? _selectedDevice = null;
    private MotorDot? _selectedMotor = null;

    // DI Instance Injection
    private readonly VibeRoomChatlog _lobbyChatLog;
    private readonly VibeLobbyManager _lobbyManager;
    private readonly VibeLobbyDistributionService _hubCaller;
    private readonly RemoteService _service;
    private readonly TutorialService _guides;
    public BuzzToyRemoteUI(ILogger<BuzzToyRemoteUI> logger, GagspeakMediator mediator,
        VibeLobbyManager lobbyManager, VibeLobbyDistributionService hubCaller,
        VibeRoomChatlog chatLog, RemoteService service,  TutorialService guides) 
        : base(logger, mediator, "Remote" + REMOTE_IDENTIFIER)
    {
        _lobbyChatLog = chatLog;
        _lobbyManager = lobbyManager;
        _hubCaller = hubCaller;
        _service = service;
        _guides = guides;
        
        Flags = WFlags.NoScrollbar | WFlags.NoResize;
        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(REMOTE_W, WIN_H));
        RespectCloseHotkey = false;
        TitleBarButtons = new TitleBarButtonBuilder().AddTutorial(_guides, TutorialType.Remote).Build();
    }

    private Vector2 WindowPos => Position.HasValue ? Position.Value : Vector2.Zero;
    private Vector2 WindowSize => Size.HasValue ? Size.Value : Vector2.Zero;

    public override void OnOpen()
    {
        base.OnOpen();
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.RemoteOpened);
    }

    // This is where we set the remote name, based on the current state our remote is in.
    protected override void PreDrawInternal()
    {
        // Set the window name here potentially.
        WindowName = _lobbyManager.IsInVibeRoom
            ? $"Vibe Room - {_lobbyManager.CurrentRoomName}" : _service.ClientData.RecordingData 
                ? "Pattern Recorder" : "Personal Remote";

        // Update size constraints (or maybe find a way to only do it on change?
        this.SetBoundaries(_lobbyManager.IsInVibeRoom ? _vibeRoomSize : _remoteSize);

        // Assign the theme if we have not already.
        // _themePushed ensures our style is not sent out of alignment.
        if (!_themePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 0));

            ImGui.PushStyleColor(ImGuiCol.WindowBg, CkColor.RemoteBgDark.Uint());
            ImGui.PushStyleColor(ImGuiCol.ChildBg, CkColor.RemoteBg.Uint());
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

    // This is where we pop the theme, to ensure we do not leak styles.
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

    // The actual juicy part of the code.
    protected override void DrawInternal()
    {
        // restore window padding?
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(4));
        // we need to know if we are creating a table or not, so have a
        // seperate draw based if we are in a vibe room or not. (also grab devices here)
        try
        {
            if (_lobbyManager.IsInVibeRoom)
                DrawVibeRoomLayout();
            else
                DrawRemoteLayout();
        }
        catch (Bagagwa e)
        {
            _logger.LogError(e, "Error while drawing the remote layout.");
            CkGui.ColorTextWrapped("An error occurred while drawing the remote layout. Please check the logs for more information.", ImGuiColors.DalamudRed);
        }
    }

    private void DrawVibeRoomLayout()
    {
        var tableFlags = TFlags.BordersOuterV | TFlags.BordersInnerV | TFlags.NoPadInnerX | TFlags.NoPadOuterX;
        using var t = ImRaii.Table("##RoomRemoteColumns", 2, tableFlags, ImGui.GetContentRegionAvail());
        if (!t) return;

        ImGui.TableSetupColumn("LayoutLeft", ImGuiTableColumnFlags.WidthFixed, REMOTE_W);
        ImGui.TableSetupColumn("LayoutRight", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        DrawRemoteLayout();

        ImGui.TableNextColumn();
        DrawParticipantsAndChat();
    }

    // Draw out the left half of the vibeRoomLayout.
    private void DrawRemoteLayout()
    {
        using var c = CkRaii.Child("##RemoteUI", new Vector2(REMOTE_W, ImGui.GetContentRegionAvail().Y));
        if (!c) return;

        // get this child's drawlist, pass it through to other areas.
        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var lineStroke = ImGui.GetStyle().ItemSpacing.Y * 0.5f;
        var lineYOffset = new Vector2(0, lineStroke * 0.5f);

        // Attempt to get the devices to draw for. If we cannot get them, write a text error and return.
        if (!_service.TryGetRemoteData(out var plotDeviceData))
        {
            CkGui.ColorTextWrapped("No devices are currently connected to the remote for the selected user.", ImGuiColors.DalamudRed);
            return;
        }

        // above and center (we hate ImPlot here)
        var plotGraphSize = new Vector2(c.InnerRegion.X, PLAYBACK_H * ImGuiHelpers.GlobalScale);
        wdl.AddLine(min + lineYOffset, min + new Vector2(plotGraphSize.X, lineYOffset.Y), CkColor.RemoteLines.Uint(), lineStroke);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineStroke);

        DrawLatestPositionGraph(plotGraphSize, plotDeviceData);

        // below and center.
        var pbMax = ImGui.GetItemRectMax();
        wdl.AddLine(pbMax with { X = min.X } + lineYOffset, pbMax + lineYOffset, CkColor.RemoteLines.Uint(), lineStroke);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - lineStroke);

        DrawSelectedToys(plotDeviceData);

        // below and center.
        var barMax = ImGui.GetItemRectMax();
        wdl.AddLine(barMax with { X = min.X } + lineYOffset, barMax + lineYOffset, CkColor.RemoteLines.Uint(), lineStroke);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - lineStroke);

        DrawInteractableRow(plotDeviceData);
        bool isHoveringTable = ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

        if (_selectedMotor is { } motor && isHoveringTable)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                motor.IsLooping = !motor.IsLooping;

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                motor.IsFloating = !motor.IsFloating;
        }
    }

    private void DrawLatestPositionGraph(Vector2 region, UserPlotedDevices plotDeviceData)
    {
        ImPlot.SetNextAxesLimits(-150, 0, -0.05, 1.1, ImPlotCond.Always);
        using var _ = ImRaii.Plot("##PLAYBACK_GRAPH", region, PLOT_FLAGS);
        if (!_) return;

        ImPlot.SetupAxes("X Label", "Y Label", PLAYBACK_AXIS_FLAGS, PLAYBACK_AXIS_FLAGS);

        // Do not process if not running.
        if (!plotDeviceData.UserIsBeingBuzzed)
            return;

        foreach (var device in plotDeviceData.Devices)
        {
            // draw out the line for each motor.
            foreach(var motorDot in device.MotorDotMap.Values)
                ImPlot.PlotLine($"Motor{motorDot.MotorIdx}", ref _xPosList[0], ref motorDot.PosHistory[0], motorDot.PosHistory.Count);
        }
    }

    private void DrawSelectedToys(UserPlotedDevices plotDeviceData)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(4));
        using var c = CkRaii.ChildPaddedW("###CenterBar", ImGui.GetContentRegionAvail().X, DEVICEBAR_H.RemoveWinPadY(), CkColor.RemoteBg.Uint());

        var imgSize = new Vector2(c.InnerRegion.Y);
        var imgCache = CosmeticService.IntifaceTextures.Cache;

        // Draw out the devices first, track which are valid since we dont
        // know if the device type is unknown or the image fails to load.
        // if at least one device was valid, we can draw the motor selections.
        if (DrawDeviceSelections(plotDeviceData.Devices, imgSize) > 0)
        {
            CkGui.VerticalSeparator(2);
            DrawMotorSelections(imgSize);
        }

        // right frame align text of the plotDeviceData user.
        var displayString = $"{plotDeviceData.Owner.DisplayName}";
        var textSize = ImGui.CalcTextSize(displayString);
        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(ImGui.GetContentRegionAvail().X - textSize.X, (imgSize.Y - textSize.Y) / 2));
        CkGui.ColorText(displayString, ImGuiColors.DalamudGrey);
    }

    private int DrawDeviceSelections(IEnumerable<DeviceDot> devices, Vector2 imgSize)
    {
        var validDevices = 0;
        foreach (var toyState in devices)
        {
            var name = toyState.FactoryName;
            if (name == 0 || CosmeticService.IntifaceTextures.Cache[name.FromBrandName()] is not { } wrap)
                continue;

            // Sameline if not the first item.
            if (validDevices > 0)
                ImGui.SameLine();

            // Was valid, so inc the validDevices counter.
            validDevices++;

            // Half opacity if not enabled, then draw the image, handling click interaction and colors accordingly.
            using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, toyState.IsEnabled ? 1.0f : 0.5f))
            {
                ImGui.Dummy(imgSize);
                var col = toyState.IsEnabled ? uint.MaxValue : ImGui.IsItemHovered() ? _hoverTint : _idleTint;
                ImGui.GetWindowDrawList().AddDalamudImageRounded(wrap, ImGui.GetItemRectMin(), imgSize, 45, col);

                // Handle left click interaction.
                if (ImGui.IsItemClicked())
                {
                    _selectedDevice = (_selectedDevice?.Equals(toyState) ?? false) ? null : toyState;
                    _selectedMotor = null; // Reset the selected motor when changing devices.
                }

                // if this is the selected toy, draw a circle around it.
                if (toyState.Equals(_selectedDevice))
                {
                    // when enabled, draw a black circle within to counterbalance the yellow on white color constrast.
                    if (toyState.IsEnabled)
                        ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + imgSize / 2, imgSize.X / 2 - 1, 0, 32, 2);
                    ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + imgSize / 2, imgSize.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);
                }

                // if right clicked, toggle IsEnabled state. If IsEnabled is false, all recorded values are always 0.0 for the device.
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    toyState.IsEnabled = !toyState.IsEnabled;
            }

            CkGui.AttachToolTip($"{name}" +
                $"--SEP----COL--Left-Click:--COL-- View Device's Motors" +
                $"--NL----COL--Right-Click:--COL-- Toggle Enabled State, {(toyState.IsEnabled ? "disabling" : "enabling")} motor input", color: ImGuiColors.ParsedGold);
        }
        // return the # of valid devices.
        return validDevices;
    }

    // we only ever want to draw motor selections for the selected device.
    // this does not record or update data, and is purely visual.
    private void DrawMotorSelections(Vector2 imgSize)
    {
        if (_selectedDevice is not { } device)
            return;

        foreach (var motorDot in device.MotorDotMap.Values)
        {
            // Don't render if not a valid texture, of course.
            if (!CosmeticService.IntifaceTextures.Cache.TryGetValue(motorDot.Motor.Type.FromMotor(), out var wrap))
                return;

            // Render the dummy, handle interaction, and draw the image.
            ImGui.Dummy(imgSize);
            var col = motorDot.Visible ? uint.MaxValue : ImGui.IsItemHovered() ? _hoverTint : _idleTint;
            ImGui.GetWindowDrawList().AddDalamudImageRounded(wrap, ImGui.GetItemRectMin(), imgSize, 45, col);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                _selectedMotor = motorDot;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                motorDot.Visible = !motorDot.Visible;

            if (motorDot.Equals(_selectedMotor))
                ImGui.GetWindowDrawList().AddCircle(ImGui.GetItemRectMin() + imgSize / 2, imgSize.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);

            CkGui.AttachToolTip($"{motorDot.Motor.Type.ToString()} Motor#{motorDot.MotorIdx}" +
                $"--SEP----COL--Left-Click:--COL--Select Motor" +
                $"--SEP----COL--Right-Click:--COL--Toggle Visibility (will still run)", color: ImGuiColors.ParsedGold);

            ImUtf8.SameLineInner();
        }
    }

    private void DrawInteractableRow(UserPlotedDevices plotDeviceData)
    {
        using var t = ImRaii.Table("Controls", 2, TFlags.NoPadInnerX | TFlags.NoPadOuterX | TFlags.BordersInnerV, ImGui.GetContentRegionAvail());
        if (!t) return;

        ImGui.TableSetupColumn("InteractiveMotors", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("RightSideButtons", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 60);

        ImGui.TableNextColumn();
        DrawInteractionPlot(plotDeviceData);
        //// _guides.OpenTutorial(TutorialType.Remote, StepsRemote.ControllableCircle, WindowPos, WindowSize);
        //// _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.DraggableCircle, WindowPos, WindowSize);

        ImGui.TableNextColumn();
        DrawInteractionButtons(plotDeviceData);
    }

    private void DrawInteractionPlot(UserPlotedDevices plotDeviceData)
    {
        ImPlot.SetNextAxesLimits(-50, +50, -0.1, 1.1, ImPlotCond.Always);
        using var plot = ImRaii.Plot("##MOTOR_DOT_INTERACTOR", ImGui.GetContentRegionAvail(), PLOT_FLAGS);
        if (!plot)
            return;

        ImPlot.SetupAxes("X Label", "Y Label", RECORDER_X_AXIS, RECORDER_Y_AXIS);
        ImPlot.SetupAxisTicks(ImAxis.Y1, ref _xPosList[0], 11, _yPosNames);

        var mouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var mouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
        var pdl = ImPlot.GetPlotDrawList();

        foreach (var device in plotDeviceData.Devices)
        {
            // Disable the motors for this device is the device is not enabled.
            // (still process them for updates though)
            using var dis = ImRaii.Disabled(!device.IsEnabled);
            var col = device.IsEnabled && plotDeviceData.UserIsBeingBuzzed ? CkColor.LushPinkButton.Vec4() : CkColor.LushPinkButtonDisabled.Vec4();


            foreach (var motor in device.MotorDotMap.Values)
            {
                // get the plot center position for this motor.
                var plotCenter = ImPlot.PlotToPixels(motor.Position[0], motor.Position[1]);

                // If this is for the selected device. Draw it out.
                if (device.Equals(_selectedDevice))
                {
                    ImPlot.DragPoint((int)motor.MotorIdx, ref motor.Position[0], ref motor.Position[1], col, 20f, ImPlotDragToolFlags.NoCursors);
                    var label = $"#{motor.MotorIdx}";
                    var textSize = ImGui.CalcTextSize(label);
                    pdl.AddText(plotCenter - textSize * 0.5f, ImGui.GetColorU32(ImGuiCol.Text), label);
                }

                // Handle a drag release.
                if (motor.IsDragging && mouseReleased)
                    motor.IsDragging = false;

                // Handle a drag begin.
                var hovering = Vector2.Distance(ImGui.GetMousePos(), plotCenter) <= 20f;
                if (!motor.IsDragging && hovering && mouseDown)
                {
                    motor.IsDragging = true;
                    _selectedMotor = motor;
                }

                // Clamp the button bounds and account for dropping to the floor.
                if (!motor.IsLooping)
                {
                    var newY = motor.Position[1];
                    if (!motor.IsFloating && !motor.IsDragging)
                        newY = (motor.Position[1] < 0.01f) ? 0.0f : motor.Position[1] - 0.075f;

                    motor.Position[0] = Math.Clamp(motor.Position[0], -X_AXIS_BOUND, X_AXIS_BOUND);
                    motor.Position[1] = Math.Clamp(newY, Y_AXIS_LOWER, Y_AXIS_UPPER);
                }

                // Add to history
                if (plotDeviceData.UserIsBeingBuzzed)
                    motor.AddPosToHistory(device.IsEnabled);
            }
        }
    }

    public void DrawInteractionButtons(UserPlotedDevices plotDeviceData)
    {
        // draw the timer
        var timerText = plotDeviceData.UserIsBeingBuzzed ? $"{plotDeviceData.TimeAlive:mm\\:ss}" : "00:00";
        CkGui.CenterTextAligned(timerText);
        //// _guides.OpenTutorial(TutorialType.Remote, StepsRemote.TimerButton, WindowPos, WindowSize);

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
            _selectedMotor!.IsLooping = !loopState;
        CkGui.AttachToolTip(disableLoop ? "No Motor currently selected!"
            : $"{(loopState ? "Disable" : "Enable")} looping for this motor.--SEP----COL--Right-Click:--COL--Keybind Alternative", color: ImGuiColors.ParsedGold);
        // _guides.OpenTutorial(TutorialType.Remote, StepsRemote.LoopButton, WindowPos, WindowSize);

        CkGui.Separator(0);

        // Process the Float Button.
        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPos().X + xOffset, ImGui.GetCursorPos().Y));
        var floatState = _selectedMotor?.IsFloating ?? false;
        var disableFloat = _selectedMotor is null;
        if (CustomImageButton(CosmeticService.CoreTextures.Cache[CoreTexture.CircleDot], disableFloat, floatState))
            _selectedMotor!.IsFloating = !_selectedMotor.IsFloating;
        CkGui.AttachToolTip(disableFloat ? "No Motor currently selected!" : $"{(floatState ? "Disable" : "Enable")} floating for this motor." +
            $"--SEP----COL--Middle-Click:--COL--Keybind Alternative", color: ImGuiColors.ParsedGold);
        // _guides.OpenTutorial(TutorialType.Remote, StepsRemote.FloatButton, WindowPos, WindowSize);

        // push to the bottom right  minus the button height to draw the last centered button.
        ImGui.SetCursorPos(powerDrawPos);
        if (CustomImageButton(CosmeticService.CoreTextures.Cache[CoreTexture.Power], false, plotDeviceData.UserIsBeingBuzzed))
            _service.SetUserRemotePower(plotDeviceData.Owner.User.UID, !plotDeviceData.RemotePowerActive, MainHub.UID);
        CkGui.AttachToolTip("Start/Stop Recording the Sex Toy DataStream");
        // _guides.OpenTutorial(TutorialType.Remote, StepsRemote.PowerButton, WindowPos, WindowSize);

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


    // Draw out the participants and chat section of the vibe room.
    private void DrawParticipantsAndChat()
    {
        using var c = CkRaii.Child("###RemoteRightChild", ImGui.GetContentRegionAvail());
        if (!c) return;

        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var lineStroke = ImGui.GetStyle().ItemSpacing.Y * 0.5f;
        var lineYOffset = new Vector2(0, lineStroke * 0.5f);

        // above.
        var plotGraphSize = new Vector2(c.InnerRegion.X, PLAYBACK_H * ImGuiHelpers.GlobalScale);
        wdl.AddLine(min + lineYOffset, min + new Vector2(plotGraphSize.X, lineYOffset.Y), CkColor.RemoteLines.Uint(), lineStroke);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineStroke);

        DrawParticipantsBox(new Vector2(c.InnerRegion.X, PLAYBACK_H));

        // below
        var pbMax = ImGui.GetItemRectMax();
        wdl.AddLine(pbMax with { X = min.X } + lineYOffset, pbMax + lineYOffset, CkColor.RemoteLines.Uint(), lineStroke);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - lineStroke);

        DrawVibeRoomChat();
    }

    private void DrawParticipantsBox(Vector2 region)
    {
        using var c = CkRaii.Child("###VibeRoomParticipants", region, WFlags.AlwaysUseWindowPadding);

        var participants = _lobbyManager.CurrentParticipants;
        var total = participants.Count;
        // if there are 0 participants, return early.
        if (total <= 0)
            return;

        // Define the Icon size as the Y height divided by 4.
        var iconSize = new Vector2(region.Y / 3);
        var spacing = iconSize.X * .25f; // allows for up to 5 icons in a row.

        // determine the splitRows.
        var topRowCount = total <= 2 ? total : (total + 1) / 2;
        var bottomRowCount = total - topRowCount;

        // compute the starting position for centering the first row.
        float StartX(int iconCount)
            => ImGui.GetCursorScreenPos().X + (c.InnerRegion.X - (iconCount * iconSize.X + (iconCount - 1) * spacing)) / 2;

        // grab the current Y position.
        var posY = ImGui.GetCursorScreenPos().Y;

        // Draw out the top row of participant icons.
        var startX = StartX(topRowCount);
        var wdl = ImGui.GetWindowDrawList();
        ImGui.SetCursorScreenPos(new Vector2(startX, posY));
        for (var i = 0; i < topRowCount; i++)
        {
            DrawParticipantIcon(participants[i], iconSize, wdl);
            ImGui.SameLine(0, spacing);
            CkGui.AttachToolTip($"({participants[i].DisplayName})");
        }

        // --- Draw Bottom Row ---
        if (topRowCount != total)
        {
            posY += iconSize.Y + spacing;
            startX = StartX(bottomRowCount);
            ImGui.SetCursorScreenPos(new Vector2(startX, posY));

            for (var j = topRowCount; j < total; j++)
            {
                DrawParticipantIcon(participants[j], iconSize, wdl);
                ImGui.SameLine(0, spacing);
                CkGui.AttachToolTip($"({participants[j].DisplayName})");
            }
        }
    }

    private void DrawParticipantIcon(RoomParticipant participant, Vector2 iconSize, ImDrawListPtr wdl)
    {
        using var disabled = ImRaii.Disabled(!participant.AllowedUids.Contains(MainHub.UID));
        // Draw the participant icon.
        ImGui.Dummy(iconSize);
        wdl.AddDalamudImageRounded(CosmeticService.CoreTextures.Cache[CoreTexture.Icon256], ImGui.GetItemRectMin(), iconSize, 45);
        // draw a circle around it if it is the selected participant.
        if (_service.SelectedKey.Equals(participant.User.UID))
            wdl.AddCircle(ImGui.GetItemRectMin() + iconSize / 2, iconSize.X / 2, CkGui.Color(ImGuiColors.ParsedGold), 32, 2);

        // Handle left click interaction.
        if (ImGui.IsItemClicked())
            _service.SelectedKey = participant.User.UID;
        CkGui.AttachToolTip($"View {participant.DisplayName}'s Toys.");
    }

    private void DrawVibeRoomChat()
    {
        // Draw the chat log for the vibe room.
        using var c = CkRaii.Child("###VibeRoomChat", ImGui.GetContentRegionAvail(), WFlags.AlwaysUseWindowPadding);
        if (!c) return;
        // Draw the chat log.
        _lobbyChatLog.DrawChat(c.InnerRegion);
    }

    public override void OnClose()
    {
        // grab the currently selected remotedata item.
        // (currently only for client, but later do for other users)
        _service.ClientData.TrySetRemotePower(false, MainHub.UID);
        // if for some reason we are still in recording mode, disable it.
        if (_service.ClientData.RecordingData)
            _service.ClientData.RecordingData = false;

        base.OnClose();
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.RemoteClosed);
        Svc.Logger.Information("You might have messed this up heavily, good luck fixing it!~");
    }
}
