//using CkCommons.Audio;
//using GagSpeak.Services.Configs;
//using NAudio.Wave;
//using NAudio.Wave.SampleProviders;

//namespace GagSpeak.Utils;

//// Random AI stuff while i figure out the mess i made 16 months ago
//public class VibeMotor : IDisposable
//{
//    private readonly AudioFileReader _audioReader;
//    private readonly LoopingSampleProvider _looper; // Your custom looper
//    private readonly SmbPitchShiftingSampleProvider _pitchShifter;
//    private readonly VolumeSampleProvider _volumeProvider;

//    public ISampleProvider OutputProvider => _volumeProvider;

//    private bool _isAdjusting = false;
//    private float _targetVolume = 0.0f;
//    private float _targetPitch = 1.0f;

//    private const float VolumeChangeRate = 0.025f;
//    private const float PitchChangeRate = 0.025f;

//    public VibeMotor(string audioPath)
//    {
//        _audioReader = new AudioFileReader(audioPath);
//        _looper = new VibeSimLooper(_audioReader);

//        // Build the signal chain: File -> Looper -> PitchShifter -> Volume
//        _pitchShifter = new SmbPitchShiftingSampleProvider(_looper.ToSampleProvider());

//        // Note: Using VolumeSampleProvider is smoother than modifying AudioFileReader.Volume
//        _volumeProvider = new VolumeSampleProvider(_pitchShifter)
//        {
//            Volume = 0.0f
//        };
//    }

//    public async void SetIntensity(float intensity)
//    {
//        // Clamp intensity to prevent the pitch shifter from breaking/scraping at absolute zero
//        intensity = Math.Clamp(intensity, 0.001f, 1.0f);

//        _targetVolume = intensity;
//        _targetPitch = 1.0f + (intensity * 0.5f);

//        if (_isAdjusting) return;
//        _isAdjusting = true;

//        try
//        {
//            while (Math.Abs(_volumeProvider.Volume - _targetVolume) > VolumeChangeRate ||
//                   Math.Abs(_pitchShifter.PitchFactor - _targetPitch) > PitchChangeRate)
//            {
//                // Smooth Volume
//                if (_volumeProvider.Volume < _targetVolume) _volumeProvider.Volume += VolumeChangeRate;
//                else if (_volumeProvider.Volume > _targetVolume) _volumeProvider.Volume -= VolumeChangeRate;

//                // Smooth Pitch
//                if (_pitchShifter.PitchFactor < _targetPitch) _pitchShifter.PitchFactor += PitchChangeRate;
//                else if (_pitchShifter.PitchFactor > _targetPitch) _pitchShifter.PitchFactor -= PitchChangeRate;

//                await Task.Delay(20);
//            }

//            // Snap to exact target at the end
//            _volumeProvider.Volume = _targetVolume;
//            _pitchShifter.PitchFactor = _targetPitch;
//        }
//        finally
//        {
//            _isAdjusting = false;
//        }
//    }

//    public void Dispose()
//    {
//        _audioReader?.Dispose();
//        _looper?.Dispose();
//    }
//}

//public sealed class VibeSimManager : IDisposable
//{
//    private readonly ILogger<VibeSimManager> _logger;
//    private WaveOutEvent _waveOutDevice;
//    private MixingSampleProvider _mixer;

//    // Thread-safe dictionary to keep track of motors by an ID (e.g., "Motor1", "Motor2")
//    private readonly ConcurrentDictionary<string, VibeMotor> _motors = new();

//    public VibeSimManager(ILogger<VibeSimManager> logger)
//    {
//        _logger = logger;
//        InitializeAudioSystem();
//    }

//    private void InitializeAudioSystem(int deviceNumber = -1)
//    {
//        // 1. Create the mixer with a standard format (e.g., 44.1kHz Stereo IEEE Float)
//        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
//        _mixer = new MixingSampleProvider(waveFormat)
//        {
//            // CRITICAL: ReadFully must be true. 
//            // If false, the mixer stops playing completely when no motors are active.
//            // If true, it quietly outputs silence until a motor is added.
//            ReadFully = true
//        };

//        // 2. Initialize the playback device
//        _waveOutDevice = new WaveOutEvent
//        {
//            DeviceNumber = deviceNumber,
//            DesiredLatency = 80,
//            NumberOfBuffers = 3
//        };

//        _waveOutDevice.Init(_mixer);
//        _waveOutDevice.Play(); // Start playing silence immediately
//    }

//    public void AddMotor(string motorId, string audioPath)
//    {
//        if (_motors.ContainsKey(motorId))
//        {
//            _logger.LogWarning($"Motor {motorId} already exists.");
//            return;
//        }

//        var fullPath = Path.Combine(GsFiles.AssemblyDirectory, "Assets", "Audio", audioPath);
//        var motor = new VibeMotor(fullPath);

//        // Note: If your WAV files are not 44.1kHz Stereo, you must wrap motor.OutputProvider 
//        // in a WdlResamplingSampleProvider to match the _mixer's wave format before adding it.

//        _motors.TryAdd(motorId, motor);
//        _mixer.AddMixerInput(motor.OutputProvider);
//    }

//    public void RemoveMotor(string motorId)
//    {
//        if (_motors.TryRemove(motorId, out var motor))
//        {
//            _mixer.RemoveMixerInput(motor.OutputProvider);
//            motor.Dispose();
//        }
//    }

//    public void SetMotorIntensity(string motorId, float intensity)
//    {
//        if (_motors.TryGetValue(motorId, out var motor))
//        {
//            motor.SetIntensity(intensity);
//        }
//    }

//    public void SwitchDevice(int deviceId)
//    {
//        _logger.LogInformation($"Switching audio device to {deviceId}");

//        _waveOutDevice?.Stop();
//        _waveOutDevice?.Dispose();

//        InitializeAudioSystem(deviceId - 1);

//        // Re-add existing motors to the new mixer instance
//        foreach (var motor in _motors.Values)
//        {
//            _mixer.AddMixerInput(motor.OutputProvider);
//        }
//    }

//    public void Dispose()
//    {
//        _waveOutDevice?.Stop();
//        _waveOutDevice?.Dispose();

//        foreach (var motor in _motors.Values)
//        {
//            motor.Dispose();
//        }
//        _motors.Clear();
//    }
//}
