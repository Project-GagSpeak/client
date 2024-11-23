namespace Penumbra.String.Functions;

public static unsafe partial class ByteStringFunctions
{
    private static readonly byte[] AsciiLowerCaseBytes = Enumerable.Range(0, 256)
        .Select(i => i < 0x80 ? (byte)char.ToLowerInvariant((char)i) : (byte)i)
        .ToArray();

    private static readonly byte[] AsciiUpperCaseBytes = Enumerable.Range(0, 256)
        .Select(i => i < 0x80 ? (byte)char.ToUpperInvariant((char)i) : (byte)i)
        .ToArray();

    /// <summary>
    /// Convert a byte to its ASCII-lowercase version.
    /// </summary>
    public static byte AsciiToLower(byte b)
        => AsciiLowerCaseBytes[b];

    /// <summary>
    /// Check if a byte is ASCII-lowercase.
    /// </summary>
    public static bool AsciiIsLower(byte b)
        => AsciiToLower(b) == b;

    /// <summary>
    /// Convert a byte to its ASCII-uppercase version.
    /// </summary>
    public static byte AsciiToUpper(byte b)
        => AsciiUpperCaseBytes[b];

    /// <summary>
    /// Check if a byte is ASCII-uppercase.
    /// </summary>
    public static bool AsciiIsUpper(byte b)
        => AsciiToUpper(b) == b;

    /// <summary>
    /// Check if a byte array of given length is ASCII-lowercase.
    /// </summary>
    public static bool IsAsciiLowerCase(byte* path, int length)
    {
        var end = path + length;
        for (; path < end; ++path)
        {
            if (*path != AsciiLowerCaseBytes[*path])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Compare two byte arrays of given lengths ASCII-case-insensitive.
    /// </summary>
    public static int AsciiCaselessCompare(byte* lhs, int lhsLength, byte* rhs, int rhsLength)
    {
        if (lhsLength == rhsLength)
            return lhs == rhs ? 0 : MemoryUtility.MemCmpCaseInsensitiveUnchecked(lhs, rhs, rhsLength);

        if (lhsLength < rhsLength)
        {
            var cmp = MemoryUtility.MemCmpCaseInsensitiveUnchecked(lhs, rhs, lhsLength);
            return cmp != 0 ? cmp : -1;
        }

        var cmp2 = MemoryUtility.MemCmpCaseInsensitiveUnchecked(lhs, rhs, rhsLength);
        return cmp2 != 0 ? cmp2 : 1;
    }

    /// <summary>
    /// Check two byte arrays of given lengths for ASCII-case-insensitive equality.
    /// </summary>
    public static bool AsciiCaselessEquals(byte* lhs, int lhsLength, byte* rhs, int rhsLength)
    {
        if (lhsLength != rhsLength)
            return false;

        if (lhs == rhs || lhsLength == 0)
            return true;

        return MemoryUtility.MemCmpCaseInsensitiveUnchecked(lhs, rhs, lhsLength) == 0;
    }

    /// <summary>
    /// Check if a byte array of given length consists purely of ASCII characters.
    /// </summary>
    public static bool IsAscii(byte* path, int length)
    {
        var length8 = length / 8;
        var end8    = (ulong*)path + length8;
        for (var ptr8 = (ulong*)path; ptr8 < end8; ++ptr8)
        {
            if ((*ptr8 & 0x8080808080808080ul) != 0)
                return false;
        }

        var end = path + length;
        for (path += length8 * 8; path < end; ++path)
        {
            if (*path > 127)
                return false;
        }

        return true;
    }
}
