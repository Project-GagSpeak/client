using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

public unsafe ref struct Id
{
    private int _counter;

    /// <inheritdoc cref="ImUtf8.PushId(int)"/>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public Id Push(int id)
    {
        ++_counter;
        ImGui.PushID(id);
        return this;
    }

    /// <inheritdoc cref="ImUtf8.PushId(nint)"/>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public Id Push(nint ptr)
    {
        ++_counter;
        ImGui.PushID(ptr);
        return this;
    }

    /// <inheritdoc cref="ImUtf8.PushId(ReadOnlySpan{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public Id Push(ReadOnlySpan<byte> label)
    {
        ++_counter;
        ImGuiNative.igPushID_StrStr(label.Start(out var end), end);
        return this;
    }

    /// <inheritdoc cref="ImUtf8.PushId(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public Id Push(ReadOnlySpan<char> label)
        => Push(label.Span<LabelStringHandlerBuffer>());

    /// <inheritdoc cref="ImUtf8.PushId(ref Utf8StringHandler{LabelStringHandlerBuffer})"/>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public Id Push(ref Utf8StringHandler<LabelStringHandlerBuffer> label)
#pragma warning disable CS9094
        => Push(label.Span());
#pragma warning restore CS9045

    /// <summary> Pop a number of IDs from the ID stack, but at most as many as this object pushed. </summary>
    /// <param name="count"> The number of IDs to pop. </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Pop(int count = 1)
    {
        if (count > _counter)
            count = _counter;
        _counter -= count;
        while (count-- > 0)
            ImGui.PopID();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
        => Pop(_counter);
}
