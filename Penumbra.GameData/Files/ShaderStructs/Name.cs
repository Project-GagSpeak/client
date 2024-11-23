namespace Penumbra.GameData.Files.ShaderStructs;

/// <summary>
/// Represents a shader resource (constant, sampler, texture, material parameter, ...) CRC32 identifier, along with the corresponding name if it is known.
/// </summary>
/// <remarks>
/// The name is used only for user interface purposes, and purposely omitted from comparisons and related operations.
/// </remarks>
public readonly struct Name : IEquatable<Name>, IComparable<Name>
{
    public const string UnknownPrefix = "\uFFFD";

    public static readonly Name Empty = new(string.Empty);

    public readonly string? Value;
    public readonly uint    Crc32;

    public bool IsValueAuthoritative
        => Value != null && Crc32 == Lumina.Misc.Crc32.Get(Value, 0xFFFFFFFFu);

    public Name(uint crc32)
    {
        Crc32 = crc32;
        Value = null;
    }

    public Name(string value)
    {
        Crc32 = Lumina.Misc.Crc32.Get(value, 0xFFFFFFFFu);
        Value = value;
    }

    public Name(ReadOnlySpan<byte> value)
    {
        Crc32 = Lumina.Misc.Crc32.Get(value, 0xFFFFFFFFu);
        Value = Encoding.UTF8.GetString(value);
    }

    public Name(uint crc32, string? value)
    {
        Crc32 = crc32;
        Value = value;
    }

    public bool Equals(Name other)
        => Crc32 == other.Crc32;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Name other && Equals(other);

    public int CompareTo(Name other)
        => Crc32.CompareTo(other.Crc32);

    public override int GetHashCode()
        => unchecked((int)Crc32);

    public override string ToString()
        => Value ?? $"0x{Crc32:X8}";

    public static implicit operator Name(uint crc32)
        => new(crc32);

    public static implicit operator Name(string value)
        => new(value);

    public static implicit operator Name(ReadOnlySpan<byte> value)
        => new(value);

    public static bool operator ==(Name left, Name right)
        => left.Equals(right);

    public static bool operator !=(Name left, Name right)
        => !left.Equals(right);

    public static bool operator <(Name left, Name right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(Name left, Name right)
        => left.CompareTo(right) <= 0;

    public static bool operator >(Name left, Name right)
        => left.CompareTo(right) > 0;

    public static bool operator >=(Name left, Name right)
        => left.CompareTo(right) >= 0;

    public static Name operator +(Name left, string right)
        => new(Lumina.Misc.Crc32.Get(right, ~left.Crc32), (left.Value ?? UnknownPrefix) + right);

    public static Name operator +(Name left, ReadOnlySpan<byte> right)
        => new(Lumina.Misc.Crc32.Get(right, ~left.Crc32), (left.Value ?? UnknownPrefix) + Encoding.UTF8.GetString(right));
}
