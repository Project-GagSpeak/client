using ImGuiNET;
using OtterGuiInternal.Enums;

namespace OtterGuiInternal.Utility;

public static class StringHelpers
{
    /// <summary> The maximum size of a stack allocation for strings. </summary>
    public const int MaxStackAlloc = 2047;

    /// <summary> Add text to a draw list. </summary>
    /// <param name="drawList"> The draw list to use. </param>
    /// <param name="position"> The (global screen) position where the text should start. </param>
    /// <param name="color"> The color of the text. </param>
    /// <param name="text"> The text to be added. </param>
    /// <param name="checkSharp"> Whether to check for ## and skip them for labels. </param>
    public static unsafe void AddText(ImDrawListPtr drawList, Vector2 position, uint color, ReadOnlySpan<char> text, bool checkSharp)
    {
        var endIdx = checkSharp ? SplitStringWithoutNull(text).VisibleEnd : text.Length;
        if (endIdx == 0)
            return;

        var bytes = endIdx * 4 > MaxStackAlloc ? new byte[endIdx * 4] : stackalloc byte[endIdx * 4];
        fixed (byte* start = bytes)
        {
            var numBytes = Encoding.UTF8.GetBytes(text[..endIdx], bytes);
            ImGuiNative.ImDrawList_AddText_Vec2(drawList.NativePtr, position, color, start, start + numBytes);
        }
    }

    /// <summary> Compute the Id of a text and the end of its visible part. </summary>
    /// <param name="text"> The text to filter. </param>
    /// <param name="withNullChecking"> Whether to check for 0-characters in the text. </param>
    /// <returns>
    /// <list type="bullet">
    ///     <item>
    ///         <term>VisibleEnd</term>
    ///         <description>The index of the character one past the last visible character.</description>
    ///     </item>
    ///     <item>
    ///         <term>Id</term>
    ///         <description>The Id produced by that string with the current id stack.</description>
    ///     </item>
    /// </list>
    /// </returns>
    public static unsafe (int VisibleEnd, ImGuiId Id) ComputeId(ReadOnlySpan<char> text, bool withNullChecking = true)
    {
        var (visibleEnd, labelStart, labelEnd) = SplitString(text, withNullChecking);
        var bytes    = visibleEnd * 4 > MaxStackAlloc ? new byte[visibleEnd * 4] : stackalloc byte[visibleEnd * 4];
        var numBytes = Encoding.UTF8.GetBytes(text[labelStart..labelEnd], bytes);
        fixed (byte* start = bytes)
        {
            var id = ImGuiNative.igGetID_StrStr(start, start + numBytes);
            return (visibleEnd, (ImGuiId)id);
        }
    }

    /// <summary> Compute the size of a text using the current font. </summary>
    /// <param name="text"> The text to compute the size of. </param>
    /// <param name="hideTextAfterHash"> Whether to remove label parts after ## or ###. </param>
    /// <param name="wrapWidth"> The desired wrap width. </param>
    /// <param name="withNullChecking"> Whether to scan the text for early 0-characters at which it stops. </param>
    /// <returns> The size of the text.</returns>
    public static unsafe Vector2 ComputeSize(ReadOnlySpan<char> text, bool hideTextAfterHash = true, float wrapWidth = 0,
        bool withNullChecking = true)
    {
        if (hideTextAfterHash)
            text = text[..SplitString(text, withNullChecking).VisibleEnd];
        if (text.Length == 0)
            return Vector2.Zero;

        var bytes    = text.Length * 4 > MaxStackAlloc ? new byte[text.Length * 4] : stackalloc byte[text.Length * 4];
        var numBytes = Encoding.UTF8.GetBytes(text, bytes);
        fixed (byte* start = bytes)
        {
            var ret = Vector2.Zero;
            ImGuiNative.igCalcTextSize(&ret, start, start + numBytes, 0, wrapWidth);
            return ret;
        }
    }

    /// <summary> Compute the size of a text using the current font and the ID of it using the current ID stack. </summary>
    /// <param name="text"> The text to use. </param>
    /// <param name="wrapWidth"> The desired wrap width. </param>
    /// <param name="withNullChecking"> Whether to scan the text for early 0-characters at which it stops. </param>
    /// <returns>
    /// <list type="bullet">
    ///     <item>
    ///         <term>VisibleEnd</term>
    ///         <description>The index of the character one past the last visible character.</description>
    ///     </item>
    ///     <item>
    ///         <term>Size</term>
    ///         <description>The size of the visible text up to the first occurrence of ##.</description>
    ///     </item>
    ///     <item>
    ///         <term>Id</term>
    ///         <description>The Id produced by the text with the current id stack.</description>
    ///     </item>
    /// </list>
    /// </returns>
    public static unsafe (int VisibleEnd, Vector2 Size, ImGuiId Id) ComputeSizeAndId(ReadOnlySpan<char> text, float wrapWidth = 0,
        bool withNullChecking = true)
    {
        var (visibleEnd, labelStart, labelEnd) = SplitString(text, withNullChecking);
        var biggerSize      = Math.Max(visibleEnd, labelEnd - labelStart) * 4;
        var bytes           = biggerSize > MaxStackAlloc ? new byte[biggerSize] : stackalloc byte[biggerSize];
        var numBytesTotal   = Encoding.UTF8.GetBytes(text[..labelEnd], bytes);
        var numBytesVisible = visibleEnd == text.Length ? numBytesTotal : Encoding.UTF8.GetByteCount(text[..visibleEnd]);
        fixed (byte* start = bytes)
        {
            var size = Vector2.Zero;
            if (numBytesVisible > 0)
                ImGuiNative.igCalcTextSize(&size, start, start + numBytesVisible, 0, wrapWidth);
            var id = (ImGuiId)ImGuiNative.igGetID_StrStr(labelStart == 0 ? start : start + numBytesVisible, start + numBytesTotal);
            return (visibleEnd, size, id);
        }
    }

    /// <inheritdoc cref="SplitStringWithNull"/>
    /// <param name="text"> The text to scan. </param>
    /// <param name="withNullChecking"> Whether the scan should check for 0-characters that terminate the string early. </param>
    public static (int VisibleEnd, int LabelStart, int LabelEnd) SplitString(ReadOnlySpan<char> text, bool withNullChecking = true)
    {
        if (withNullChecking)
            return SplitStringWithNull(text);

        var (visibleEnd, labelStart) = SplitStringWithoutNull(text);
        return (visibleEnd, labelStart, text.Length);
    }

    /// <summary> Obtain the indices that denote the end of the visible part of a text string and the beginning of the label. </summary>
    /// <param name="text"> The text to scan. </param>
    /// <returns>
    /// <list type="bullet">
    ///     <item>
    ///         <term>VisibleEnd</term>
    ///         <description>The index of the character one past the last visible character.</description>
    ///     </item>
    ///     <item>
    ///         <term>LabelStart</term>
    ///         <description>Either the same as VisibleEnd (in case of '###') or 0.</description>
    ///     </item>
    /// </list>
    /// </returns>
    /// <remarks> Does not check for 0-characters. </remarks>
    public static (int VisibleEnd, int LabelStart) SplitStringWithoutNull(ReadOnlySpan<char> text)
    {
        var idx = text.IndexOf("##");
        if (idx < 0)
            return (text.Length, 0);

        if (idx < text.Length - 2 && text[idx + 2] == '#')
            return (idx, idx);

        return (idx, 0);
    }

    /// <summary> Obtain the indices that denote the end of the visible part of a text string, the beginning of the label, and the end of the label. </summary>
    /// <param name="text"> The text to scan. </param>
    /// <returns>
    /// <list type="bullet">
    ///     <item>
    ///         <term>VisibleEnd</term>
    ///         <description>The index of the character one past the last visible character.</description>
    ///     </item>
    ///     <item>
    ///         <term>LabelStart</term>
    ///         <description>Either the same as VisibleEnd (in case of '###') or 0.</description>
    ///     </item>
    ///     <item>
    ///         <term>LabelEnd</term>
    ///         <description>Either the length of the text, or the index of the first 0-character.</description>
    ///     </item>
    /// </list>
    /// </returns>
    public static (int VisibleEnd, int LabelStart, int LabelEnd) SplitStringWithNull(ReadOnlySpan<char> text)
    {
        var idx        = 0;
        var labelStart = 0;
        while (idx >= 0)
        {
            var newIdx = text[idx..].IndexOfAny("#\0");
            if (newIdx < 0)
                break;

            idx += newIdx;
            // We have not encountered ## before since that leads to a return.
            if (text[idx] == '\0')
                return (idx, 0, idx);

            // Check for ##
            if (idx < text.Length - 1 && text[idx + 1] == '#')
            {
                // Check for ###
                if (idx < text.Length - 2 && text[idx + 2] == '#')
                    labelStart = idx;

                // check End.
                newIdx = text[idx..].IndexOf('\0');
                return (idx, labelStart, newIdx >= 0 ? newIdx : text.Length);
            }

            ++idx;
        }

        return (text.Length, labelStart, text.Length);
    }
}
