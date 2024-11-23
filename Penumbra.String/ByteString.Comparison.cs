using Penumbra.String.Functions;

namespace Penumbra.String;

public sealed unsafe partial class ByteString : IEquatable<ByteString>, IComparable<ByteString>
{
    /// <param name="other">The string to compare with.</param>
    /// <returns>Whether this string and <paramref name="other"/> are equal.</returns>
    public bool Equals(ByteString? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return EqualsInternal(other);
    }

    /// <returns>Whether this string and the object <paramref name="obj"/> are equal. </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is ByteString other && EqualsInternal(other);

    /// <param name="other">The string to compare with.</param>
    /// <returns>Whether this string and <paramref name="other"/> are equal ignoring (ASCII) case.</returns>
    public bool EqualsCi(ByteString? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if ((IsAsciiLowerInternal ?? false) && (other.IsAsciiLowerInternal ?? false))
            return EqualsInternal(other);

        return ByteStringFunctions.AsciiCaselessEquals(_path, Length, other._path, other.Length);
    }

    /// <param name="other">The string to compare with.</param>
    /// <returns>Whether this string is lexicographically smaller (less than 0), bigger (greater than 0) or equal (0) to <paramref name="other"/>.</returns>
    public int CompareTo(ByteString? other)
    {
        if (ReferenceEquals(this, other))
            return 0;

        if (ReferenceEquals(null, other))
            return 1;

        return ByteStringFunctions.Compare(_path, Length, other._path, other.Length);
    }

    /// <param name="other">The string to compare with.</param>
    /// <returns>Whether this string is lexicographically smaller (less than 0), bigger (greater than 0) or equal (0) to <paramref name="other"/> ignoring (ASCII) case.</returns>
    public int CompareToCi(ByteString? other)
    {
        if (ReferenceEquals(null, other))
            return 0;

        if (ReferenceEquals(this, other))
            return 1;

        if ((IsAsciiLowerInternal ?? false) && (other.IsAsciiLowerInternal ?? false))
            return ByteStringFunctions.Compare(_path, Length, other._path, other.Length);

        return ByteStringFunctions.AsciiCaselessCompare(_path, Length, other._path, other.Length);
    }

    /// <param name="other">The prefix to check for.</param>
    /// <returns>Whether this string has <paramref name="other"/> as a prefix.</returns>
    public bool StartsWith(ByteString other)
    {
        var otherLength = other.Length;
        return otherLength <= Length && ByteStringFunctions.Equals(other.Path, otherLength, Path, otherLength);
    }

    /// <param name="other">The suffix to check for.</param>
    /// <returns>Whether this string has <paramref name="other"/> as a suffix.</returns>
    public bool EndsWith(ByteString other)
    {
        var otherLength = other.Length;
        var offset      = Length - otherLength;
        return offset >= 0 && ByteStringFunctions.Equals(other.Path, otherLength, Path + offset, otherLength);
    }

    /// <inheritdoc cref="StartsWith(ByteString)"/>
    public bool StartsWith(ReadOnlySpan<byte> chars)
    {
        if (chars.Length > Length)
            return false;

        return chars.SequenceEqual(new ReadOnlySpan<byte>(_path, chars.Length));
    }

    /// <inheritdoc cref="StartsWith(ByteString)"/>
    public bool EndsWith(ReadOnlySpan<byte> chars)
    {
        if (chars.Length > Length)
            return false;

        var ptr = _path + Length - chars.Length;
        return chars.SequenceEqual(new ReadOnlySpan<byte>(ptr, chars.Length));
    }

    /// <summary>
    /// Find the first occurrence of <paramref name="b"/> in this string.
    /// </summary>
    /// <param name="b">The needle.</param>
    /// <param name="from">An optional starting index in this string.</param>
    /// <returns>The index of the first occurrence of <paramref name="b"/> after <paramref name="from"/> or -1 if it is not found.</returns>
    public int IndexOf(byte b, int from = 0)
    {
        var end = _path + Length;
        for (var tmp = _path + from; tmp < end; ++tmp)
        {
            if (*tmp == b)
                return (int)(tmp - _path);
        }

        return -1;
    }

    /// <summary>
    /// Find the last occurrence of <paramref name="b"/> in this string.
    /// </summary>
    /// <param name="b">The needle.</param>
    /// <param name="to">An optional stopping index in this string.</param>
    /// <returns>The index of the last occurrence of <paramref name="b"/> before <paramref name="to"/> or -1 if it is not found.</returns>
    public int LastIndexOf(byte b, int to = 0)
    {
        var end = _path + to;
        for (var tmp = _path + Length - 1; tmp >= end; --tmp)
        {
            if (*tmp == b)
                return (int)(tmp - _path);
        }

        return -1;
    }

    /// <returns>Whether two strings are equal.</returns>
    public static bool operator ==(ByteString lhs, ByteString? rhs)
        => lhs.Equals(rhs);

    /// <returns>Whether two strings are different.</returns>
    public static bool operator !=(ByteString lhs, ByteString rhs)
        => !lhs.Equals(rhs);


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool EqualsInternal(ByteString other)
        => _crc32 == other._crc32 && ByteStringFunctions.Equals(_path, Length, other._path, other.Length);
}
