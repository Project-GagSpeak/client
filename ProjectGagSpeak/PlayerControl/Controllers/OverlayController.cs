using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Controls the players current POV.
/// </summary>
public sealed class POVController : DisposableMediatorSubscriberBase
{
    private readonly PlayerControlCache _cache;

    private CameraControlMode _perspective = CameraControlMode.Unknown;

    public POVController(ILogger<POVController> logger, GagspeakMediator mediator, 
        PlayerControlCache cache) : base(logger, mediator)
    {
        _cache = cache;

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreStatus());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => OnUpdate());
    }

    private void UpdateHardcoreStatus()
    {
        // update local perspective to match if different.
        var cachePerspective = _cache.GetPerspectiveToLock();
        if (_perspective != cachePerspective)
            _perspective = cachePerspective;
    }

    private unsafe void OnUpdate()
    {
        if (!Svc.ClientState.IsLoggedIn)
            return;

        // If the forced perspective is not set, we can skip this.
        if (_perspective is CameraControlMode.Unknown)
            return;

        // Set the mode if it is not equal to the perspective we need to have.
        if (AddonCameraManager.IsActiveCameraValid && AddonCameraManager.ActiveMode != _perspective)
            SetCameraPerspective(_perspective);
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
