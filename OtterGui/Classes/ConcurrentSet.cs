namespace OtterGui.Classes;

/// <summary>
/// An empty structure. Can be used as value of a concurrent dictionary, to use it as a set.
/// </summary>
public readonly struct NullValue : IEquatable<NullValue>
{
    public static readonly NullValue Void = new();

    public bool Equals(NullValue other)
        => true;

    public override bool Equals(object? obj)
        => obj is NullValue;

    public override int GetHashCode()
        => 0;

    public static bool operator ==(NullValue _1, NullValue _2)
        => true;

    public static bool operator !=(NullValue _1, NullValue _2)
        => false;
}

/// <summary> An easy implementation of ConcurrentSet </summary>
public sealed class ConcurrentSet<T> : ConcurrentDictionary<T, NullValue> where T : notnull
{
    public new IEnumerator<T> GetEnumerator()
        => Keys.GetEnumerator();

    /// <summary> Try to add a value to the set. </summary>
    public bool TryAdd(T value)
        => base.TryAdd(value, NullValue.Void);

    /// <summary> Try to remove a value from the set. </summary>
    public bool TryRemove(T value)
        => base.TryRemove(value, out _);

    /// <remarks> Hide from public interface. </remarks>
    private new bool TryAdd(T key, NullValue value)
        => base.TryAdd(key, value);

    /// <remarks> Hide from public interface. </remarks>
    private new bool TryRemove(T value, out NullValue ret)
        => base.TryRemove(value, out ret);
}
