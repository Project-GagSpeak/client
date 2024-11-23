using System.Collections;
using System.Runtime.CompilerServices;

namespace Penumbra.Api.Helpers;

/// <summary> A dictionary that implicitly can be converted to a read-only dictionary with different value type. </summary>
/// <typeparam name="TKey"> The type of the keys. </typeparam>
/// <typeparam name="TValueFrom"> The actual type of the values. </typeparam>
/// <typeparam name="TValueTo"> The read-only type of the values. </typeparam>
public abstract class ConvertingDict<TKey, TValueFrom, TValueTo>(IReadOnlyDictionary<TKey, TValueFrom> dict)
    : IReadOnlyDictionary<TKey, TValueTo>
    where TKey : notnull
{
    /// <summary> Obtain the original dictionary. </summary>
    public IReadOnlyDictionary<TKey, TValueFrom> Original
        => dict;

    /// <summary> Conversion between values. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected abstract TValueTo ConvertValue(in TValueFrom from);

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
        => dict.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, out TValueTo value)
    {
        if (dict.TryGetValue(key, out var v))
        {
            value = ConvertValue(v);
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc/>
    public TValueTo this[TKey key]
        => ConvertValue(dict[key]);

    public IEnumerable<TKey> Keys
        => dict.Keys;

    /// <inheritdoc/>
    public IEnumerable<TValueTo> Values
    {
        get
        {
            foreach (var value in dict.Values)
                yield return ConvertValue(value);
        }
    }

    /// <inheritdoc/>
    public int Count
        => dict.Count;

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValueTo>> GetEnumerator()
    {
        foreach (var kvp in dict)
            yield return new KeyValuePair<TKey, TValueTo>(kvp.Key, ConvertValue(kvp.Value));
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

/// <summary> A dictionary that implicitly can be converted to a read-only dictionary with different value type. </summary>
/// <typeparam name="TKeyFrom"> The actual type of the keys. </typeparam>
/// <typeparam name="TKeyTo"> The read-only type of the keys. </typeparam>
/// <typeparam name="TValueFrom"> The actual type of the values. </typeparam>
/// <typeparam name="TValueTo"> The read-only type of the values. </typeparam>
public abstract class ConvertingDict<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IReadOnlyDictionary<TKeyFrom, TValueFrom> dict)
    : IReadOnlyDictionary<TKeyTo, TValueTo>
    where TKeyFrom : notnull
    where TKeyTo : notnull
{
    /// <summary> Obtain the original dictionary. </summary>
    public IReadOnlyDictionary<TKeyFrom, TValueFrom> Original
        => dict;

    /// <summary> Conversion between keys. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected abstract TKeyTo ConvertKey(in TKeyFrom from);

    /// <summary> Conversion between keys. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected abstract TKeyFrom ConvertKeyBack(in TKeyTo from);

    /// <summary> Conversion between values. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected abstract TValueTo ConvertValue(in TValueFrom from);

    /// <inheritdoc/>
    public bool ContainsKey(TKeyTo key)
        => dict.ContainsKey(ConvertKeyBack(key));

    /// <inheritdoc/>
    public bool TryGetValue(TKeyTo key, out TValueTo value)
    {
        if (dict.TryGetValue(ConvertKeyBack(key), out var v))
        {
            value = ConvertValue(v);
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc/>
    public TValueTo this[TKeyTo key]
        => ConvertValue(dict[ConvertKeyBack(key)]);

    /// <inheritdoc/>
    public IEnumerable<TKeyTo> Keys
    {
        get
        {
            foreach (var key in dict.Keys)
                yield return ConvertKey(key);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<TValueTo> Values
    {
        get
        {
            foreach (var value in dict.Values)
                yield return ConvertValue(value);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKeyTo, TValueTo>> GetEnumerator()
    {
        foreach (var kvp in dict)
            yield return new KeyValuePair<TKeyTo, TValueTo>(ConvertKey(kvp.Key), ConvertValue(kvp.Value));
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => dict.Count;
}
