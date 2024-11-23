namespace Penumbra.String.Functions;

public static unsafe partial class ByteStringFunctions
{
    /// <summary>
    /// Lexicographically compare two byte arrays of given length.
    /// </summary>
    public static int Compare(byte* lhs, int lhsLength, byte* rhs, int rhsLength)
    {
        if (lhsLength == rhsLength)
            return lhs == rhs ? 0 : MemoryUtility.MemCmpUnchecked(lhs, rhs, rhsLength);

        if (lhsLength < rhsLength)
        {
            var cmp = MemoryUtility.MemCmpUnchecked(lhs, rhs, lhsLength);
            return cmp != 0 ? cmp : -1;
        }

        var cmp2 = MemoryUtility.MemCmpUnchecked(lhs, rhs, rhsLength);
        return cmp2 != 0 ? cmp2 : 1;
    }

    /// <summary>
    /// Lexicographically compare one byte array of given length with a null-terminated byte array of unknown length.
    /// </summary>
    public static int Compare(byte* lhs, int lhsLength, byte* rhs)
    {
        var end = lhs + lhsLength;
        for (var tmp = lhs; tmp < end; ++tmp, ++rhs)
        {
            if (*rhs == 0)
                return 1;

            var diff = *tmp - *rhs;
            if (diff != 0)
                return diff;
        }

        return 0;
    }

    /// <summary>
    /// Lexicographically compare two null-terminated byte arrays of unknown length not larger than maxLength.
    /// </summary>
    public static int Compare(byte* lhs, byte* rhs, int maxLength = int.MaxValue)
    {
        var end = lhs + maxLength;
        for (var tmp = lhs; tmp < end; ++tmp, ++rhs)
        {
            if (*lhs == 0)
                return *rhs == 0 ? 0 : -1;

            if (*rhs == 0)
                return 1;

            var diff = *tmp - *rhs;
            if (diff != 0)
                return diff;
        }

        return 0;
    }

    /// <summary>
    /// Check two byte arrays of given length for equality.
    /// </summary>
    public static bool Equals(byte* lhs, int lhsLength, byte* rhs, int rhsLength)
    {
        if (lhsLength != rhsLength)
            return false;

        if (lhs == rhs || lhsLength == 0)
            return true;

        return MemoryUtility.MemCmpUnchecked(lhs, rhs, lhsLength) == 0;
    }

    /// <summary>
    /// Check one byte array of given length for equality against a null-terminated byte array of unknown length.
    /// </summary>
    private static bool Equal(byte* lhs, int lhsLength, byte* rhs)
        => Compare(lhs, lhsLength, rhs) == 0;

    /// <summary>
    /// Check two null-terminated byte arrays of unknown length not larger than maxLength for equality.
    /// </summary>
    private static bool Equal(byte* lhs, byte* rhs, int maxLength = int.MaxValue)
        => Compare(lhs, rhs, maxLength) == 0;
}
