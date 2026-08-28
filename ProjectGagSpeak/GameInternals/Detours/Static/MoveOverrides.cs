using CkCommons;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Structs;
using GagSpeak.PlayerControl;
using GagSpeak.Services;

namespace GagSpeak.GameInternals.Detours;
// made static in StaticDetours for multi-access, but idealy we should place it into movement detours, so they can be manually toggled.
public unsafe class MoveOverrides : IDisposable
{
    public bool InMoveTask => OverrideCamera || OverrideMoveInput;
    public bool OverrideCamera
    {
        get => CameraOverrideHook.IsEnabled;
        set
        {
            if (value) CameraOverrideHook.Enable();
            else CameraOverrideHook.Disable();
        }
    }

    public bool OverrideMoveInput
    {
        get => RMIWalkHook.IsEnabled;
        set
        {
            if (value) RMIWalkHook.Enable();
            else RMIWalkHook.Disable();
        }
    }

    // Where to move to. (can maybe make into a list but prefer not,
    // as it becomes pathing at this point.
    public Angle DesiredAzimuth;
    public Angle DesiredAltitude;
    public Angle SpeedH = 360.Degrees(); // per second
    public Angle SpeedV = 360.Degrees(); // per second
    public Vector3 TargetPos;
    public Vector3 PrevPos;
    public float Proximity = 0.01f;
    public bool _legacyMode;

    // Progress tracking, so a caller can tell 'walking' apart from 'wedged against a wall'.
    private bool _trackingProgress;
    private long _lastProgressTick;
    // Closest we have come to the target so far, to catch traveling confidently the wrong way.
    private float _closestDistanceXZ;
    private float _currentDistanceXZ;

    /// <summary> How long we have gone without meaningful displacement, in ms. 0 when not tracking. </summary>
    public long StalledFor => _trackingProgress ? Environment.TickCount64 - _lastProgressTick : 0;

    /// <summary> How much further from the target we are than our best approach. 0 when not tracking. </summary>
    public float DivergedBy => _trackingProgress ? _currentDistanceXZ - _closestDistanceXZ : 0f;

    public MoveOverrides()
    {
        Svc.Hook.InitializeFromAttributes(this);
        Svc.GameConfig.UiControlChanged += OnConfigChanged;
        _legacyMode = Svc.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;
    }

    public void Dispose()
    {
        Svc.GameConfig.UiControlChanged -= OnConfigChanged;
        RMIWalkHook?.SafeDispose();
        CameraOverrideHook?.SafeDispose();
    }

    private void OnConfigChanged(object? sender, ConfigChangeEvent evt)
        => _legacyMode = Svc.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;
    
    private delegate byte RMIWalkIsInputEnabledDelegate(void* self);
    [Signature(Signatures.RMIWalkIsInputEnabled1, Fallibility = Fallibility.Fallible)]
    private readonly RMIWalkIsInputEnabledDelegate? _rmiWalkIsInputEnabled1 = null;
    [Signature(Signatures.RMIWalkIsInputEnabled2, Fallibility = Fallibility.Fallible)]
    private readonly RMIWalkIsInputEnabledDelegate? _rmiWalkIsInputEnabled2 = null;

    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    [Signature(Signatures.RMIWalk)]
    private Hook<RMIWalkDelegate> RMIWalkHook = null!;
    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        RMIWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);

        if (bAdditiveUnk != 0 || !GameWantsWalkInput(self))
            return;

        if (DirectionToDestination(false) is not { } relDir)
            return;

        // Upstream only fills in a heading when the player is supplying none of their own, which is
        // right for a navigation convenience tool but is an escape hatch for forced movement: the
        // movement-key blocks never see LMB+RMB mouse-running, so it arrives here as player input and
        // would win. This hook is only live during a forced move task, so overwrite regardless.
        var dir = relDir.h.ToDirection();
        *sumLeft = dir.X;
        *sumForward = dir.Y;
    }

    /// <summary>
    ///   Mirrors the pair of checks PlayerMoveController::readInput performs. If the signatures
    ///   could not be resolved, we fall back to the old, weaker condition rather than doing nothing.
    /// </summary>
    private bool GameWantsWalkInput(void* self)
    {
        if (_rmiWalkIsInputEnabled1 is { } first && _rmiWalkIsInputEnabled2 is { } second)
            return first(self) != 0 && second(self) != 0;

        return !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BeingMoved];
    }

    /// <summary>
    ///   Offset between the camera's stored DirH and the direction it actually looks. <para />
    ///   In third person mode DirH is the character-to-camera orbit azimuth, so the look direction sits
    ///   180 degrees off it. In first person the camera rides the character and DirH already is the
    ///   look direction. Both verified against observed travel headings.
    /// </summary>
    private static Angle CameraLookOffset
        => AddonCameraManager.ActiveMode is CameraControlMode.FirstPerson ? default : 180.Degrees();

    /// <summary> The direction the camera is actually looking. </summary>
    private static Angle CameraLookDir(GameCamera* camera)
        => camera->DirH.Radians() + CameraLookOffset;

    /// <summary> The DirH value that would have the camera looking along <paramref name="look"/>. </summary>
    private static Angle AzimuthForLook(Angle look)
        => look - CameraLookOffset;

    private (Angle h, Angle v)? DirectionToDestination(bool allowVertical)
    {
        var player = Svc.Objects.LocalPlayer;
        if (player == null)
            return null;

        var dist = TargetPos - player.Position;
        if (dist.LengthSquared() <= Proximity * Proximity)
            return null;

        var dirH = Angle.FromDirectionXZ(dist);
        var dirV = allowVertical ? Angle.FromDirection(new(dist.Y, new Vector2(dist.X, dist.Z).Length())) : default;

        // Legacy movement is camera-relative, so it resolves against where the camera looks - which
        // is not DirH itself in every perspective, hence CameraLookDir.
        var refDir = _legacyMode
            ? CameraLookDir((GameCamera*)CameraManager.Instance()->GetActiveCamera())
            : player.Rotation.Radians();
        return (dirH - refDir, dirV);
    }

    private delegate void RMICameraDelegate(GameCamera* self, int inputMode, float speedH, float speedV);
    [Signature(Signatures.RMICamera)]
    private Hook<RMICameraDelegate> CameraOverrideHook = null!;
    private void RMICameraDetour(GameCamera* self, int inputMode, float speedH, float speedV)
    {
        CameraOverrideHook.Original(self, inputMode, speedH, speedV);
        var dt = Framework.Instance()->FrameDeltaTime;
        var deltaH = (DesiredAzimuth - self->DirH.Radians()).Normalized();
        var deltaV = (DesiredAltitude - self->DirV.Radians()).Normalized();
        var maxH = SpeedH.Rad * dt;
        var maxV = SpeedV.Rad * dt;
        self->InputDeltaH = Math.Clamp(deltaH.Rad, -maxH, maxH);
        self->InputDeltaV = Math.Clamp(deltaV.Rad, -maxV, maxV);
    }

    public void Disable()
    {
        OverrideCamera = false;
        OverrideMoveInput = false;
        TargetPos = Vector3.Zero;
        Proximity = 0.01f;
        // Reset progress tracking too, otherwise the next task starts measuring against a stale position.
        PrevPos = Vector3.Zero;
        _trackingProgress = false;
        _lastProgressTick = 0;
        _closestDistanceXZ = 0f;
        _currentDistanceXZ = 0f;
    }

    // a version of MoveToNode but for position.
    // returns true when it has reached the point within the specified proximity.
    // recommended to call disable after it returns true.
    public unsafe bool MoveToPoint(Vector3 point, float proximity = 0.01f)
    {
        if (!PlayerData.Available)
        {
            // Don't accrue stall time while zoning or otherwise unavailable.
            _trackingProgress = false;
            return false;
        }

        TargetPos = point;
        Proximity = proximity;

        // get where to go
        var toNext = point - PlayerData.Position;
        toNext.Y = 0;

        // if we are within the cage origin, stop and return complete.
        if (toNext.LengthSquared() <= proximity * proximity)
            return true;

        // otherwise, movement towards the cage origin.
        OverrideMoveInput = true;
        OverrideCamera = true;
        SpeedH = SpeedV = 360.Degrees();
        DesiredAzimuth = AzimuthForLook(Angle.FromDirectionXZ(TargetPos - PlayerData.Position));
        DesiredAltitude = -30.Degrees();

        TrackProgress(toNext.Length());
        return false;
    }

    /// <summary>
    ///   <see cref="MoveToPoint"/>, but returns null once we have made no progress for
    ///   <paramref name="stallMs"/>, which the task manager treats as a hard failure.
    /// </summary>
    public unsafe bool? MoveToPointOrFail(Vector3 point, float proximity, int stallMs, float divergeMargin)
    {
        if (MoveToPoint(point, proximity))
            return true;

        if (stallMs > 0 && StalledFor > stallMs)
            return null;

        // There is no pathing here, only a straight line, so steadily getting further away is never
        // legitimate progress - it means we are being driven somewhere we did not ask to go.
        if (divergeMargin > 0 && DivergedBy > divergeMargin)
            return null;

        return false;
    }

    /// <summary>
    ///   Records whether we actually displaced this frame, and jumps to try shaking loose of
    ///   whatever we are caught on when we did not.
    /// </summary>
    private unsafe void TrackProgress(float distanceXZ)
    {
        var now = Environment.TickCount64;
        var pos = PlayerData.Position;
        _currentDistanceXZ = distanceXZ;

        if (!_trackingProgress)
        {
            _trackingProgress = true;
            _lastProgressTick = now;
            _closestDistanceXZ = distanceXZ;
            PrevPos = pos;
            return;
        }

        if (distanceXZ < _closestDistanceXZ)
            _closestDistanceXZ = distanceXZ;

        var minSpeedAllowed = Control.Instance()->IsWalking ? 0.015f : 0.05f;
        if (Vector3.Distance(pos, PrevPos) >= minSpeedAllowed)
            _lastProgressTick = now;
        // Something is potentially obstructing our movement. If we have slowed to a crawl and have
        // not jumped recently, try jumping.
        else if (AgentMap.Instance()->IsPlayerMoving && HcTaskManager.ElapsedTime > 500 && !PlayerData.IsJumping)
        {
            if (NodeThrottler.Throttle("HcTaskFunc.Jump", 1250))
            {
                ChatControlService.SendGeneralActionCommand(2); // Jumping!
                Svc.Logger.Verbose("Jumping to try and get unstuck.");
            }
        }

        PrevPos = pos;
    }
}
