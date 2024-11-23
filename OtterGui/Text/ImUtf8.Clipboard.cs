using Dalamud.Memory;
using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Copy the given text to the clipboard. </summary>
    /// <param name="text"> The text as a UTF8 string. HAS to be null-terminated. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetClipboardText(ReadOnlySpan<byte> text)
        => ImGuiNative.igSetClipboardText(text.Start());

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="SetClipboardText(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetClipboardText(ref Utf8StringHandler<TextStringHandlerBuffer> text)
        => SetClipboardText(text.Span());

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="SetClipboardText(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetClipboardText(ReadOnlySpan<char> text)
        => SetClipboardText(text.Span<TextStringHandlerBuffer>());


    /// <summary> Obtain the current text from the clipboard. </summary>
    /// <returns> An owned, null-terminated span of UTF8 text. An empty (still null-terminated) span on failure. </returns>
    public static TerminatedByteString GetClipboardTextUtf8()
    {
        var ptr = ImGuiNative.igGetClipboardText();
        return ptr == null
            ? TerminatedByteString.Empty
            : MemoryHelper.CastNullTerminated<byte>((nint)ptr).CloneNullTerminated();
    }

    /// <inheritdoc cref="GetClipboardTextUtf8"/>
    /// <returns> A string of the current clipboard text. An empty string on failure. </returns>
    public static string GetClipboardText()
    {
        var ptr = ImGuiNative.igGetClipboardText();
        return ptr == null
            ? string.Empty
            : MemoryHelper.ReadStringNullTerminated((nint)ptr);
    }
}
