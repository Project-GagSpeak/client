using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using ImGuiNET;
using System.Timers;
using Timer = System.Timers.Timer;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Manages the rendering of a hypnosis overlay onto your screen.
/// </summary>
public class HypnoService
{
    private const int ANIMATION_DURATION_MS =  1500;
    private const float SPIRAL_SPEED_SCALE_MAX = 0.003f;
    private const float SPIRAL_SCALE_SCALE_MIN = 0.000f;

    private readonly ILogger<HypnoService> _logger;
    private readonly MainConfig _config;
    private readonly CosmeticService _disp;

    // Animation Control
    private SemaphoreSlim _animationSlim = new(1, 1);
    private CancellationTokenSource _opacityAnimCTS = new();
    private float _currentOpacity = 0.0f;

    // Internally stored Item for display.
    // Not synced with cache intentionally to allow for animations.
    private HypnoticOverlay? _appliedItem = null;
    private string _applierUid = string.Empty;

    // Overlay control.
    private readonly Timer _textDisplayTimer;
    private string _currentText = string.Empty;
    private float _currentRotation;
    private float _spiralScale = 1f;
    private int _lastTextIdx;

    // Self Expiration Timeouts.
    private readonly Timer _expireTimer;

    // Display Corners.
    private readonly Vector2[] _hypnoUV = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];

    public HypnoService(ILogger<HypnoService> logger, MainConfig config, CosmeticService render)
    {
        _logger = logger;
        _config = config;
        _disp = render;

        // Initialize the timer for the Hypnotic animation
        _textDisplayTimer = new Timer(int.MaxValue) { AutoReset = true };
        _textDisplayTimer.Elapsed += ToNextPhrase;
    }

    public bool HasValidEffect => _appliedItem is not null;
    public bool CanRemoveEffect => string.Equals(_applierUid, MainHub.UID);

    /// <summary> Swaps between two Hypnotic Effect by removing one, then applying another. </summary
    public async Task SwapHypnoEffect(HypnoticOverlay overlay, string enactor)
    {
        _logger.LogDebug($"Swapping Hypnotic Effect to: ({overlay.OverlayPath}) by [{enactor}]");
        await ExecuteWithSemaphore(async () =>
        {
            // If we have an applied item, remove it first.
            if (_appliedItem is not null)
                await RemoveAnimationInternal();

            // Now apply the new Hypnotic Effect.
            await EquipAnimationInternal(overlay, enactor);
        });
    }

    /// <summary> Performs the equip animation then clears the stored data. Will inturrupt removal-animation. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task EquipHypnoEffect(HypnoticOverlay overlay, string enactor)
    {
        // reset the token thingy.
        _opacityAnimCTS?.CancelDispose();
        _opacityAnimCTS = new CancellationTokenSource();
        await ExecuteWithSemaphore(() => EquipAnimationInternal(overlay, enactor));
    }

    /// <summary> Performs the remove animation then clears the stored data. Will inturrupt equip-animation. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task RemoveHypnoEffect()
    {
        if (_appliedItem is null)
            return;
        // reset the token thingy.
        _opacityAnimCTS?.CancelDispose();
        _opacityAnimCTS = new CancellationTokenSource();
        await ExecuteWithSemaphore(RemoveAnimationInternal);
    }


    private async Task EquipAnimationInternal(HypnoticOverlay item, string enactor)
    {
        _logger.LogDebug($"{enactor} applied a hypnotic effect: ({item.OverlayPath})");
        _appliedItem = item;
        _applierUid = enactor;
        _textDisplayTimer.Interval = 1000;
        _textDisplayTimer.Start();
        _currentOpacity = 0.0f;
        // Perform the equip animation!
        await AnimateOpacityTransition(_currentOpacity, _config.Current.OverlayMaxOpacity, _opacityAnimCTS.Token);

        // handle expiration.
    }

    private async Task RemoveAnimationInternal()
    {
        _logger.LogDebug($"Removing HypnoEffect: ({_appliedItem?.OverlayPath}) applied by [{_applierUid}]");
        // Perform the removal animation!
        await AnimateOpacityTransition(_currentOpacity, 0.0f, _opacityAnimCTS.Token);
        // Then null the items and complete.
        _appliedItem = null;
        _applierUid = string.Empty;
    }

    private async Task AnimateOpacityTransition(float startOpacity, float endOpacity, CancellationToken token)
    {
        try
        {
            await AnimateOpacityInternal(startOpacity, endOpacity, token);
        }
        catch (OperationCanceledException) { /* Consume */ }
        catch (Exception ex)
        {
            _logger.LogError($"Error during opacity animation: {ex}");
        }
    }

    private async Task AnimateOpacityInternal(float startOpacity, float endOpacity, CancellationToken token)
    {
        try
        {
            // Redefine duration based on how close the current transition is.
            var opacityDelta = Math.Abs(endOpacity - startOpacity);
            if (opacityDelta < 0.01f)
            {
                _currentOpacity = endOpacity;
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
                _currentOpacity = GsExtensions.Lerp(startOpacity, endOpacity, progress);

                // Perform a small delay before processing the next progress point in opacity transition.
                await Task.Delay(20, token);
            }

            // Ensure we set the final opacity to the end value.
            _currentOpacity = endOpacity;
        }
        catch (OperationCanceledException) { /* Consume */ }
        catch (Exception ex)
        {
            _logger.LogError($"Error during opacity animation: {ex}");
        }

        _logger.LogDebug($"HypnoEffect opacity transition completed: {startOpacity:F2} -> {_currentOpacity:F2}");
    }

    public void DrawHypnoEffect()
    {
        if (_appliedItem is null || _appliedItem?.Effect is not { } effect)
            return;

        if (_disp.GetImageMetadataPath(ImageDataType.Hypnosis, _appliedItem.OverlayPath) is not { } hypnoImage)
            return;

        // Recalculate the necessary cycle speed that we should need for the rotation (may need optimizations later)
        var speed = (SPIRAL_SPEED_SCALE_MAX - SPIRAL_SCALE_SCALE_MIN) * (effect.SpinSpeed * 0.01f) + SPIRAL_SCALE_SCALE_MIN;
        _currentRotation += Svc.Framework.UpdateDelta.Milliseconds * speed;
        _currentRotation %= MathF.PI * 2f;

        // Fetch the windows foreground drawlist to avoid conflict with other UI's & be layered ontop.
        var foregroundList = ImGui.GetForegroundDrawList();

        // Screen positions
        var screenSize = ImGui.GetIO().DisplaySize;
        var screenCenter = screenSize * 0.5f;

        // time for rotation maths
        var cos = MathF.Cos(_currentRotation);
        var sin = MathF.Sin(_currentRotation);

        var corners = new[]
        {
            new Vector2(-hypnoImage.Width, -hypnoImage.Height),
            new Vector2(hypnoImage.Width, -hypnoImage.Height),
            new Vector2(hypnoImage.Width, hypnoImage.Height),
            new Vector2(-hypnoImage.Width, hypnoImage.Height)
        };

        var rotatedBounds = new Vector2[4];
        for (var i = 0; i < corners.Length; i++)
        {
            var x = corners[i].X;
            var y = corners[i].Y;

            rotatedBounds[i] = new Vector2(
                screenCenter.X + (x * cos - y * sin) * _spiralScale,
                screenCenter.Y + (x * sin + y * cos) * _spiralScale
            );
        }

        var imgTint = effect.TintColor;

        // Display the image stretched to the bounds of the screen and stuff.
        foregroundList.AddImageQuad(
            hypnoImage.ImGuiHandle,
            rotatedBounds[0],
            rotatedBounds[1],
            rotatedBounds[2],
            rotatedBounds[3],
            _hypnoUV[0],
            _hypnoUV[1],
            _hypnoUV[2],
            _hypnoUV[3],
            imgTint);

        // Can set the windows font scale here if we want for the attributes.


        // Display the text in the center of the spiral.
        using (UiFontService.FullScreenFont.Push())
        {
            var size = ImGui.CalcTextSize(_currentText);
            CkGui.OutlinedFont(foregroundList, _currentText, screenCenter - size * 0.5f, effect.TextColor, 0xFF000000, 3);
        }

        // Then can pop the font scale here.
    }

    private void ToNextPhrase(object? sender, ElapsedEventArgs e)
    {
        // abort if there is no effect active.
        if (_appliedItem is null || _appliedItem?.Effect is not { } effect)
            return;

        // Set the current text to nothing and return if no text was assigned.
        if (effect.DisplayWords.Length <= 0)
        {
            _currentText = string.Empty;
            return;
        }
        // if only one was assigned, just set it to that.
        else if (effect.DisplayWords.Length == 1)
        {
            _currentText = effect.DisplayWords[0];
            return;
        }


        if (effect.Attributes.HasAny(HypnoAttributes.TextIsRandom))
        {
            var randomIndex = Random.Shared.Next(0, effect.DisplayWords.Length);
            // If the random INDEX (not text) is the same as the current text, pick again.
            // This should only ever occur with more than one index, so it should be fine.
            while (_lastTextIdx == randomIndex)
                randomIndex = Random.Shared.Next(0, effect.DisplayWords.Length);

            _lastTextIdx = randomIndex;
            _currentText = effect.DisplayWords[randomIndex];
            return;
        }

        // We should do it sequentially if we are not random.
        _lastTextIdx++;
        _lastTextIdx %= effect.DisplayWords.Length;
        _currentText = effect.DisplayWords[_lastTextIdx];
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
        catch (Exception ex)
        {
            _logger.LogError($"Error during semaphore execution: {ex}");
        }
        finally
        {
            _animationSlim.Release();
        }
    }
}

