namespace Penumbra.String.Functions;

/// <summary> Wrapper class for all utility functions. </summary>
public static unsafe partial class ByteStringFunctions
{
    /// <summary>
    /// Replace all occurrences of from in a byte array of known length with to.
    /// </summary>
    public static int Replace(byte* ptr, int length, byte from, byte to)
    {
        var end         = ptr + length;
        var numReplaced = 0;
        for (; ptr < end; ++ptr)
        {
            if (*ptr == from)
            {
                *ptr = to;
                ++numReplaced;
            }
        }

        return numReplaced;
    }

    /// <summary>
    /// Convert a byte array of given length to ASCII-lowercase.
    /// </summary>
    public static void AsciiToLowerInPlace(byte* path, int length)
    {
        for (var i = 0; i < length; ++i)
            path[i] = AsciiLowerCaseBytes[path[i]];
    }

    /// <summary>
    /// Copy a byte array and convert the copy to ASCII-lowercase.
    /// </summary>
    public static byte* AsciiToLower(byte* path, int length)
    {
        var ptr = PenumbraStringMemory.Allocate(length + 1);
        ptr[length] = 0;
        for (var i = 0; i < length; ++i)
            ptr[i] = AsciiLowerCaseBytes[path[i]];

        return ptr;
    }
}
