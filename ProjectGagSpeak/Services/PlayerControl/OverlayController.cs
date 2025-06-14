using GagSpeak.CkCommons.Gui;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;

namespace GagSpeak.Services.Control;

/// <summary>
///     Unsure how this will be used atm. Look into later.
///     Idealy it should control the maximum of 1 of each effect that can be active at each time.
/// </summary>
/// <remarks>
///     It should also be able to accept sent effects from others with proper permissions,
///     however its best to put that into a handler or something.
/// </remarks>
public sealed class OverlayController : DisposableMediatorSubscriberBase
{
    private readonly PlayerData _player;
    private readonly BlindfoldService _blindfoldService;
    private readonly HypnoService _hypnoService;
    public OverlayController(
        ILogger<OverlayController> logger,
        GagspeakMediator mediator,
        PlayerData player,
        BlindfoldService blindfoldService,
        HypnoService hypnoService)
        : base(logger, mediator)
    {
        _player = player;
        _blindfoldService = blindfoldService;
        _hypnoService = hypnoService;

        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    /// <summary> Lame overhead necessary to avoid mare conflicts with first person fuckery. </summary>
    public bool InitialBlindfoldRedrawMade = false;

    /// <summary>
    ///     The forced perspective we want to make the camera have.
    /// </summary>
    public CameraControlMode ForcedPerspective { get; private set; } = CameraControlMode.Unknown;

    private unsafe void FrameworkUpdate()
    {
        // If the forced perspective is not set, we can skip this.
        if (ForcedPerspective is CameraControlMode.Unknown)
            return;

        // Set the mode if it is not equal to the perspective we need to have.
        if (AddonCameraManager.IsActiveCameraValid && AddonCameraManager.ActiveMode != ForcedPerspective)
            AddonCameraManager.SetMode(ForcedPerspective);
    }

    /// <summary>
    ///     A generic helper method to set the camera to 
    /// </summary>
    /// <param name="mode"></param>
    public void SetCameraPerspective(CameraControlMode mode)
    {
        if (AddonCameraManager.IsActiveCameraValid)
            AddonCameraManager.SetMode(mode);
    }
}
