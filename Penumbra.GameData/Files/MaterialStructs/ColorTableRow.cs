using Penumbra.GameData.Files.StainMapStructs;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files.MaterialStructs;

/// <summary>
/// The number after the parameter is the bit in the dye flags.
/// <code>
/// #       |    X (+0)    |    |    Y (+1)    |    |    Z (+2)   |    |   W (+3)    |
/// --------------------------------------------------------------------------------------
/// 0 (+ 0) |    Diffuse.R |  0 |    Diffuse.G |  0 |   Diffuse.B |  0 |         Unk |  
/// 1 (+ 4) |   Specular.R |  1 |   Specular.G |  1 |  Specular.B |  1 |         Unk |
/// 2 (+ 8) |   Emissive.R |  2 |   Emissive.G |  2 |  Emissive.B |  2 |         Unk |  3
/// 3 (+12) |   Sheen Rate |  6 |   Sheen Tint |  7 |  Sheen Apt. |  8 |         Unk |
/// 4 (+16) |   Rougnhess? |  5 |              |    |  Metalness? |  4 |  Anisotropy |  9
/// 5 (+20) |          Unk |    |  Sphere Mask | 11 |         Unk |    |         Unk |   
/// 6 (+24) |   Shader Idx |    |   Tile Index |    |  Tile Alpha |    |  Sphere Idx | 10
/// 7 (+28) |   Tile XF UU |    |   Tile XF UV |    |  Tile XF VU |    |  Tile XF VV |
/// </code>
/// </summary>
[InlineArray(NumVec4 * Halves)]
public struct ColorTableRow : IEquatable<ColorTableRow>
{
    public const int NumVec4 = 8;
    public const int Halves  = 4;
    public const int Size    = NumVec4 * Halves * 2;

    private Half _element0;

    public static readonly ColorTableRow LegacyDefault = new(LegacyColorTableRow.Default);

    public static readonly ColorTableRow Default = new()
    {
        DiffuseColor   = HalfColor.White,
        Scalar3        = Half.One,
        SpecularColor  = HalfColor.White,
        Scalar7        = Half.Zero,
        EmissiveColor  = HalfColor.Black,
        Scalar11       = Half.One,
        SheenRate      = (Half)0.1f,
        SheenTintRate  = (Half)0.2f,
        SheenAperture  = (Half)5.0f,
        Scalar15       = Half.Zero,
        Roughness      = (Half)0.5f,
        Scalar17       = Half.Zero,
        Metalness      = Half.Zero,
        Anisotropy     = Half.Zero,
        Scalar20       = Half.Zero,
        SphereMapMask  = Half.Zero,
        Scalar22       = Half.Zero,
        Scalar23       = Half.Zero,
        ShaderId       = 0,
        TileIndex      = 0,
        TileAlpha      = Half.One,
        SphereMapIndex = 0,
        TileTransform  = HalfMatrix2x2.ScaledIdentity((Half)16.0f),
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

    public Half Scalar3
    {
        get => this[3];
        set => this[3] = value;
    }

    public HalfColor SpecularColor
    {
        get => new(((ReadOnlySpan<Half>)this)[4], this[5], this[6]);
        set
        {
            this[4] = value.Red;
            this[5] = value.Green;
            this[6] = value.Blue;
        }
    }

    public Half Scalar7
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

    public Half Scalar11
    {
        get => this[11];
        set => this[11] = value;
    }

    public Half SheenRate
    {
        get => this[12];
        set => this[12] = value;
    }

    public Half SheenTintRate
    {
        get => this[13];
        set => this[13] = value;
    }

    public Half SheenAperture
    {
        get => this[14];
        set => this[14] = value;
    }

    public Half Scalar15
    {
        get => this[15];
        set => this[15] = value;
    }

    public Half Roughness
    {
        get => this[16];
        set => this[16] = value;
    }

    public Half Scalar17
    {
        get => this[17];
        set => this[17] = value;
    }

    public Half Metalness
    {
        get => this[18];
        set => this[18] = value;
    }

    public Half Anisotropy
    {
        get => this[19];
        set => this[19] = value;
    }

    public Half Scalar20
    {
        get => this[20];
        set => this[20] = value;
    }

    public Half SphereMapMask
    {
        get => this[21];
        set => this[21] = value;
    }

    public Half Scalar22
    {
        get => this[22];
        set => this[22] = value;
    }

    public Half Scalar23
    {
        get => this[23];
        set => this[23] = value;
    }

    public ushort ShaderId
    {
        get => (ushort)this[24];
        set => this[24] = (Half)value;
    }

    public ushort TileIndex
    {
        get => (ushort)((float)this[25] * 64f);
        set => this[25] = (Half)((value + 0.5f) / 64f);
    }

    public Half TileAlpha
    {
        get => this[26];
        set => this[26] = value;
    }

    public ushort SphereMapIndex
    {
        get => (ushort)this[27];
        set => this[27] = (Half)value;
    }

    public HalfMatrix2x2 TileTransform
    {
        get => new(this[28], this[29], this[30], this[31]);
        set
        {
            this[28] = value.UU;
            this[29] = value.UV;
            this[30] = value.VU;
            this[31] = value.VV;
        }
    }

    public bool ApplyDye(ColorDyeTableRow dyeRow, LegacyDyePack dyes)
    {
        // This does a legacy interpretation of the new structures.

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

        if (dyeRow.Scalar3 && Scalar3 != dyes.Shininess)
        {
            Scalar3 = dyes.Shininess;
            ret     = true;
        }

        if (dyeRow.Metalness && Scalar7 != dyes.SpecularMask)
        {
            Scalar7 = dyes.SpecularMask;
            ret     = true;
        }

        return ret;
    }

    public bool ApplyDye(ColorDyeTableRow dyeRow, DyePack dye)
    {
        var ret = false;

        if (dyeRow.DiffuseColor && DiffuseColor != dye.DiffuseColor)
        {
            DiffuseColor = dye.DiffuseColor;
            ret          = true;
        }

        if (dyeRow.SpecularColor && SpecularColor != dye.SpecularColor && dye.SpecularColor != HalfColor.Black)
        {
            SpecularColor = dye.SpecularColor;
            ret           = true;
        }

        if (dyeRow.EmissiveColor && EmissiveColor != dye.EmissiveColor)
        {
            EmissiveColor = dye.EmissiveColor;
            ret           = true;
        }

        if (dyeRow.Scalar3 && Scalar11 != dye.Scalar3)
        {
            Scalar11 = dye.Scalar3;
            ret      = true;
        }

        if (dyeRow.Metalness && Metalness != dye.Metalness)
        {
            Metalness = dye.Metalness;
            ret       = true;
        }

        if (dyeRow.Roughness && Roughness != dye.Roughness)
        {
            Roughness = dye.Roughness;
            ret       = true;
        }

        if (dyeRow.SheenRate && SheenRate != dye.SheenRate)
        {
            SheenRate = dye.SheenRate;
            ret       = true;
        }

        if (dyeRow.SheenTintRate && SheenTintRate != dye.SheenTintRate)
        {
            SheenTintRate = dye.SheenTintRate;
            ret           = true;
        }

        if (dyeRow.SheenAperture && SheenAperture != dye.SheenAperture)
        {
            SheenAperture = dye.SheenAperture;
            ret           = true;
        }

        if (dyeRow.Anisotropy && Anisotropy != dye.Anisotropy)
        {
            Anisotropy = dye.Anisotropy;
            ret        = true;
        }

        if (dyeRow.SphereMapIndex && this[27] != dye.RawSphereMapIndex)
        {
            this[27] = dye.RawSphereMapIndex;
            ret      = true;
        }

        if (dyeRow.SphereMapMask && SphereMapMask != dye.SphereMapMask)
        {
            SphereMapMask = dye.SphereMapMask;
            ret           = true;
        }

        return ret;
    }

    public ColorTableRow(in LegacyColorTableRow oldRow)
    {
        DiffuseColor  = oldRow.DiffuseColor;
        Scalar3       = oldRow.Shininess;
        SpecularColor = oldRow.SpecularColor;
        Scalar7       = oldRow.SpecularMask;
        EmissiveColor = oldRow.EmissiveColor;
        TileIndex     = oldRow.TileIndex;
        TileAlpha     = Half.One;
        TileTransform = oldRow.TileTransform;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is ColorTableRow other && Equals(other);

    public readonly bool Equals(ColorTableRow other)
        => ((ReadOnlySpan<Half>)this).SequenceEqual(other);

    public override readonly int GetHashCode()
    {
        var hc = new HashCode();
        hc.AddBytes(MemoryMarshal.AsBytes(new ReadOnlySpan<ColorTableRow>(in this)));
        return hc.ToHashCode();
    }

    public static bool operator ==(ColorTableRow row1, ColorTableRow row2)
        => row1.Equals(row2);

    public static bool operator !=(ColorTableRow row1, ColorTableRow row2)
        => !row1.Equals(row2);
}
