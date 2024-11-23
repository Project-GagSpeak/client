namespace OtterGui.Text;

/// <summary> A null-terminated UTF8 string. </summary>
public readonly struct TerminatedByteString
{
    private static readonly byte[]               EmptyArray = [0];
    public static readonly  TerminatedByteString Empty      = new(EmptyArray);

    public TerminatedByteString()
        => _text = EmptyArray;

    public TerminatedByteString(ReadOnlySpan<byte> text)
    {
        _text              = new byte[text.Length + 1];
        _text[text.Length] = 0;
        text.CopyTo(_text);
    }

    public TerminatedByteString(ReadOnlySpan<char> text)
    {
        var bytes = Encoding.UTF8.GetByteCount(text);
        _text        = new byte[bytes + 1];
        _text[bytes] = 0;
        Encoding.UTF8.GetBytes(text, _text);
    }

    internal TerminatedByteString(byte[] text)
        => _text = text;

    private readonly byte[] _text;

    public IReadOnlyList<byte> TextWithNull
        => _text;

    public int Length
        => _text.Length - 1;

    public int Count
        => Length;

    public bool IsEmpty
        => _text.Length <= 1;

    public static implicit operator ReadOnlySpan<byte>(TerminatedByteString text)
        => text._text.AsSpan(^1);

    public override string ToString()
        => Encoding.UTF8.GetString(_text.AsSpan(^1));
}
