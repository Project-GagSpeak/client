using Buttplug.Core.Messages;
using CkCommons;
using Dalamud.Game.ClientState.Keys;
using GagSpeak.Interop;

namespace GagSpeak.State.Models;

public abstract class BuzzToy : IDisposable
{
    /// <summary>
    ///     Determines the kind of connected device
    /// </summary>
    public abstract SexToyType Type { get; }

    /// <summary>
    ///     Unique identifier for the BuzzToy, useful for maintaining data
    ///     loaded from config storage.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    ///     Factory Name of the SexToy [Lovense Hush]
    /// </summary>
    /// <remarks> This is static and pre-assigned. (its Identifier) </remarks>
    public string FactoryName { get; protected set; } = string.Empty;

    /// <summary> 
    ///     The labeled name given for the connected device.
    /// </summary>
    /// <remarks> Used for display in the UI. </remarks>
    public string LabelName { get; set; } = string.Empty;

    /// <summary>
    ///     The current battery level of the device.
    /// </summary>
    /// <remarks> This should be modified by a battery level fetch task. </remarks>
    public double BatteryLevel { get; protected set; } = -1.0;

    /// <summary>
    ///     If the device can be interacted with. <para/>
    ///     (An indicator for other kinksters and toy manager)
    /// </summary>
    public bool CanInteract { get; set; } = false;

    // Validators.
    public bool CanVibrate => VibeMotors.Length > 0;
    public bool CanRotate => RotateMotors.Length > 0;
    public bool CanOscillate => OscillateMotors.Length > 0;
    public int VibeMotorCount => VibeMotors.Length;
    public int RotateMotorCount => RotateMotors.Length;
    public int OscillateMotorCount => OscillateMotors.Length;

    /// <summary>
    ///    The motors that are used for vibration.
    /// </summary>
    public SexToyMotor[] VibeMotors { get; protected set; } = Array.Empty<SexToyMotor>();

    /// <summary>
    ///    The motors that are used for rotation.
    /// </summary>
    public SexToyMotor[] RotateMotors { get; protected set; } = Array.Empty<SexToyMotor>();

    /// <summary>
    ///     The motors that are used for oscillation.
    /// </summary>
    public SexToyMotor[] OscillateMotors { get; protected set; } = Array.Empty<SexToyMotor>();

    public virtual void Dispose()
    {
        // Any Disposal logic here.
    }

    /// <summary>
    ///     Halts all current activity on the device.
    /// </summary>
    public virtual void StopAllMotors()
    {
        if (CanVibrate)
            VibrateAll(0.0);

        if (CanRotate)
            RotateAll(0.0, true);

        if (CanOscillate)
            OscillateAll(0.0);
    }

    /// <summary>
    ///     Set all VibeMotors to the defined <paramref name="intensity"/>
    /// </summary>
    public virtual void VibrateAll(double intensity)
    {
        if (!CanVibrate)
            return;

        foreach (var motor in VibeMotors)
            motor.Intensity = intensity;
    }

    /// <summary>
    ///     Set the intensity of a specific motor.
    /// </summary>
    public virtual void Vibrate(int motorIdx, double intensity)
    {
        if (!CanVibrate || !motorIdx.IsInRange(VibeMotors))
            return;

        VibeMotors[motorIdx].Intensity = intensity;
    }

    /// <summary>
    ///     Update all motors to their new unique intensity at once.
    /// </summary>
    /// <remarks> This will likely never happen as its difficult to encode. </remarks>
    public virtual void ViberateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!CanVibrate)
            return;

        foreach (var cmd in newValues)
            VibeMotors[cmd.index].Intensity = cmd.scalar;
    }

    /// <summary>
    ///     Set all Oscillations to the defined <paramref name="speed"/>
    /// </summary>
    public virtual void OscillateAll(double speed)
    {
        if (!CanOscillate)
            return;

        foreach (var motor in OscillateMotors)
            motor.Intensity = speed;
    }

    /// <summary>
    ///    Set the speed of a specific oscillation motor.
    /// </summary>
    public virtual void Oscillate(int motorIdx, double speed)
    {
        if (!CanOscillate || !motorIdx.IsInRange(OscillateMotors))
            return;

        OscillateMotors[motorIdx].Intensity = speed;
    }

    /// <summary>
    ///     Update all motors to their new unique Oscillation at once.
    /// </summary>
    /// <remarks> This will likely never happen as its difficult to encode. </remarks>
    public virtual void OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!CanOscillate)
            return;

        foreach (var cmd in newValues)
            OscillateMotors[cmd.index].Intensity = cmd.scalar;
    }

    /// <summary>
    ///     Update the rotation speed of all rotation motors, in the defined direction.
    /// </summary>
    public virtual void RotateAll(double speed, bool clockwise)
    {
        if (!CanRotate)
            return;

        foreach (var motor in RotateMotors)
            motor.Intensity = speed;
    }

    /// <summary>
    ///     Updates the battery level of the device.
    /// </summary>
    public abstract Task UpdateBattery();

    public virtual JObject Serialize()
    {
        return new JObject
        {
            ["Type"] = Type.ToString(),
            ["FactoryName"] = FactoryName,
            ["LabelName"] = LabelName,
            ["BatteryLevel"] = BatteryLevel,
            ["CanInteract"] = CanInteract,
            ["VibeMotors"] = JArray.FromObject(VibeMotors.Select(m => m.StepCount)),
            ["RotateMotors"] = JArray.FromObject(RotateMotors.Select(m => m.StepCount)),
            ["OscillateMotors"] = JArray.FromObject(OscillateMotors.Select(m => m.StepCount)),
        };
    }
}
