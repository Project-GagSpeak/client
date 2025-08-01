using CkCommons.Classes;
using GagSpeak.State.Models;

namespace GagSpeak.Gui.Remote;

/// <summary>
///     Represents a single plot point in a SexToyRemote used to control a Toy's Motor. <para/>
///     The <see cref="BuzzToyMotor"/> associated with the MotorDot is intended to be 
///     defined by <see cref="DeviceDot"/>, which holds the <see cref="BuzzToy"/>'s data.
/// </summary>
public class MotorDot(BuzzToyMotor motor) : IEquatable<MotorDot>
{
    public RemotePlaybackRef PlaybackRef { get; private set; } = new();
    public readonly BuzzToyMotor Motor = motor;
    public uint MotorIdx => Motor.MotorIdx;

    private List<double> _dragLoopData = new();
    private int _dragLoopPlaybackIdx = 0;
    private bool _useDragLoopData = false;
    private bool _dragging = false;
    private bool _looping = false;

    /// <summary> The current Position of the MotorDot on the PlotGraph. </summary>
    public double[] Position = new double[2];

    /// <summary> A rolling buffer array of the last 10s of recorded positions </summary>
    /// <remarks> Should add a position here every time the DragPoint/Position changes. </remarks>
    public RollingBuffer<double> PosHistory { get; private set; } = new(1000);

    /// <summary> The final data that will be saved into a pattern, or played back to the client. </summary>
    /// <remarks> This is not for UI Display, and occurs on a set interval you define. </remarks>
    public List<double> RecordedData { get; private set; } = new();

    /// <summary> The position where a looping drag position started. </summary>
    public double DragLoopStartPos { get; private set; } = -1;

    /// <summary> If we are moving the MotorDot around. </summary>
    public bool IsDragging
    {
        get => _dragging;
        set
        {
            Svc.Logger.Verbose($"Dragging is now {value.ToString()}");
            _dragging = value;
            // if the new value was false, disable drag logic.
            if (!value)
            {
                // if we are looping, we should revert the playback idx and enable using drag loop data.
                if (IsLooping)
                {
                    DragLoopStartPos = -1;
                    _useDragLoopData = true;
                    _dragLoopPlaybackIdx = 0;
                }
            }
            // the new value was true, so enable drag logic.
            else
            {
                // if looping, we should begin to record our new loop data chunk.
                _dragLoopData.Clear();
                if (IsLooping)
                    DragLoopStartPos = Position[1];

                _dragLoopPlaybackIdx = 0;
                _useDragLoopData = false;
            }
        }
    }

    /// <summary> 
    ///     Determines if the MotorDot is recording data in loop mode. <para/>
    ///     While in loop mode, all data from the start to the end of a dragged motion is played back repeatedly.
    /// </summary>
    /// <remarks> Looping playback stops upon being false, or starting another drag loop. </remarks>
    public bool IsLooping
    {
        get => _looping;
        set
        {
            Svc.Logger.Verbose($"Looping is now {value.ToString()}");
            if (value)
                DragLoopStartPos = Position[1];
            else
                DragLoopStartPos = -1;

            _looping = value;
            // enabling or disabling looping, it doesnt madder, at the start of either state, loop data should be reset.
            _dragLoopData.Clear();
            _dragLoopPlaybackIdx = 0;
            _useDragLoopData = false;
        }
    }

    /// <summary> Determines if the MotorDot should defy the laws of gravity or not. </summary>
    public bool IsFloating { get; set; }

    /// <summary> Just a visual property that determines how strong the opacity of the dot is. </summary>
    public bool Visible { get; set; } = true;

    public double LatestIntervalPos(bool deviceEnabled)
        => deviceEnabled
            ? Math.Round((_useDragLoopData ? _dragLoopData[_dragLoopPlaybackIdx] : PosHistory[0]) / Motor.Interval) * Motor.Interval
            : 0.0;

    // Lightweight cleanup method to be used whenever playbacks finish.
    public void OnPlaybackEnd()
    {
        PlaybackRef = new();
        RecordedData.Clear();
        _dragLoopPlaybackIdx = 0;
        Visible = true;
        IsDragging = false;
    }

    // Fully cleans out the data and stops everything.
    public void ClearData()
    {
        OnPlaybackEnd();
        PosHistory.Clear();
        _dragLoopData.Clear();
        _dragLoopPlaybackIdx = 0;
        _useDragLoopData = false;
        IsLooping = false;
        IsDragging = false;
        IsFloating = false;
        Visible = true;
    }

    /// <summary>
    ///     Adds the latest position of the motorDot to the PosHistory. <para/>
    ///     If <paramref name="deviceEnabled"/> is false, store 0.0 instead.
    /// </summary>
    public void AddPosToHistory(bool deviceEnabled)
    {
        var posToPush = !deviceEnabled
            ? 0.0 : PlaybackRef.Idx != -1
                ? RecordedData[PlaybackRef.Idx] : (_useDragLoopData ? _dragLoopData[_dragLoopPlaybackIdx] : Position[1]);

        PosHistory.PushFront(posToPush);

        // if we recorded from the loop cache, be sure to increment it.
        if (_useDragLoopData)
        {
            _dragLoopPlaybackIdx++;
            if (_dragLoopPlaybackIdx >= _dragLoopData.Count)
                _dragLoopPlaybackIdx = 0;
        }

        // add to cache if recording a loop atm.
        if (_looping && _dragging)
            _dragLoopData.Add(Position[1]);
    }

    /// <summary> 
    ///     Records the latest position to the recorded data. <para/>
    ///     Records 0.0 if <paramref name="deviceEnabled"/> is false.
    /// </summary>
    /// <returns> True an the latest data is a different interval from the last recorded one. </returns>
    public bool RecordPosition(bool deviceEnabled)
    {
        var posToRecord = deviceEnabled ? LatestIntervalPos(deviceEnabled) : 0.0;
        var differentValue = RecordedData.LastOrDefault() != posToRecord;
        RecordedData.Add(posToRecord);
        return differentValue;
    }

    // UNKNOWN HOW TO INCORPORATE THIS INTO THE NEW FRAMEWORK.
    public void InjectPlaybackData(IEnumerable<double> playbackData, RemotePlaybackRef playbackRef)
    {
        // set a reference so that we can easily access the intensity at the right point for sending off recorded data.
        PlaybackRef = playbackRef;
        // inject the recorded data for the motor.
        RecordedData.Clear();
        RecordedData.AddRange(playbackData);
        Svc.Logger.Verbose("Recorded Positions Injected!");
    }

    public bool Equals(MotorDot? other)
    {
        if (other is null) return false;
        return ReferenceEquals(Motor, other.Motor);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is not MotorDot other) return false;
        return ReferenceEquals(Motor, other.Motor);
    }

    public override int GetHashCode()
        => Motor.GetHashCode();
}
