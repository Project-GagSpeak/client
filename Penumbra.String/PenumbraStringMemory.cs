using System.Diagnostics;
using System.Threading;

namespace Penumbra.String;

/// <summary> Class managing unmanaged memory for ByteStrings and CiByteStrings. </summary>
public static class PenumbraStringMemory
{
    private static ulong _allocatedBytes;
    private static ulong _freedBytes;

    private static ulong _allocatedStrings;
    private static ulong _freedStrings;

    /// <summary> The total number of allocated bytes for strings. </summary>
    /// <remarks> Only available when compiled in debug mode. </remarks>
    public static ulong AllocatedBytes
        => _allocatedBytes;

    /// <summary> The total number of freed bytes for strings. </summary>
    /// <remarks> Only available when compiled in debug mode. </remarks>
    public static ulong FreedBytes
        => _freedBytes;

    /// <summary> The current number of allocated bytes for strings. </summary>
    /// <remarks> Only available when compiled in debug mode. </remarks>
    public static ulong CurrentBytes
        => _allocatedBytes - _freedBytes;

    /// <summary> The total number of allocated strings. </summary>
    /// <remarks> Only available when compiled in debug mode. </remarks>
    public static ulong AllocatedStrings
        => _allocatedStrings;

    /// <summary> The total number of freed strings. </summary>
    /// <remarks> Only available when compiled in debug mode. </remarks>
    public static ulong FreedStrings
        => _freedStrings;

    /// <summary> The current number of allocated strings. </summary>
    /// <remarks> Only available when compiled in debug mode. </remarks>
    public static ulong CurrentStrings
        => _allocatedStrings - _freedStrings;

    internal static unsafe byte* Allocate(int size)
    {
        var ret = (byte*)Marshal.AllocHGlobal(size);
        GC.AddMemoryPressure(size);
        AllocateString((ulong)size);
        return ret;
    }

    internal static unsafe void Free(byte* ptr, int size)
    {
        Marshal.FreeHGlobal((nint)ptr);
        GC.RemoveMemoryPressure(size);
        FreeString((ulong)size);
    }

    [Conditional("DEBUG")]
    private static void AllocateString(ulong size)
    {
        Interlocked.Add(ref _allocatedBytes, size);
        Interlocked.Increment(ref _allocatedStrings);
    }

    [Conditional("DEBUG")]
    private static void FreeString(ulong size)
    {
        Interlocked.Add(ref _freedBytes, size);
        Interlocked.Increment(ref _freedStrings);
    }
}
