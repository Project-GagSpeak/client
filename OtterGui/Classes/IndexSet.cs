namespace OtterGui.Classes;

public class IndexSet : IEnumerable<int>
{
    private readonly BitArray _set;
    private          int      _count;

    public int Capacity
        => _set.Count;

    public int Count
        => _count;

    public bool this[Index index]
    {
        get => _set[index];
        set
        {
            if (value)
                Add(index);
            else
                Remove(index);
        }
    }

    public IndexSet(int capacity, bool initiallyFull)
    {
        _set   = new BitArray(capacity, initiallyFull);
        _count = initiallyFull ? capacity : 0;
    }

    public bool Add(Index index)
    {
        var ret = !_set[index];
        if (ret)
        {
            ++_count;
            _set[index] = true;
        }

        return ret;
    }

    public bool Remove(Index index)
    {
        var ret = _set[index];
        if (ret)
        {
            --_count;
            _set[index] = false;
        }

        return ret;
    }

    public int AddRange(int offset, int length)
    {
        var ret = 0;
        for (var idx = 0; idx < length; ++idx)
        {
            if (Add(offset + idx))
                ++ret;
        }

        return ret;
    }

    public int RemoveRange(int offset, int length)
    {
        var ret = 0;
        for (var idx = 0; idx < length; ++idx)
        {
            if (Remove(offset + idx))
                ++ret;
        }

        return ret;
    }

    /// <summary>
    /// Gets an enumerable that will return the indices that are either part of this set, or missing from it.
    /// </summary>
    /// <param name="complement">false (default) to get the indices that are part of this set, true to get those that are missing from it</param>
    /// <returns>The index enumerable</returns>
    public IEnumerable<int> Indices(bool complement = false)
    {
        var capacity = _set.Count;
        var remaining = complement ? capacity - _count : _count;

        if (remaining <= 0)
            yield break;

        for (var i = 0; i < capacity; ++i)
        {
            if (_set[i] == complement)
                continue;

            yield return i;

            if (--remaining == 0)
                yield break;
        }
    }

    public IEnumerator<int> GetEnumerator()
        => Indices().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Indices().GetEnumerator();

    /// <summary>
    /// Gets an enumerable that will return the ranges of indices that are either part of this set, or missing from it.
    /// </summary>
    /// <param name="complement">false (default) to get the ranges of indices that are part of this set, true to get those that are missing from it</param>
    /// <returns>The range enumerable</returns>
    public IEnumerable<(int Start, int End)> Ranges(bool complement = false)
    {
        var capacity = _set.Count;
        var remaining = complement ? capacity - _count : _count;

        if (remaining <= 0)
            yield break;

        for (var i = 0; i < capacity; ++i)
        {
            if (_set[i] == complement)
                continue;

            var start = i;
            while (i < capacity && _set[i] != complement)
                ++i;

            yield return (start, i);

            remaining -= i - start;
            if (remaining == 0)
                yield break;
        }
    }
}
