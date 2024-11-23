
namespace Penumbra.GameData.Structs;

public struct SharedSet<TItem, TBitVector>(SharedSet<TItem, TBitVector>.Universe? possibleValues) : IEnumerable<TItem>, IEquatable<SharedSet<TItem, TBitVector>>, ICloneable
    where TItem : IEquatable<TItem> where TBitVector : unmanaged, IBinaryInteger<TBitVector>
{
    public readonly Universe?  PossibleValues = possibleValues;
    private         TBitVector _bitVector;

    public readonly TBitVector BitVector
        => _bitVector;

    public readonly int Count
        => int.CreateSaturating(TBitVector.PopCount(_bitVector));

    private SharedSet(Universe? possibleValues, TBitVector bitVector) : this(possibleValues)
        => _bitVector = bitVector;

    public bool Add(TItem value)
    {
        if (PossibleValues == null)
            throw new NotSupportedException("Cannot add anything to a SharedSet without a backing Universe");
        var index = PossibleValues.Find(value, true);
        var mask  = TBitVector.One << index;
        if ((_bitVector & mask) != TBitVector.Zero)
            return false;
        _bitVector |= mask;
        return true;
    }

    public bool AddExisting(TItem value)
    {
        if (PossibleValues == null)
            return false;
        var index = PossibleValues.Find(value, true);
        if (index < 0)
            return false;
        var mask = TBitVector.One << index;
        if ((_bitVector & mask) != TBitVector.Zero)
            return false;
        _bitVector |= mask;
        return true;
    }

    public bool Remove(TItem value)
    {
        if (PossibleValues == null)
            return false;
        var index = PossibleValues.Find(value, false);
        if (index < 0)
            return false;
        var mask = TBitVector.One << index;
        if ((_bitVector & mask) == TBitVector.Zero)
            return false;
        _bitVector &= ~mask;
        return true;
    }

    public readonly bool Contains(TItem value)
    {
        if (PossibleValues == null)
            return false;
        var index = PossibleValues.Find(value, false);
        if (index < 0)
            return false;
        var mask = TBitVector.One << index;
        return (_bitVector & mask) != TBitVector.Zero;
    }

    public readonly bool IsSubsetOf(SharedSet<TItem, TBitVector> other)
        => PossibleValues == other.PossibleValues && (_bitVector & other._bitVector) == _bitVector;

    public readonly bool IsProperSubsetOf(SharedSet<TItem, TBitVector> other)
        => IsSubsetOf(other) && _bitVector != other._bitVector;

    public readonly bool IsSupersetOf(SharedSet<TItem, TBitVector> other)
        => PossibleValues == other.PossibleValues && (_bitVector & other._bitVector) == other._bitVector;

    public readonly bool IsProperSupersetOf(SharedSet<TItem, TBitVector> other)
        => IsSupersetOf(other) && _bitVector != other._bitVector;

    public readonly bool Overlaps(SharedSet<TItem, TBitVector> other)
        => PossibleValues == other.PossibleValues && (_bitVector & other._bitVector) != TBitVector.Zero;

    public readonly SharedSet<TItem, TBitVector> CreateShared()
        => new(PossibleValues);

    public readonly Enumerator GetEnumerator()
        => new(this);

    readonly IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        => new Enumerator(this);

    readonly IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    readonly object ICloneable.Clone()
        => this;

    public static SharedSet<TItem, TBitVector> CreateNew()
        => new([]);

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is SharedSet<TItem, TBitVector> other && Equals(other);

    public readonly bool Equals(SharedSet<TItem, TBitVector> other)
        => PossibleValues == other.PossibleValues && _bitVector == other._bitVector;

    public override readonly int GetHashCode()
        => HashCode.Combine(PossibleValues, _bitVector);

    public static bool operator ==(SharedSet<TItem, TBitVector> lhs, SharedSet<TItem, TBitVector> rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(SharedSet<TItem, TBitVector> lhs, SharedSet<TItem, TBitVector> rhs)
        => !lhs.Equals(rhs);

    public static SharedSet<TItem, TBitVector> operator ~(SharedSet<TItem, TBitVector> value)
        => new(value.PossibleValues, value.PossibleValues == null ? TBitVector.Zero : ((TBitVector.One << value.PossibleValues.Count) - TBitVector.One) ^ value._bitVector);

    public static SharedSet<TItem, TBitVector> operator &(SharedSet<TItem, TBitVector> lhs, SharedSet<TItem, TBitVector> rhs)
        => lhs.PossibleValues == rhs.PossibleValues
            ? new SharedSet<TItem, TBitVector>(lhs.PossibleValues, lhs._bitVector & rhs._bitVector)
            : throw new ArgumentException($"Cannot combine SharedSets not backed by the same Universe");

    public static SharedSet<TItem, TBitVector> operator |(SharedSet<TItem, TBitVector> lhs, SharedSet<TItem, TBitVector> rhs)
        => lhs.PossibleValues == rhs.PossibleValues
            ? new SharedSet<TItem, TBitVector>(lhs.PossibleValues, lhs._bitVector | rhs._bitVector)
            : throw new ArgumentException($"Cannot combine SharedSets not backed by the same Universe");

    public static SharedSet<TItem, TBitVector> operator ^(SharedSet<TItem, TBitVector> lhs, SharedSet<TItem, TBitVector> rhs)
        => lhs.PossibleValues == rhs.PossibleValues
            ? new SharedSet<TItem, TBitVector>(lhs.PossibleValues, lhs._bitVector ^ rhs._bitVector)
            : throw new ArgumentException($"Cannot combine SharedSets not backed by the same Universe");

    public class Universe : IReadOnlyList<TItem>
    {
        protected readonly TItem[] Array;
        protected          int     InternalCount;

        public int Capacity
            => Array.Length;

        public int Count
            => InternalCount;

        public TItem this[int index]
            => index >= 0 && index < InternalCount
                ? Array[index]
                : throw new IndexOutOfRangeException($"Index {index} out of range of SharedSet.Universe of count {InternalCount}");

        public ReadOnlySpan<TItem> Span
            => Array.AsSpan(0, InternalCount);

        public unsafe Universe()
            => Array = new TItem[sizeof(TBitVector) << 3];

        public ReadOnlySpan<TItem>.Enumerator GetEnumerator()
            => Span.GetEnumerator();

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
            => Array.Take(InternalCount).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Array.Take(InternalCount).GetEnumerator();

        public SharedSet<TItem, TBitVector> EmptySet()
            => new(this);

        public SharedSet<TItem, TBitVector> FullSet()
            => ~EmptySet();

        public int IndexOf(TItem value)
            => Find(value, false);

        public int Add(TItem value)
            => Find(value, true);

        public virtual int Find(TItem value, bool orAdd)
        {
            for (var i = 0; i < InternalCount; ++i)
            {
                if (value.Equals(Array[i]))
                    return i;
            }

            if (!orAdd)
                return -1;

            if (InternalCount == Array.Length)
                throw new InvalidOperationException($"The SharedSet.Universe (of capacity {Array.Length}) is full");

            var newI    = InternalCount++;
            Array[newI] = value;

            return newI;
        }

        public override string ToString()
            => $"{GetType().Name} {{ {string.Join(", ", this)} }}";

        public static implicit operator SharedSet<TItem, TBitVector>(Universe values)
            => values.FullSet();
    }

    public class SortedUniverse(IComparer<TItem>? comparer = null) : Universe
    {
        public SortedUniverse(IEnumerable<TItem> items, IComparer<TItem>? comparer = null) : this(comparer)
        {
            foreach (var item in items)
            {
                var i = System.Array.BinarySearch(Array, 0, InternalCount, item, comparer);
                if (i < 0)
                {
                    if (InternalCount == Array.Length)
                        throw new InvalidOperationException($"The SharedSet.Universe (of capacity {Array.Length}) is full");

                    // Inserting items in the middle violates the invariant that a Universe's Array must be append-only,
                    // but it is not an issue to do so during construction, as no SharedSet can possibly be backed by it yet.

                    i = ~i;
                    for (var j = InternalCount; j-- > i;)
                        Array[j + 1] = Array[j];
                    Array[i] = item;
                    ++InternalCount;
                }
            }
        }

        public override int Find(TItem value, bool orAdd)
        {
            var i = System.Array.BinarySearch(Array, 0, InternalCount, value, comparer);
            if (i >= 0)
                return i;

            if (!orAdd)
                return -1;

            if (InternalCount == Array.Length)
                throw new InvalidOperationException($"The SharedSet.Universe (of capacity {Array.Length}) is full");

            i = ~i;
            if (i != InternalCount)
                throw new ArgumentException($"Elements must be inserted in the SharedSet.SortedUniverse in ascending order");

            Array[InternalCount++] = value;

            return i;
        }
    }

    public struct Enumerator(SharedSet<TItem, TBitVector> set) : IEnumerator<TItem>
    {
        private readonly Universe?  _possibleValues = set.PossibleValues;
        private readonly TBitVector _bitVector      = set._bitVector;
        private          int        _position       = -1;

        public readonly TItem Current
            => _possibleValues![_position];

        readonly object IEnumerator.Current
            => _possibleValues![_position];

        public void Reset()
        {
            _position = -1;
        }

        public bool MoveNext()
        {
            if (_possibleValues == null)
                return false;

            while (_position < _possibleValues.Count)
            {
                ++_position;
                if ((_bitVector & (TBitVector.One << _position)) != TBitVector.Zero)
                    return true;
            }

            return false;
        }

        public readonly void Dispose()
        { }
    }
}
