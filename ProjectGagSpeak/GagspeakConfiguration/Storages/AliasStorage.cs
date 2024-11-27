
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
    public bool HasNameStored => !string.IsNullOrEmpty(CharacterNameWithWorld);
    public string CharacterNameWithWorld { get; set; } = string.Empty;
    public List<AliasTrigger> AliasList { get; set; } = [];



    public List<AliasTrigger> CloneAliasList()
    {
        return AliasList.Select(alias => new AliasTrigger
        {
            Enabled = alias.Enabled,
            InputCommand = alias.InputCommand,
            OutputCommand = alias.OutputCommand
        }).ToList();
    }

    public CharaAliasData ToAliasData()
    {
        return new CharaAliasData()
        {
            HasNameStored = HasNameStored,
            AliasList = AliasList,
        };
    }
}
