namespace Penumbra.String.Functions;

/// <summary>
/// Import some standard functions for memory handling.
/// </summary>
public static class MemoryUtility
{
    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    private static extern unsafe nint memcpy(void* dest, void* src, int count);

    /// <summary> Copies <paramref name="count"/> bytes from <paramref name="src"/> to <paramref name="dest"/>. </summary>
    /// <remarks>Call memcpy from msvcrt.dll.</remarks>
    public static unsafe void MemCpyUnchecked(void* dest, void* src, int count)
        => memcpy(dest, src, count);


    [DllImport("msvcrt.dll", EntryPoint = "memcmp", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    private static extern unsafe int memcmp(void* b1, void* b2, int count);

    /// <summary> Compares <paramref name="count"/> bytes from <paramref name="ptr1"/> with <paramref name="ptr2"/> lexicographically. </summary>
    /// <remarks>Call memcmp from msvcrt.dll.</remarks>
    public static unsafe int MemCmpUnchecked(void* ptr1, void* ptr2, int count)
        => memcmp(ptr1, ptr2, count);


    [DllImport("msvcrt.dll", EntryPoint = "_memicmp", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    private static extern unsafe int memicmp(void* b1, void* b2, int count);

    /// <summary> Compares <paramref name="count"/> bytes from <paramref name="ptr1"/> with <paramref name="ptr2"/> lexicographically and disregarding ascii-case. </summary>
    /// <remarks>Call memicmp from msvcrt.dll.</remarks>
    public static unsafe int MemCmpCaseInsensitiveUnchecked(void* ptr1, void* ptr2, int count)
        => memicmp(ptr1, ptr2, count);

    [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    private static extern unsafe void* memset(void* dest, int c, int count);

    /// <summary> Sets <paramref name="count"/> bytes from <paramref name="dest"/> on to <paramref name="value"/>. </summary>
    /// <remarks>Call memset from msvcrt.dll.</remarks>
    public static unsafe void* MemSet(void* dest, byte value, int count)
        => memset(dest, value, count);
}
