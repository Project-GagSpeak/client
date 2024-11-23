using Dalamud.Game;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers.Bases;

/// <summary> A base class for dictionaries from an item id to the corresponding item. </summary>
/// <param name="pluginInterface"> The plugin interface. </param>
/// <param name="log"> A logger. </param>
/// <param name="name"> The name of the data share. </param>
/// <param name="language"> The language of the data share. </param>
/// <param name="version"> The version of the data share. </param>
/// <param name="factory"> A factory function to create the data. </param>
/// <param name="continuation"> Awaiter of the dependencies of the factory function. </param>
public abstract class ItemDictionary(
    IDalamudPluginInterface pluginInterface,
    Logger log,
    string name,
    ClientLanguage language,
    int version,
    Func<IReadOnlyDictionary<ulong, PseudoEquipItem>> factory,
    Task? continuation = null)
    : DataSharer<IReadOnlyDictionary<ulong, PseudoEquipItem>>(pluginInterface, log, name, language, version, factory, continuation),
        IReadOnlyDictionary<CustomItemId, EquipItem>
{
    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<CustomItemId, EquipItem>> GetEnumerator()
        => Value.Select(kvp => new KeyValuePair<CustomItemId, EquipItem>(kvp.Key, kvp.Value)).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(CustomItemId key)
        => Value.ContainsKey(key.Id);

    /// <inheritdoc/>
    public bool TryGetValue(CustomItemId key, out EquipItem value)
    {
        if (Value.TryGetValue(key.Id, out var v))
        {
            value = v;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public EquipItem this[CustomItemId key]
        => Value[key.Id];

    /// <inheritdoc/>
    public IEnumerable<CustomItemId> Keys
        => Value.Keys.Select(i => (CustomItemId) i);

    /// <inheritdoc/>
    public IEnumerable<EquipItem> Values
        => Value.Values.Select(e => (EquipItem)e);

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(40, Count);

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Count;
}
