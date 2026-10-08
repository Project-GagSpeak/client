using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using OtterGui;

namespace GagSpeak.Services;

/// <summary> Handles GagSpeaks Arousal system. Stores a static and non-static arousal meter. </summary> 
/// <remarks> The higher the meter, the more likely certain events are to occur </remarks>
public sealed class ArousalService : IDisposable
{
    private readonly ILogger<ArousalService> _logger;
    private readonly MainConfig _config;

    private readonly CancellationTokenSource _timerCts = new();
    private Task? _timerTask;
    private DateTime _lastSave = DateTime.MinValue;

    public ArousalService(ILogger<ArousalService> logger, MainConfig config)
    {
        _logger = logger;
        _config = config;
        Arousal = config.Data.Arousal;
        UpdateFinalCache();
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
    private const float MAX_GEN_RATE = 0.5f;          // Upper bound per tick
    private const float MIN_GEN_RATE = 0.005f;        // Lower bound
    private const float MAX_FREQ = 0.1f;              // 10 times per second
    private const float MIN_FREQ = 2.0f;              // 1 times per 2 seconds
    private const float IDLE_DECAY_RATE = 0.05f;      // Decay per tick while no arousal items are worn
    private const float STIM_SOFTCAP = 200f;          // Total stimulation where deminishing returns begin
    private const float STIM_HARD_CAP = 400f;         // Max total considered for gen rate

    // Current State Fields
    private SortedList<CombinedCacheKey, Arousal> _arousals = new();
    private float _generationRate;
    private float _generationFrequency;
    private float _degenerationRate;
    private static float _pulsePhase;

    // Exposed Properties
    public static float StaticArousal { get; private set; } = 0f;
    public static float Arousal { get; private set; } = 0f;
    public static float ArousalPercent => Arousal / AROUSAL_CAP;
    // Effects only apply while Arousal Effects is enabled, though the meter itself keeps running.
    public static float EffectPercent => ClientData.Globals?.GlobalArousal == true ? ArousalPercent : 0f;
    public static bool DoScreenBlur => ArousalEffects.ShouldBlur(EffectPercent);
    public static float BlurIntensity => ArousalEffects.BlurIntensity(EffectPercent);
    public static bool DoBlush => ArousalEffects.ShouldBlush(EffectPercent);
    public static float BlushOpacity => ArousalEffects.BlushOpacity(EffectPercent);
    public static bool DoStutter => ArousalEffects.ShouldStutter(EffectPercent);
    public static float StutterFrequency => ArousalEffects.StutterFrequency(EffectPercent);
    public static bool DoPulse => ArousalEffects.ShouldPulse(EffectPercent);
    public static float PulseRate => ArousalEffects.PulseRate(EffectPercent);
    public static bool DoLimitedWords => ArousalEffects.ShouldLimitWords(EffectPercent);
    public static float WordLimitMultiplier => ArousalEffects.MaxWordLimitFactor(EffectPercent);
    public static bool DoGcdDelay => ArousalEffects.ShouldSlowGCD(EffectPercent);
    public static float GcdDelayFactor => ArousalEffects.GCDFactor(EffectPercent);
    public static bool HasChatEffects => DoStutter || DoLimitedWords;

    #region Public Methods
    /// <summary> Marks a <see cref="CombinedCacheKey"/> for an Arousal <paramref name="strength"/>.</summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool TryAddArousalToCache(CombinedCacheKey combinedKey, Arousal strength)
    {
        if (_arousals.TryAdd(combinedKey, strength))
        {
            _logger.LogDebug($"Added ([{combinedKey}] <-> [{strength.ToString()}]) to Cache.", LogFilter.Arousal);
            return true;
        }
        else
        {
            _logger.LogWarning($"KeyValuePair ([{combinedKey}]) already exists in the Cache!");
            return false;
        }
    }

    /// <summary>Removes a strength for the <paramref name="combinedKey"/> from the cache.</summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool TryRemArousalFromCache(CombinedCacheKey combinedKey)
    {
        if (_arousals.Remove(combinedKey, out var a))
        {
            _logger.LogDebug($"Removed Arousal of strength [{a.ToString()}] from cache at key [{combinedKey}].", LogFilter.Arousal);
            return true;
        }
        else
        {
            _logger.LogWarning($"ArousalCache key ([{combinedKey}]) not found!");
            return false;
        }
    }

    public async Task ClearArousals()
    {
        _arousals.Clear();
        _logger.LogDebug("Cleared all Arousals from cache.", LogFilter.Arousal);
        await UpdateFinalCache();
    }

    public Task UpdateFinalCache()
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
        _generationRate = GagspeakEx.Lerp(MIN_GEN_RATE, MAX_GEN_RATE, percent);

        // Frequency: faster when more stimulated
        _generationFrequency = GagspeakEx.Lerp(MIN_FREQ, MAX_FREQ, percent);

        // Decay: half of generation while stimulated, a fixed idle rate otherwise.
        _degenerationRate = _arousals.Count > 0 ? _generationRate * 0.5f : IDLE_DECAY_RATE;

        _logger.LogDebug("Finished Updating Arousal Caches.", LogFilter.Arousal);

        return Task.CompletedTask;
    }

    /// <summary> Applies the word limit and stutter effects for the current arousal to a chat message. </summary>
    public static string ApplyChatEffects(string msg)
    {
        var words = msg.Split(' ');

        // Text between * (RP actions) is never stuttered or cut, matching the garbler.
        var isAction = new bool[words.Length];
        var inAction = false;
        for (var i = 0; i < words.Length; i++)
        {
            isAction[i] = inAction || words[i].Contains('*');
            if (words[i].Count(c => c == '*') % 2 == 1)
                inAction = !inAction;
        }

        if (DoStutter)
        {
            for (var i = 0; i < words.Length; i++)
            {
                if (isAction[i] || words[i].Length == 0 || !char.IsLetter(words[i][0]))
                    continue;

                var stutter = $"{words[i][0]}-";
                if (Random.Shared.NextSingle() < StutterFrequency)
                    words[i] = stutter + words[i];
                if (Random.Shared.NextSingle() < StutterFrequency - 1f)
                    words[i] = stutter + words[i];
            }
        }

        if (DoLimitedWords)
        {
            var spoken = words.Length - isAction.Count(a => a);
            var limit = Math.Max(2, (int)MathF.Ceiling(spoken * WordLimitMultiplier));
            var kept = new List<string>();
            var spokenSeen = 0;
            for (var i = 0; i < words.Length; i++)
            {
                if (isAction[i] || ++spokenSeen <= limit)
                    kept.Add(words[i]);
                // Mark each cut stretch of speech once, on its first cut word:
                // trail off the kept word before it, or stand in for a fully cut stretch.
                else if (isAction[i - 1])
                    kept.Add("...");
                else if (spokenSeen - 1 == limit)
                    kept[^1] += "...";
            }
            words = kept.ToArray();
        }

        return string.Join(' ', words);
    }

    /// <summary> Draws a pink vignette from the screen edges for blush, throbbing when pulse is active. </summary>
    /// <remarks> When blur effects are implemented, make this work the same as how blur does it </remarks>
    public static void DrawBlush()
    {
        if (!DoBlush)
            return;

        var alpha = BlushOpacity * 0.6f;
        if (DoPulse)
        {
            _pulsePhase = (_pulsePhase + ImGui.GetIO().DeltaTime * (2f + 4f * PulseRate)) % MathF.Tau;
            alpha *= 0.75f + 0.25f * MathF.Sin(_pulsePhase);
        }

        var edge = CkGui.Color(new Vector4(1f, 0.3f, 0.5f, alpha));
        var clear = CkGui.Color(new Vector4(1f, 0.3f, 0.5f, 0f));
        var size = ImGui.GetIO().DisplaySize;
        var depth = size * 0.25f;
        var drawList = ImGui.GetForegroundDrawList();
        // Corner order: upper-left, upper-right, bottom-right, bottom-left.
        drawList.AddRectFilledMultiColor(Vector2.Zero, new(size.X, depth.Y), edge, edge, clear, clear);
        drawList.AddRectFilledMultiColor(new(0, size.Y - depth.Y), size, clear, clear, edge, edge);
        drawList.AddRectFilledMultiColor(Vector2.Zero, new(depth.X, size.Y), edge, clear, clear, edge);
        drawList.AddRectFilledMultiColor(new(size.X - depth.X, 0), size, clear, edge, edge, clear);
    }
    #endregion Public Methods

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
        SaveArousal();
        if (_arousals.Count <= 0)
        {
            // Decay if no arousals are present.
            Arousal = MathF.Max(0f, Arousal - _degenerationRate);
            return;
        }

        // Calculate the new arousal value.
        float newArousal = Arousal + _generationRate - _degenerationRate;

        // Clamp the new arousal value to the maximum cap.
        Arousal = Math.Clamp(newArousal, 0f, AROUSAL_CAP);
        // Log the current arousal state.
        _logger.LogTrace($"Updated Arousal: {(float)Arousal} (Static: {StaticArousal})", LogFilter.Arousal);
    }

    /// <summary> Persists the current arousal at most once a minute. </summary>
    private void SaveArousal()
    {
        if (_config.Data.Arousal == Arousal || DateTime.UtcNow - _lastSave < TimeSpan.FromMinutes(1))
            return;

        _config.Data.Arousal = Arousal;
        _config.Save();
        _lastSave = DateTime.UtcNow;
    }

    #region DebugHelper
    public void DrawCacheTable()
    {
        using var _ = ImRaii.Group();

        using (var n = ImRaii.TreeNode("Arousal Cache"))
        {
            if (n)
            {
                using (var t = ImRaii.Table("ArousalCache", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!t)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Arousal Strength");
                    ImGui.TableHeadersRow();

                    foreach (var (combinedKey, strength) in _arousals)
                    {
                        ImGuiUtil.DrawFrameColumn($"{combinedKey.Manager} / {combinedKey.LayerIndex}");
                        ImGui.TableNextColumn();
                        CkGui.ColorText(strength.ToString(), GsCol.LushPinkButton.Uint());
                    }
                }
            }
        }
        ImGui.Separator();
        ImGui.TextUnformatted($"Static Arousal: {StaticArousal:F2}");
#if DEBUG
        var arousal = Arousal;
        if (ImGui.SliderFloat("Current Arousal", ref arousal, 0f, AROUSAL_CAP, "%.1f", ImGuiSliderFlags.AlwaysClamp))
            Arousal = arousal;
#else
        ImGui.TextUnformatted($"Current Arousal: {Arousal:F2}");
#endif
        ImGui.TextUnformatted($"Arousal Percent: {ArousalPercent:P2}");
        ImGui.TextUnformatted($"Generation Rate: {_generationRate:F5} per tick");
        ImGui.TextUnformatted($"Generation Frequency: {_generationFrequency:F2}s");
        ImGui.TextUnformatted($"Degeneration Rate: {_degenerationRate:F5} per tick");
        ImGui.Separator();
        ImGui.TextUnformatted("Arousal Effects:");

        ImGui.Text("Blur:");
        CkGui.ColorTextInline(DoScreenBlur.ToString(), DoScreenBlur ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGreen);
        CkGui.TextInline($"| Intensity: {BlurIntensity:P2}");

        ImGui.Text("Blush:");
        CkGui.ColorTextInline(DoBlush.ToString(), DoBlush ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGreen);
        CkGui.TextInline($"| Opacity: {BlushOpacity:P2}");

        ImGui.Text("Stutter:");
        CkGui.ColorTextInline(DoStutter.ToString(), DoStutter ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGreen);
        CkGui.TextInline($"| Frequency: {StutterFrequency:P2}");

        ImGui.Text("Pulse:");
        CkGui.ColorTextInline(DoPulse.ToString(), DoPulse ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGreen);
        CkGui.TextInline($"| Rate: {PulseRate:P2}");

        ImGui.Text("Limited Words:");
        CkGui.ColorTextInline(DoLimitedWords.ToString(), DoLimitedWords ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGreen);
        CkGui.TextInline($"| {WordLimitMultiplier:P2} of words kept.");

        ImGui.Text("GCD Delay:");
        CkGui.ColorTextInline(DoGcdDelay.ToString(), DoGcdDelay ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGreen);
        CkGui.TextInline($"GCD Speed: {GcdDelayFactor:P2}");
    }
    #endregion Debug Helper
}
