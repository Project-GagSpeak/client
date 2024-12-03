using GagSpeak.GagspeakConfiguration.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class AliasConfig : IGagspeakConfiguration
{
    /// <summary> AliasList Storage per-paired user. </summary>
    public int Version { get; set; } = CurrentVersion;
    public static int CurrentVersion => 1;

    // personal global alias triggers. Only saved Locally, others cannot see.
    public List<AliasTrigger> GlobalAliasList { get; set; } = new();

    // Shared Alias Triggers per-kinkster. This is shared for them to see.
    public Dictionary<string, AliasStorage> AliasStorage { get; set; } = new();

    // Helper Method.
    public Dictionary<string, CharaAliasData> FromAliasStorage()
    {
        return AliasStorage.ToDictionary(x => x.Key, x => x.Value.ToAliasData());
    }
}
