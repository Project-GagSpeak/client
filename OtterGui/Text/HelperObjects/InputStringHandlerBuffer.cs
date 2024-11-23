using Dalamud.Memory;
using ImGuiNET;

namespace OtterGui.Text.HelperObjects;

public unsafe struct InputStringHandlerBuffer : IStringHandlerBuffer
{
    public static int Size
        => 8 * 1024 * 1024;

    public static byte* Buffer   { get; }
    public static uint  LastId   { get; private set; }
    public static bool  IsActive { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Update(Span<byte> span, uint id)
    {
        span.CopyNullTerminated<InputStringHandlerBuffer>();
        LastId   = id;
        IsActive = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool Return(ReadOnlySpan<byte> oldValue, out TerminatedByteString result)
    {
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            IsActive = false;
            var length = Span.IndexOf((byte)0);
            result = new ReadOnlySpan<byte>(Buffer, length).CloneNullTerminated();
            return !oldValue.SequenceEqual(result);
        }

        if (ImGui.IsItemDeactivated())
            IsActive = false;

        result = TerminatedByteString.Empty;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool Return(ReadOnlySpan<char> oldValue, out string result)
    {
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            IsActive = false;
            result   = MemoryHelper.ReadStringNullTerminated((nint)Buffer);
            return !oldValue.Equals(result, StringComparison.Ordinal);
        }

        if (ImGui.IsItemDeactivated())
            IsActive = false;

        result = string.Empty;
        return false;
    }

    public static Span<byte> Span
        => new(Buffer, Size);

    private static readonly Cleaner Cleanup = new();

    private class Cleaner
    {
        ~Cleaner()
        {
            Marshal.FreeHGlobal((nint)Buffer);
        }
    }

    static InputStringHandlerBuffer()
        => Buffer = (byte*)Marshal.AllocHGlobal(Size);

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool Write(ReadOnlySpan<char> text, out byte* end)
        => IStringHandlerBuffer.Write<InputStringHandlerBuffer>(text, out end);
}
