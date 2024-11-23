using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Data;


[MessagePackObject(keyAsPropertyName: true)]
public record DeviceUpdate
{
    // The device we are updating. If blank, sending to all Connected Devices.
    public string DeviceName { get; set; } = string.Empty;

    // the motor it is being applied to. -1 for all motors.
    public int MotorAffected { get; set; } = -1;

    // the intensity of the vibration. 0-100
    public byte Intensity { get; set; } = 0;

    // if type is rotate, reference this for the direction of rotation.
    public bool Clockwise { get; set; } = true;
}

