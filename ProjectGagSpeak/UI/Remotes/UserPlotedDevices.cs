using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.Gui.Remote;

// The source being used to execute remote actions.
public enum RemoteSource
{
    Safeword,
    External,
    PowerOn,
    UpdateTick,
    PowerOff,
    PatternSwitch,
}

public class UserPlotedDevices
{
    protected readonly ILogger Log;
    protected readonly GagspeakMediator Mediator;

    protected HashSet<DeviceDot> _devices = new();
    protected Stopwatch _timeAlive = new();
    public UserPlotedDevices(ILogger log, GagspeakMediator mediator, RoomParticipantBase user, RemoteAccess access)
    {
        Log = log;
        Mediator = mediator;
        Owner = user;
        Access = access;
    }

    public RemoteAccess Access { get; protected set; } = RemoteAccess.Previewing;
    public bool CanControl => _devices.Count > 0;
    public bool RemotePowerActive { get; private set; } = false;
    public bool UserIsBeingBuzzed => RemotePowerActive && _timeAlive.IsRunning;
    public TimeSpan TimeAlive => _timeAlive.Elapsed;

    public readonly RoomParticipantBase Owner;
    public IReadOnlySet<DeviceDot> Devices => _devices;

    public bool CanSetPower(bool desiredState)
    {
        // if already at desired state, refuse.
        if (desiredState == RemotePowerActive)
            return false;
        // if we want to turn it on but cannot control them, refuse.
        else if (desiredState && !CanControl)
            return false;
        // otherwise, valid.
        return true;
    }

    public bool TrySetRemotePower(bool newValue, string enactor)
    {
        if (!CanSetPower(newValue))
            return false;

        SetPowerInternal(newValue, enactor);
        return true;
    }

    // Internal method used to set power without safety checks, use only if you know what you're doing.
    protected void SetPowerInternal(bool newValue, string enactor)
    {
        if (newValue)
        {
            RemotePowerActive = true;
            OnControlBegin(enactor);
        }
        else
        {
            RemotePowerActive = false;
            OnControlEnd(enactor);
        }
    }

    public bool AddDevice(DeviceDot device)
    {
        if (!_devices.Add(device))
        {
            Log.LogWarning($"Failed to add device to {Owner.DisplayName}'s remote data, as it already exists.");
            return false;
        }
        Log.LogTrace($"Added Device to {Owner.DisplayName}'s remote data.");
        return true;
    }

    public bool RemoveDevice(DeviceDot device)
    {
        // probably a good idea to grab the item from here and clean it up first ye?
        device.CleanupData();
        Log.LogInformation($"Powered down device {device.FactoryName} for kinkster.");
        if (!_devices.Remove(device))
        {
            Log.LogWarning($"Failed to remove device from {Owner.DisplayName}'s remote data, as it does not exist.");
            return false;
        }
        Log.LogTrace($"Removed Device from {Owner.DisplayName}'s remote data.");
        return true;
    }

    public void RemoveAll()
    {
        // cleanup and remove all data within.
        foreach (var device in _devices)
        {
            device.CleanupData();
            Log.LogInformation($"Powered down device {device.FactoryName} for kinkster.");
            _devices.Remove(device);
        }
        Log.LogTrace($"Removed all devices from their remote data.");
    }

    protected virtual void OnControlBegin(string enactor) => _timeAlive.Start();
    protected virtual void OnControlEnd(string enactor) => _timeAlive.Reset();

    /// <summary>
    ///     The update loop task is the essential component of a user's device data. <para />
    ///     Once the timer is running, the update loop task will execute every 20milliseconds. <para />
    ///     Data will be played back, recorded, or update devices based on various conditions. <para />
    /// </summary>
    public virtual void OnUpdateTick()
        => throw new NotImplementedException("OnUpdateTick must be implemented in derived classes.");

}
