using CkCommons.Gui;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using ImGuiNET;

namespace GagSpeak.Services.Controller;

/// <summary> Manages the rendering of a blindfold overlay onto your screen. </summary>
public class BlindfoldService : IDisposable
{
    private const int ANIMATION_DURATION_MS = 1500;

    private readonly ILogger<BlindfoldService> _logger;
    private readonly MainConfig _config;

    // Animation Control
    private SemaphoreSlim _animationSlim = new(1, 1);
    private CancellationTokenSource _opacityAnimCTS = new();
    private float _currentOpacity = 0.0f;

    // Internally stored Item for display.
    // Not synced with cache intentionally to allow for animations.
    private BlindfoldOverlay? _appliedItem = null;
    private string            _applierUid  = string.Empty;

    public BlindfoldService(ILogger<BlindfoldService> logger, MainConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public bool HasValidBlindfold => _appliedItem is not null;
    public bool CanRemove(string enactor) => string.Equals(_applierUid, enactor);

    public void Dispose()
    {
        _logger.LogDebug("Disposing BlindfoldService");
        _opacityAnimCTS?.Cancel();
        _opacityAnimCTS?.Dispose();
    }

    /// <summary> Swaps between two blindfolds by removing one, then applying another. </summary
    public async Task SwapBlindfold(BlindfoldOverlay overlay, string enactor)
    {
        if (HasValidBlindfold && !CanRemove(enactor))
        {
            _logger.LogWarning($"Cannot Switch! Current is Applied by [{_applierUid}]. Swapper [{enactor}] does not match!");
            return;
        }

        _logger.LogDebug($"Swapping blindfolds: ({overlay.OverlayPath}) by [{enactor}]");
        await ExecuteWithSemaphore(async () =>
        {
            // If we have an applied item, remove it first.
            if (_appliedItem is not null)
                await RemoveAnimationInternal();

            // Now apply the new blindfold.
            await EquipAnimationInternal(overlay, enactor);
        });
    }

    /// <summary> Performs the equip animation then clears the stored data. Will inturrupt removal-animation. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task EquipBlindfold(BlindfoldOverlay overlay, string enactor)
    {
        // reset the token thingy.
        _opacityAnimCTS?.Cancel();
        _opacityAnimCTS?.Dispose();
        _opacityAnimCTS = new CancellationTokenSource();
        await ExecuteWithSemaphore(() => EquipAnimationInternal(overlay, enactor));
    }

    /// <summary> Performs the remove animation then clears the stored data. Will inturrupt equip-animation. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task RemoveBlindfold(string enactor)
    {
        if (!HasValidBlindfold || !CanRemove(enactor))
            return;

        // reset the token thingy.
        _opacityAnimCTS?.Cancel();
        _opacityAnimCTS?.Dispose();
        _opacityAnimCTS = new CancellationTokenSource();
        await ExecuteWithSemaphore(RemoveAnimationInternal);
    }


    private async Task EquipAnimationInternal(BlindfoldOverlay item, string enactor)
    {
        _logger.LogDebug($"{enactor} applied a blindfold: ({item.OverlayPath})");
        _appliedItem = item;
        _applierUid = enactor;
        _currentOpacity = 0.0f;
        // Perform the equip animation!
        await AnimateOpacityTransition(_currentOpacity, _config.Current.OverlayMaxOpacity, _opacityAnimCTS.Token);
    }

    private async Task RemoveAnimationInternal()
    {
        _logger.LogDebug($"Removing Blindfold: ({_appliedItem?.OverlayPath}) applied by [{_applierUid}]");
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
        catch (Bagagwa ex)
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

            _logger.LogDebug($"Starting blindfold opacity transition: {startOpacity:F2} -> {endOpacity:F2}");
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
        catch (Bagagwa ex)
        {
            _logger.LogError($"Error during opacity animation: {ex}");
        }

        _logger.LogDebug($"Blindfold opacity transition completed: {startOpacity:F2} -> {_currentOpacity:F2}");
    }

    public void DrawBlindfoldOverlay()
    {
        if (_appliedItem is null)
            return;

        if (TextureManagerEx.GetMetadataPath(ImageDataType.Blindfolds, _appliedItem.OverlayPath) is not { } blindfoldImage)
            return;

        // Fetch the windows foreground drawlist. This ensures that we do not conflict drawlist layers, with ChatTwo, or other windows.
        var foregroundList = ImGui.GetForegroundDrawList();

        // Screen positions
        var screenSize = ImGui.GetIO().DisplaySize;
        var screenCenter = screenSize / 2;

        // Display the blindfold to the screen. (Don't worry about UV's just yet.)
        foregroundList.AddImage(
            blindfoldImage.ImGuiHandle,
            Vector2.Zero,
            screenSize,
            Vector2.Zero,
            Vector2.One,
            CkGui.Color(new Vector4(1.0f, 1.0f, 1.0f, _currentOpacity))
        );
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

