using Dalamud.Game;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;

namespace Penumbra.GameData.DataContainers.Bases;

/// <summary> A base class for dictionaries from a string to multiple objects of a type. </summary>
/// <typeparam name="T"> The type associated with strings. </typeparam>
/// <param name="pluginInterface"> The plugin interface. </param>
/// <param name="log"> A logger. </param>
/// <param name="name"> The name of the data share. </param>
/// <param name="language"> The language of the data share. </param>
/// <param name="version"> The version of the data share. </param>
/// <param name="factory"> A factory function to create the data. </param>
/// <param name="continuation"> Awaiter of the dependencies of the factory function. </param>
public abstract class DictLuminaName<T>(
    IDalamudPluginInterface pluginInterface,
    Logger log,
    string name,
    ClientLanguage language,
    int version,
    Func<IReadOnlyDictionary<string, IReadOnlyList<T>>> factory,
    Task? continuation = null)
    : DataSharer<IReadOnlyDictionary<string, IReadOnlyList<T>>>(pluginInterface, log, name, language, version, factory, continuation),
        IReadOnlyDictionary<string, IReadOnlyList<T>>
{
    /// <summary> The approximate size of the type. </summary>
    protected abstract int TypeSize { get; }

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(40, Count) + Keys.Sum(s => s.Length + 1) * 2 + Values.Sum(v => v.Count) * TypeSize;

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Value.Count;

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, IReadOnlyList<T>>> GetEnumerator()
        => Value.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(string key)
        => Value.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out IReadOnlyList<T>? value)
        => Value.TryGetValue(key, out value);

    /// <inheritdoc/>
    public IReadOnlyList<T> this[string key]
        => Value[key];

    /// <inheritdoc/>
    public IEnumerable<string> Keys
        => Value.Keys;

    /// <inheritdoc/>
    public IEnumerable<IReadOnlyList<T>> Values
        => Value.Values;
}
