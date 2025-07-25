using Dalamud.Interface;
using CkCommons;
using GagSpeak.Gui;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using Timer = System.Timers.Timer;
using CkCommons.Gui;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Manages the rendering of a hypnosis overlay onto your screen.
/// </summary>
public class HypnoService
{
    // Constants.
    public const int ANIMATION_DURATION_MS = 1500;
    public const float SPIN_SPEED_MIN = 0.1f;
    public const float SPIN_SPEED_MAX = 5f;
    public const float ZOOM_MIN = 0.25f;
    public const float ZOOM_MAX = 3.50f; // Account for people with higher resolution monitors.
    public const int DISPLAY_TIME_MIN = 250;
    public const int DISPLAY_TIME_MAX = 10000;
    public const int FONTSIZE_MIN = 150; // px
    public const int FONTSIZE_MAX = 500; // px
    public const int SPEED_BETWEEN_MIN = 20; // ms
    public const int SPEED_BETWEEN_MAX = 500; // ms
    public const int STROKE_THICKNESS_MIN = 0; // px
    public const int STROKE_THICKNESS_MAX = 16; // px

    // Constants that cant be constant because struct stuff.
    public static readonly Vector2[] UVCorners = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

    // Local variables for the active display.
    private HypnosisState   _activeState = new();

    // Active Display Item.
    private HypnoticEffect? _activeEffect = null;          
    private string          _applierUid   = string.Empty;
    private string          _overlayPath  = string.Empty; // For Effects with local FilePaths.
    private byte[]          _imageBytes   = Array.Empty<byte>(); // For items sent with image data. (look into KinkPlates for more info)

    // Animation Control
    private SemaphoreSlim            _animationSlim = new(1, 1);
    private CancellationTokenSource  _opacityCTS = new();
    private CancellationTokenSource  _tasksCTS = new();
    private Task?                    _colorTask;
    private Task?                    _textTask;
    private Timer                    _expireTime;

    private readonly ILogger<HypnoService> _logger;
    private readonly MainConfig            _config;
    private readonly CosmeticService       _disp;
    public HypnoService(ILogger<HypnoService> logger, MainConfig config, CosmeticService render)
    {
        _logger = logger;
        _config = config;
        _disp = render;

        // Setup the timer's interval elapsed to remove the item automatically (Handle how we do this later)
        _expireTime = new(int.MaxValue) { AutoReset = true };
        _expireTime.Elapsed += (_,_) => RemoveActiveItem();
    }

    public bool HasValidEffect => _activeEffect is not null;

    private void RemoveActiveItem()
        => _logger.LogInformation("Our Timer Expired!");

    public bool CanRemove(string requesterUID) 
        => string.Equals(_applierUid, requesterUID);

    /// <summary> Swaps between two Hypnotic Effect by removing one, then applying another. </summary>
    public async Task SwapEffect(HypnoticOverlay overlay, string enactor)
    {
        if (HasValidEffect && !CanRemove(enactor))
        {
            _logger.LogWarning($"Cannot Switch! Current is Applied by [{_applierUid}]. Swapper [{enactor}] does not match!");
            return;
        }

        _logger.LogDebug($"Swapping Hypnotic Effect to: ({overlay.OverlayPath}) by [{enactor}]");
        await ExecuteWithSemaphore(async () =>
        {
            if (HasValidEffect)
                await RemoveAnimationInternal();

            await EquipAnimationInternal(overlay, enactor);
        });
    }

    /// <summary> Safely, manually remove a Hypno Effect. (Must match assigner) </summary>
    public async Task RemoveEffect(string enactor)
    {
        if (!HasValidEffect || !CanRemove(enactor))
            return;
        // Cancel, Dispose, Recreate all active tasks.
        // (This halts any progress on previous animation not yet finished.)
        _tasksCTS = _tasksCTS.SafeCancelRecreate();
        _opacityCTS = _opacityCTS.SafeCancelRecreate();
        // Run the Removeal Internal 
        await ExecuteWithSemaphore(RemoveAnimationInternal);
    }

    // Assumes the tasks have already been canceled and recreated.
    private async Task EquipAnimationInternal(HypnoticOverlay item, string enactor)
    {
        _logger.LogDebug($"{enactor} applied a hypnotic effect: ({item.OverlayPath})");
        // set the effect, image path, and applier 
        _activeEffect = item.Effect;
        _overlayPath = item.OverlayPath;
        _applierUid = enactor;
        _activeState = new HypnosisState { ImageColor = _activeEffect.ImageColor };

        // Assign the new Tasks.
        _colorTask = ColorTransposeTask(_activeEffect, _activeState, _tasksCTS.Token);
        _textTask = TextDisplayTask(_activeEffect, _activeState, _tasksCTS.Token);
        // Begin animation
        _activeState.ImageOpacity = 0;
        await AnimateOpacityInternal(_activeState.ImageOpacity, CkGui.GetAlpha(_activeEffect.ImageColor), _opacityCTS.Token);
    }

    private async Task RemoveAnimationInternal()
    {
        _logger.LogDebug($"Removing HypnoEffect applied by [{_applierUid}]");
        // Perform the removal animation!
        await AnimateOpacityInternal(_activeState.ImageOpacity, 0.0f, _opacityCTS.Token);
        // Upon completion, cancel the other tasks.
        _tasksCTS?.Cancel();
        _activeEffect = null;
        _overlayPath = string.Empty;
        _imageBytes = Array.Empty<byte>();
        _activeState = new HypnosisState();
        _applierUid = string.Empty;
    }

    private async Task AnimateOpacityInternal(float startOpacity, float endOpacity, CancellationToken token)
    {
        try
        {
            // Redefine duration based on how close the current transition is.
            var opacityDelta = Math.Abs(endOpacity - startOpacity);
            if (opacityDelta < 0.01f)
            {
                _activeState.ImageOpacity = endOpacity;
                return;
            }

            // Duration scales based on remaining distance
            var adjustedDuration = ANIMATION_DURATION_MS * opacityDelta;
            var start = DateTime.UtcNow;
            var progress = 0.0f;

            _logger.LogDebug($"Starting Hypnotic opacity transition: {startOpacity:F2} -> {endOpacity:F2}");
            while (progress < 1.0f && !token.IsCancellationRequested)
            {
                var elapsed = (float)(DateTime.UtcNow - start).TotalMilliseconds;
                progress = Math.Clamp(elapsed / adjustedDuration, 0.0f, 1.0f);
                // Perform SmoothStep easing for interpolation.
                _activeState.ImageOpacity = GsExtensions.Lerp(startOpacity, endOpacity, progress);
                
                await Task.Delay(Svc.Framework.UpdateDelta.Milliseconds, token);
            }
            // Set final Opacity
            _activeState.ImageOpacity = endOpacity;
        }
        catch (OperationCanceledException) { /* Consume */ }
        catch (Bagagwa ex) { _logger.LogError($"Error during opacity animation: {ex}"); }

        _logger.LogDebug($"HypnoEffect opacity transition completed: {startOpacity:F2} -> {_activeState.ImageOpacity:F2}");
    }

    public void DrawHypnoEffect()
    {
        if (_activeEffect is not { } effect)
            return;

        if (TextureManagerEx.GetMetadataPath(ImageDataType.Hypnosis, _overlayPath) is not { } hypnoImage)
            return;

        // Recalculate the necessary cycle speed that we should need for the rotation (may need optimizations later)
        var speed = _activeState.SpinSpeed * 0.001f;
        var direction = _activeEffect.Attributes.HasFlag(HypnoAttributes.InvertDirection) ? -1f : 1f;
        _activeState.Rotation += direction * (Svc.Framework.UpdateDelta.Milliseconds * speed);
        _activeState.Rotation %= MathF.PI * 2f;

        // Fetch the windows foreground drawlist to avoid conflict with other UI's & be layered ontop.
        var drawList = ImGui.GetForegroundDrawList();

        // Screen positions
        var screenSize = ImGui.GetIO().DisplaySize;
        var center = screenSize * 0.5f;

        // time for rotation maths
        var cos = MathF.Cos(_activeState.Rotation);
        var sin = MathF.Sin(_activeState.Rotation);

        // Impacted by zoom factor. (Nessisary for Pulsating)
        var corners = new[]
        {
            new Vector2(-hypnoImage.Width, -hypnoImage.Height) * _activeEffect.ZoomDepth,
            new Vector2(hypnoImage.Width, -hypnoImage.Height) * _activeEffect.ZoomDepth,
            new Vector2(hypnoImage.Width, hypnoImage.Height) * _activeEffect.ZoomDepth,
            new Vector2(-hypnoImage.Width, hypnoImage.Height) * _activeEffect.ZoomDepth
        };

        var rotatedBounds = new Vector2[4];
        for (var i = 0; i < corners.Length; i++)
        {
            var x = corners[i].X;
            var y = corners[i].Y;

            rotatedBounds[i] = new Vector2(
                center.X + (x * cos - y * sin) * _activeEffect.ZoomDepth,
                center.Y + (x * sin + y * cos) * _activeEffect.ZoomDepth
            );
        }

        // So we can account for transposing colors.
        var imgTint = _activeState.ImageColor;

        // Workaround the popup / windows cliprect and draw it at the correct dimentions.
        drawList.AddImageQuad(
            hypnoImage.ImGuiHandle,
            rotatedBounds[0],
            rotatedBounds[1],
            rotatedBounds[2],
            rotatedBounds[3],
            UVCorners[0],
            UVCorners[1],
            UVCorners[2],
            UVCorners[3],
            imgTint);

        if (string.IsNullOrEmpty(_activeState.CurrentText))
            return;

        // determine the font scalar.
        var fontScaler = UiFontService.FullScreenFont.Available
            ? (_activeEffect.TextFontSize / UiFontService.FullScreenFontPtr.FontSize) * _activeState.TextScale
            : _activeState.TextScale;

        // determine the new target position.
        var targetPos = _activeEffect.Attributes.HasAny(HypnoAttributes.LinearTextScale)
            ? center - Vector2.Lerp(_activeState.TextOffsetStart, _activeState.TextOffsetEnd, _activeState.TextScaleProgress)
            : center - (CkGui.CalcFontTextSize(_activeState.CurrentText, UiFontService.FullScreenFont) * fontScaler) * 0.5f;

        Svc.Logger.Debug($"Drawing Hypno Text: '{_activeState.CurrentText}' at {targetPos} with scale {fontScaler:F2}");

        drawList.OutlinedFontScaled(
            UiFontService.FullScreenFontPtr,
            UiFontService.FullScreenFontPtr.FontSize * fontScaler,
            targetPos,
            _activeState.CurrentText,
            ColorHelpers.ApplyOpacity(_activeEffect.TextColor, _activeState.TextOpacity),
            ColorHelpers.ApplyOpacity(_activeEffect.StrokeColor, _activeState.TextOpacity),
            _activeEffect.StrokeThickness);
    }

    /// <summary> Assignable Task that processes the color transpose effect. </summary>
    /// <remarks> Assumes you have already performed a cancelRecreate on the passed in token before calling. </remarks>
    public static async Task ColorTransposeTask(HypnoticEffect effect, HypnosisState state, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (effect.Attributes.HasAny(HypnoAttributes.TransposeColors))
                {
                    var start = ColorHelpers.RgbaUintToVector4(state.ImageColor);
                    var target = CkGui.InvertColor(start); // Keep Alpha.
                    await TransposeColor(state, effect.TransposeTime, start, target, token);
                    await TransposeColor(state, effect.TransposeTime, target, start, token);
                }
                else
                {
                    // Just chill out for like a second or 2 doing nothing.
                    await Task.Delay(500, token);
                }
            }
        }
        catch (TaskCanceledException) { /* Consume */ }
        catch (Bagagwa ex) { Svc.Logger.Error($"Error in ColorTransposeTask: {ex}"); }
    }

    /// <summary> Assignable Task that processes the displayText cycling. </summary>
    /// <remarks> Assumes you have already performed a cancelRecreate on the passed in token before calling. </remarks>
    public static async Task TextDisplayTask(HypnoticEffect effect, HypnosisState state, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
                await HandleTextDisplay(effect, state, token);
        }
        catch (TaskCanceledException) { /* Consume */ }
        catch (Bagagwa ex) { Svc.Logger.Error($"Error in TextDisplayTask: {ex}"); }
    }


    /// <summary> Internal helper task for the ColorTranspose loop. Can be canceled. </summary>
    private static async Task TransposeColor(HypnosisState state, int duration, Vector4 sCol, Vector4 tCol, CancellationToken token)
    {
        var startTime = DateTime.UtcNow;
        while (!token.IsCancellationRequested)
        {
            var elapsed = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var t = Math.Clamp(elapsed / duration, 0f, 1f);
            // Attempt Smoothstep Easing
            t = t * t * (3f - 2f * t);
            Vector4 lerped = new(
                GsExtensions.Lerp(sCol.X, tCol.X, t),
                GsExtensions.Lerp(sCol.Y, tCol.Y, t),
                GsExtensions.Lerp(sCol.Z, tCol.Z, t),
                sCol.W // Keep Alpha
            );
            // Set the state color.
            state.ImageColor = ColorHelpers.RgbaVector4ToUint(lerped);
            // If the elapsed time exceeds the transpose lifetime, break out of the loop.
            if (elapsed >= duration)
                break;

            await Task.Delay(Svc.Framework.UpdateDelta.Milliseconds, token);
        }
    }

    private static async Task HandleTextDisplay(HypnoticEffect effect, HypnosisState state, CancellationToken token)
    {
        // No message
        if (effect.DisplayMessages is not { Length: > 0 } words)
            state.CurrentText = string.Empty;
        // Only Message
        else if (words.Length == 1)
        {
            state.CurrentText = words[0];
            state.LastTextIndex = 0;
        }
        // Random Message
        else if (effect.Attributes.HasAny(HypnoAttributes.TextDisplayRandom))
        {
            int randomIdx;
            do { randomIdx = Random.Shared.Next(0, effect.DisplayMessages.Length); } while (randomIdx == state.LastTextIndex);

            state.LastTextIndex = randomIdx;
            state.CurrentText = effect.DisplayMessages[state.LastTextIndex];
        }
        // Sequential Message
        else
        {
            state.LastTextIndex = (state.LastTextIndex + 1) % effect.DisplayMessages.Length;
            state.CurrentText = effect.DisplayMessages[state.LastTextIndex];
        }

        // Start the DisplayText process with the visual appearance reflecting our set attributes.
        var doTextFade = effect.Attributes.HasAny(HypnoAttributes.TextFade);
        var linearScale = effect.Attributes.HasAny(HypnoAttributes.LinearTextScale);
        var randomScale = effect.Attributes.HasAny(HypnoAttributes.RandomTextScale);

        // Update the current opacity.
        state.TextOpacity = doTextFade ? 0f : CkGui.GetAlpha(effect.TextColor);
        state.TextScale = randomScale ? (0.75f + Random.Shared.NextSingle() * (1.35f - 0.75f)) : linearScale ? 0.8f : 1f;

        // If Scaling linearily, store the start and end draw regions.
        if (linearScale)
        {
            // Calculate it with the font pointer since we run this off the main thread.
            var sizeBase = CkGui.CalcTextSizeFontPtr(UiFontService.FullScreenFontPtr, state.CurrentText);
            var sizeScaled = sizeBase * (effect.TextFontSize / UiFontService.FullScreenFontPtr.FontSize);
            state.TextOffsetStart = (sizeScaled * 0.75f) * 0.5f; // offset from center
            state.TextOffsetEnd = (sizeScaled * 1.35f) * 0.5f; // offset from center
            state.TextScaleProgress = 0f; // Reset the progress for linear scaling.
        }

        var start = DateTime.UtcNow;
        var duration = effect.TextDisplayTime;
        var alpha = CkGui.GetAlpha(effect.TextColor);

        while (!token.IsCancellationRequested)
        {
            var elapsed = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            if (elapsed >= duration)
                break;

            state.TextScaleProgress = elapsed / (float)duration;

            if(doTextFade)
            {
                if (doTextFade && elapsed < effect.TextFadeInTime)
                    state.TextOpacity = GsExtensions.EaseInExpo(elapsed / (float)effect.TextFadeInTime);
                // Handle Opacity Fade Out.
                else if (doTextFade && elapsed >= (duration - effect.TextFadeOutTime))
                {
                    var fadeOutStart = duration - effect.TextFadeOutTime;
                    var value = (elapsed - fadeOutStart) / (float)effect.TextFadeOutTime;
                    state.TextOpacity = CkGui.GetAlpha(effect.TextColor) * GsExtensions.EaseOutExpo(1f - value);
                }
                // Handle simply displaying the text.
                else
                {
                    state.TextOpacity = CkGui.GetAlpha(effect.TextColor);
                }
            }

            if (linearScale) // Linear Scaling
                state.TextScale = GsExtensions.Lerp(0.75f, 1.35f, state.TextScaleProgress);

            // Lighten Stress Load a little bit.
            await Task.Delay(Svc.Framework.UpdateDelta.Milliseconds);
        }

        if (effect.Attributes.HasAny(HypnoAttributes.SpeedUpOnCycle))
            _ = AccelerateTemporarily(state, effect.SpeedupTime, duration, token);
    }

    // prevDuration is the latest time, and this time must be clamped.
    // time is the speedup time, which will be divided into 3 for its ramps.
    private static async Task AccelerateTemporarily(HypnosisState state, int speedupTime, int duration, CancellationToken token)
    {
        // No breaking the game plz.
        if (speedupTime < SPEED_BETWEEN_MIN)
            return;

        var holdTime = Math.Clamp(speedupTime, SPEED_BETWEEN_MIN, duration / 2);
        var original = state.SpinSpeed;
        state.SpinSpeed *= 2.5f;
        await Task.Delay(holdTime, token);
        state.SpinSpeed = original; // reset back to the original speed.
    }

    // Performs animation operations sequentially one after the other.
    private async Task ExecuteWithSemaphore(Func<Task> action)
    {
        // First, acquire the semaphore.
        await _animationSlim.WaitAsync();
        try
        {
            await action();
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"Error during semaphore execution: {ex}");
        }
        finally
        {
            _animationSlim.Release();
        }
    }
}

