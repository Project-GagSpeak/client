namespace Penumbra.GameData.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct HalfMatrix2x2(Half uu, Half uv, Half vu, Half vv) : IEquatable<HalfMatrix2x2>
{
    public static readonly HalfMatrix2x2 Zero     = new(Half.Zero, Half.Zero, Half.Zero, Half.Zero);
    public static readonly HalfMatrix2x2 Identity = new(Half.One,  Half.Zero, Half.Zero, Half.One);

    public Half UU = uu;
    public Half UV = uv;
    public Half VU = vu;
    public Half VV = vv;

    public static HalfMatrix2x2 Compose(Vector2 scale, float rotation, float shear)
    {
        var vLength = scale.Y / MathF.Cos(shear);
        var vAngle  = shear + MathF.PI * 0.5f + rotation;
        return new(
            (Half)(scale.X * MathF.Cos(rotation)),
            (Half)(vLength * MathF.Cos(vAngle)),
            (Half)(scale.X * MathF.Sin(rotation)),
            (Half)(vLength * MathF.Sin(vAngle)));
    }

    public readonly void Decompose(out Vector2 scale, out float rotation, out float shear)
    {
        var floats = (Vector4)this;
        var scaleX = MathF.Sqrt(floats.X * floats.X + floats.Z * floats.Z);
        rotation   = MathF.Atan2(floats.Z, floats.X);
        shear      = MathF.Atan2(floats.W, floats.Y) - MathF.PI * 0.5f - rotation;
        if (shear < -MathF.PI)
            shear += MathF.Tau;
        else if (shear > MathF.PI)
            shear -= MathF.Tau;
        var scaleY = MathF.Sqrt(floats.Y * floats.Y + floats.W * floats.W) * MathF.Cos(shear);
        scale      = new(scaleX, scaleY);
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is HalfMatrix2x2 other && Equals(other);

    public readonly bool Equals(HalfMatrix2x2 other)
        => UU == other.UU && UV == other.UV && VU == other.VU && VV == other.VV;

    public override readonly int GetHashCode()
        => HashCode.Combine(UU, UV, VU, VV);

    public static HalfMatrix2x2 ScaledIdentity(Half scale)
        => new(scale, Half.Zero, Half.Zero, scale);

    public static explicit operator Vector4(HalfMatrix2x2 mat)
        => new((float)mat.UU, (float)mat.UV, (float)mat.VU, (float)mat.VV);

    public static explicit operator HalfMatrix2x2(Vector4 mat)
        => new((Half)mat.X, (Half)mat.Y, (Half)mat.Z, (Half)mat.W);

    public static bool operator ==(HalfMatrix2x2 left, HalfMatrix2x2 right)
        => left.Equals(right);

    public static bool operator !=(HalfMatrix2x2 left, HalfMatrix2x2 right)
        => !left.Equals(right);
}
