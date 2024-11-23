using Dalamud.Memory;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files;

/// <summary> The files the game uses to store character appearances. </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
public unsafe struct DatCharacterFile
{
    /// <summary> The total size of the file. Should be 212. </summary>
    public const int Size = 4 + 4 + 4 + 4 + CustomizeArray.Size + 2 + 4 + 41 * 4;

    /// <summary> All of the data. </summary>
    [FieldOffset(0)]
    private fixed byte _data[Size];

    /// <summary> Magic number for the format. </summary>
    [FieldOffset(0)]
    public readonly uint Magic = 0x2013FF14;

    /// <summary> The current version. </summary>
    [FieldOffset(4)]
    public readonly uint Version = 0x05;

    /// <summary> A checksum computed from all the other content. </summary>
    [FieldOffset(8)]
    private uint _checksum;

    /// <summary> 4 bytes of padding. </summary>
    [FieldOffset(12)]
    private readonly uint _padding = 0;

    /// <summary> The customize array for the character. </summary>
    [FieldOffset(16)]
    private CustomizeArray _customize;

    /// <summary> The selected voice. </summary>
    [FieldOffset(16 + CustomizeArray.Size)]
    private ushort _voice;

    /// <summary> A timestamp of the creation. </summary>
    [FieldOffset(16 + CustomizeArray.Size + 2)]
    private uint _timeStamp;

    /// <summary> The user supplied description in far more space than necessary. UTF8, despite allocating space for UTF32. </summary>
    [FieldOffset(Size - 41 * 4)]
    private fixed byte _description[41 * 4];

    /// <summary> Write this file to a stream. </summary>
    public readonly void Write(Stream stream)
    {
        for (var i = 0; i < Size; ++i)
            stream.WriteByte(_data[i]);
    }

    /// <summary> Try to read a dat file from a stream. </summary>
    /// <param name="stream"> The stream. </param>
    /// <param name="file"> The returned file on success. </param>
    /// <returns> True on success. </returns>
    public static bool Read(Stream stream, out DatCharacterFile file)
    {
        if (stream.Length - stream.Position != Size)
        {
            file = default;
            return false;
        }

        file = new DatCharacterFile(stream);
        return true;
    }

    /// <summary> Reads a dat file from a binary stream. </summary>
    private DatCharacterFile(Stream stream)
    {
        for (var i = 0; i < Size; ++i)
            _data[i] = (byte)stream.ReadByte();
    }

    /// <summary> Create a new dat file from a given customize array, voice selection and description text. </summary>
    public DatCharacterFile(in CustomizeArray customize, byte voice, string text)
    {
        SetCustomize(customize);
        SetVoice(voice);
        SetTime(DateTimeOffset.UtcNow);
        SetDescription(text);
        _checksum = CalculateChecksum();
    }

    /// <summary> Calculate the checksum the way the game does. </summary>
    public readonly uint CalculateChecksum()
    {
        var ret = 0u;
        for (var i = 16; i < Size; i++)
            ret ^= (uint)(_data[i] << ((i - 16) % 24));
        return ret;
    }

    /// <summary> The current checksum. </summary>
    public readonly uint Checksum
        => _checksum;

    /// <summary> Get or set the customize data. </summary>
    public CustomizeArray Customize
    {
        readonly get => _customize;
        set
        {
            SetCustomize(value);
            _checksum = CalculateChecksum();
        }
    }

    /// <summary> Get or set the selected voice. </summary>
    public ushort Voice
    {
        readonly get => _voice;
        set
        {
            SetVoice(value);
            _checksum = CalculateChecksum();
        }
    }

    /// <summary> Get or set the description. </summary>
    public string Description
    {
        readonly get
        {
            fixed (byte* ptr = _description)
            {
                return MemoryHelper.ReadStringNullTerminated((nint)ptr);
            }
        }
        set
        {
            SetDescription(value);
            _checksum = CalculateChecksum();
        }
    }

    /// <summary> Get or set the creation time. </summary>
    public DateTimeOffset Time
    {
        readonly get => DateTimeOffset.FromUnixTimeSeconds(_timeStamp);
        set
        {
            SetTime(value);
            _checksum = CalculateChecksum();
        }
    }

    /// <summary> Set the timestamp. </summary>
    private void SetTime(DateTimeOffset time)
        => _timeStamp = (uint)time.ToUnixTimeSeconds();

    /// <summary> Set the customize array. </summary>
    private void SetCustomize(in CustomizeArray customize)
        => _customize = customize;

    /// <summary> Set the voice. </summary>
    private void SetVoice(ushort voice)
        => _voice = voice;

    /// <summary> Set the description. </summary>
    private void SetDescription(string text)
    {
        fixed (byte* ptr = _description)
        {
            var span = new Span<byte>(ptr, 41 * 4);
            Encoding.UTF8.GetBytes(text.AsSpan(0, Math.Min(40, text.Length)), span);
        }
    }
}
