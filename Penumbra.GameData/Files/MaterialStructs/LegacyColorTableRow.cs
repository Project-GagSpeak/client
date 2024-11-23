using Penumbra.GameData.Files.StainMapStructs;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files.MaterialStructs;

/// <summary>
/// The number after the parameter is the bit in the dye flags.
/// <code>
/// #       |    X (+0)    |    |    Y (+1)    |    |    Z (+2)   |    |   W (+3)    |
/// --------------------------------------------------------------------------------------
/// 0 (+ 0) |    Diffuse.R |  0 |    Diffuse.G |  0 |   Diffuse.B |  0 | SpecularStr |  3
/// 1 (+ 4) |   Specular.R |  1 |   Specular.G |  1 |  Specular.B |  1 |       Gloss |  4
/// 2 (+ 8) |   Emissive.R |  2 |   Emissive.G |  2 |  Emissive.B |  2 |  Tile Index |
/// 7 (+28) |   Tile XF UU |    |   Tile XF UV |    |  Tile XF VU |    |  Tile XF VV |
/// </code>
/// </summary>
[InlineArray(NumVec4 * Halves)]
public struct LegacyColorTableRow : IEquatable<LegacyColorTableRow>
{
    public const int NumVec4 = 4;
    public const int Halves  = 4;
    public const int Size    = NumVec4 * Halves * 2;

    private Half _element0;

    public static readonly LegacyColorTableRow Default = new()
    {
        DiffuseColor  = HalfColor.White,
        SpecularMask  = Half.One,
        SpecularColor = HalfColor.White,
        Shininess     = (Half)20.0f,
        EmissiveColor = HalfColor.Black,
        TileIndex     = 0,
        TileTransform = HalfMatrix2x2.ScaledIdentity((Half)16.0f),
    };

    public HalfColor DiffuseColor
    {
        get => new(_element0, this[1], this[2]);
        set
        {
            _element0 = value.Red;
            this[1] = value.Green;
            this[2] = value.Blue;
        }
    }

    public Half SpecularMask
    {
        get => this[3];
        set => this[3] = value;
    }

    public HalfColor SpecularColor
    {
        get => new(this[4], this[5], this[6]);
        set
        {
            this[4] = value.Red;
            this[5] = value.Green;
            this[6] = value.Blue;
        }
    }

    public Half Shininess
    {
        get => this[7];
        set => this[7] = value;
    }

    public HalfColor EmissiveColor
    {
        get => new(this[8], this[9], this[10]);
        set
        {
            this[8]  = value.Red;
            this[9]  = value.Green;
            this[10] = value.Blue;
        }
    }

    public ushort TileIndex
    {
        get => (ushort)((float)this[11] * 64f);
        set => this[11] = (Half)((value + 0.5f) / 64f);
    }

    public HalfMatrix2x2 TileTransform
    {
        get => new(this[12], this[13], this[14], this[15]);
        set
        {
            this[12] = value.UU;
            this[13] = value.UV;
            this[14] = value.VU;
            this[15] = value.VV;
        }
    }

    public bool ApplyDye(LegacyColorDyeTableRow dyeRow, LegacyDyePack dyes)
    {
        var ret = false;

        if (dyeRow.DiffuseColor && DiffuseColor != dyes.DiffuseColor)
        {
            DiffuseColor = dyes.DiffuseColor;
            ret          = true;
        }

        if (dyeRow.SpecularColor && SpecularColor != dyes.SpecularColor && dyes.SpecularColor != HalfColor.Black)
        {
            SpecularColor = dyes.SpecularColor;
            ret           = true;
        }

        if (dyeRow.EmissiveColor && EmissiveColor != dyes.EmissiveColor)
        {
            EmissiveColor = dyes.EmissiveColor;
            ret           = true;
        }

        if (dyeRow.SpecularMask && SpecularMask != dyes.SpecularMask)
        {
            SpecularMask = dyes.SpecularMask;
            ret          = true;
        }

        if (dyeRow.Shininess && Shininess != dyes.Shininess)
        {
            Shininess = dyes.Shininess;
            ret       = true;
        }

        return ret;
    }

    public LegacyColorTableRow(in ColorTableRow row)
    {
        DiffuseColor  = row.DiffuseColor;
        SpecularMask  = row.Scalar7;
        SpecularColor = row.SpecularColor;
        Shininess     = row.Scalar3;
        EmissiveColor = row.EmissiveColor;
        TileIndex     = row.TileIndex;
        TileTransform = row.TileTransform;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is LegacyColorTableRow other && Equals(other);

    public readonly bool Equals(LegacyColorTableRow other)
        => ((ReadOnlySpan<Half>)this).SequenceEqual(other);

    public override readonly int GetHashCode()
    {
        var hc = new HashCode();
        hc.AddBytes(MemoryMarshal.AsBytes(new ReadOnlySpan<LegacyColorTableRow>(in this)));
        return hc.ToHashCode();
    }

    public static bool operator ==(LegacyColorTableRow row1, LegacyColorTableRow row2)
        => row1.Equals(row2);

    public static bool operator !=(LegacyColorTableRow row1, LegacyColorTableRow row2)
        => !row1.Equals(row2);
}
