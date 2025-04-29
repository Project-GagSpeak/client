using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Configs;
using GagSpeak.Toybox.Services;
using GagspeakAPI.Data;

namespace GagSpeak.PlayerState.Toybox;

// Funny haha class that does the magic voodoo with playback and makes the kinky toys go brrr.
public class PatternApplier : IDisposable
{
    private readonly ILogger<PatternApplier> _logger;
    private readonly GagspeakConfigService _config;
    private readonly SexToyManager _vibeService;

    private CancellationTokenSource? _playbackCTS;
    private Task? _playbackTask;

    // Pattern MetaData
    public Stopwatch DisplayTime { get; private set; }
    public int ReadBufferIdx { get; private set; }
    public IReadOnlyList<byte> PlaybackData { get; private set; }
    public Pattern? ActivePatternInfo { get; private set; }
    private List<float> SimulatedVolumes = new();

    public PatternApplier(ILogger<PatternApplier> logger, GagspeakConfigService config,
        SexToyManager vibeService)
    {
        _logger = logger;
        _config = config;
        _vibeService = vibeService;
    }

    public bool CanPlaybackPattern => _playbackTask is null || _playbackTask.IsCompleted;

    public void Dispose() => StopPlayback();

    /// <summary> Starts the playback of the defined pattern. </summary>
    public bool StartPlayback(Pattern patternToPlay)
        => StartPlayback(patternToPlay, patternToPlay.StartPoint, patternToPlay.Duration);

    /// <summary> Starts the playback of the defined pattern, with customized startpoint and duration. </summary>
    /// <remarks> Attempted invalid data paramaters will result in the duration being trimmed if it goes out of bounds. </remarks>
    public bool StartPlayback(Pattern patternToPlay, TimeSpan customStartPoint, TimeSpan customDuration)
    {
        // if any existing pattern is playing, abort.
        if (!CanPlaybackPattern)
        {
            _logger.LogDebug("Attempted to start a new pattern while one is already playing. Stopping previous first!");
            StopPlayback();
        }

        _logger.LogDebug($"Starting playback of pattern {patternToPlay.Label}", LoggerType.ToyboxPatterns);
        ReadBufferIdx = 0;

        // Store in the vibrator data from the defined startpoint, for the defined duration
        PlaybackData = TrimDataToPlayableRegion(patternToPlay.PatternData, customStartPoint, customDuration);
        ActivePatternInfo = patternToPlay;

        if (_config.Config.VibratorMode is VibratorEnums.Simulated)
            InitializeVolumeLevels(PlaybackData);

        // Start the cancelation token, and begin playback task.
        _playbackCTS = new CancellationTokenSource();
        DisplayTime.Start();
        _playbackTask = Task.Run(() => PlaybackLoop(_playbackCTS.Token), _playbackCTS.Token);

        UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Started, patternToPlay.Identifier, false);
        return true;
    }

    /// <summary> Stops the currently running pattern. </summary>
    public void StopPlayback()
    {
        if (CanPlaybackPattern || ActivePatternInfo is null)
            return;

        _logger.LogDebug($"Stopping playback of pattern", LoggerType.ToyboxPatterns);
        _playbackCTS?.Cancel();
        _playbackTask?.Wait();
        _vibeService.StopActiveVibes();

        // turn off all meta.
        DisplayTime.Stop();
        DisplayTime.Reset();
        PlaybackData = Array.Empty<byte>();
        _playbackCTS?.Dispose();
        _playbackCTS = null;

        UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Stopped, ActivePatternInfo.Identifier, false);
        ActivePatternInfo = null;
    }

    /// <summary> Extracts the range of byte data from the defined startpoint, for the playback duration. </summary>
    private IReadOnlyList<byte> TrimDataToPlayableRegion(List<byte> bytes, TimeSpan startPoint, TimeSpan duration)
    {
        // The pattern stores 1 byte every 20ms. We need to compress this information to contain the subset starting at the defined startpoint, for the defined duration.
        _logger.LogDebug($"Start point at " + startPoint + " and duration at " + duration, LoggerType.ToyboxPatterns);
        var startIndex = (int)(startPoint.TotalSeconds * 50);
        var length = (int)(duration.TotalSeconds * 50);

        // Ensure startIndex is within bounds
        if (startIndex >= bytes.Count || length >= bytes.Count - startIndex)
        {
            _logger.LogWarning("Total Byte size exceeds the available data range.");
            return Array.Empty<byte>(); // Return empty list if start index is out of range
        }

        // Clamp the length just incase.
        var endIndex = Math.Min(startIndex + length, bytes.Count);

        // return the correct range.
        return bytes.GetRange(startIndex, endIndex - startIndex);

    }

    /// <summary> Helps make the simulated vibrator audio have volumes levels that dont turn your headphone audio into a shredding machine. </summary>
    private void InitializeVolumeLevels(IReadOnlyList<byte> intensityPattern)
    {
        SimulatedVolumes.Clear();
        foreach (var intensity in intensityPattern)
            SimulatedVolumes.Add(intensity / 100f);
    }

    /// <summary> The operating task responcible for running the active pattern. </summary>
    /// <remarks> This task can be cancelled at any time by the token. </remarks>
    private async Task PlaybackLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // If we reached the end of the data. Stop playback or restart if looping.
                if (ReadBufferIdx >= PlaybackData.Count)
                {
                    if (ActivePatternInfo?.ShouldLoop ?? false)
                    {
                        ReadBufferIdx = 0;
                        DisplayTime.Restart();
                    }
                    else
                    {
                        StopPlayback();
                        return;
                    }
                }

                // send off the vibe instruction to the actual or simulated sex toy.
                _vibeService.SendNextIntensity(PlaybackData[ReadBufferIdx]);
                ReadBufferIdx++;
                // 20 millisecond delay before next read.
                await Task.Delay(20, token);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Playback task was cancelled.");
        }
    }
}

