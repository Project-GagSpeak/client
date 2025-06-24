using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Unsure how this will be used atm. Look into later.
///     Idealy it should control the maximum of 1 of each effect that can be active at each time.
/// </summary>
/// <remarks>
///     It should also be able to accept sent effects from others with proper permissions,
///     however its best to put that into a handler or something.
/// </remarks>
public sealed class OverlayController : IDisposable
{
    private readonly ILogger<OverlayController> _logger;
    private readonly OverlayCache _cache;
    private readonly BlindfoldService _bfService;
    private readonly HypnoService _hypnoService;

    // Might need for mare fuckery.
    private CameraControlMode _forcedPerspective = CameraControlMode.Unknown;
    private bool _initialRedrawMade = false;

    public OverlayController(ILogger<OverlayController> logger, OverlayCache cache,
        BlindfoldService bfService, HypnoService hypnoService)
    {
        _logger = logger;
        _cache = cache;
        _bfService = bfService;
        _hypnoService = hypnoService;

        Svc.PluginInterface.UiBuilder.Draw += DrawOverlays;
    }

    public bool ShouldControlCamera => _cache.ShouldBeFirstPerson;
    public bool HasValidBlindfold => _bfService.HasValidBlindfold;
    public bool HasValidHypnoEffect => _hypnoService.HasValidEffect;

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= DrawOverlays;
    }

    private unsafe void DrawOverlays()
    {
        if (!Svc.ClientState.IsLoggedIn)
            return;

        // Handle Perspective
        ForceCameraPerspective();

        // Control Blindfold Draw
        _bfService.DrawBlindfoldOverlay();

        // Control Hypnosis Draw (Since it is 'under' the blindfold, but you 'see' it infront of the blindfold)
        _hypnoService.DrawHypnoEffect();
    }

    private void ForceCameraPerspective()
    {
        // If the forced perspective is not set, we can skip this.
        if (_forcedPerspective is CameraControlMode.Unknown)
            return;

        // Set the mode if it is not equal to the perspective we need to have.
        if (AddonCameraManager.IsActiveCameraValid && AddonCameraManager.ActiveMode != _forcedPerspective)
            AddonCameraManager.SetMode(_forcedPerspective);
    }

    public async Task ApplyBlindfold(BlindfoldOverlay overlay, string enactor)
    {
        // update the expected perspective.
        if (overlay.ForceFirstPerson)
            _forcedPerspective = CameraControlMode.FirstPerson;
        // Swap them and stuff.
        await _bfService.SwapBlindfold(overlay, enactor);
    }

    public async Task RemoveBlindfold()
    {
        // reset the forced perspective.
        if (_forcedPerspective is CameraControlMode.FirstPerson)
        {
            _forcedPerspective = CameraControlMode.Unknown;
            SetCameraPerspective(CameraControlMode.ThirdPerson);
        }
        // Remove the blindfold.
        await _bfService.RemoveBlindfold();
    }

    public async Task ApplyHypnoEffect(HypnoticOverlay overlay, string enactor)
    {
        // update the expected perspective.
        if (overlay.ForceFirstPerson)
            _forcedPerspective = CameraControlMode.FirstPerson;
        // Swap the hypno effect.
        await _hypnoService.SwapEffect(overlay, enactor);
    }

    public async Task RemoveHypnoEffect(string enactor)
    {
        // reset the forced perspective.
        if (_forcedPerspective is CameraControlMode.FirstPerson)
        {
            _forcedPerspective = CameraControlMode.Unknown;
            SetCameraPerspective(CameraControlMode.ThirdPerson);
        }
        // Remove the hypno effect.
        await _hypnoService.RemoveEffect(enactor);
    }

    /// <summary>
    ///     A generic helper method to set the camera to 
    /// </summary>
    /// <param name="mode"></param>
    private void SetCameraPerspective(CameraControlMode mode)
    {
        if (AddonCameraManager.IsActiveCameraValid)
            AddonCameraManager.SetMode(mode);
    }
}
