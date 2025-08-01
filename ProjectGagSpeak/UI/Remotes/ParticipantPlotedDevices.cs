using GagSpeak.Services.Mediator;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.Gui.Remote;

public sealed class ParticipantPlotedDevices : UserPlotedDevices
{
    public TimeSpan LastCompileTime { get; private set; } = TimeSpan.Zero;
    public ParticipantPlotedDevices(ILogger log, GagspeakMediator mediator, RoomParticipantBase user)
        : base(log, mediator, user, RemoteAccess.Previewing)
    { }

    protected override void OnControlBegin(string enactor)
    {
        // fire any achievements related to starting control on another participant's devices.

        // do the base startup control logic
        _timeAlive.Start();
    }

    protected override void OnControlEnd(string enactor)
    {
        // stop the timer.
        _timeAlive.Stop();
        // fire any achievements related to ending control on another participant's devices.
    }

    public override void OnUpdateTick()
    {
        // run inside a generic safe task to ensure cancellation and exceptions are handled.
        foreach (var device in _devices)
            device.RecordAndUpdatePosition();
    }

    // Compiles the latest datastream from the participant to be sent off.
    public UserDeviceStream CompileFromRecordingForUser()
    {
        var deviceStreams = new List<DeviceStream>();
        // Compile together the full pattern data from all devices recorded data.
        foreach (var device in _devices)
        {
            // compile the motor data for this device.
            var motorData = device.MotorDotMap.Values
                .Select(m => new MotorStream(m.Motor.Type, m.Motor.MotorIdx, m.RecordedData.ToArray()))
                .ToArray();
            // add the device data if it exists.
            if (motorData.Length > 0)
                deviceStreams.Add(new DeviceStream(device.FactoryName, motorData));
        }

        return new UserDeviceStream(Owner.User, deviceStreams.ToArray());
    }
}
