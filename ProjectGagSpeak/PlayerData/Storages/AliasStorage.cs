using GagspeakAPI.Data.Character;

namespace GagSpeak.PlayerData.Storage;

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
            Label = alias.Label,
            InputCommand = alias.InputCommand,
            Executions = alias.Executions
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
