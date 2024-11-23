using ByteStringFunctions = Penumbra.String.Functions.ByteStringFunctions;

namespace Penumbra.String;

public sealed unsafe partial class ByteString : IDisposable
{
    /// <summary> 67108863 </summary>
    /// <returns> 67108863 </returns>
    public const int MaxLength = (int)FlagMask;

    /// <summary> Statically allocated null-terminator for empty strings to point to. </summary>
    private static readonly ByteStringFunctions.NullTerminator Null = new();

    /// <summary> An empty string of length 0 that is null-terminated. </summary>
    public static readonly ByteString Empty = new();

    // actual data members.
    private byte* _path;
    private uint  _length;
    private int   _crc32;

    /// <summary>
    /// Create a new empty string.
    /// </summary>
    public ByteString()
    {
        _path   =  Null.NullBytePtr;
        _length |= AsciiCheckedFlag | AsciiFlag | AsciiLowerCheckedFlag | AsciiLowerFlag | NullTerminatedFlag | AsciiFlag;
        _crc32  =  0;
    }

    /// <summary> Create a temporary ByteString from a byte pointer. </summary>
    /// <param name="path">A pointer to an existing string.</param>
    /// <param name="maxLength"> The maximum length until it stops looking for a null-terminator. </param>
    /// <remarks> This computes CRC, checks for ASCII and AsciiLower and assumes Null-Termination. </remarks>
    public ByteString(byte* path, int maxLength = int.MaxValue)
    {
        if (path == null)
        {
            _path   =  Null.NullBytePtr;
            _length |= AsciiCheckedFlag | AsciiFlag | AsciiLowerCheckedFlag | AsciiLowerFlag | NullTerminatedFlag | AsciiFlag;
            _crc32  =  0;
        }
        else
        {
            var length = ByteStringFunctions.ComputeCrc32AsciiLowerAndSize(path, out var crc32, out var lower, out var ascii, out var nullTerminated, maxLength);
            Setup(path, length, crc32, nullTerminated, false, lower, ascii);
        }
    }

    /// <summary> Create a temporary ByteString from a byte span. </summary>
    /// <param name="path">A null-terminated span of an existing string.</param>
    /// <remarks>
    /// This computes CRC, checks for ASCII and AsciiLower and assumes Null-Termination.
    /// </remarks>
    public ByteString(Span<byte> path)
        : this(path.Length == 0 ? null : (byte*) Unsafe.AsPointer(ref path[0]), path.Length)
    { }


    /// <summary>
    /// Construct a temporary ByteString from a given byte string of known size. 
    /// </summary>
    /// <param name="path">A pointer to an existing string.</param>
    /// <param name="length">The known length of the string.</param>
    /// <param name="isNullTerminated">Whether the string is known to be null-terminated or not.</param>
    /// <param name="isLower">Optionally, whether the string is known to only contain (ASCII) lower-case characters.</param>
    /// <param name="isAscii">Optionally, whether the string is known to only contain ASCII characters.</param>
    /// <param name="crc32">Optionally, the known CRC32 hash of the string.</param>
    /// <exception cref="ArgumentOutOfRangeException">If length is larger than <inheritdoc cref="MaxLength"/>.</exception>
    /// <remarks>Always computes the CRC32 for the path.</remarks>
    public static ByteString FromByteStringUnsafe(byte* path, int length, bool isNullTerminated, bool? isLower = null, bool? isAscii = false, int? crc32 = null)
        => new ByteString().Setup(path, length, crc32, isNullTerminated, false, isLower, isAscii);

    /// <inheritdoc cref="FromByteStringUnsafe(byte*, int, bool, bool?, bool?, int?)"/>
    public static ByteString FromSpanUnsafe(ReadOnlySpan<byte> path, bool isNullTerminated, bool? isLower = null, bool? isAscii = false)
    {
        fixed (byte* ptr = path)
        {
            return FromByteStringUnsafe(ptr, path.Length, isNullTerminated, isLower, isAscii);
        }
    }

    /// <summary>
    /// Construct an ByteString from a given unicode string, possibly converted to ascii lowercase.
    /// </summary>
    /// <param name="path">The existing UTF16 string.</param>
    /// <param name="ret">The converted, owned ByteString on success, an empty string on failure.</param>
    /// <param name="toAsciiLower">Optionally, whether to convert the string to lower-case.</param>
    /// <returns>False if the resulting length is larger than <inheritdoc cref="MaxLength"/>.</returns>
    public static bool FromString(string? path, out ByteString ret, bool toAsciiLower = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            ret = Empty;
            return true;
        }

        var p = ByteStringFunctions.Utf8FromString(path, out var l, (int)FlagMask);
        if (p == null)
        {
            ret = Empty;
            return false;
        }

        if (toAsciiLower)
            ByteStringFunctions.AsciiToLowerInPlace(p, l);

        ret = new ByteString().Setup(p, l, null, true, true, toAsciiLower ? true : null, l == path.Length);
        return true;
    }

    /// <summary>
    /// Construct an ByteString from a given unicode string without checking for length, and assuming <paramref name="isLower"/> from the argument.
    /// </summary>
    /// <param name="path">The existing UTF16 string.</param>
    /// <param name="isLower">Whether the string is known to be (ASCII) lower-case.</param>
    /// <returns>The converted, owned string.</returns>
    public static ByteString FromStringUnsafe(string? path, bool? isLower)
    {
        if (string.IsNullOrEmpty(path))
            return Empty;

        var p   = ByteStringFunctions.Utf8FromString(path, out var l);
        var ret = new ByteString().Setup(p, l, null, true, true, isLower, l == path.Length);
        return ret;
    }

    /// <summary> Free memory if the string is owned. </summary>
    private void ReleaseUnmanagedResources()
    {
        if (!IsOwned)
            return;

        PenumbraStringMemory.Free(_path, Length + 1);
        _length = AsciiCheckedFlag | AsciiFlag | AsciiLowerCheckedFlag | AsciiLowerFlag | NullTerminatedFlag;
        _path   = Null.NullBytePtr;
        _crc32  = 0;
    }

    /// <summary> Manually free memory. Sets the string to an empty string, also updates CRC32. </summary>
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary> Automatic release of memory if not disposed before. </summary>
    ~ByteString()
    {
        ReleaseUnmanagedResources();
    }

    /// <summary> Setup from all given values. </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal ByteString Setup(byte* path, int length, int? crc32, bool isNullTerminated, bool isOwned,
        bool? isLower = null, bool? isAscii = null)
    {
        if (length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(length));

        _path   = path;
        _length = (uint)length;
        _crc32  = crc32 ?? (int)~Lumina.Misc.Crc32.Get(new ReadOnlySpan<byte>(path, length));
        if (isNullTerminated)
            _length |= NullTerminatedFlag;

        if (isOwned)
            _length |= OwnedFlag;

        if (isLower != null)
        {
            _length |= AsciiLowerCheckedFlag;
            if (isLower.Value)
                _length |= AsciiLowerFlag;
        }

        if (isAscii != null)
        {
            _length |= AsciiCheckedFlag;
            if (isAscii.Value)
                _length |= AsciiFlag;
        }

        return this;
    }

    /// <summary>
    /// Check if the string is known to be or not be ASCII, otherwise test for it and store it in the cache.
    /// </summary>
    private bool CheckAscii()
    {
        switch (_length & (AsciiCheckedFlag | AsciiFlag))
        {
            case AsciiCheckedFlag:             return false;
            case AsciiCheckedFlag | AsciiFlag: return true;
            default:
                _length |= AsciiCheckedFlag;
                var isAscii = ByteStringFunctions.IsAscii(_path, Length);
                if (isAscii)
                    _length |= AsciiFlag;

                return isAscii;
        }
    }

    /// <summary>
    /// Check if the string is known to be or not be (ASCII) lower-case, otherwise test for it and store it in the cache.
    /// </summary>
    private bool CheckAsciiLower()
    {
        switch (_length & (AsciiLowerCheckedFlag | AsciiLowerFlag))
        {
            case AsciiLowerCheckedFlag:                  return false;
            case AsciiLowerCheckedFlag | AsciiLowerFlag: return true;
            default:
                _length |= AsciiLowerCheckedFlag;
                var isAsciiLower = ByteStringFunctions.IsAsciiLowerCase(_path, Length);
                if (isAsciiLower)
                    _length |= AsciiLowerFlag;

                return isAsciiLower;
        }
    }

    private bool? IsAsciiInternal
        => (_length & (AsciiCheckedFlag | AsciiFlag)) switch
        {
            AsciiCheckedFlag => false,
            AsciiFlag        => true,
            _                => null,
        };

    private bool? IsAsciiLowerInternal
        => (_length & (AsciiLowerCheckedFlag | AsciiLowerFlag)) switch
        {
            AsciiLowerCheckedFlag => false,
            AsciiLowerFlag        => true,
            _                     => null,
        };
}
