using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers.Bases;

/// <summary> A base class for dictionaries from NPC IDs to their names. </summary>
/// <param name="pluginInterface"> The plugin interface. </param>
/// <param name="log"> A logger. </param>
/// <param name="gameData"> The data manger to fetch the data from. </param>
/// <param name="name"> The name of the data share. </param>
/// <param name="version"> The version of the data share. </param>
/// <param name="factory"> The factory function to create the data from. </param>
public abstract class NameDictionary(
    IDalamudPluginInterface pluginInterface,
    Logger log,
    IDataManager gameData,
    string name,
    int version,
    Func<IReadOnlyDictionary<uint, string>> factory)
    : DataSharer<IReadOnlyDictionary<uint, string>>(pluginInterface, log, name, gameData.Language, version, factory),
        IReadOnlyDictionary<NpcId, string>
{
    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<NpcId, string>> GetEnumerator()
        => Value.Select(kvp => new KeyValuePair<NpcId, string>(new NpcId(kvp.Key), kvp.Value)).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(NpcId key)
        => Value.ContainsKey(key.Id);

    /// <inheritdoc/>
    public bool TryGetValue(NpcId key, [NotNullWhen(true)] out string? value)
        => Value.TryGetValue(key.Id, out value);

    /// <inheritdoc/>
    public string this[NpcId key]
        => Value[key.Id];

    /// <inheritdoc/>
    public IEnumerable<NpcId> Keys
        => Value.Keys.Select(k => new NpcId(k));

    /// <inheritdoc/>
    public IEnumerable<string> Values
        => Value.Values;

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(16, Count) + Values.Sum(v => v.Length * 2);

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Count;
}
