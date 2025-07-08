using Buttplug.Client;
using Buttplug.Core.Messages;
using GagSpeak.Interop;
using GagSpeak.Services;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;

namespace GagSpeak.State.Handlers;
public sealed class BuzzToyHandler
{
    private readonly ILogger<BuzzToyHandler> _logger;
    private readonly IpcCallerIntiface _ipc;
    private readonly BuzzToyManager _manager;
    private readonly VibeSimService _vibeSim;

    public BuzzToyHandler(ILogger<BuzzToyHandler> logger, IpcCallerIntiface ipc,
        BuzzToyManager manager, VibeSimService vibeSim)
    {
        _logger = logger;
        _ipc = ipc;
        _manager = manager;
        _vibeSim = vibeSim;
    }

    public void AddDevice(VirtualBuzzToy newToy)
        => _manager.AddDevice(newToy);

    public void AddOrUpdateDevice(ButtplugClientDevice newToy)
        => _manager.AddOrUpdateDevice(newToy);
    public void RemoveDevice(BuzzToy device)
        => _manager.RemoveDevice(device);

    /// <summary>
    ///     Stop ALL motors for ALL devices.
    /// </summary>
    public void StopAllDevices()
    {
        _logger.LogInformation("Stopping all connected devices.", LoggerType.Toys);
        foreach (var toy in _manager.SexToys.Values)
            toy.StopAllMotors();
    }

    public void StopAllMotors(Guid deviceId)
    {
        if (_manager.SexToys.TryGetValue(deviceId, out var toy))
        {
            _logger.LogInformation($"Stopping all motors for device: {toy.LabelName}", LoggerType.Toys);
            toy.StopAllMotors();
        }
        else
        {
            _logger.LogWarning($"Device for ID {deviceId} not found when trying to stop all motors.", LoggerType.Toys);
        }
    }

    public void VibrateAll(double intensity)
    {
        foreach (var toy in _manager.SexToys.Values)
            toy.VibrateAll(intensity);
    }

    public void VibrateAll(Guid deviceId, double intensity)
    {

    }

    public void Vibrate(Guid deviceId, int motorIdx, double intensity)
    {

    }

    public void ViberateDistinct(Guid deviceId, IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {

    }

    public void OscillateAll(Guid deviceId, double speed)
    {

    }

    public void Oscillate(Guid deviceId, int motorIdx, double speed)
    {

    }

    public void OscillateDistinct(Guid deviceId, IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {

    }

    public void RotateAll(Guid deviceId, double speed, bool clockwise)
    {

    }

    public void StartBatteryCheck()
        => _manager.StartBatteryCheck();

    public void StopBatteryCheck()
        => _manager.StopBatteryCheck();

    public static string VibeSimAudioPath(VibeSimType type)
    {
        return type switch
        {
            VibeSimType.Normal => "vibrator.wav",
            VibeSimType.Quiet => "vibratorQuiet.wav",
            _ => "vibratorQuiet.wav",
        };
    }
}





