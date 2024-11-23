namespace Penumbra.GameData.Files.MaterialStructs;

/// <inheritdoc cref="ColorTableRow"/>
public struct ColorDyeTableRow : IEquatable<ColorDyeTableRow>
{
    public const int  Size = 4;
    private      uint _data;

    public ushort Template
    {
        readonly get => (ushort)((_data >> 16) & 0x7FF);
        set => _data = (_data & ~0x7FF0000u) | ((uint)(value & 0x7FF) << 16);
    }

    public byte Channel
    {
        readonly get => (byte)((_data >> 27) & 0x3);
        set => _data = (_data & ~0x18000000u) | ((uint)(value & 0x3) << 27);
    }

    public bool DiffuseColor
    {
        readonly get => (_data & 0x0001) != 0;
        set => _data = value ? _data | 0x0001u : _data & ~0x0001u;
    }

    public bool SpecularColor
    {
        readonly get => (_data & 0x0002) != 0;
        set => _data = value ? _data | 0x0002u : _data & ~0x0002u;
    }

    public bool EmissiveColor
    {
        readonly get => (_data & 0x0004) != 0;
        set => _data = value ? _data | 0x0004u : _data & ~0x0004u;
    }

    public bool Scalar3
    {
        readonly get => (_data & 0x0008) != 0;
        set => _data = value ? _data | 0x0008u : _data & ~0x0008u;
    }

    public bool Metalness
    {
        readonly get => (_data & 0x0010) != 0;
        set => _data = value ? _data | 0x0010u : _data & ~0x0010u;
    }

    public bool Roughness
    {
        readonly get => (_data & 0x0020) != 0;
        set => _data = value ? _data | 0x0020u : _data & ~0x0020u;
    }

    public bool SheenRate
    {
        readonly get => (_data & 0x0040) != 0;
        set => _data = value ? _data | 0x0040u : _data & ~0x0040u;
    }

    public bool SheenTintRate
    {
        readonly get => (_data & 0x0080) != 0;
        set => _data = value ? _data | 0x0080u : _data & ~0x0080u;
    }

    public bool SheenAperture
    {
        readonly get => (_data & 0x0100) != 0;
        set => _data = value ? _data | 0x0100u : _data & ~0x0100u;
    }

    public bool Anisotropy
    {
        readonly get => (_data & 0x0200) != 0;
        set => _data = value ? _data | 0x0200u : _data & ~0x0200u;
    }

    public bool SphereMapIndex
    {
        readonly get => (_data & 0x0400) != 0;
        set => _data = value ? _data | 0x0400u : _data & ~0x0400u;
    }

    public bool SphereMapMask
    {
        readonly get => (_data & 0x0800) != 0;
        set => _data = value ? _data | 0x0800u : _data & ~0x0200u;
    }

    public ColorDyeTableRow(in LegacyColorDyeTableRow oldRow)
    {
        Template      = oldRow.Template;
        DiffuseColor  = oldRow.DiffuseColor;
        SpecularColor = oldRow.SpecularColor;
        EmissiveColor = oldRow.EmissiveColor;
        Scalar3       = oldRow.Shininess;
        Metalness     = oldRow.SpecularMask;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is ColorDyeTableRow other && Equals(other);

    public readonly bool Equals(ColorDyeTableRow other)
        => _data == other._data;

    public override readonly int GetHashCode()
        => _data.GetHashCode();

    public static bool operator ==(ColorDyeTableRow row1, ColorDyeTableRow row2)
        => row1.Equals(row2);

    public static bool operator !=(ColorDyeTableRow row1, ColorDyeTableRow row2)
        => !row1.Equals(row2);
}
