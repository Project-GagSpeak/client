using System.Diagnostics;
using Microsoft.VisualBasic.CompilerServices;
using Penumbra.String.Functions;

namespace Penumbra.String;

/// <summary>
/// A wrapper around unsafe byte strings that ignores case for comparisons and hashes, that can either be owned or allocated in unmanaged space.
/// </summary>
/// <remarks>
/// Unowned strings may change their value and thus become corrupt, so they should not be stored without cloning. <br/>
/// The string can keep track of whether it is owned or not, being pure ASCII, ASCII-lowercase, or null-terminated.<br/>
/// Owned strings are always null-terminated.<br/>
/// </remarks>
public sealed unsafe partial class CiByteString : IReadOnlyList<byte>
{
    /// <summary> The pointer to the memory of the string. </summary>
    public byte* Path
        => _path;

    /// <summary> The length of the string. </summary>
    public int Length
        => _length;

    /// <summary> Returns whether the string is empty. </summary>
    public bool IsEmpty
        => Length == 0;

    /// <summary> The case-insensitive CRC32 hash. </summary>
    /// <remarks> This information is cached. </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public int CiCrc32
        => GetCiCrc32();

    /// <summary> The case-sensitive CRC32 hash. </summary>
    /// <remarks> This information is cached. </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public int Crc32
        => GetCrc32();

    /// <summary> Returns whether the current string consists purely of ASCII characters. </summary>
    /// <remarks> This information is cached. </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsAscii
        => CheckAscii();

    /// <summary> Returns whether the current string contains only ASCII lower-case or non-ASCII characters. </summary>
    /// <remarks> This information is cached. </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsAsciiLowerCase
        => CheckAsciiLower();

    /// <summary> Returns a ReadOnlySpan to the actual memory of the string using its length, without null-terminator. </summary>
    public ReadOnlySpan<byte> Span
        => new(_path, Length);

    /// <summary> Returns whether the current string is known to be null-terminated. </summary>
    public bool IsNullTerminated
        => _flags.HasFlag(Flags.NullTerminated);

    /// <summary> Returns whether the current string is owned, i.e. allocated in unmanaged space. </summary>
    public bool IsOwned
        => _flags.HasFlag(Flags.Owned);

    /// <inheritdoc />
    public IEnumerator<byte> GetEnumerator()

    {
        for (var i = 0; i < Length; ++i)
            yield return this[i];
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc />
    public int Count
        => Length;

    /// <summary> Access a specific byte in the string by index. </summary>
    /// <param name="index">The index of the requested byte.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> is less than 0 or larger than the string. </exception>
    public byte this[int index]
        => (uint)index < Length ? Path[index] : throw new IndexOutOfRangeException();

    /// <returns> The case-insensitive CRC32 hash of the string. </returns>
    public override int GetHashCode()
        => CiCrc32;

    /// <summary> Convert to ByteString. </summary>
    public static explicit operator ByteString(CiByteString s)
        => ByteString.FromByteStringUnsafe(s.Path, s.Length, s.IsNullTerminated, s.IsAsciiLowerInternal, s.IsAsciiInternal,
            s._flags.HasFlag(Flags.HasCrc32) ? s._crc32 : null);
}
