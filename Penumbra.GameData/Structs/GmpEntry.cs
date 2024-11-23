using Newtonsoft.Json;

namespace Penumbra.GameData.Structs;

/// <summary>
/// Gimmick Parameter entries contain information about visors and exist for all primary equipment IDs for head slots only.
/// It is 5 bytes per entry.
/// </summary>
public readonly struct GmpEntry : IEquatable<GmpEntry>
{
    /// <summary> The default GMP entry is empty. </summary>
    public static readonly GmpEntry Default = new();

    private readonly byte _value1;
    private readonly byte _value2;
    private readonly byte _value3;
    private readonly byte _value4;
    private readonly byte _value5;

    /// <summary> Whether the visor is enabled at all. Bit 1. </summary>
    public bool Enabled
    {
        get => (_value1 & 1) == 1;
        init
        {
            if (value)
                _value1 = (byte)(_value1 | 1);
            else
                _value1 = (byte)(_value1 & ~1);
        }
    }

    /// <summary> Whether toggling the visor is animated. Bit 2.</summary>
    public bool Animated
    {
        get => (_value1 & 2) == 2;
        init
        {
            if (value)
                _value1 = (byte)(_value1 | 2);
            else
                _value1 = (byte)(_value1 & ~2);
        }
    }

    /// <summary> Rotation a in degrees for a toggled visor. Bit 3-12. </summary>
    public ushort RotationA
    {
        get => (ushort)((_value1 >> 2) | ((_value2 & 0x0F) << 6));
        init
        {
            _value1 = (byte)((_value1 & 0x03) | ((value << 2) & 0xFC));
            _value2 = (byte)((_value2 & 0xF0) | ((value >> 6) & 0x0F));
        }
    }

    /// <summary> Rotation b in degrees for a toggled visor. Bit 13-22</summary>
    public ushort RotationB
    {
        get => (ushort)((_value2 >> 4) | ((_value3 & 0x3F) << 4));
        init
        {
            _value2 = (byte)((_value2 & 0x0F) | ((value << 4) & 0xF0));
            _value3 = (byte)((_value3 & 0xC0) | ((value >> 4) & 0x3F));
        }
    }

    /// <summary> Rotation c in degrees for a toggled visor. Bit 23-32</summary>
    public ushort RotationC
    {
        get => (ushort)((_value3 >> 6) | (_value4 << 2));
        init
        {
            _value3 = (byte)((_value3 & 0x3F) | ((value << 6) & 0xC0));
            _value4 = (byte)(value >> 2);
        }
    }

    /// <summary> Unknown parameter 1. Bit 33-36</summary>
    public byte UnknownA
    {
        get => (byte)(_value5 & 0x0F);
        init => _value5 = (byte)((_value5 & 0xF0) | (value & 0x0F));
    }

    /// <summary> Unknown parameter 2. Bit 37-40</summary>
    public byte UnknownB
    {
        get => (byte)(_value5 >> 4);
        init => _value5 = (byte)((_value5 & 0x0F) | (value << 4));
    }

    /// <summary> Both unknown parameters together. Bits 32-40 or byte 5. </summary>
    [JsonIgnore]
    public byte UnknownTotal
    {
        get => _value5;
        init => _value5 = value;
    }

    /// <summary> The total value of the parameter as single 8 byte integer, the upper 3 bytes are always 0. </summary>
    [JsonIgnore]
    public ulong Value
    {
        readonly get => _value1 | ((ulong)_value2 << 8) | ((ulong)_value3 << 16) | ((ulong)_value4 << 24) | ((ulong)_value5 << 32);
        init
        {
            _value1 = (byte)value;
            _value2 = (byte)(value >> 8);
            _value3 = (byte)(value >> 16);
            _value4 = (byte)(value >> 24);
            _value5 = (byte)(value >> 32);
        }
    }


    /// <summary> Read 5 bytes from the array. </summary>
    public static GmpEntry FromTexToolsMeta(ReadOnlySpan<byte> data)
        => new(data);

    /// <inheritdoc/>
    public readonly bool Equals(GmpEntry other)
        => _value1 == other._value1
         && _value2 == other._value2
         && _value3 == other._value3
         && _value4 == other._value4
         && _value5 == other._value5;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
        => obj is GmpEntry other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode()
        => Value.GetHashCode();

    public static bool operator ==(GmpEntry left, GmpEntry right)
        => left.Equals(right);

    public static bool operator !=(GmpEntry left, GmpEntry right)
        => !(left == right);

    private GmpEntry(ReadOnlySpan<byte> data)
    {
        _value1 = data[0];
        _value2 = data[1];
        _value3 = data[2];
        _value4 = data[3];
        _value5 = data[4];
    }

    public override string ToString()
    {
        var sb = new StringBuilder(128);
        sb.Append(Enabled ? "Enabled" : "Disabled");
        if (Animated)
            sb.Append(", Animated");

        var rotA = RotationA;
        var rotB = RotationB;
        var rotC = RotationC;
        if (rotA != 0 || rotB != 0 || rotC != 0)
            sb.Append(", Rotation (")
                .Append(rotA)
                .Append("°, ")
                .Append(rotB)
                .Append("°, ")
                .Append(rotC)
                .Append(')');

        var unkA = UnknownA;
        var unkB = UnknownB;
        if (unkA != 0 || unkB != 0)
            sb.Append(", Unknown (")
                .Append(UnknownA)
                .Append(", ")
                .Append(UnknownB)
                .Append(')');
        return sb.ToString();
    }
}
