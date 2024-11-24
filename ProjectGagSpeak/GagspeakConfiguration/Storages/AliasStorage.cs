
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using Newtonsoft.Json;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// Contains a list of alias triggers for a spesified user
/// </summary>
[Serializable]
public class AliasStorage
{
    /// <summary> The storage of all aliases you have for the spesified user (defined in key) </summary>
    public string CharacterName { get; set; } = string.Empty;
    public string CharacterWorld { get; set; } = string.Empty;
    public List<AliasTrigger> AliasList { get; set; } = [];

    [JsonIgnore]
    public string NameWithWorld => CharacterName+"@"+CharacterWorld;

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(CharacterName) && !string.IsNullOrEmpty(CharacterWorld);


    public AliasStorage DeepCloneStorage()
    {
        return new AliasStorage()
        {
            CharacterName = CharacterName,
            CharacterWorld = CharacterWorld,
            AliasList = AliasList,
        };
    }

    public CharaAliasData ToAliasData()
    {
        return new CharaAliasData()
        {
            AliasList = AliasList,
        };
    }
}
