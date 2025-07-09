using GagSpeak.State.Models;

namespace GagSpeak.Gui.Remote;

/// <summary>
///    The state of a <see cref="BuzzToy"/>, whose motors are to be drawn in a ImPlot chart for recording or playback.
/// </summary>
public class DevicePlotState
{
    /// <summary> The <see cref="BuzzToy"/> the attached <see cref="MotorDot"/>'s stem from. </summary>
    public BuzzToy Device { get; }

    public List<MotorDot> VibeDots { get; } = new();
    public List<MotorDot> OscillateDots { get; } = new();
    public MotorDot RotateDot { get; } = new(SexToyMotor.Empty);
    public MotorDot ConstrictDot { get; } = new(SexToyMotor.Empty);
    public MotorDot InflateDot { get; } = new(SexToyMotor.Empty);

    /// <summary> If this Device is currently powered on. </summary>
    /// <remarks> A device not powered on cannot send updates to toys, and they will remain dormant. </remarks>
    public bool IsPoweredOn { get; set; }

    /// <summary> A control for devices with a rotation motor, to direct the rotation movement. </summary>
    public bool IsClockwise { get; set; } = true;

    public DevicePlotState(BuzzToy device)
    {
        Device = device;
        VibeDots.AddRange(device.VibeMotors.Select(m => new MotorDot(m)));
        OscillateDots.AddRange(device.OscillateMotors.Select(m => new MotorDot(m)));
        if (device.CanRotate)
            RotateDot = new MotorDot(device.RotateMotor);
        if (device.CanConstrict)
            ConstrictDot = new MotorDot(device.ConstrictMotor);
        if (device.CanInflate)
            InflateDot = new MotorDot(device.InflateMotor);
    }

    public void SendLatestToToys()
    {
        // Update Vibe Motors
        for (var i = 0; i < VibeDots.Count; i++)
            VibeDots[i].TrySendLatestValue((newValue) => Device.Vibrate((uint)i, newValue));

        // Update Oscillation Motors
        for (var i = 0; i < OscillateDots.Count; i++)
            OscillateDots[i].TrySendLatestValue((newValue) => Device.Oscillate((uint)i, newValue));

        // Update Rotation Motor
        if (Device.CanRotate)
            RotateDot.TrySendLatestValue((newValue) => Device.Rotate(newValue, RotateDot.IsFloating));

        // Update Constrict Motor
        if (Device.CanConstrict)
            ConstrictDot.TrySendLatestValue((newValue) => Device.Constrict(newValue));

        // Update Inflate Motor
        if (Device.CanInflate)
            InflateDot.TrySendLatestValue((newValue) => Device.Inflate(newValue));
    }

    public void PowerDown()
    {
        if (!IsPoweredOn)
            return;
        // power down by stopping all motors.
        Device.StopAllMotors();
    }
}
