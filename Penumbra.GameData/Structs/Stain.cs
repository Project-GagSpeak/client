using Dalamud.Interface;
using Dalamud.Utility;

namespace Penumbra.GameData.Structs;

/// <summary> A wrapper for the clothing dyes the game provides with their RGBA color value, game ID, unmodified color value and name. </summary>
public readonly struct Stain
{
    /// <summary> An empty stain with transparent color. </summary>
    public static readonly Stain None = new("None");

    /// <summary> The name of the stain. </summary>
    public readonly string Name;

    /// <summary> The color as RGBA32-value (alpha is always 255). </summary>
    public readonly uint RgbaColor;

    /// <summary> The index of the stain in the sheet. </summary>
    public readonly StainId RowIndex;

    /// <summary> Whether the stain is glossy. </summary>
    public readonly bool Gloss;

    /// <summary> The R-value of the stain. </summary>
    public byte R
        => (byte)(RgbaColor & 0xFF);

    /// <summary> The G-value of the stain. </summary>
    public byte G
        => (byte)((RgbaColor >> 8) & 0xFF);

    /// <summary> The B-value of the stain. </summary>
    public byte B
        => (byte)((RgbaColor >> 16) & 0xFF);

    /// <summary> The approximate lumen-intensity of the stain. </summary>
    public float Intensity
    {
        get
        {
            var vec = ColorHelpers.RgbaUintToVector4(RgbaColor);
            return 2 * vec.X * vec.X + 7 * vec.Y * vec.Y + vec.Z * vec.Z;
        }
    }

    /// <summary> Square stores its colors as BGR values so R and B need to be shuffled and Alpha set to max. </summary>
    private static uint SeColorToRgba(uint color)
        => ((color & 0xFF) << 16) | ((color >> 16) & 0xFF) | (color & 0xFF00) | 0xFF000000;

    /// <summary> Create a Stain from the sheet data. </summary>
    public Stain(Lumina.Excel.Sheets.Stain stain)
        : this(stain.Name.ExtractText(), SeColorToRgba(stain.Color), (StainId)stain.RowId, stain.Unknown1)
    { }

    /// <summary> Simple constructor for all data. </summary>
    internal Stain(string name, uint dye, StainId index, bool gloss)
    {
        Name      = name;
        RowIndex  = index;
        Gloss     = gloss;
        RgbaColor = dye;
    }

    /// <summary> Only used by None. </summary>
    private Stain(string name)
    {
        Name      = name;
        RowIndex  = 0;
        RgbaColor = 0;
        Gloss     = false;
    }
}
