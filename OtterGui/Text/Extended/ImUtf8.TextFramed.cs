using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui.Text.HelperObjects;
using OtterGuiInternal;
using OtterGuiInternal.Structs;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Draw the given text framed as if it were a button but without interactivity. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="frameColor"> The color of the frame. </param>
    /// <param name="size"> The size of the frame. If 0, the text size is used. Otherwise, Text is aligned according to ButtonTextAlign and corners are rounded according to style. </param>
    /// <param name="textColor"> The color of the text. If 0, the current style is used. </param>
    /// <param name="borderColor"> The color of the frame border. If 0, no border will be drawn. </param>
    public static unsafe void TextFramed(ReadOnlySpan<byte> text, uint frameColor, Vector2 size = default, uint textColor = 0,
        uint borderColor = 0)
    {
        var textSize = CalcTextSize(text, false);
        if (size.X == 0)
            size.X = textSize.X + 2 * FramePadding.X;
        if (size.Y == 0)
            size.Y = textSize.Y + 2 * FramePadding.Y;

        var pos  = ImGui.GetCursorScreenPos();
        var rect = new ImRect(pos, pos + size);
        using var color = ImRaii.PushColor(ImGuiCol.Border, borderColor, borderColor != 0)
            .Push(ImGuiCol.Text, textColor, textColor != 0);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, GlobalScale, borderColor != 0);
        ImGuiInternal.RenderFrame(rect, frameColor, borderColor != 0, Style.FrameRounding);
        ImGuiNativeInterop.RenderTextClippedEx(ImGui.GetWindowDrawList().NativePtr, rect.Min, rect.Max, text.Start(out var textEnd), textEnd,
            (ImVec2*)&textSize, Style.ButtonTextAlign, null);
        ImGuiInternal.ItemSize(rect, FramePadding.Y);
        ImGuiInternal.ItemAdd(rect, 0);
    }

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="TextFramed(ReadOnlySpan{byte}, uint, Vector2, uint, uint)"/>
    /// <exception cref="ImUtf8FormatException" />
    public static void TextFramed(ReadOnlySpan<char> text, uint frameColor, Vector2 size = default, uint textColor = 0,
        uint borderColor = 0)
        => TextFramed(text.Span<TextStringHandlerBuffer>(), frameColor, size, textColor, borderColor);

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="TextFramed(ReadOnlySpan{char}, uint, Vector2, uint, uint)"/>
    public static void TextFramed(ref Utf8StringHandler<TextStringHandlerBuffer> text, uint frameColor, Vector2 size = default,
        uint textColor = 0, uint borderColor = 0)
        => TextFramed(text.Span(), frameColor, size, textColor, borderColor);
}
