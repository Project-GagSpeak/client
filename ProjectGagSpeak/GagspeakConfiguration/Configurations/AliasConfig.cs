using GagSpeak.GagspeakConfiguration.Models;
using GagspeakAPI.Data.Character;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class AliasConfig : IGagspeakConfiguration
{
    /// <summary> AliasList Storage per-paired user. </summary>
    public int Version { get; set; } = CurrentVersion;
    public static int CurrentVersion => 1;



    public Dictionary<string, AliasStorage> AliasStorage { get; set; } = new();

    // Helper Method.
    public Dictionary<string, CharaAliasData> FromAliasStorage()
    {
        return AliasStorage.ToDictionary(x => x.Key, x => x.Value.ToAliasData());
    }
}
