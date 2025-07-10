using CkCommons.Classes;
using GagSpeak.State.Models;

namespace GagSpeak.Gui.Remote;

/// <summary>
///    The state of a <see cref="BuzzToy"/>, whose motors are to be drawn in a ImPlot chart for recording or playback.
/// </summary>
public class DevicePlotState : IEquatable<DevicePlotState>
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
            if(VibeDots[i].RecordPosition())
                Device.Vibrate((uint)i, VibeDots[i].RecordedData.LastOrDefault());

        // Update Oscillation Motors
        for (var i = 0; i < OscillateDots.Count; i++)
            if (OscillateDots[i].RecordPosition())
                Device.Vibrate((uint)i, OscillateDots[i].RecordedData.LastOrDefault());

        // Update Rotation Motor
        if (Device.CanRotate && RotateDot.RecordPosition())
            Device.Rotate(RotateDot.RecordedData.LastOrDefault(), IsClockwise);

        // Update Constrict Motor
        if (Device.CanConstrict && ConstrictDot.RecordPosition())
            Device.Constrict(ConstrictDot.RecordedData.LastOrDefault());

        // Update Inflate Motor
        if (Device.CanInflate && InflateDot.RecordPosition())
            Device.Inflate(InflateDot.RecordedData.LastOrDefault());
    }

    public void ThrottlePower()
    {
        if (IsPoweredOn)
            PowerDown();
        else
            IsPoweredOn = true;
    }

    public void PowerDown()
    {
        if (!IsPoweredOn)
            return;
        // power down by stopping all motors.
        Svc.Logger.Verbose($"Powering down device {Device.FactoryName} ({Device.LabelName})");
        Device.StopAllMotors();
        IsPoweredOn = false;
    }

    public static bool operator ==(DevicePlotState? left, DevicePlotState? right)
    {
        // Same reference or both null → true
        if (ReferenceEquals(left, right))
            return true;

        // One is null → false
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(DevicePlotState? left, DevicePlotState? right)
    {
        return !(left == right);
    }

    public bool Equals(DevicePlotState? other)
    {
        if (other is null)
            return false;

        // Check if Device or Device.Id are null
        return Device?.Id == other.Device?.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is DevicePlotState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Device?.Id.GetHashCode() ?? 0;
    }
}
