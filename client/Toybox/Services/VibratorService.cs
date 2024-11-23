using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Controllers;
using GagSpeak.Toybox.SimulatedVibe;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Enums;

namespace GagSpeak.Toybox.Services;
// handles the management of the connected devices or simulated vibrator.
public class VibratorService : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly DeviceService _deviceHandler; // handles the actual connected devices.
    private readonly VibeSimAudio _vibeSimAudio; // handles the simulated vibrator

    public VibratorService(ILogger<VibratorService> logger,
        GagspeakMediator mediator, ClientConfigurationManager clientConfigs,
        DeviceService deviceHandler, VibeSimAudio vibeSimAudio) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _deviceHandler = deviceHandler;
        _vibeSimAudio = vibeSimAudio;

        // restore the chosen simulated audio type from the config
        _vibeSimAudio.ChangeAudioPath(VibeSimAudioPath(_clientConfigs.GagspeakConfig.VibeSimAudio));

        if (UsingSimulatedVibe)
        {
            // play it
            _vibeSimAudio.Play();
        }

        Mediator.Subscribe<MainHubConnectedMessage>(this, _ =>
        {
            if (_clientConfigs.GagspeakConfig.IntifaceAutoConnect && !_deviceHandler.ConnectedToIntiface)
            {
                if (ToyboxHelper.AppPath == string.Empty)
                {
                    ToyboxHelper.GetApplicationPath();
                }
                ToyboxHelper.OpenIntiface(logger, false);
                _deviceHandler.ConnectToIntifaceAsync();
            }
        });
    }

    // public accessors here.
    public VibratorMode CurrentVibratorModeUsed => _clientConfigs.GagspeakConfig.VibratorMode;
    public bool UsingSimulatedVibe => CurrentVibratorModeUsed == VibratorMode.Simulated;
    public bool UsingRealVibe => CurrentVibratorModeUsed == VibratorMode.Actual;
    public bool ConnectedToyActive => (CurrentVibratorModeUsed == VibratorMode.Actual) ? _deviceHandler.ConnectedToIntiface && _deviceHandler.AnyDeviceConnected : VibeSimAudioPlaying;
    public bool IntifaceConnected => _deviceHandler.ConnectedToIntiface;
    public bool ScanningForDevices => _deviceHandler.ScanningForDevices;


    public bool VibeSimAudioPlaying { get; private set; } = false;
    public float VibeSimVolume { get; private set; } = 0.0f;
    public string ActiveSimPlaybackDevice => _vibeSimAudio.PlaybackDevices[_vibeSimAudio.ActivePlaybackDeviceId];
    public List<string> PlaybackDevices => _vibeSimAudio.PlaybackDevices;


    // Grab device handler via toyboxvibeService.
    public DeviceService DeviceHandler => _deviceHandler;
    public VibeSimAudio VibeSimAudio => _vibeSimAudio;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // stop the sound player if it is playing
        if (_vibeSimAudio.isPlaying)
        {
            _vibeSimAudio.Stop();
        }
        _vibeSimAudio.Dispose();
    }

    public void ExecuteShockAction(string shareCode, ShockTriggerAction shockAction)
    {
        Mediator.Publish(new PiShockExecuteOperation(shareCode, (int)shockAction.OpCode, shockAction.Intensity, shockAction.Duration));
    }

    public void UpdateVibeSimAudioType(VibeSimType newType)
    {
        _clientConfigs.GagspeakConfig.VibeSimAudio = newType;
        _clientConfigs.Save();

        _vibeSimAudio.ChangeAudioPath(VibeSimAudioPath(newType));
        _vibeSimAudio.SetVolume(VibeSimVolume);
    }

    public void SwitchPlaybackDevice(int deviceId)
    {
        _vibeSimAudio.SwitchDevice(deviceId);
    }


    public void StartActiveVibes()
    {
        // start the vibe based on the type used for the vibe.
        if (UsingRealVibe)
        {
            // do something?
        }
        else if (UsingSimulatedVibe)
        {
            VibeSimAudioPlaying = true;
            _vibeSimAudio.Play();
        }
        UnlocksEventManager.AchievementEvent(UnlocksEvent.VibratorsToggled, NewState.Enabled);
    }


    public void StopActiveVibes()
    {
        // stop the vibe based on the type used for the vibe.
        if (UsingRealVibe)
        {
            _deviceHandler.StopAllDevices();
        }
        else if (UsingSimulatedVibe)
        {
            VibeSimAudioPlaying = false;
            _vibeSimAudio.Stop();
        }
        UnlocksEventManager.AchievementEvent(UnlocksEvent.VibratorsToggled, NewState.Disabled);
    }

    public void SendNextIntensity(byte intensity)
    {
        if (ConnectedToyActive)
        {
            if (UsingRealVibe)
            {
                DeviceHandler.SendVibeToAllDevices(intensity);
            }
            else if (UsingSimulatedVibe)
            {
                _vibeSimAudio.SetVolume(intensity / 100f);
            }
        }
    }

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





