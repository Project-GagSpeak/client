using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Data.Character;

/// <summary>
/// CharacterData class stores all of the user's settings, permissions, and apperance data
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class CharaAliasData
{
    public string CharacterName { get; set; } = string.Empty;
    public string CharacterWorld { get; set; } = string.Empty;
    public List<AliasTrigger> AliasList { get; set; } = new List<AliasTrigger>();
}
