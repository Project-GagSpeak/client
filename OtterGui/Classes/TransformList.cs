namespace OtterGui.Classes;

/// <summary>
/// A IReadOnlyList based on any other IReadOnlyList that applies a transforming step before fetching.
/// </summary>
public readonly struct TransformList<TIn, TOut> : IReadOnlyList<TOut>
{
    private readonly IReadOnlyList<TIn> _list;
    private readonly Func<TIn, TOut>    _transform;

    public TransformList(IReadOnlyList<TIn> items, Func<TIn, TOut> transform)
    {
        _list      = items;
        _transform = transform;
    }

    public IEnumerator<TOut> GetEnumerator()
        => _list.Select(_transform).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _list.Count;

    public TOut this[int index]
        => _transform(_list[index]);
}
