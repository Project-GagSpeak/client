using Dalamud.Game.Config;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.GameInternals.Structs;
using System.Runtime.InteropServices;
 
namespace GagSpeak.GameInternals.Detours;
#nullable enable
// Pulled from Lifestream, modified for use with Imprisonment. (RMI == Read Movement Input)
public unsafe class MovementOverride : IDisposable
{
    public bool Enabled
    {
        get => RMIWalkHook.IsEnabled;
        set
        {
            if (value)
                RMIWalkHook.Enable();
            else
                RMIWalkHook.Disable();
        }
    }

    // Where to move to.
    public Vector3 DesiredPosition;
    // Within how close is acceptable.
    public float Precision = 0.01f;

    // try and remove?
    private bool _legacyMode;

    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    [Signature(Signatures.RMIWalk)]
    private Hook<RMIWalkDelegate> RMIWalkHook = null!;

    public MovementOverride()
    {
        Svc.Hook.InitializeFromAttributes(this);
        Svc.GameConfig.UiControlChanged += OnConfigChanged;
        _legacyMode = Svc.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;
    }

    public void Dispose()
    {
        Svc.GameConfig.UiControlChanged -= OnConfigChanged;
        RMIWalkHook.Dispose();
    }

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

        var dist = DesiredPosition - player.Position;
        if (dist.LengthSquared() <= Precision * Precision)
            return null;

        var dirH = Angle.FromDirectionXZ(dist);
        var dirV = allowVertical ? Angle.FromDirection(new(dist.Y, new Vector2(dist.X, dist.Z).Length())) : default;

        var refDir = _legacyMode
            ? ((GameCamera*)CameraManager.Instance()->GetActiveCamera())->DirH.Radians() + 180.Degrees()
            : player.Rotation.Radians();
        return (dirH - refDir, dirV);
    }

    private void OnConfigChanged(object? sender, ConfigChangeEvent evt) 
        => _legacyMode = Svc.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;
}
