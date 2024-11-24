namespace GagSpeak.Utils.ChatLog;

public class ChatCircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _start;
    private int _end;
    private int _size;

    public ChatCircularBuffer(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentException("Circular buffer cannot have negative or zero capacity.", nameof(capacity));
        }

        _buffer = new T[capacity];
        _size = 0;
        _start = 0;
        _end = 0;
    }

    public int Capacity => _buffer.Length;
    public bool IsFull => Size == Capacity;
    public bool IsEmpty => Size == 0;
    public int Size => _size;

    public T Front()
    {
        ThrowIfEmpty();
        return _buffer[_start];
    }

    public T Back()
    {
        ThrowIfEmpty();
        return _buffer[(_end != 0 ? _end : Capacity) - 1];
    }

    public T this[int index]
    {
        get
        {
            if (IsEmpty)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
            }
            if (index >= _size)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {_size}");
            }
            var actualIndex = InternalIndex(index);
            return _buffer[actualIndex];
        }
        set
        {
            if (IsEmpty)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
            }
            if (index >= _size)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {_size}");
            }
            var actualIndex = InternalIndex(index);
            _buffer[actualIndex] = value;
        }
    }

    public void PushBack(T item)
    {
        if (IsFull)
        {
            _buffer[_end] = item;
            Increment(ref _end);
            _start = _end;
        }
        else
        {
            _buffer[_end] = item;
            Increment(ref _end);
            ++_size;
        }
    }

    public void Clear()
    {
        _start = 0;
        _end = 0;
        _size = 0;
        Array.Clear(_buffer, 0, _buffer.Length);
    }

    public IList<ArraySegment<T>> ToArraySegments()
    {
        return new[] { ArrayOne(), ArrayTwo() };
    }

    public IEnumerator<T> GetEnumerator()
    {
        var segments = ToArraySegments();
        foreach (var segment in segments)
        {
            if (segment.Array is null)
                continue;

            for (var i = 0; i < segment.Count; i++)
                yield return segment.Array[segment.Offset + i];
        }
    }



    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void Increment(ref int index)
    {
        if (++index == Capacity)
        {
            index = 0;
        }
    }

    private void Decrement(ref int index)
    {
        if (index == 0)
        {
            index = Capacity;
        }
        index--;
    }

    private int InternalIndex(int index)
    {
        return _start + (index < Capacity - _start ? index : index - Capacity);
    }

    private ArraySegment<T> ArrayOne()
    {
        if (IsEmpty)
        {
            return new ArraySegment<T>(new T[0]);
        }
        else if (_start < _end)
        {
            return new ArraySegment<T>(_buffer, _start, _end - _start);
        }
        else
        {
            return new ArraySegment<T>(_buffer, _start, _buffer.Length - _start);
        }
    }

    private ArraySegment<T> ArrayTwo()
    {
        if (IsEmpty)
        {
            return new ArraySegment<T>(new T[0]);
        }
        else if (_start < _end)
        {
            return new ArraySegment<T>(_buffer, _end, 0);
        }
        else
        {
            return new ArraySegment<T>(_buffer, 0, _end);
        }
    }
}
