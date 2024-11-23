namespace Penumbra.String.Functions;

public static unsafe partial class ByteStringFunctions
{
    /// <summary> Used for static null-terminators. </summary>
    internal class NullTerminator
    {
        public readonly byte* NullBytePtr;

        internal NullTerminator()
        {
            NullBytePtr  = PenumbraStringMemory.Allocate(1);
            *NullBytePtr = 0;
        }

        ~NullTerminator()
            => PenumbraStringMemory.Free(NullBytePtr, 1);
    }

    /// <summary>
    /// Convert a C# unicode-string to an unmanaged UTF8-byte array and return the pointer.
    /// If the length would exceed the given maxLength, return a nullpointer instead.
    /// </summary>
    public static byte* Utf8FromString(string s, out int length, int maxLength = int.MaxValue)
    {
        length = Encoding.UTF8.GetByteCount(s);
        if (length >= maxLength)
            return null;

        var path = PenumbraStringMemory.Allocate(length + 1);
        fixed (char* ptr = s)
        {
            Encoding.UTF8.GetBytes(ptr, s.Length, path, length + 1);
        }

        path[length] = 0;
        return path;
    }

    /// <summary>
    /// Create a copy of a given string and return the pointer.
    /// </summary>
    public static byte* CopyString(byte* path, int length)
    {
        var ret = PenumbraStringMemory.Allocate(length + 1);
        MemoryUtility.MemCpyUnchecked(ret, path, length);
        ret[length] = 0;
        return ret;
    }

    /// <summary>
    /// Check the length of a null-terminated byte array no longer than the given maxLength.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int CheckLength(byte* path, int maxLength = int.MaxValue)
    {
        var end = path + maxLength;
        for (var it = path; it < end; ++it)
        {
            if (*it == 0)
                return (int)(it - path);
        }

        throw new ArgumentOutOfRangeException();
    }
}
