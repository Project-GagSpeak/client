namespace Penumbra.GameData.Files.MaterialStructs;

public struct LegacyColorDyeTableRow : IEquatable<LegacyColorDyeTableRow>
{
    public const int Size = 2;

    private ushort _data;

    public ushort Template
    {
        readonly get => (ushort)(_data >> 5);
        set => _data = (ushort)((_data & 0x1F) | (value << 5));
    }

    public bool DiffuseColor
    {
        readonly get => (_data & 0x01) != 0;
        set => _data = (ushort)(value ? _data | 0x01 : _data & 0xFFFE);
    }

    public bool SpecularColor
    {
        readonly get => (_data & 0x02) != 0;
        set => _data = (ushort)(value ? _data | 0x02 : _data & 0xFFFD);
    }

    public bool EmissiveColor
    {
        readonly get => (_data & 0x04) != 0;
        set => _data = (ushort)(value ? _data | 0x04 : _data & 0xFFFB);
    }

    public bool Shininess
    {
        readonly get => (_data & 0x08) != 0;
        set => _data = (ushort)(value ? _data | 0x08 : _data & 0xFFF7);
    }

    public bool SpecularMask
    {
        readonly get => (_data & 0x10) != 0;
        set => _data = (ushort)(value ? _data | 0x10 : _data & 0xFFEF);
    }

    public LegacyColorDyeTableRow(in ColorDyeTableRow row)
    {
        Template      = row.Template;
        DiffuseColor  = row.DiffuseColor;
        SpecularColor = row.SpecularColor;
        EmissiveColor = row.EmissiveColor;
        Shininess     = row.Scalar3;
        SpecularMask  = row.Metalness;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is LegacyColorDyeTableRow other && Equals(other);

    public readonly bool Equals(LegacyColorDyeTableRow other)
        => _data == other._data;

    public override readonly int GetHashCode()
        => _data.GetHashCode();

    public static bool operator ==(LegacyColorDyeTableRow row1, LegacyColorDyeTableRow row2)
        => row1.Equals(row2);

    public static bool operator !=(LegacyColorDyeTableRow row1, LegacyColorDyeTableRow row2)
        => !row1.Equals(row2);
}
