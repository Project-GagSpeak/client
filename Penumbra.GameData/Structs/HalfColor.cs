namespace Penumbra.GameData.Structs;

/// <summary>
/// A RGB tuple of <see cref="Half"/>. Used for color tables notably.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct HalfColor(Half red, Half green, Half blue) : IEquatable<HalfColor>
{
    public static readonly HalfColor Black = new(Half.Zero, Half.Zero, Half.Zero);
    public static readonly HalfColor White = new(Half.One, Half.One, Half.One);

    public Half Red   = red;
    public Half Green = green;
    public Half Blue  = blue;

    public readonly void Deconstruct(out Half red, out Half green, out Half blue)
    {
        red   = Red;
        green = Green;
        blue  = Blue;
    }

    public override readonly bool Equals(object? obj)
        => obj is HalfColor other && Equals(other);

    public readonly bool Equals(HalfColor other)
        => Red == other.Red && Green == other.Green && Blue == other.Blue;

    public override int GetHashCode()
        => HashCode.Combine(Red, Green, Blue);

    public static explicit operator Vector3(HalfColor color)
        => new((float)color.Red, (float)color.Green, (float)color.Blue);

    public static explicit operator HalfColor(Vector3 color)
        => new((Half)color.X, (Half)color.Y, (Half)color.Z);

    public static bool operator ==(HalfColor left, HalfColor right)
        => left.Equals(right);

    public static bool operator !=(HalfColor left, HalfColor right)
        => !left.Equals(right);
}
