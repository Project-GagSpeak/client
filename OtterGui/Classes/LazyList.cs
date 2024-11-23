namespace OtterGui.Classes;

public interface ICachingList<out T> : IReadOnlyList<T>
{
    public void ClearList();
    public bool IsInitialized { get; }
}

/// <summary> A ReadOnlyList using a generator on access and caching the result until ClearList is called. </summary>
public class LazyList<T>(Func<IReadOnlyList<T>> generator) : ICachingList<T>
{
    private IReadOnlyList<T>? _list;

    public IEnumerator<T> GetEnumerator()
        => InitList().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => InitList().Count;

    private IReadOnlyList<T> InitList()
        => _list ??= generator();

    public void ClearList()
        => _list = null;

    public T this[int index]
        => InitList()[index];

    public bool IsInitialized
        => _list != null;

    public override string ToString()
        => _list == null ? "NULL" : $"Initialized ({_list.Count})";
}
