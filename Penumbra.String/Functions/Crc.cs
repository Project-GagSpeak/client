namespace Penumbra.String.Functions;

public static unsafe partial class ByteStringFunctions
{
    /// <summary> Compute the FFXIV-CRC64 value of a UTF16 string. </summary>
    /// <remarks>
    /// The FFXIV-CRC64 consists of the CRC32 of the string up to the last '/' in the lower bytes,
    /// and the CRC32 of the string from the last '/' in the upper bytes.
    /// </remarks>
    public static ulong ComputeCrc64(string name)
    {
        if (name.Length == 0)
            return 0;

        var lastSlash = name.LastIndexOf('/');
        if (lastSlash == -1)
            return Lumina.Misc.Crc32.Get(name);

        var folder = name[..lastSlash];
        var file   = name[(lastSlash + 1)..];
        return ((ulong)Lumina.Misc.Crc32.Get(folder) << 32) | Lumina.Misc.Crc32.Get(file);
    }

    /// <summary> Compute the FFXIV-CRC64 value of a UTF8 string. </summary>
    /// <remarks><inheritdoc cref="ComputeCrc64(string)" /></remarks>
    public static ulong ComputeCrc64(ReadOnlySpan<byte> name)
    {
        if (name.Length == 0)
            return 0;

        var lastSlash = name.LastIndexOf((byte)'/');
        if (lastSlash == -1)
            return Lumina.Misc.Crc32.Get(name);

        var folder = name[..lastSlash];
        var file   = name[(lastSlash + 1)..];
        return ((ulong)Lumina.Misc.Crc32.Get(folder) << 32) | Lumina.Misc.Crc32.Get(file);
    }

    private static readonly uint[] CrcTable =
        typeof(Lumina.Misc.Crc32).GetField("CrcTable", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as uint[]
     ?? throw new Exception("Could not fetch CrcTable from Lumina.");


    /// <summary>
    /// Compute the FFXIV-CRC64 value with lower-cased ascii letters.
    /// </summary>
    public static ulong ComputeLowerCaseCrc64(CiByteString name)
    {
        if (name.Length == 0)
            return 0;

        var tmp       = name.Path;
        var crcFolder = 0u;
        var crcFile   = 0u;
        var crc32     = uint.MaxValue;
        while (true)
        {
            var value = *tmp;
            if (value == 0)
                break;

            if (value == (byte)'/')
            {
                crcFolder = crc32;
                crcFile   = uint.MaxValue;
                crc32     = CrcTable[(byte)(crc32 ^ value)] ^ (crc32 >> 8);
            }
            else
            {
                var lower = AsciiToLower(value);
                crcFile = CrcTable[(byte)(crcFile ^ lower)] ^ (crcFile >> 8);
                crc32   = CrcTable[(byte)(crc32 ^ lower)] ^ (crc32 >> 8);
            }

            ++tmp;
        }

        return ((ulong)crcFolder << 32) | crcFile;
    }

    /// <summary>
    /// Compute the case-insensitive CRC32 value and the length.
    /// </summary>
    public static int ComputeCiCrc32AndSize(byte* ptr, out int ciCrc32Ret, out bool nullTerminated, int maxLength = int.MaxValue)
    {
        var ciCrc32 = uint.MaxValue;
        var tmp     = ptr;
        nullTerminated = false;
        for (var end = ptr + maxLength; tmp < end; ++tmp)
        {
            var value = *tmp;
            if (value == 0)
            {
                nullTerminated = true;
                break;
            }

            var lower = AsciiToLower(*tmp);
            ciCrc32 = CrcTable[(byte)(ciCrc32 ^ lower)] ^ (ciCrc32 >> 8);
        }

        var size = (int)(tmp - ptr);
        ciCrc32Ret = (int)~ciCrc32;
        return size;
    }


    /// <summary>
    /// Compute the case-insensitive CRC32 value, the length, the ASCII state and the Lowercase state while iterating only once.
    /// </summary>
    public static int ComputeCiCrc32AsciiLowerAndSize(byte* ptr, out int ciCrc32Ret, out bool isLower, out bool isAscii,
        out bool nullTerminated, int maxLength = int.MaxValue)
    {
        var ciCrc32 = uint.MaxValue;
        isLower = true;
        isAscii = true;
        var tmp = ptr;
        nullTerminated = false;
        for (var end = ptr + maxLength; tmp < end; ++tmp)
        {
            var value = *tmp;
            if (value == 0)
            {
                nullTerminated = true;
                break;
            }

            var lower = AsciiToLower(*tmp);
            if (lower != value)
                isLower = false;
            if (value > 0x80)
                isAscii = false;

            ciCrc32 = CrcTable[(byte)(ciCrc32 ^ lower)] ^ (ciCrc32 >> 8);
        }

        var size = (int)(tmp - ptr);
        ciCrc32Ret = (int)~ciCrc32;
        return size;
    }

    /// <summary>
    /// Compute the case-insensitive and case-sensitive CRC32 value, the length, the ASCII state and the Lowercase state while iterating only once.
    /// </summary>
    public static int ComputeCiCrc32AsciiLowerAndSize(byte* ptr, out int ciCrc32Ret, out int crc32Ret, out bool isLower, out bool isAscii,
        out bool nullTerminated, int maxLength = int.MaxValue)
    {
        var ciCrc32 = uint.MaxValue;
        var crc32   = uint.MaxValue;
        isLower = true;
        isAscii = true;
        var tmp = ptr;
        nullTerminated = false;
        for (var end = ptr + maxLength; tmp < end; ++tmp)
        {
            var value = *tmp;
            if (value == 0)
            {
                nullTerminated = true;
                break;
            }

            var lower = AsciiToLower(*tmp);
            if (lower != value)
                isLower = false;
            if (value > 0x80)
                isAscii = false;

            ciCrc32 = CrcTable[(byte)(ciCrc32 ^ lower)] ^ (ciCrc32 >> 8);
            crc32   = CrcTable[(byte)(crc32 ^ value)] ^ (crc32 >> 8);
        }

        var size = (int)(tmp - ptr);
        ciCrc32Ret = (int)~ciCrc32;
        crc32Ret   = (int)~crc32;
        return size;
    }


    /// <summary>
    /// Compute the CRC32 value, the length, the ASCII state and the Lowercase state while iterating only once.
    /// </summary>
    public static int ComputeCrc32AsciiLowerAndSize(byte* ptr, out int crc32Ret, out bool isLower, out bool isAscii, out bool nullTerminated,
        int maxLength = int.MaxValue)
    {
        var crc32 = uint.MaxValue;
        isLower = true;
        isAscii = true;
        var tmp = ptr;
        nullTerminated = false;
        for (var end = ptr + maxLength; tmp < end; ++tmp)
        {
            var value = *tmp;
            if (value == 0)
            {
                nullTerminated = true;
                break;
            }

            if (AsciiToLower(*tmp) != *tmp)
                isLower = false;

            if (value > 0x80)
                isAscii = false;

            crc32 = CrcTable[(byte)(crc32 ^ value)] ^ (crc32 >> 8);
        }

        var size = (int)(tmp - ptr);
        crc32Ret = (int)~crc32;
        return size;
    }

    /// <summary>
    /// Compute the CRC32 value and the length while iterating only once.
    /// </summary>
    public static int ComputeCrc32AndSize(byte* ptr, out int crc32Ret, out bool nullTerminated, int maxLength = int.MaxValue)
    {
        var crc32 = uint.MaxValue;
        var tmp   = ptr;
        nullTerminated = false;
        for (var end = ptr + maxLength; tmp < end; ++tmp)
        {
            var value = *tmp;
            if (value == 0)
            {
                nullTerminated = true;
                break;
            }

            crc32 = CrcTable[(byte)(crc32 ^ value)] ^ (crc32 >> 8);
        }

        var size = (int)(tmp - ptr);
        crc32Ret = (int)~crc32;
        return size;
    }

    /// <summary> Compute the length of a null-terminated string. </summary>
    public static int ComputeSize(byte* ptr, out bool nullTerminated, int maxLength = int.MaxValue)
    {
        var tmp = ptr;
        nullTerminated = false;
        for (var end = ptr + maxLength; tmp < end; ++tmp)
        {
            var value = *tmp;
            if (value == 0)
            {
                nullTerminated = true;
                break;
            }
        }

        return (int)(tmp - ptr);
    }
}
