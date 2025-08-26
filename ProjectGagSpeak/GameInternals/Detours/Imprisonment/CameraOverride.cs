using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using GagSpeak.GameInternals.Structs;
using System.Runtime.InteropServices;
 
namespace GagSpeak.GameInternals.Detours;
#nullable enable
// Pulled from Lifestream, modified for use with Imprisonment.
public unsafe class CameraOverride : IDisposable
{
    public bool Enabled
    {
        get => CameraOverrideHook.IsEnabled;
        set
        {
            if (value)
                CameraOverrideHook.Enable();
            else
                CameraOverrideHook.Disable();
        }
    }

    public Angle DesiredAzimuth;
    public Angle DesiredAltitude;
    public Angle SpeedH = 360.Degrees(); // per second
    public Angle SpeedV = 360.Degrees(); // per second

    private delegate void RMICameraDelegate(GameCamera* self, int inputMode, float speedH, float speedV);
    [Signature(Signatures.RMICamera)]
    private Hook<RMICameraDelegate> CameraOverrideHook = null!;

    public CameraOverride()
        => Svc.Hook.InitializeFromAttributes(this);

    // be sure to dispose of the hooks!
    public void Dispose()
        => CameraOverrideHook.Dispose();

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
}
