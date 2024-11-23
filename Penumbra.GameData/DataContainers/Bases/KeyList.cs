using Dalamud.Game;
using Dalamud.Plugin;
using OtterGui.Log;

namespace Penumbra.GameData.DataContainers.Bases;

/// <summary> A list sorting objects based on a key which then allows efficiently finding all objects between a pair of keys via binary search. </summary>
public abstract class KeyList<T> : DataSharer<IReadOnlyList<(ulong Key, T Data)>>
{
    /// <summary> We need to cast the list back to a regular list for the binary search. </summary>
    private List<(ulong Key, T Data)> InternalValue
        => (List<(ulong Key, T Data)>)Value;

    /// <summary> Iterate over all objects between the given minimal and maximal keys (inclusive). </summary>
    protected IEnumerable<T> Between(ulong minKey, ulong maxKey)
    {
        var (minIdx, maxIdx) = GetMinMax(minKey, maxKey);
        if (minIdx < 0)
            yield break;

        for (var i = minIdx; i <= maxIdx; ++i)
            yield return Value[i].Data;
    }

    /// <summary> Obtain the minimum index and maximum index for a minimum and maximum key. </summary>
    private (int MinIdx, int MaxIdx) GetMinMax(ulong minKey, ulong maxKey)
    {
        // Find the minimum index by binary search.
        var idx    = InternalValue.BinarySearch((minKey, default!), ListComparer);
        var minIdx = idx;

        // If the key does not exist, check if it is an invalid range or set it correctly.
        if (minIdx < 0)
        {
            minIdx = ~minIdx;
            if (minIdx == InternalValue.Count || InternalValue[minIdx].Key > maxKey)
                return (-1, -1);

            idx = minIdx;
        }
        else
        {
            // If it does exist, go upwards until the first key is reached that is actually smaller.
            while (minIdx > 0 && InternalValue[minIdx - 1].Key >= minKey)
                --minIdx;
        }

        // Check if the range can be valid.
        if (InternalValue[minIdx].Key < minKey || InternalValue[minIdx].Key > maxKey)
            return (-1, -1);


        // Do pretty much the same but in the other direction with the maximum key.
        var maxIdx = InternalValue.BinarySearch(idx, InternalValue.Count - idx, (maxKey, default!), ListComparer);
        if (maxIdx < 0)
        {
            maxIdx = ~maxIdx;
            return maxIdx > minIdx ? (minIdx, maxIdx - 1) : (-1, -1);
        }

        while (maxIdx < InternalValue.Count - 1 && InternalValue[maxIdx + 1].Key <= maxKey)
            ++maxIdx;

        if (InternalValue[maxIdx].Key < minKey || InternalValue[maxIdx].Key > maxKey)
            return (-1, -1);

        return (minIdx, maxIdx);
    }

    /// <summary> Create a KeyList. </summary>
    /// <param name="pi"> The plugin interface. </param>
    /// <param name="log"> A logger. </param>
    /// <param name="name"> The name of the data share. </param>
    /// <param name="language"> The language of the data share. </param>
    /// <param name="version"> The version of the data share. </param>
    /// <param name="data"> A factory for the available data. </param>
    /// <param name="toKey"> A generator converting the data to keys. </param>
    /// <param name="validKey"> A predicate checking for valid or invalid keys. </param>
    /// <param name="valueKeySelector"> A sorter that can sort multiple identical keys based on the data. </param>
    /// <param name="continuation"> An awaiter that has to have finished before calling the factory. </param>
    protected KeyList(IDalamudPluginInterface pi, Logger log, string name, ClientLanguage language, int version, Func<IEnumerable<T>> data,
        Func<T, ulong> toKey, Func<ulong, bool> validKey, Func<T, int> valueKeySelector, Task? continuation = null)
        : base(pi, log, name, language, version,
            () => data().Select(d => (toKey(d), d)).Where(p => validKey(p.Item1)).OrderBy(p => p.Item1)
                .ThenBy(p => valueKeySelector(p.d)).ToList(), continuation)
    { }

    /// <summary> A comparer that compares based on the key only for the binary searches.. </summary>
    private class Comparer : IComparer<(ulong, T)>
    {
        public int Compare((ulong, T) x, (ulong, T) y)
            => x.Item1.CompareTo(y.Item1);
    }

    private static readonly Comparer ListComparer = new();

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Value.Count;
}
