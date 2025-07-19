using GagspeakAPI.Attributes;
using GagspeakAPI.Network;

namespace GagSpeak.State.Models;

public abstract class BuzzToy : IDisposable
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
    public abstract ToyBrandName FactoryName { get; protected set; }

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
    public bool CanVibrate => _motorTypeMap.ContainsKey(ToyMotor.Vibration);
    public bool CanOscillate => _motorTypeMap.ContainsKey(ToyMotor.Oscillation);
    public bool CanRotate => _motorTypeMap.ContainsKey(ToyMotor.Rotation);
    public bool CanConstrict => _motorTypeMap.ContainsKey(ToyMotor.Constriction);
    public bool CanInflate => _motorTypeMap.ContainsKey(ToyMotor.Inflation);

    // Motors via motor mapping.
    /// <summary>
    ///     An internal Motor mapping for efficient Motor access via MotorIdx.
    /// </summary>
    protected readonly Dictionary<uint, BuzzToyMotor> _motorMap = new Dictionary<uint, BuzzToyMotor>();

    /// <summary>
    ///     Internal motor mapping by type for efficient access.
    /// </summary>
    protected readonly Dictionary<ToyMotor, List<BuzzToyMotor>> _motorTypeMap = new();

    /// <summary>
    ///     Public read-only accessor (dont by reference so no data duplication). <para />
    ///     Efficient access to all buzzToyMotors via their MotorIdx. (Useful for datastreams)
    /// </summary>
    public IReadOnlyDictionary<uint, BuzzToyMotor> MotorMap => _motorMap;

    public IReadOnlyDictionary<ToyMotor, List<BuzzToyMotor>> MotorTypeMap => _motorTypeMap;

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

        foreach (var motor in _motorTypeMap[ToyMotor.Vibration])
            motor.Intensity = intensity;
        return true;
    }

    /// <summary>
    ///     Set the intensity of a specific motor.
    /// </summary>
    public virtual bool Vibrate(uint motorIdx, double intensity)
    {
        if (!_motorMap.TryGetValue(motorIdx, out var m) || m.Type != ToyMotor.Vibration)
            return false;

        m.Intensity = intensity;
        return true;
    }

    /// <summary>
    ///     Set all Oscillations to the defined <paramref name="speed"/>
    /// </summary>
    public virtual bool OscillateAll(double speed)
    {
        if (!CanOscillate)
            return false;

        foreach (var motor in _motorTypeMap[ToyMotor.Oscillation])
            motor.Intensity = speed;
        return true;
    }

    /// <summary>
    ///    Set the speed of a specific oscillation motor.
    /// </summary>
    public virtual bool Oscillate(uint motorIdx, double speed)
    {
        if (!_motorMap.TryGetValue(motorIdx, out var m) || m.Type != ToyMotor.Oscillation)
            return false;

        m.Intensity = speed;
        return true;
    }

    /// <summary>
    ///     Update the speed and direction of the rotation motor.
    /// </summary>
    public virtual bool Rotate(double speed, bool clockwise)
    {
        if (!CanRotate)
            return false;

        _motorTypeMap[ToyMotor.Rotation][0].Intensity = speed;
        return true;
    }

    /// <summary>
    ///     Update the severity of the constriction motor.
    /// </summary>
    public virtual bool Constrict(double severity)
    {
        if (!CanConstrict)
            return false;

        _motorTypeMap[ToyMotor.Constriction][0].Intensity = severity;
        return true;
    }

    /// <summary>
    ///     Update the severity of the inflation motor.
    /// </summary>
    public virtual bool Inflate(double severity)
    {
        if (!CanInflate)
            return false;

        _motorTypeMap[ToyMotor.Inflation][0].Intensity = severity;
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
            // Motor Mapping serialization
            ["DeviceMotors"] = new JArray(_motorMap.Values.Select(m => m.SerializeCompact())),
        };
    }

    public ToyInfo ToToyInfo()
        => new ToyInfo(FactoryName, Interactable, MotorMap.ToDictionary(i => i.Key, m => new Motor(m.Value.Type, m.Key, (int)m.Value.StepCount)));
}
