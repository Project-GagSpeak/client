using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxStruct
{
    [FieldOffset(0x38)] public byte Flags;
    [FieldOffset(0x50)] public Vector3 Position;
    [FieldOffset(0x60)] public Quat Rotation;
    [FieldOffset(0x70)] public Vector3 Scale;

    [FieldOffset(0x128)] public int ActorCaster;
    [FieldOffset(0x130)] public int ActorTarget;

    [FieldOffset(0x1B8)] public int StaticCaster;
    [FieldOffset(0x1C0)] public int StaticTarget;
}

[StructLayout(LayoutKind.Sequential)]
public struct Quat
{
    public float X;
    public float Z;
    public float Y;
    public float W;

    public static implicit operator Vector4(Quat pos) => new(pos.X, pos.Y, pos.Z, pos.W);

    // Would require adding SharpDX as a dependency
    /* public static implicit operator SharpDX.Vector4(Quat pos) => new(pos.X, pos.Z, pos.Y, pos.W); */

}

public unsafe class ActorVfx
{
    public VfxStruct* Vfx;
    public string Path;

    public ActorVfx(VfxStruct* vfx, string path)
    {
        Vfx = vfx;
        Path = path;
    }

    public void Update()
    {
        if (Vfx == null) return;
        Vfx->Flags |= 0x2;
    }

    public void UpdatePosition(Vector3 position)
    {
        if (Vfx == null) return;
        Vfx->Position = new Vector3
        {
            X = position.X,
            Y = position.Y,
            Z = position.Z
        };
    }

    public void UpdatePosition(IGameObject actor)
    {
        if (Vfx == null) return;
        Vfx->Position = actor.Position;
    }

    public void UpdateScale(Vector3 scale)
    {
        if (Vfx == null) return;
        Vfx->Scale = new Vector3
        {
            X = scale.X,
            Y = scale.Y,
            Z = scale.Z
        };
    }

    public void UpdateRotation(Vector3 rotation)
    {
        if (Vfx == null) return;

        var q = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
        Vfx->Rotation = new Quat
        {
            X = q.X,
            Y = q.Y,
            Z = q.Z,
            W = q.W
        };
    }
}
