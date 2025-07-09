using CkCommons;
using CkCommons.Classes;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using ImGuiNET;
using ImPlotNET;
using System.Timers;

namespace GagSpeak.Gui.Remote;

/// <summary>
///     Represents a single plot point in a SexToyRemote used to control a Toy's Motor.
/// </summary>
/// <remarks> 
///     The <see cref="SexToyMotor"/> associated with the MotorDot is intended to be 
///     defined by <see cref="DevicePlotState"/>, which holds the <see cref="BuzzToy"/>'s data.
/// </remarks>
public class MotorDot(SexToyMotor motor)
{
    private SexToyMotor Motor { get; } = motor;

    private double[] _position = new double[2];
    private int _loopPlaybackIdx = 0;

    /// <summary> Cache holding position data during a drag, when <see cref="IsLooping"/> is true. </summary>
    public List<double> CachedLoopData { get; private set; } = new();

    /// <summary> A rolling buffer array of the last 10s of recorded positions </summary>
    /// <remarks> Buffer will always keep the latest 200 positions cached even after a cleanup. </remarks>
    public RollingBuffer<double> RecordedPositions { get; private set; } = new(1000, 0.2);

    /// <summary> If we are currently moving the MotorDot around on the Plot Graph.</summary>
    public bool IsDragging { get; set; }

    /// <summary> 
    ///     Determines if the MotorDot is recording data in loop mode. <para/>
    ///     
    ///     While in loop mode, all recorded data from the start to end point of a 
    ///     dragged motion will be played back repeatedly.
    /// </summary>
    /// <remarks> Looping playback stops upon being false, or starting another drag loop. </remarks>
    public bool IsLooping { get; set; }

    /// <summary> Determines if the MotorDot should defy the laws of gravity or not. </summary>
    public bool IsFloating { get; set; }

    /// <summary> Determines if the MotorDot is displayed in the Plot Graph. </summary>
    /// <remarks> Positions are still updated when not visible. </remarks>
    public bool Visible { get; set; } = true;

    /// <summary> Public accessor for <see cref="_position"/></summary>
    /// <remarks> Runs <see cref="UpdatePosition(double[])"/> when changed. </remarks>
    public double[] Position
    {
        get => _position;
        set
        {
            // maybe remove, idk.
            if (value != _position && !IsDragging)
                BeginDrag();
            // Update value.
            _position = value;
            // Process updated value.
            UpdatePosition(value);
        }
    }

    /// <summary>
    ///     If the value to be sent off to the devices should come from the loop playback or the recorded display.
    /// </summary>
    public bool SendLatestFromBuffer => IsLooping && !IsDragging && CachedLoopData.Count > 0;

    /// <summary>
    ///     The most recently sent out intensity for this motor. You MUST 
    ///     track to ensure same-intensity values are not sent to the device.
    /// </summary>
    /// <remarks> If ignored, updates are sent to the toy every 20ms, killing the battery. </remarks>
    public double LastSentIntensity = 0.0;

    /// <summary>
    ///     Begin the dragging state on a motor.
    /// </summary>
    /// <remarks> 
    ///     Resets <see cref="_loopPlaybackIdx"/> and <see cref="CachedLoopData"/> to record a new loop, if looping.
    /// </remarks>
    public void BeginDrag()
    {
        IsDragging = true;
        if (IsLooping)
        {
            // reset the loop idx and stored loop data to begin saving a new loop.
            _loopPlaybackIdx = 0;
            CachedLoopData.Clear();
        }
        Svc.Logger.Verbose("Dragging Period Started!");
    }

    /// <summary>
    ///     Ends the dragging state on a motor.
    /// </summary>
    /// <remarks>
    ///     If looping, resets <see cref="_loopPlaybackIdx"/> but keeps <see cref="CachedLoopData"/> so it can be played back.
    /// </remarks>
    public void EndDrag()
    {
        IsDragging = false;
        if (IsLooping)
        {
            // If we end dragging while looping, we should reset the loop index.
            _loopPlaybackIdx = 0;
        }
        Svc.Logger.Verbose("Dragging Period Ended!");
    }

    /// <summary>
    ///     Updates the position point with the latest data from whatever
    ///     the motor data is being drawn on. <para/>
    ///     
    ///     This is set every time <see cref="Position"/> is set to a new value.
    /// </summary>
    /// <remarks>
    ///     Handles automatic <see cref="_loopPlaybackIdx"/> resetting
    ///     when hitting the end of cached loop playback.
    /// </remarks>
    private void UpdatePosition(double[] newPos)
    {
        // if we are not looping, or looping and not
        var cachePositionToLoop = IsLooping && IsDragging;
        var posToRecord = SendLatestFromBuffer ? CachedLoopData[_loopPlaybackIdx] : newPos[1];

        RecordedPositions.Add(posToRecord);
        // cache position if we should
        if(cachePositionToLoop)
            CachedLoopData.Add(newPos[1]);

        // Handle playback buffer for loops.
        if (SendLatestFromBuffer)
        {
            _loopPlaybackIdx++;
            if (_loopPlaybackIdx >= CachedLoopData.Count)
                _loopPlaybackIdx = 0;
        }
    }

    /// <summary>
    ///     Attempts to send off the latest value, if different from the last value sent.
    /// </summary>
    /// <remarks> Actions can be denied if not valid to help save on battery life. </remarks>
    public void TrySendLatestValue(Action<double> sendAction)
    {
        var rawLatest = SendLatestFromBuffer ? CachedLoopData[_loopPlaybackIdx] : Position[1];

        // vastly improves battery life by reducing the ammount of updates sent to the device when values are the same.
        var valueToSend = Math.Round(rawLatest / Motor.Interval) * Motor.Interval;
        if (LastSentIntensity != valueToSend)
        {
            LastSentIntensity = valueToSend;
            sendAction(valueToSend);
        }
    }
}
