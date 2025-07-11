using Buttplug.Core.Messages;
using GagSpeak.Utils;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

public abstract class BuzzToy : IDisposable, IEditableStorageItem<BuzzToy>
{
    /// <summary>
    ///     Determines the kind of connected device
    /// </summary>
    public abstract SexToyType Type { get; }

    /// <summary>
    ///     If a device is currently valid or not.
    /// </summary>
    public abstract bool ValidForRemotes { get; }

    /// <summary>
    ///     Unique identifier for the BuzzToy, useful for maintaining data
    ///     loaded from config storage.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    ///     Factory Name of the SexToy [Lovense Hush]
    /// </summary>
    /// <remarks> This is static and pre-assigned. (its Identifier) </remarks>
    public abstract CoreIntifaceElement FactoryName { get; protected set; }

    /// <summary> 
    ///     The labeled name given for the connected device.
    /// </summary>
    /// <remarks> Used for display in the UI. </remarks>
    public abstract string LabelName { get; set; }

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

    public BuzzToy()
    { }

    public BuzzToy(BuzzToy other, bool keepId)
    {
        Id = keepId ? other.Id : Guid.NewGuid();
        ApplyChanges(other);
    }

    public abstract BuzzToy Clone(bool keepId);

    public virtual void ApplyChanges(BuzzToy other)
    {
        LabelName = other.LabelName;
        BatteryLevel = other.BatteryLevel;
        Interactable = other.Interactable;
        VibeMotors = other.VibeMotors.Select(m => new SexToyMotor(m)).ToArray();
        OscillateMotors = other.OscillateMotors.Select(m => new SexToyMotor(m)).ToArray();
        RotateMotor = new SexToyMotor(other.RotateMotor);
        ConstrictMotor = new SexToyMotor(other.ConstrictMotor);
        InflateMotor = new SexToyMotor(other.InflateMotor);
    }

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
    public virtual bool VibrateAll(double intensity)
    {
        if (!CanVibrate)
            return false;

        foreach (var motor in VibeMotors)
            motor.Intensity = intensity;
        return true;
    }

    /// <summary>
    ///     Set the intensity of a specific motor.
    /// </summary>
    public virtual bool Vibrate(uint motorIdx, double intensity)
    {
        if (!CanVibrate || motorIdx > VibeMotorCount)
            return false;

        VibeMotors[motorIdx].Intensity = intensity;
        return true;
    }

    /// <summary>
    ///     Update all motors to their new unique intensity at once.
    /// </summary>
    /// <remarks> This will likely never happen as its difficult to encode. </remarks>
    public virtual bool VibrateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!CanVibrate)
            return false;

        foreach (var cmd in newValues)
            VibeMotors[cmd.index].Intensity = cmd.scalar;
        return true;
    }

    /// <summary>
    ///     Set all Oscillations to the defined <paramref name="speed"/>
    /// </summary>
    public virtual bool OscillateAll(double speed)
    {
        if (!CanOscillate)
            return false;

        foreach (var motor in OscillateMotors)
            motor.Intensity = speed;
        return true;
    }

    /// <summary>
    ///    Set the speed of a specific oscillation motor.
    /// </summary>
    public virtual bool Oscillate(uint motorIdx, double speed)
    {
        if (!CanOscillate || motorIdx > OscillateMotorCount)
            return false;

        OscillateMotors[motorIdx].Intensity = speed;
        return true;
    }

    /// <summary>
    ///     Update all motors to their new unique Oscillation at once.
    /// </summary>
    /// <remarks> This will likely never happen as its difficult to encode. </remarks>
    public virtual bool OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!CanOscillate)
            return false;

        foreach (var cmd in newValues)
            OscillateMotors[cmd.index].Intensity = cmd.scalar;
        return true;
    }

    /// <summary>
    ///     Update the speed and direction of the rotation motor.
    /// </summary>
    public virtual bool Rotate(double speed, bool clockwise)
    {
        if (!CanRotate)
            return false;
        
        RotateMotor.Intensity = speed;
        return true;
    }

    /// <summary>
    ///     Update the severity of the constriction motor.
    /// </summary>
    public virtual bool Constrict(double severity)
    {
        if (!CanConstrict)
            return false;

        ConstrictMotor.Intensity = severity;
        return true;
    }

    /// <summary>
    ///     Update the severity of the inflation motor.
    /// </summary>
    public virtual bool Inflate(double severity)
    {
        if (!CanInflate)
            return false;

        RotateMotor.Intensity = severity;
        return true;
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
            ["FactoryName"] = FactoryName.ToString(),
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
