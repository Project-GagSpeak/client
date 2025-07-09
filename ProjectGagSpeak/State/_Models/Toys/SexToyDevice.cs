using Buttplug.Core.Messages;

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
    public bool Interactable { get; set; } = false;

    // Validators.
    public bool CanVibrate => VibeMotors.Length > 0;
    public bool CanOscillate => OscillateMotors.Length > 0;
    public bool CanRotate => RotateMotor.MotorIdx != uint.MaxValue;
    public bool CanConstrict => RotateMotor.MotorIdx != uint.MaxValue;
    public bool CanInflate => RotateMotor.MotorIdx != uint.MaxValue;

    public int VibeMotorCount => VibeMotors.Length;
    public int OscillateMotorCount => OscillateMotors.Length;

    public SexToyMotor[] VibeMotors { get; protected set; } = Array.Empty<SexToyMotor>();
    public SexToyMotor[] OscillateMotors { get; protected set; } = Array.Empty<SexToyMotor>();
    public SexToyMotor RotateMotor { get; protected set; } = SexToyMotor.Empty;
    public SexToyMotor ConstrictMotor { get; protected set; } = SexToyMotor.Empty;
    public SexToyMotor InflateMotor { get; protected set; } = SexToyMotor.Empty;

    public virtual void Dispose() { }

    /// <summary>
    ///     Halts all current activity on the device.
    /// </summary>
    public void StopAllMotors()
    {
        VibrateAll(0.0);
        OscillateAll(0.0);
        Rotate(0.0, true);
        Constrict(0.0);
        Inflate(0.0);
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
    public virtual void Vibrate(uint motorIdx, double intensity)
    {
        if (!CanVibrate || motorIdx > OscillateMotorCount)
            return;

        VibeMotors[motorIdx].Intensity = intensity;
    }

    /// <summary>
    ///     Update all motors to their new unique intensity at once.
    /// </summary>
    /// <remarks> This will likely never happen as its difficult to encode. </remarks>
    public virtual void VibrateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
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
    public virtual void Oscillate(uint motorIdx, double speed)
    {
        if (!CanOscillate || motorIdx > OscillateMotorCount)
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
    ///     Update the speed and direction of the rotation motor.
    /// </summary>
    public virtual void Rotate(double speed, bool clockwise)
    {
        if (!CanRotate)
            return;
        
        RotateMotor.Intensity = speed;
    }

    /// <summary>
    ///     Update the severity of the constriction motor.
    /// </summary>
    public virtual void Constrict(double severity)
    {
        if (!CanConstrict)
            return;

        ConstrictMotor.Intensity = severity;
    }

    /// <summary>
    ///     Update the severity of the inflation motor.
    /// </summary>
    public virtual void Inflate(double severity)
    {
        if (!CanInflate)
            return;

        RotateMotor.Intensity = severity;
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
            ["Id"] = Id.ToString(),
            ["FactoryName"] = FactoryName,
            ["LabelName"] = LabelName,
            ["BatteryLevel"] = BatteryLevel,
            ["Interactable"] = Interactable,
            ["VibeMotors"] = new JArray(VibeMotors.Select(m => m.SerializeCompact())),
            ["OscillateMotors"] = new JArray(OscillateMotors.Select(m => m.SerializeCompact())),
            ["RotateMotor"] = RotateMotor.SerializeCompact(),
            ["ConstrictMotor"] = ConstrictMotor.SerializeCompact(),
            ["InflateMotor"] = InflateMotor.SerializeCompact(),
        };
    }
}
