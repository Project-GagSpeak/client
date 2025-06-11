using GagSpeak.CkCommons;
using GagSpeak.Services.Configs;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.PlayerState.Visual;

public static class ArousalEffects
{
    // ------------------------ BLUR ------------------------ (WIP, need more vfx experience for this)

    /// <summary>
    ///     Start blurring vision at 80% arousal.
    /// </summary>
    public const float BlurStart = 0.80f;

    /// <summary>
    ///     Maximum blur intensity reached at 100%.
    /// </summary>
    public const float BlurMax = 1.00f;

    /// <summary>
    ///     True if screen blur should be applied.
    /// </summary>
    public static bool ShouldBlur(float percent) => percent >= BlurStart;

    /// <summary>
    ///     The Blur's intensity. Scales linearly from 0% to 50% as it goes from 80% to 100%.
    /// </summary>
    public static float BlurIntensity(float percent) => ShouldBlur(percent)
        ? Math.Clamp((percent - BlurStart) / (BlurMax - BlurStart), 0f, 1f) * .5f
        : 0f;

    // ------------------------ BLUSH OVERLAY ------------------------
    /// <summary>
    ///     Blush overlay begins showing faintly at 40%.
    /// </summary>
    public const float BlushStart = 0.40f;

    /// <summary>
    ///     Reaches full opacity by 100% arousal.
    /// </summary>
    public const float BlushMax = 1.00f;

    /// <summary>
    ///     True if blushing overlay should be drawn.
    /// </summary>
    public static bool ShouldBlush(float percent) => percent >= BlushStart;

    /// <summary>
    ///     Scales opacity of blush overlay between 0%Alpha (40%) to 100%Alpha (100%).
    /// </summary>
    public static float BlushOpacity(float percent) => ShouldBlush(percent)
        ? Math.Clamp((percent - BlushStart) / (BlushMax - BlushStart), 0f, 1f)
        : 0f;

    // ------------------------ SPEECH STUTTER ------------------------

    /// <summary>
    ///     Speech stutter begins very rarely around 10%.
    /// </summary>
    public const float StutterStart = 0.10f;

    /// <summary>
    ///     Stutter becomes frequent or even every word by 100%.
    /// </summary>
    public const float StutterMax = 1.00f;

    /// <summary>
    ///     Enables stutter logic when above 10%.
    /// </summary>
    public static bool ShouldStutter(float percent) => percent >= StutterStart;

    /// <summary>
    ///     Determines stutter frequency, scaling up exponentially for more natural buildup.
    /// </summary>
    /// <returns> Around 0f to 2f, exponentially — double stutters can occur over 1.0f. </returns>
    public static float StutterFrequency(float percent) => ShouldStutter(percent)
        ? MathF.Pow((percent - StutterStart) / (StutterMax - StutterStart), 1.5f) * 2f
        : 0f;

    // ------------------------ SCREEN PULSE ------------------------

    /// <summary>
    ///     Subtle screen pulsing starts at 85%.
    /// </summary>
    public const float PulseStart = 0.85f;

    /// <summary>
    ///     Pulsing becomes faster and more intense by 100%.
    /// </summary>
    public const float PulseMax = 1.00f;

    /// <summary>
    ///     True if pulse effect should be active.
    /// </summary>
    public static bool ShouldPulse(float percent) => percent >= PulseStart;

    /// <summary>
    ///     Pulse rate scales from slow to fast as it nears 100%, using exponential growth.
    /// </summary>
    public static float PulseRate(float percent) => ShouldPulse(percent)
        ? MathF.Pow(Math.Clamp((percent - PulseStart) / (PulseMax - PulseStart), 0f, 1f), 1.5f)
        : 0f;

    // ------------------------ WORD LIMIT ------------------------

    /// <summary>
    ///     Word limit mechanic starts affecting the user at 65%.
    /// </summary>
    public const float WordCapStart = 0.65f;

    /// <summary>
    ///     By 100%, the user might only be able to say 1–2 words per message.
    /// </summary>
    public const float WordCapMax = 1.00f;

    /// <summary>
    ///     If a word cap should be enacted.
    /// </summary>
    public static bool ShouldLimitWords(float percent) => percent >= WordCapStart;

    /// <summary>
    ///     Returns multiplier of max word count, reflected as an inverse exponential curve.
    ///     1.0f = all words allowed, 0.0f = only 1–2 words allowed.
    /// </summary>
    public static float MaxWordLimitFactor(float percent) => ShouldLimitWords(percent)
        ? 1f - MathF.Pow((percent - WordCapStart) / (WordCapMax - WordCapStart), 1.5f)
        : 1f;

    // ------------------------ ATTACK GCD SLOWDOWN ------------------------

    /// <summary>
    ///     Slower attack speed begins around 45% arousal.
    /// </summary>
    public const float SlowGCDStart = 0.45f;

    /// <summary>
    ///     By 100%, actions can become 1.5x slower.
    /// </summary>
    public const float SlowGCDMax = 1.00f;

    /// <summary>
    ///     If GCD should be slowed down.
    /// </summary>
    public static bool ShouldSlowGCD(float percent) => percent >= SlowGCDStart;

    /// <summary>
    ///     Scales GCD factor from 1.0x to 1.5x with exponential curve for late-stage sluggishness.
    /// </summary>
    public static float GCDFactor(float percent)
    {
        if (!ShouldSlowGCD(percent)) return 1f;

        float scale = Math.Clamp((percent - SlowGCDStart) / (SlowGCDMax - SlowGCDStart), 0f, 1f);
        return 1f + MathF.Pow(scale, 2f) * 0.5f; // 1.0x to 1.5x multiplier
    }
}
