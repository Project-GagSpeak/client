using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Push a numerical ID to the ID stack and pop it on leaving scope. </summary>
    /// <param name="id"> The ID. </param>
    /// <returns> A disposable object that counts the number of pushes and can be used to push further IDs. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Id PushId(int id)
        => new Id().Push(id);

    /// <summary> Push a pointer ID to the ID stack and pop it on leaving scope. </summary>
    /// <param name="id"> The pointer. </param>
    /// <inheritdoc cref="PushId(int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Id PushId(nint id)
        => new Id().Push(id);

    /// <summary> Push a label ID to the ID stack and pop it on leaving scope. </summary>
    /// <param name="id"> The label as a UTF8 string. Does not have to be null-terminated. </param>
    /// <inheritdoc cref="PushId(int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Id PushId(ReadOnlySpan<byte> id)
        => new Id().Push(id);

    /// <param name="id"> The label as a UTF16 string and pop it on leaving scope. </param>
    /// <inheritdoc cref="PushId(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Id PushId(ReadOnlySpan<char> id)
        => new Id().Push(id);

    /// <param name="id"> The label as a formatted string and pop it on leaving scope. </param>
    /// <inheritdoc cref="PushId(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Id PushId(ref Utf8StringHandler<LabelStringHandlerBuffer> id)
        => new Id().Push(ref id);
}
