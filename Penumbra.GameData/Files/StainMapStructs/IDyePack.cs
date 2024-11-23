using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files.StainMapStructs;

public interface IDyePack
{
    HalfColor DiffuseColor  { get; }
    HalfColor SpecularColor { get; }
    HalfColor EmissiveColor { get; }

    abstract static string DefaultStmPath { get; }
    abstract static int    ColorCount     { get; }
    abstract static int    ScalarCount    { get; }
}
