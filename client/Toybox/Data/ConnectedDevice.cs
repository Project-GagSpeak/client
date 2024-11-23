using Buttplug.Client;
using Buttplug.Core.Messages;
using DebounceThrottle;

namespace GagSpeak.Toybox.Data;

// at the same time, the payoff of storing disconnected devices could make reconnection faster.
public class ConnectedDevice
{
    private readonly ILogger<ConnectedDevice> _logger;
    private readonly ButtplugClientDevice ClientDevice;

    public ConnectedDevice(ILogger<ConnectedDevice> logger, ButtplugClientDevice buttplugClientDevice)
    {
        _logger = logger;

        // Set the client Device up.
        ClientDevice = buttplugClientDevice;

        // init the array sizes for the motors.
        Array.Resize(ref CurrentVibratorIntensity, VibeMotors);
        Array.Resize(ref CurrentRotationIntensity, RotateMotors);

        // init the initial battery check.
        if (ClientDevice.HasBattery)
            UpdateBatteryPercentage();
    }

    public ButtplugClientDevice SexToyDevice => ClientDevice;

    public uint DeviceIdx => ClientDevice.Index;
    public string DeviceName => ClientDevice.Name;
    public string DisplayName => ClientDevice.DisplayName;

    public List<GenericDeviceMessageAttributes> VibeAttributes => ClientDevice.VibrateAttributes;
    public bool CanVibrate => ClientDevice.VibrateAttributes.Any();
    public int VibeMotors => ClientDevice.VibrateAttributes.Count;
    public uint VibeMotorStepCount(int motorIdx) => VibeAttributes[motorIdx].StepCount;
    public double VibeMotorInterval => 1.0 / VibeAttributes[0].StepCount; // This should always be true, but if not can easily change logic.

    public List<GenericDeviceMessageAttributes> RotateAttributes => ClientDevice.RotateAttributes;
    public bool CanRotate => ClientDevice.RotateAttributes.Any();
    public int RotateMotors => ClientDevice.RotateAttributes.Count;
    public uint RotateMotorStepCount(int motorIdx) => RotateAttributes[motorIdx].StepCount;
    public double RotateMotorInterval => 1.0 / RotateAttributes[0].StepCount; // This should always be true, but if not can easily change logic.

    public bool BatteryPresent => ClientDevice.HasBattery;
    public double BatteryLevel { get; private set; } = -1.0;
    // if our device even exists or not.
    public bool IsConnected { get; set; } = false;

    // store the current intensity of each motor.
    private byte[] CurrentVibratorIntensity;
    private byte[] CurrentRotationIntensity;

    // create our Debouncer to prevent overload from multiple inputs occuring too 
    // create a new Debouncer with a 20ms delay. (extend if too fast or run into issues, but this allows for max accuracy)
    private DebounceDispatcher VibrateDebouncer = new DebounceDispatcher(TimeSpan.FromMilliseconds(20));
    private DebounceDispatcher RotateDebouncer = new DebounceDispatcher(TimeSpan.FromMilliseconds(20));

    /// <summary> Resets all motors of the device to 0, stopping all vibrations. </summary>
    public void ResetMotors()
    {
        _logger.LogInformation("Reset Motors called!", LoggerType.ToyboxDevices);
    }

    public async void UpdateBatteryPercentage()
    {
        // early return if the device is null or we can't check the battery.
        if (!BatteryPresent) { return; }
        // otherwise, fetch the batter.
        try
        {
            BatteryLevel = await ClientDevice.BatteryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error Updating Battery Lvl: {ex.Message}");
        }
    }

    /// <summary> Helper function to return a devices battery level in a XX% format </summary>
    public string BatteryPercentString() => BatteryLevel == -1.0 ? "Unknown" : $"{BatteryLevel * 100}%";

    /// <summary> Halt all vibrations </summary>
    public async void StopInTheNameOfTheVibe()
    {
        try
        {
            // halt each type of vibration.
            if (CanVibrate) { await ClientDevice.VibrateAsync(0.0); }
            if (CanRotate) { await ClientDevice.RotateAsync(0.0, clockwise: true); }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error Stopping Device: {ex.Message}");
        }
        ResetMotors();
    }

    public double GetVibrationDoubleFromByte(byte intensity) => Math.Round((intensity / 100.0) / VibeMotorInterval) * VibeMotorInterval;
    public double GetRotateDoubleFromByte(byte intensity) => Math.Round((intensity / 100.0) / RotateMotorInterval) * RotateMotorInterval;


    /// <summary> Send a vibration command.(create worker threads for this later or something idfk) </summary>
    public void SendVibration(byte intensity, int motorIndex = -1)
    {
        if (!CanVibrate || !IsConnected)
        {
            _logger.LogError("Cannot send vibration command, device is not connected or does not support vibration.");
            return;
        }
        // attempt to calulate and send off vibration
        try
        {
            if (motorIndex != -1)
            {
                // append the byte to the corrent motor index spesified.
                CurrentVibratorIntensity[motorIndex] = intensity;
            }
            else
            {
                // otherwise, append the new intensity to all motors
                for (int i = 0; i < VibeMotors; i++)
                {
                    CurrentVibratorIntensity[i] = intensity;
                }
            }
            // create the storage we are sending off.
            double[] motorIntensitysToSend = new double[VibeMotors];
            // convert the bytes to the doubles that will be sent off
            for (int i = 0; i < CurrentVibratorIntensity.Length; i++)
            {
                motorIntensitysToSend[i] = GetVibrationDoubleFromByte(CurrentVibratorIntensity[i]);
            }

            // send the vibration off to each motor asyncronously.
            VibrateDebouncer.Debounce(delegate
            {
                ClientDevice.VibrateAsync(motorIntensitysToSend);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error Sending Vibration: {ex}");
        }
    }

    // UNSURE IF THIS NEEDS TO BE ASYNC OR NOT I'VE NEVER WORKED WITH ANYTHING BESIDES VIBRATE
    /// <summary> Send a rotation command. </summary>
    public void SendRotate(byte intensity, bool clockwise = true, int motorIndex = -1)
    {
        _logger.LogDebug("Rotation Disabled Currently", LoggerType.ToyboxDevices);
        /*
        if (ClientDevice == null || !CanRotate || !IsConnected)
        {
            _logger.LogError("Cannot send rotation command, device is not connected or does not support rotation.");
            return;
        }

        try
        {
            // if motor spesified, update that motor only.
            if (motorIndex != -1)
            {
                CurrentRotationIntensity[motorIndex] = intensity;
            }
            // otherwise update all motors
            else
            {
                for (int i = 0; i < RotateMotors; i++)
                {
                    CurrentRotationIntensity[i] = intensity;
                }
            }
            // prepare sendoff
            List<(double, bool)> motorIntensitysToSend = new List<(double, bool)>();
            // store the bytes as doubles.
            for (int i = 0; i < CurrentRotationIntensity.Length; i++)
            {
                motorIntensitysToSend.Add((GetRotateDoubleFromByte(CurrentRotationIntensity[i]), clockwise));
            }

            // send them off.
            RotateDebouncer.Debounce(delegate
            {
                ClientDevice.RotateAsync(motorIntensitysToSend);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error Sending Rotation: {ex.Message}");
        }*/
    }
}
