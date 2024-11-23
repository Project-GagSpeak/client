using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using JetBrains.Annotations;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

// ReSharper disable EntityNameCapturedOnly.Global

namespace OtterGui.Text;

[PublicAPI]
public static unsafe partial class ImUtf8
{
    /// <summary> Draw text. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(ReadOnlySpan<byte> text)
        => ImGuiNative.igTextUnformatted(text.Start(out var end), end);

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="Text(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(ReadOnlySpan<char> text)
        => Text(text.Span<TextStringHandlerBuffer>());

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="Text(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(ref Utf8StringHandler<TextStringHandlerBuffer> text)
        => Text(text.Span());


    /// <summary> Draw text in a given color. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="color"> The desired color as RGBA. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(ReadOnlySpan<byte> text, uint color)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        Text(text);
    }

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="Text(ReadOnlySpan{byte}, uint)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(ReadOnlySpan<char> text, uint color)
        => Text(text.Span<TextStringHandlerBuffer>(), color);

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="Text(ReadOnlySpan{char}, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(ref Utf8StringHandler<TextStringHandlerBuffer> text, uint color)
        => Text(text.Span(), color);
}
