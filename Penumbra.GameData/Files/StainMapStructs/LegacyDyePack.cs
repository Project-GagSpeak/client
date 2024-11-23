using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files.StainMapStructs;

/// <summary>
/// All dye-able color set information for a row - legacy format.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public record struct LegacyDyePack : IDyePack
{
    public const string DefaultStmPath = "chara/base_material/stainingtemplate.stm";

    static string IDyePack.DefaultStmPath => DefaultStmPath;

    public HalfColor DiffuseColor;
    public HalfColor SpecularColor;
    public HalfColor EmissiveColor;
    public Half      Shininess;
    public Half      SpecularMask;

    readonly HalfColor IDyePack.DiffuseColor  => DiffuseColor;
    readonly HalfColor IDyePack.SpecularColor => SpecularColor;
    readonly HalfColor IDyePack.EmissiveColor => EmissiveColor;

    public static int ColorCount  => 3;
    public static int ScalarCount => 2;
}
