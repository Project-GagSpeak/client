using GagSpeak.CkCommons;
using GagSpeak.Services.Configs;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.State.Handlers;

/// <summary> Handles GagSpeaks Arousal system. Stores a static and non-static arousal meter. </summary> 
/// <remarks> The higher the meter, the more likely certain events are to occur </remarks>
public sealed class ArousalManager : IDisposable
{
    private readonly ILogger<ArousalManager> _logger;
    private readonly MainConfig _config;

    private readonly CancellationTokenSource _timerCts = new();
    private Task? _timerTask;

    public ArousalManager(ILogger<ArousalManager> logger, MainConfig config)
    {
        _logger = logger;
        _config = config;
        RecalculateArousals();
        _timerTask = Task.Run(TimerTask, _timerCts.Token);
    }

    private async Task TimerTask()
    {
        try
        {
            while (!_timerCts.Token.IsCancellationRequested)
            {
                Update();
                await Task.Delay(TimeSpan.FromSeconds(_generationFrequency), _timerCts.Token);
            }
        }
        catch (TaskCanceledException) { }
    }

    public void Dispose()
    {
        _timerCts.Cancel();
        try
        {
            _timerTask?.Wait();
        }
        catch (AggregateException) { /* Consume */ }
        _timerCts.Dispose();
    }

    // Tweakable Values for different results.
    private const float AROUSAL_CAP = 100f;           // Max total arousal
    private const float MAX_GEN_RATE = 0.01f;         // Upper bound per tick
    private const float MIN_GEN_RATE = 0.0001f;       // Lower bound
    private const float MAX_FREQ = 0.1f;              // 10 times per second
    private const float MIN_FREQ = 2.0f;              // 1 times per 2 seconds
    private const float STIM_SOFTCAP = 200f;          // Total stimulation where deminishing returns begin
    private const float STIM_HARD_CAP = 400f;         // Max total considered for gen rate

    // Current State Fields
    private SortedList<CombinedCacheKey, Arousal> _arousals = new();
    private float _generationRate;
    private float _generationFrequency;
    private float _degenerationRate;

    // Exposed Properties
    public static float StaticArousal { get; private set; } = 0f;
    public static float Arousal { get; private set; } = 0f;
    private static float ArousalPercent => Arousal / AROUSAL_CAP;
    public static bool DoScreenBlur => ArousalEffects.ShouldBlur(ArousalPercent);
    public static float BlurIntensity => ArousalEffects.BlurIntensity(ArousalPercent);
    public static bool DoBlush => ArousalEffects.ShouldBlush(ArousalPercent);
    public static float BlushOpacity => ArousalEffects.BlushOpacity(ArousalPercent);
    public static bool DoStutter => ArousalEffects.ShouldStutter(ArousalPercent);
    public static float StutterFrequency => ArousalEffects.StutterFrequency(ArousalPercent);
    public static bool DoPulse => ArousalEffects.ShouldPulse(ArousalPercent);
    public static float PulseRate => ArousalEffects.PulseRate(ArousalPercent);
    public static bool DoLimitedWords => ArousalEffects.ShouldLimitWords(ArousalPercent);
    public static float WordLimitMultiplier => ArousalEffects.MaxWordLimitFactor(ArousalPercent);
    public static bool DoGcdDelay => ArousalEffects.ShouldSlowGCD(ArousalPercent);
    public static float GcdDelayFactor => ArousalEffects.GCDFactor(ArousalPercent);

    #region Public Methods
    /// <summary> Marks a <see cref="CombinedCacheKey"/> for an Arousal <paramref name="strength"/>, then updates Arousals.</summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public void AddAndUpdateArousal(CombinedCacheKey combinedKey, Arousal strength)
    {
        if (_arousals.TryAdd(combinedKey, strength))
            _logger.LogDebug($"Added ([{combinedKey}] <-> [{strength.ToString()}]) to Cache.");
        else
            _logger.LogWarning($"KeyValuePair ([{combinedKey}]) already exists in the Cache!");

        RecalculateArousals();
    }

    /// <summary>Removes a strength for a <see cref="CombinedCacheKey"/> matching <paramref name="combinedKey"/>, then updates Arousals.</summary>
    /// <returns>True if any change occured in <see cref="_finalProfile"/>, false otherwise. </returns>
    public void RemoveAndUpdateArousal(CombinedCacheKey combinedKey)
    {
        if (_arousals.Remove(combinedKey, out var a))
            _logger.LogDebug($"Removed Arousal of strength [{a.ToString()}] from cache at key [{combinedKey}].");
        else
            _logger.LogWarning($"ArousalCache key ([{combinedKey}]) not found!");

        RecalculateArousals();
    }

    public void ClearArousals()
    {
        _arousals.Clear();
        _logger.LogDebug("Cleared all Arousals from cache.");

        RecalculateArousals();
    }

    #endregion Public Methods

    private void RecalculateArousals()
    {
        // Obtain the total arousal from all cached arousals.
        int totalArousal = _arousals.Values.Sum(x => (byte)x); // 0–N range

        // grab a softened arousal value to apply a diminishing curve.
        float softenedArousal = SoftcapStimuli(totalArousal, STIM_SOFTCAP, STIM_HARD_CAP);

        // Update the StaticArousal value.
        StaticArousal = Math.Clamp(softenedArousal, 0f, AROUSAL_CAP);

        // Get how close to our arousal cap we are.
        float percent = StaticArousal / AROUSAL_CAP;

        // Generation rate: scaled based on softcapped stimulation
        _generationRate = Lerp(MIN_GEN_RATE, MAX_GEN_RATE, percent);

        // Frequency: faster when more stimulated
        _generationFrequency = Lerp(MIN_FREQ, MAX_FREQ, percent);

        // Decay: usually a fraction of generation
        _degenerationRate = _generationRate * 0.5f;
    }

    // Maps a high stimulation value for a bounded growth curve to make more realistic sense.
    private float SoftcapStimuli(float value, float softcap, float hardcap)
    {
        if (value <= softcap)
            return value;

        float over = value - softcap;
        float range = MathF.Max(hardcap - softcap, 1f);
        float reduced = over * (0.5f * (1f - (over / range)));

        return softcap + MathF.Max(0f, reduced); // Diminishing curve
    }

    /// <summary> Called on each new frequency point. </summary>
    public void Update()
    {
        if (_arousals.Count <= 0)
        {
            // Decay if no arousals are present.
            Arousal = MathF.Max(0f, Arousal - _degenerationRate);
        }

        // Calculate the new arousal value.
        float newArousal = Arousal + _generationRate - _degenerationRate;

        // Clamp the new arousal value to the maximum cap.
        Arousal = Math.Clamp(newArousal, 0f, AROUSAL_CAP);
        // Log the current arousal state.
        _logger.LogTrace($"Updated Arousal: {Arousal} (Static: {StaticArousal})", LoggerType.Toys);
    }

    /// <summary> Linearly interpolates between two values based on a factor t. </summary>
    /// <remarks> Think, “What number is 35% between 56 and 132?" </remarks>
    /// <param name="a"> lower bound value </param>
    /// <param name="b"> upper bound value </param>
    /// <param name="t"> should be in the range [a, b] </param>
    /// <returns> the interpolated value between a and b </returns>
    private float Lerp(float a, float b, float t) => a + (b - a) * t;




    #region DebugHelper
    public void DrawCacheTable()
    { }
    #endregion Debug Helper
}
