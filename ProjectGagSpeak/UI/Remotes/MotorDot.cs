using CkCommons.Classes;
using GagSpeak.State.Models;

namespace GagSpeak.Gui.Remote;

// NOTE: Maybe replace lists with stacks for better performance? idk.

/// <summary>
///     Represents a single plot point in a SexToyRemote used to control a Toy's Motor.
/// </summary>
/// <remarks> 
///     The <see cref="SexToyMotor"/> associated with the MotorDot is intended to be 
///     defined by <see cref="DevicePlotState"/>, which holds the <see cref="BuzzToy"/>'s data.
/// </remarks>
public class MotorDot(SexToyMotor motor)
{
    private SexToyMotor _motor { get; } = motor;

    /// <summary> Cache holding position data during a drag, when <see cref="IsLooping"/> is true. </summary>
    private List<double> _cachedLoopData = new();

    /// <summary> The index of <see cref="_cachedLoopData"/> being played back. </summary>
    private int _loopPlaybackIdx = 0;

    /// <summary> If the latest insertion should grab the loop cache or the position </summary>
    private bool _prioritizeLoopCache => IsLooping && !IsDragging && _cachedLoopData.Count > 0;

    /// <summary> The current Position of the MotorDot on the PlotGraph. </summary>
    public double[] Position = new double[2];

    /// <summary> A rolling buffer array of the last 10s of recorded positions </summary>
    /// <remarks> Should add a position here every time the DragPoint/Position changes. </remarks>
    public RollingBuffer<double> PosHistory { get; private set; } = new(1000);

    /// <summary> The final data that will be saved into a pattern, or played back to the client. </summary>
    /// <remarks> This is not for UI Display, and occurs on a set interval you define. </remarks>
    public List<double> RecordedData { get; private set; } = new();

    /// <summary> If we are moving the MotorDot around. </summary>
    public bool IsDragging { get; set; }

    /// <summary> 
    ///     Determines if the MotorDot is recording data in loop mode. <para/>
    ///     While in loop mode, all data from the start to the end of a dragged motion is played back repeatedly.
    /// </summary>
    /// <remarks> Looping playback stops upon being false, or starting another drag loop. </remarks>
    public bool IsLooping { get; set; }

    /// <summary> Determines if the MotorDot should defy the laws of gravity or not. </summary>
    public bool IsFloating { get; set; }

    /// <summary> Determines if the MotorDot is displayed in the Plot Graph. </summary>
    /// <remarks> Positions are still updated when not visible. </remarks>
    public bool Visible { get; set; } = true;

    public double GetLastMotorPos() => _prioritizeLoopCache 
        ? _cachedLoopData[_loopPlaybackIdx] : Position[1];

    public double GetLastMotorIntervalPos() => _prioritizeLoopCache 
        ? _cachedLoopData[_loopPlaybackIdx] : Math.Round(Position[1] / _motor.Interval) * _motor.Interval;

    public void ClearData(bool keepRecordedData)
    {
        _cachedLoopData.Clear();
        PosHistory.Clear();
        if (!keepRecordedData)
            RecordedData.Clear();
        Svc.Logger.Verbose("MotorDot Data Cleared!");
    }

    /// <summary> Begin the dragging state on a motor. </summary>
    /// <remarks> Resets <see cref="_loopPlaybackIdx"/> and <see cref="_cachedLoopData"/> to record a new loop if looping. </remarks>
    public void BeginDrag()
    {
        IsDragging = true;
        if (IsLooping)
        {
            // reset the loop idx and stored loop data to begin saving a new loop.
            _loopPlaybackIdx = 0;
            _cachedLoopData.Clear();
        }
        Svc.Logger.Verbose("Dragging Period Started!");
    }

    /// <summary> Ends the dragging state on a motor. </summary>
    /// <remarks> If looping, resets <see cref="_loopPlaybackIdx"/> but keeps <see cref="_cachedLoopData"/> so it can be played back. </remarks>
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
    ///     Adds the latest position to the PosDisplayHistory. <para/>
    ///     
    ///     This should be used for handling the <see cref="PosDisplayHistory"/> and 
    ///     <see cref="_cachedLoopData"/>, as they are not interval dependant
    /// </summary>
    public void AddPosToHistory()
    {
        var pos= _prioritizeLoopCache ? _cachedLoopData[_loopPlaybackIdx] : Position[1];
        PosHistory.PushFront(pos);
        // add to cache if recording a loop atm.
        if (IsLooping && IsDragging)
            _cachedLoopData.Add(Position[1]);
    }

    /// <summary> Updates the pos with latest data. </summary>
    /// <returns> True if it was different from the last sent data, false otherwise. </returns>
    /// <remarks> Handles <see cref="_loopPlaybackIdx"/> automatically for the looped cache. </remarks>
    public bool RecordPosition()
    {
        // if we are not looping, or looping and not
        var posToRecord = _prioritizeLoopCache ? _cachedLoopData[_loopPlaybackIdx] 
            : Math.Round(Position[1] / _motor.Interval) * _motor.Interval;

        var differentValue = RecordedData.LastOrDefault() != posToRecord;
        RecordedData.Add(posToRecord);

        // Handle playback buffer for loops.
        if (_prioritizeLoopCache)
        {
            _loopPlaybackIdx++;
            if (_loopPlaybackIdx >= _cachedLoopData.Count)
                _loopPlaybackIdx = 0;
        }
        return differentValue;
    }

    public void InjectRecordedPositions(IEnumerable<double> positions)
    {
        RecordedData.Clear();
        RecordedData.AddRange(positions);
        _cachedLoopData.Clear();
        _loopPlaybackIdx = 0;
        Svc.Logger.Verbose("Recorded Positions Injected!");
    }
}
