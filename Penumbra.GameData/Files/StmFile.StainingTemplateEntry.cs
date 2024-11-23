using Penumbra.GameData.Files.Utility;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files;

public partial class StmFile<TDyePack>
{
    public readonly struct StainingTemplateEntry
    {
        /// <summary>
        /// The number of stains is capped at 128 at the moment
        /// </summary>
        public const int NumElements = 128;

        // ColorTable row information for each stain.
        public readonly IReadOnlyList<HalfColor>[] Colors;
        public readonly IReadOnlyList<Half>[]      Scalars;

        public TDyePack this[StainId idx]
            => this[(int)idx.Id];

        public TDyePack this[int idx]
        {
            get
            {
                // The 0th index is skipped.
                if (idx is <= 0 or > NumElements)
                    return default;

                --idx;

                var pack     = new TDyePack();
                var packSpan = MemoryMarshal.Cast<TDyePack, Half>(new Span<TDyePack>(ref pack));
                var colors   = MemoryMarshal.Cast<Half, HalfColor>(packSpan[0..(Colors.Length * 3)]);
                var scalars  = packSpan[(Colors.Length * 3)..];

                for (var i = 0; i < colors.Length; ++i)
                    colors[i] = Colors[i][idx];
                for (var i = 0; i < scalars.Length; ++i)
                    scalars[i] = Scalars[i][idx];

                return pack;
            }
        }

        // Actually parse an entry.
        public StainingTemplateEntry(SpanBinaryReader br)
        {
            Span<ushort> byteCounts = stackalloc ushort[TDyePack.ColorCount + TDyePack.ScalarCount];
            ushort       lastEnd    = 0;
            for (var i = 0; i < byteCounts.Length; ++i)
            {
                var nextEnd   = (ushort)(br.ReadUInt16() * 2); // because the ends are in terms of ushort.
                byteCounts[i] = (ushort)(nextEnd - lastEnd);
                lastEnd       = nextEnd;
            }

            Colors  = new IReadOnlyList<HalfColor>[TDyePack.ColorCount];
            Scalars = new IReadOnlyList<Half>[TDyePack.ScalarCount];
            var j = 0;
            for (var i = 0; i < Colors.Length; i++, j++)
                Colors[i] = ReadArray<HalfColor>(br.SliceFromHere(byteCounts[j]));
            for (var i = 0; i < Scalars.Length; i++, j++)
                Scalars[i] = ReadArray<Half>(br.SliceFromHere(byteCounts[j]));
        }

        private static unsafe IReadOnlyList<T> ReadArray<T>(SpanBinaryReader br) where T : unmanaged
        {
            var arraySize = br.Remaining / sizeof(T);
            // The actual amount of entries informs which type of list we use.
            switch (arraySize)
            {
                case 0: return new RepeatingList<T>(default!,     NumElements); // All default
                case 1: return new RepeatingList<T>(br.Read<T>(), NumElements); // All single entry
                case NumElements: return br.Read<T>(NumElements).ToArray();     // 1-to-1 entries
                // Indexed access.
                case < NumElements: return new IndexedList<T>(br, (br.Remaining - NumElements) / sizeof(T), NumElements);
                // Should not happen.
                case > NumElements: throw new InvalidDataException($"Stain Template can not have more than {NumElements} elements.");
            }
        }

        /// <summary>
        /// Used if a single value is used for all entries of a list.
        /// </summary>
        private sealed class RepeatingList<T> : IReadOnlyList<T>
        {
            private readonly T   _value;
            public           int Count { get; }

            public RepeatingList(T value, int size)
            {
                _value = value;
                Count  = size;
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < Count; ++i)
                    yield return _value;
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public T this[int index]
                => index >= 0 && index < Count ? _value : throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Used if there is a small set of values for a bigger list, accessed via index information.
        /// </summary>
        private sealed class IndexedList<T> : IReadOnlyList<T> where T : unmanaged
        {
            private readonly T[]    _values;
            private readonly byte[] _indices;

            /// <summary>
            /// Reads <paramref name="count"/> values from <paramref name="br"/> via <paramref name="read"/>, then reads <paramref name="indexCount"/> byte indices.
            /// </summary>
            public IndexedList(SpanBinaryReader br, int count, int indexCount)
            {
                _values    = new T[count + 1];
                _indices   = new byte[indexCount];
                _values[0] = default!;
                br.Read<T>(count).CopyTo(_values.AsSpan(1));

                // First byte seems to be an unused 0xFF byte marker.
                // Necessary for correct offsets.
                br.Read<byte>(indexCount)[1..].CopyTo(_indices);
                for (var i = 0; i < indexCount; ++i)
                {
                    if (_indices[i] > count)
                        _indices[i] = 0;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < NumElements; ++i)
                    yield return _values[_indices[i]];
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public int Count
                => _indices.Length;

            public T this[int index]
                => index >= 0 && index < Count ? _values[_indices[index]] : default!;
        }
    }
}
