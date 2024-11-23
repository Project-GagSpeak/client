using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Copy a UTF16 string to a null-terminated, owned UTF8 string. </summary>
    /// <param name="text"> The UTF16 string. </param>
    /// <returns> An owned UTF8 string that is null-terminated, but does not include the null-terminator in its length. </returns>
    public static TerminatedByteString GetBytes(ReadOnlySpan<char> text)
        => text.Span<TextStringHandlerBuffer>().CloneNullTerminated();

    /// <summary> Copy a formatted string to a null-terminated, owned UTF8 string. </summary>
    /// <param name="text"> The formatted string. </param>
    /// <returns> An owned UTF8 string that is null-terminated, but does not include the null-terminator in its length. </returns>
    public static TerminatedByteString GetBytes(ref Utf8StringHandler<TextStringHandlerBuffer> text)
        => text.Span().CloneNullTerminated();
}
