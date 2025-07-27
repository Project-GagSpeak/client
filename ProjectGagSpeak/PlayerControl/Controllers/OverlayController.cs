using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Controls the players current POV.
/// </summary>
public sealed class POVController : DisposableMediatorSubscriberBase
{
    private readonly OverlayCache _cache;
    private CameraControlMode _forcedPerspective = CameraControlMode.Unknown;
    // MARE might require this but idk.
    // private bool _initialRedrawMade = false;

    public POVController(ILogger<POVController> logger, GagspeakMediator mediator, 
        OverlayCache cache) : base(logger, mediator)
    {
        _cache = cache;
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => UpdatePerspective());
    }

    public bool ShouldControlCamera => _cache.ShouldBeFirstPerson;

    private unsafe void UpdatePerspective()
    {
        if (!Svc.ClientState.IsLoggedIn)
            return;

        // If the forced perspective is not set, we can skip this.
        if (_forcedPerspective is CameraControlMode.Unknown)
            return;

        // Set the mode if it is not equal to the perspective we need to have.
        if (AddonCameraManager.IsActiveCameraValid && AddonCameraManager.ActiveMode != _forcedPerspective)
            AddonCameraManager.SetMode(_forcedPerspective);
    }

    /// <summary>
    ///     Sets the camera's perspective to a spesified control mode.
    /// </summary>
    private void SetCameraPerspective(CameraControlMode mode)
    {
        if (AddonCameraManager.IsActiveCameraValid)
            AddonCameraManager.SetMode(mode);
    }
}
