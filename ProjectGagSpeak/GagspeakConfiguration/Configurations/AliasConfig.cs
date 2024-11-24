using GagSpeak.GagspeakConfiguration.Models;
using GagspeakAPI.Data.Character;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class AliasConfig : IGagspeakConfiguration
{
    /// <summary> AliasList Storage per-paired user. </summary>
    public Dictionary<string, AliasStorage> AliasStorage { get; set; } = new();
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;

    public Dictionary<string, CharaAliasData> FromAliasStorage()
    {
        return AliasStorage.ToDictionary(x => x.Key, x => x.Value.ToAliasData());
    }
}
