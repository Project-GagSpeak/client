using CkCommons;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.GameInternals.Structs;
using GagSpeak.PlayerControl;
using GagSpeak.Services;

namespace GagSpeak.GameInternals.Detours;
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

    public MoveOverrides()
    {
        Svc.Hook.InitializeFromAttributes(this);
        Svc.GameConfig.UiControlChanged += OnConfigChanged;
        _legacyMode = Svc.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;
    }

    public void Dispose()
    {
        Svc.GameConfig.UiControlChanged -= OnConfigChanged;
        RMIWalkHook?.Dispose();
        CameraOverrideHook?.Dispose();
    }

    private void OnConfigChanged(object? sender, ConfigChangeEvent evt)
        => _legacyMode = Svc.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;

    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    [Signature(Signatures.RMIWalk)]
    private Hook<RMIWalkDelegate> RMIWalkHook = null!;
    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        RMIWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);

        var movementAllowed = bAdditiveUnk == 0 && !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BeingMoved];
        if (movementAllowed && *sumLeft == 0 && *sumForward == 0 && DirectionToDestination(false) is var relDir && relDir != null)
        {
            var dir = relDir.Value.h.ToDirection();
            *sumLeft = dir.X;
            *sumForward = dir.Y;
        }
    }

    private (Angle h, Angle v)? DirectionToDestination(bool allowVertical)
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null)
            return null;

        var dist = TargetPos - player.Position;
        if (dist.LengthSquared() <= Proximity * Proximity)
            return null;

        var dirH = Angle.FromDirectionXZ(dist);
        var dirV = allowVertical ? Angle.FromDirection(new(dist.Y, new Vector2(dist.X, dist.Z).Length())) : default;

        var refDir = _legacyMode
            ? ((GameCamera*)CameraManager.Instance()->GetActiveCamera())->DirH.Radians() + 180.Degrees()
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
    }

    // a version of MoveToNode but for position.
    // returns true when it has reached the point within the spesified proximity.
    // recommended to call disable after it returns true.
    public unsafe bool MoveToPoint(Vector3 point, float proximity = 0.01f)
    {
        if (!PlayerData.Available)
            return false;

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
        DesiredAzimuth = Angle.FromDirectionXZ(TargetPos - PlayerData.Position) + 180.Degrees();
        DesiredAltitude = -30.Degrees();

        // help with stuckage.
        if (AgentMap.Instance()->IsPlayerMoving)
        {
            var minSpeedAllowed = Control.Instance()->IsWalking ? 0.015f : 0.05f;
            if (HcTaskManager.ElapsedTime > 500 && !PlayerData.IsJumping)
            {
                if (PlayerData.DistanceTo(PrevPos) < minSpeedAllowed && NodeThrottler.Throttle("HcTaskFunc.Jump", 1250))
                {
                    ChatService.SendGeneralActionCommand(2); // Jumping!
                    Svc.Logger.Verbose("Jumping to try and get unstuck.");
                }
            }

            PrevPos = PlayerData.Position;
        }
        return false;
    }
}
