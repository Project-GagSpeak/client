using System.Collections.Frozen;

namespace Penumbra.GameData.Enums;

/// <summary> A grouping of files representing specific types for the game, not only used by extension. </summary>
public enum FileType : byte
{
    Unknown,
    Sound,
    Imc,
    Vfx,
    Animation,
    Pap,
    MetaInfo,
    Material,
    Texture,
    Model,
    Shader,
    Font,
    Environment,
}

public static partial class Names
{
    /// <summary> Dictionary to convert extensions to grouped FileType. </summary>
    public static readonly IReadOnlyDictionary<string, FileType> ExtensionToFileType = FrozenDictionary.ToFrozenDictionary(
    [
        new KeyValuePair<string, FileType>(".mdl",  FileType.Model),
        new KeyValuePair<string, FileType>(".tex",  FileType.Texture),
        new KeyValuePair<string, FileType>(".mtrl", FileType.Material),
        new KeyValuePair<string, FileType>(".atex", FileType.Animation),
        new KeyValuePair<string, FileType>(".avfx", FileType.Vfx),
        new KeyValuePair<string, FileType>(".scd",  FileType.Sound),
        new KeyValuePair<string, FileType>(".imc",  FileType.Imc),
        new KeyValuePair<string, FileType>(".pap",  FileType.Pap),
        new KeyValuePair<string, FileType>(".eqp",  FileType.MetaInfo),
        new KeyValuePair<string, FileType>(".eqdp", FileType.MetaInfo),
        new KeyValuePair<string, FileType>(".est",  FileType.MetaInfo),
        new KeyValuePair<string, FileType>(".exd",  FileType.MetaInfo),
        new KeyValuePair<string, FileType>(".exh",  FileType.MetaInfo),
        new KeyValuePair<string, FileType>(".shpk", FileType.Shader),
        new KeyValuePair<string, FileType>(".shcd", FileType.Shader),
        new KeyValuePair<string, FileType>(".fdt",  FileType.Font),
        new KeyValuePair<string, FileType>(".envb", FileType.Environment),
    ]);
}
