using Penumbra.GameData.Enums;

namespace Penumbra.GameData.Structs;

/// <summary> A Racial Scaling Parameter entry controls scaling for specific attributes for a specific gender and race. </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct RspData
{
    /// <summary> The byte size of all attributes. </summary>
    public const int ByteSize = (int)RspAttribute.NumAttributes * 4;

    /// <summary> The attributes. </summary>
    private readonly float[] _attributes;

    /// <summary> Copy an RSP Entry. </summary>
    public RspData(RspData copy)
        => _attributes = (float[])copy._attributes.Clone();


    /// <summary> Read a set of entries from byte data at a given position. </summary>
    public RspData(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset < 0 || offset + ByteSize > bytes.Length)
            throw new ArgumentOutOfRangeException();

        _attributes = new float[(int)RspAttribute.NumAttributes];
        for (var i = 0; i < (int)RspAttribute.NumAttributes; ++i)
            _attributes[i] = MemoryMarshal.Read<float>(bytes[(i * 4 + offset)..]);
    }

    /// <summary> Obtain the index of an attribute. </summary>
    private static int ToIndex(RspAttribute attribute)
        => attribute < RspAttribute.NumAttributes
            ? (int)attribute
            : throw new InvalidEnumArgumentException();

    /// <summary> Obtain the value for a given attribute. </summary>
    public float this[RspAttribute attribute]
    {
        get => _attributes[ToIndex(attribute)];
        set => _attributes[ToIndex(attribute)] = value;
    }

    /// <summary> Write all attributes to a block of bytes. </summary>
    public byte[] ToBytes()
    {
        using var s  = new MemoryStream(ByteSize);
        using var bw = new BinaryWriter(s);
        foreach (var attribute in _attributes)
            bw.Write(attribute);

        return s.ToArray();
    }
}
