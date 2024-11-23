namespace Penumbra.String;

/// <summary>
/// A wrapper around unsafe byte strings that can either be owned or allocated in unmanaged space.
/// </summary>
/// <remarks>
/// Unowned strings may change their value and thus become corrupt, so they should not be stored without cloning. <br/>
/// The string keeps track of whether it is owned or not, being pure ASCII, ASCII-lowercase, or null-terminated.<br/>
/// Owned strings are always null-terminated.<br/>
/// Any constructed string will compute its own CRC32-value (as long as the string itself is not changed).
/// </remarks>
public sealed unsafe partial class ByteString : IEnumerable<byte>
{
    // We keep information on some of the state of the ByteString in specific bits.
    // This costs some potential max size, but that is not relevant for our case.
    // Except for destruction/dispose, or if the non-owned pointer changes values,
    // the CheckedFlag, AsciiLowerCaseFlag and AsciiFlag are the only things that are mutable.
    private const uint NullTerminatedFlag    = 0x80000000;
    private const uint OwnedFlag             = 0x40000000;
    private const uint AsciiCheckedFlag      = 0x04000000;
    private const uint AsciiFlag             = 0x08000000;
    private const uint AsciiLowerCheckedFlag = 0x10000000;
    private const uint AsciiLowerFlag        = 0x20000000;
    private const uint FlagMask              = 0x03FFFFFF;

    /// <summary> Returns whether the current string is known to be null-terminated. </summary>
    public bool IsNullTerminated
        => (_length & NullTerminatedFlag) != 0;

    /// <summary> Returns whether the current string is owned, i.e. allocated in unmanaged space. </summary>
    public bool IsOwned
        => (_length & OwnedFlag) != 0;

    /// <summary> Returns whether the current string consists purely of ASCII characters. </summary>
    /// <remarks> This information is cached. </remarks>
    public bool IsAscii
        => CheckAscii();

    /// <summary> Returns whether the current string contains only ASCII lower-case or non-ASCII characters. </summary>
    /// <remarks> This information is cached. </remarks>
    public bool IsAsciiLowerCase
        => CheckAsciiLower();

    /// <summary> Returns the pointer to the actual memory of the string. </summary>
    public byte* Path
        => _path;

    /// <summary> Returns the CRC32 value of the string. </summary>
    /// <remarks> This value is computed on construction and stored. </remarks>
    public int Crc32
        => _crc32;

    /// <summary> Returns the length of the string. </summary>
    public int Length
        => (int)(_length & FlagMask);

    /// <summary> Returns whether the string is empty. </summary>
    public bool IsEmpty
        => Length == 0;

    /// <summary> Returns a ReadOnlySpan to the actual memory of the string using its length, without null-terminator. </summary>
    public ReadOnlySpan<byte> Span
        => new(_path, Length);

    /// <summary> Access a specific byte in the string by index. </summary>
    /// <param name="idx">The index of the requested byte.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="idx"/> is less than 0 or larger than the string. </exception>
    public byte this[int idx]
        => (uint)idx < Length ? _path[idx] : throw new IndexOutOfRangeException();

    /// <returns>An iterator over all the bytes of the string.</returns>
    public IEnumerator<byte> GetEnumerator()
    {
        for (var i = 0; i < Length; ++i)
            yield return this[i];
    }

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    // Only not readonly due to dispose.
    // ReSharper disable once NonReadonlyMemberInGetHashCode
    /// <returns>The CRC32 hash of the string.</returns>
    public override int GetHashCode()
        => _crc32;


    /// <summary> Convert to CiByteString. </summary>
    public static explicit operator CiByteString(ByteString s)
        => new CiByteString().Setup(s.Path, s.Length, null, s.Crc32, s.IsNullTerminated, false, s.IsAsciiLowerInternal, s.IsAsciiInternal);
}
