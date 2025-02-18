using GagspeakAPI.Data.Character;

namespace GagSpeak.PlayerData.Storage;

public class PairAliasStorage : Dictionary<string, NamedAliasStorage>
{
    public PairAliasStorage() { }

    // Helpful for config read-write
    public PairAliasStorage(Dictionary<string, NamedAliasStorage> init) : base(init) { }

    public bool NameIsStored(string key) => !string.IsNullOrEmpty(this[key].StoredNameWorld);
}

public class NamedAliasStorage
{
    public NamedAliasStorage() { }

    public string StoredNameWorld { get; set; } = string.Empty;
    public AliasStorage Storage { get; set; } = new AliasStorage();
}

// This can double as a use for a GlobalAliasStorage.
public class AliasStorage : List<AliasTrigger>
{
    internal AliasStorage() { }
    public AliasStorage(IEnumerable<AliasTrigger> init) : base(init) { }

    public AliasStorage CloneAliasStorage()
        => new AliasStorage(this.Select(x => new AliasTrigger(x, false)).ToList());

    public CharaAliasData ToAliasData()
        => new CharaAliasData()
        {
            AliasList = this.ToList()
        };
}

