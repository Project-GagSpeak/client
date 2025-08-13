using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.Services.Controller;

/// <summary> Manages the rendering of a blindfold overlay onto your screen. </summary>
public class BlindfoldService : IDisposable
{
    private const int ANIMATION_DURATION_MS = 1500;

    private readonly ILogger<BlindfoldService> _logger;
    private readonly MainConfig _config;

    // Animation Control
    private SemaphoreSlim _animationSlim = new(1, 1);
    private CancellationTokenSource _opacityCTS = new();
    private float _currentOpacity = 0.0f;

    // Internally stored Item for display.
    // Not synced with cache intentionally to allow for animations.
    private BlindfoldOverlay?   _appliedItem = null;
    private CombinedCacheKey    _activeSourceKey = CombinedCacheKey.Empty;
    private IDalamudTextureWrap?_storedImage;

    public BlindfoldService(ILogger<BlindfoldService> logger, MainConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public CombinedCacheKey ActiveSourceKey => _activeSourceKey;
    public string BlindfoldEnactor => _activeSourceKey.EnactorUID;
    public bool HasValidBlindfold => _appliedItem is not null;

    public void Dispose()
    {
        _logger.LogDebug("Disposing BlindfoldService");
        _opacityCTS?.Cancel();
        _opacityCTS?.Dispose();
    }

    public async Task ApplyBlindfold(BlindfoldOverlay overlay, CombinedCacheKey source)
    {
        if (!overlay.IsValid())
            _storedImage = await TextureManagerEx.RentMetadataPath(ImageDataType.Blindfolds, Constants.DefaultBlindfoldPath);
        else
            _storedImage = await TextureManagerEx.RentMetadataPath(ImageDataType.Blindfolds, overlay.OverlayPath);
        // Img was valid, so set other properties.
        _logger.LogDebug($"{BlindfoldEnactor} applied a blindfold from ({source.ToString()}): ({overlay.OverlayPath})");
        // set source and enactor.
        _activeSourceKey = source;
        _appliedItem = overlay;
        _opacityCTS = _opacityCTS.SafeCancelRecreate();
        _currentOpacity = 0.0f;
        // Perform the equip animation!
        await ExecuteWithSemaphore(() => AnimateOpacityTransition(_currentOpacity, _config.Current.OverlayMaxOpacity, _opacityCTS.Token));
    }

    /// <summary> 
    ///     Performs the remove animation then clears the stored data. 
    ///     Will inturrupt equip-animation.
    /// </summary>
    public async Task RemoveBlindfold()
    {
        if (!HasValidBlindfold)
            return;

        _logger.LogDebug($"Removing Blindfold: ({_appliedItem?.OverlayPath}), originally set by [{BlindfoldEnactor}]");

        // reset the token thingy.
        _opacityCTS = _opacityCTS.SafeCancelRecreate();
        // Then null the items and complete.
        await ExecuteWithSemaphore(() => AnimateOpacityTransition(_currentOpacity, 0.0f, _opacityCTS.Token));
        _appliedItem = null;
        _activeSourceKey = CombinedCacheKey.Empty;
        _storedImage?.Dispose();
        _storedImage = null;
    }

    private async Task AnimateOpacityTransition(float startOpacity, float endOpacity, CancellationToken token)
        => await Generic.Safe(async () => await AnimateOpacityInternal(startOpacity, endOpacity, token));

    private async Task AnimateOpacityInternal(float startOpacity, float endOpacity, CancellationToken token)
    {
        await Generic.Safe(async () =>
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
        });
        _logger.LogDebug($"Blindfold opacity transition completed: {startOpacity:F2} -> {_currentOpacity:F2}");
    }

    public void DrawBlindfoldOverlay()
    {
        if (_storedImage is not { } blindfoldImage)
            return;

        // Fetch the windows foreground drawlist. This ensures that we do not conflict drawlist layers, with ChatTwo, or other windows.
        var foregroundList = ImGui.GetForegroundDrawList();

        // Screen positions
        var screenSize = ImGui.GetIO().DisplaySize;
        var screenCenter = screenSize / 2;

        // Display the blindfold to the screen. (Don't worry about UV's just yet.)
        foregroundList.AddImage(
            blindfoldImage.Handle,
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

