using ByteStringFunctions = Penumbra.String.Functions.ByteStringFunctions;

namespace Penumbra.String;

/// <summary> What meta-data for a byte string should be precomputed on construction. </summary>
[Flags]
public enum MetaDataComputation
{
    /// <summary> Pre-compute nothing. Usually at least the case-insensitive CRC32 is needed. </summary>
    None = 0x00,

    /// <summary> Pre-compute the case-insensitive CRC32, which is used as the general hash. </summary>
    CiCrc32 = 0x01,

    /// <summary> Pre-compute the case-sensitive CRC32. This should not generally be needed. </summary>
    Crc32 = 0x02,

    /// <summary> Pre-compute whether the string is entirely in ASCII-lower case. </summary>
    AsciiLowerCase = 0x04,

    /// <summary> Pre-compute whether the string is not using any characters outside the ASCII range. </summary>
    Ascii = 0x08,

    /// <summary> Pre-compute everything. </summary>
    All = CiCrc32 | Crc32 | AsciiLowerCase | Ascii,
}

public sealed unsafe partial class CiByteString : IDisposable
{
    /// <summary> Statically allocated null-terminator for empty strings to point to. </summary>
    private static readonly ByteStringFunctions.NullTerminator Null = new();

    /// <summary> An empty string of length 0 that is null-terminated. </summary>
    public static readonly CiByteString Empty = new();

    [Flags]
    private enum Flags : uint
    {
        Owned                 = 0x01,
        NullTerminated        = 0x02,
        Ascii                 = 0x04,
        AsciiChecked          = 0x08,
        AsciiLowerCase        = 0x10,
        AsciiLowerCaseChecked = 0x20,
        HasCiCrc32            = 0x40,
        HasCrc32              = 0x80,
    }

    private const Flags EmptyFlags = Flags.NullTerminated
      | Flags.Ascii
      | Flags.AsciiChecked
      | Flags.AsciiLowerCase
      | Flags.AsciiLowerCaseChecked
      | Flags.HasCiCrc32
      | Flags.HasCrc32;

    // actual data members.
    private byte* _path;
    private int   _length;
    private int   _ciCrc32;
    private int   _crc32;
    private Flags _flags;

    /// <summary>
    /// Create a new empty string.
    /// </summary>
    public CiByteString()
    {
        _path    = Null.NullBytePtr;
        _length  = 0;
        _ciCrc32 = 0;
        _crc32   = 0;
        _flags   = EmptyFlags;
    }

    /// <summary> Create a temporary ByteString from a byte pointer which should be null-terminated. </summary>
    /// <param name="path"> A pointer to an existing string. </param>
    /// <param name="flags"> Which meta information to precompute. </param>
    /// <param name="maxLength"> The maximum length to scan for a null-terminator. </param>
    public CiByteString(byte* path, MetaDataComputation flags = MetaDataComputation.None, int maxLength = int.MaxValue)
    {
        if (path == null)
        {
            _path    =  Null.NullBytePtr;
            _length  =  0;
            _flags   |= EmptyFlags;
            _ciCrc32 =  0;
            _crc32   =  0;
        }
        else
        {
            var length = SetupFromFlags(path, maxLength, flags, out var ciCrc32, out var crc32, out var asciiLower, out var ascii, out var nullTerminated);
            Setup(path, length, ciCrc32, crc32, nullTerminated, false, asciiLower, ascii);
        }
    }

    private static int SetupFromFlags(byte* path, int maxLength, MetaDataComputation flags, out int? ciCrc32, out int? crc32,
        out bool? asciiLower, out bool? ascii, out bool nullTerminated, int? length = null)
    {
        switch (flags)
        {
            case MetaDataComputation.CiCrc32:
            {
                length     = ByteStringFunctions.ComputeCiCrc32AndSize(path, out var c, out nullTerminated, maxLength);
                ciCrc32    = c;
                crc32      = null;
                asciiLower = null;
                ascii      = null;
                return length.Value;
            }
            case MetaDataComputation.Crc32:
            {
                length     = ByteStringFunctions.ComputeCrc32AndSize(path, out var c, out nullTerminated, maxLength);
                crc32      = c;
                ciCrc32    = null;
                asciiLower = null;
                ascii      = null;
                return length.Value;
            }

            case MetaDataComputation.CiCrc32 | MetaDataComputation.AsciiLowerCase:
            case MetaDataComputation.AsciiLowerCase:
            case MetaDataComputation.AsciiLowerCase | MetaDataComputation.Ascii:
            case MetaDataComputation.CiCrc32 | MetaDataComputation.Ascii:
            case MetaDataComputation.Ascii:
            case MetaDataComputation.CiCrc32 | MetaDataComputation.AsciiLowerCase | MetaDataComputation.Ascii:
            {
                length     = ByteStringFunctions.ComputeCiCrc32AsciiLowerAndSize(path, out var c, out var l, out var a, out nullTerminated, maxLength);
                ciCrc32    = c;
                crc32      = null;
                asciiLower = l;
                ascii      = a;
                return length.Value;
            }

            case MetaDataComputation.Crc32 | MetaDataComputation.AsciiLowerCase:
            case MetaDataComputation.Crc32 | MetaDataComputation.Ascii:
            case MetaDataComputation.Crc32 | MetaDataComputation.AsciiLowerCase | MetaDataComputation.Ascii:
            {
                length     = ByteStringFunctions.ComputeCrc32AsciiLowerAndSize(path, out var c, out var l, out var a, out nullTerminated, maxLength);
                ciCrc32    = null;
                crc32      = c;
                asciiLower = l;
                ascii      = a;
                return length.Value;
            }

            case MetaDataComputation.Crc32 | MetaDataComputation.CiCrc32:
            case MetaDataComputation.Crc32 | MetaDataComputation.CiCrc32 | MetaDataComputation.AsciiLowerCase:
            case MetaDataComputation.Crc32 | MetaDataComputation.CiCrc32 | MetaDataComputation.Ascii:
            case MetaDataComputation.Crc32 | MetaDataComputation.CiCrc32 | MetaDataComputation.AsciiLowerCase | MetaDataComputation.Ascii:
            {
                length     = ByteStringFunctions.ComputeCiCrc32AsciiLowerAndSize(path, out var ci, out var c, out var l, out var a, out nullTerminated, maxLength);
                ciCrc32    = ci;
                crc32      = c;
                asciiLower = l;
                ascii      = a;
                return length.Value;
            }

            default:
                ciCrc32        = null;
                crc32          = null;
                asciiLower     = null;
                ascii          = null;
                nullTerminated = false;
                return length ?? ByteStringFunctions.ComputeSize(path, out nullTerminated, maxLength);
        }
    }

    /// <summary> Create a temporary ByteString from a byte span. </summary>
    /// <param name="path">A preferably null-terminated span of an existing string.</param>
    /// <param name="flags"> Which meta information to precompute. </param>
    /// <remarks>
    /// This computes all required values for the string up to null-termination or the length of the span.
    /// </remarks>
    public CiByteString(ReadOnlySpan<byte> path, MetaDataComputation flags = 0)
        : this(SpanHelper(path), flags, path.Length)
    { }

    /// <summary>
    /// Construct an ByteString from a given unicode string, possibly converted to ascii lowercase.
    /// </summary>
    /// <param name="path">The existing UTF16 string.</param>
    /// <param name="ret">The converted, owned ByteString on success, an empty string on failure.</param>
    /// <param name="flags"> Which meta information to precompute. </param>
    /// <param name="toAsciiLower">Optionally, whether to convert the string to lower-case.</param>
    public static bool FromString(string? path, out CiByteString ret, MetaDataComputation flags = MetaDataComputation.CiCrc32,
        bool toAsciiLower = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            ret = Empty;
            return true;
        }

        var p = ByteStringFunctions.Utf8FromString(path, out var l);
        if (p == null)
        {
            ret = Empty;
            return false;
        }

        flags &= ~MetaDataComputation.Ascii;
        if (toAsciiLower)
        {
            ByteStringFunctions.AsciiToLowerInPlace(p, l);
            flags &= ~MetaDataComputation.AsciiLowerCase;
        }

        SetupFromFlags(p, l, flags, out var ciCrc32, out var crc32, out var lower, out _, out _, l);
        ret = new CiByteString().Setup(p, l, ciCrc32, crc32, true, true, toAsciiLower ? true : lower, l == path.Length);
        return true;
    }

    /// <summary>
    /// Construct a temporary ByteString from a given byte string of known size. 
    /// </summary>
    /// <param name="path">A pointer to an existing string.</param>
    /// <param name="length">The known length of the string.</param>
    /// <param name="isNullTerminated">Whether the string is known to be null-terminated or not.</param>
    /// <param name="isLower">Optionally, whether the string is known to only contain (ASCII) lower-case characters.</param>
    /// <param name="isAscii">Optionally, whether the string is known to only contain ASCII characters.</param>
    public static CiByteString FromByteStringUnsafe(byte* path, int length, bool isNullTerminated, bool? isLower = null, bool? isAscii = false)
        => new CiByteString().Setup(path, length, null, null, isNullTerminated, false, isLower, isAscii);

    /// <inheritdoc cref="FromByteStringUnsafe(byte*, int, bool, bool?, bool?)"/>
    public static CiByteString FromSpanUnsafe(ReadOnlySpan<byte> path, bool isNullTerminated, bool? isLower = null, bool? isAscii = false)
    {
        fixed (byte* ptr = path)
        {
            return FromByteStringUnsafe(ptr, path.Length, isNullTerminated, isLower, isAscii);
        }
    }

    /// <summary> Free memory if the string is owned. </summary>
    private void ReleaseUnmanagedResources()
    {
        if (!IsOwned)
            return;

        PenumbraStringMemory.Free(_path, Length + 1);
        _path    = Null.NullBytePtr;
        _length  = 0;
        _flags   = EmptyFlags;
        _ciCrc32 = 0;
        _crc32   = 0;
    }

    /// <summary> Manually free memory. Sets the string to an empty string, also updates CRC32. </summary>
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary> Automatic release of memory if not disposed before. </summary>
    ~CiByteString()
    {
        ReleaseUnmanagedResources();
    }

    /// <summary> Setup from all given values. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal CiByteString Setup(byte* path, int length, int? ciCrc32, int? crc32, bool isNullTerminated, bool isOwned,
        bool? isLower = null, bool? isAscii = null)
    {
        _path   = path;
        _length = length;
        _flags  = 0;
        if (ciCrc32.HasValue)
        {
            _ciCrc32 =  ciCrc32.Value;
            _flags   |= Flags.HasCiCrc32;
        }

        if (crc32.HasValue)
        {
            _crc32 =  crc32.Value;
            _flags |= Flags.HasCrc32;
        }

        if (isNullTerminated)
            _flags |= Flags.NullTerminated;

        if (isOwned)
            _flags |= Flags.Owned;

        _flags |= isLower switch
        {
            true  => Flags.AsciiLowerCase | Flags.AsciiLowerCaseChecked,
            false => Flags.AsciiLowerCaseChecked,
            _     => 0,
        };

        _flags |= isAscii switch
        {
            true  => Flags.Ascii | Flags.AsciiChecked,
            false => Flags.AsciiChecked,
            _     => 0,
        };

        return this;
    }

    /// <summary>
    /// Check if the string is known to be or not be ASCII, otherwise test for it and store it in the cache.
    /// </summary>
    private bool CheckAscii()
    {
        switch (_flags & (Flags.Ascii | Flags.AsciiChecked))
        {
            case Flags.AsciiChecked:               return false;
            case Flags.Ascii | Flags.AsciiChecked: return true;
            default:
                var isAscii = ByteStringFunctions.IsAscii(Path, Length);
                if (isAscii)
                    _flags |= Flags.Ascii | Flags.AsciiChecked;
                else
                    _flags |= Flags.AsciiChecked;

                return isAscii;
        }
    }

    /// <summary>
    /// Check if the string is known to be or not be (ASCII) lower-case, otherwise test for it and store it in the cache.
    /// </summary>
    private bool CheckAsciiLower()
    {
        switch (_flags & (Flags.AsciiLowerCase | Flags.AsciiLowerCaseChecked))
        {
            case Flags.AsciiLowerCaseChecked:                        return false;
            case Flags.AsciiLowerCase | Flags.AsciiLowerCaseChecked: return true;
            default:
                var isAsciiLower = ByteStringFunctions.IsAsciiLowerCase(_path, Length);
                if (isAsciiLower)
                    _flags |= Flags.AsciiLowerCase | Flags.AsciiLowerCaseChecked;
                else
                    _flags |= Flags.AsciiLowerCaseChecked;

                return isAsciiLower;
        }
    }

    private int GetCrc32()
    {
        if (_flags.HasFlag(Flags.HasCrc32))
            return _crc32;

        ByteStringFunctions.ComputeCrc32AndSize(_path, out _crc32, out _);
        _flags |= Flags.HasCrc32;
        return _crc32;
    }

    private int GetCiCrc32()
    {
        if (_flags.HasFlag(Flags.HasCiCrc32))
            return _ciCrc32;

        ByteStringFunctions.ComputeCiCrc32AndSize(_path, out _ciCrc32, out _);
        _flags |= Flags.HasCiCrc32;
        return _ciCrc32;
    }


    private int? CiCrc32Internal
        => _flags.HasFlag(Flags.HasCiCrc32) ? _ciCrc32 : null;

    private int? Crc32Internal
        => _flags.HasFlag(Flags.HasCrc32) ? _crc32 : null;

    private bool? IsAsciiLowerInternal
        => (_flags
              & (Flags.AsciiLowerCase | Flags.AsciiLowerCaseChecked)) switch
            {
                Flags.AsciiLowerCase | Flags.AsciiLowerCaseChecked => true,
                Flags.AsciiLowerCaseChecked                        => false,
                _                                                  => null,
            };

    private bool? IsAsciiInternal
        => (_flags
              & (Flags.Ascii | Flags.AsciiChecked)) switch
            {
                Flags.Ascii | Flags.AsciiChecked => true,
                Flags.AsciiChecked               => false,
                _                                => null,
            };

    private static byte* SpanHelper(ReadOnlySpan<byte> path)
    {
        if (path.Length == 0)
            return null;

        fixed (byte* ptr = path)
        {
            return ptr;
        }
    }
}
