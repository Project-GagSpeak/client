using OtterGui;

namespace Penumbra.GameData;

public static class UtilityFunctions
{
    /// <summary> Try to find the index of the first object fulfilling the predicate in an enumerable. </summary>
    /// <param name="enumerable"> The enumerable. </param>
    /// <param name="predicate"> The predicate to compare on. </param>
    /// <param name="index"> The index of the first object, if any. </param>
    /// <returns> True if one was found, false otherwise. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool FindIndex<T>(IEnumerable<T> enumerable, Predicate<T> predicate, out int index)
    {
        index = enumerable.IndexOf(predicate);
        return index != -1;
    }

    /// <summary> Return the first object fulfilling the predicate or null for structs. </summary>
    /// <param name="values"> The enumerable. </param>
    /// <param name="predicate"> The predicate. </param>
    /// <returns> The first object fulfilling the predicate, or a null-optional. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T? FirstOrNull<T>(this IEnumerable<T> values, Func<T, bool> predicate) where T : struct
    {
        foreach(var val in values)
            if (predicate(val))
                return val;

        return null;
    }


    /// <summary> Add an item <paramref name="count"/> times to the end of a given array. </summary>
    /// <param name="array"> The existing array. </param>
    /// <param name="element"> The element to be added. </param>
    /// <param name="count"> The number of times to add the item. </param>
    /// <returns> The new array. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T[] AddItem<T>(this T[] array, T element, int count = 1)
    {
        var length = array.Length;
        Array.Resize(ref array, length + count);
        for (var i = length; i < array.Length; ++i)
            array[i] = element;

        return array;
    }

    /// <summary> Remove <paramref name="count"/> items starting from <paramref name="offset"/> from an array. </summary>
    /// <param name="array"> The existing array. </param>
    /// <param name="offset"> The index of the first item to remove. </param>
    /// <param name="count"> The number of items to remove. </param>
    /// <returns> The new array. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T[] RemoveItems<T>(this T[] array, int offset, int count = 1)
    {
        var newArray = new T[array.Length - count];
        Array.Copy(array, newArray,       offset);
        Array.Copy(array, offset + count, newArray, offset, newArray.Length - offset);
        return newArray;
    }

    /// <summary> Reinterprets some memory as another type. </summary>
    /// <typeparam name="TFrom"> Source type. </typeparam>
    /// <typeparam name="TTo"> Target type. Must be smaller or of the same size as the source type. </typeparam>
    /// <param name="value"> The reference to reinterpret. </param>
    /// <param name="index"> If several instances of the target type fit in the source type, index of the one to use. </param>
    /// <returns> Reinterpreted reference. </returns>
    /// <seealso cref="MemoryMarshal.Cast{TFrom, TTo}(Span{TFrom})"/>
    internal static ref TTo Cast<TFrom, TTo>(ref TFrom value, int index = 0) where TFrom : struct where TTo : struct
        => ref MemoryMarshal.Cast<TFrom, TTo>(new Span<TFrom>(ref value))[index];

    /// <inheritdoc cref="Cast{TFrom, TTo}(ref TFrom, int)"/>
    /// <seealso cref="MemoryMarshal.Cast{TFrom, TTo}(ReadOnlySpan{TFrom})"/>
    internal static ref readonly TTo ReadOnlyCast<TFrom, TTo>(in TFrom value, int index = 0) where TFrom : struct where TTo : struct
        => ref MemoryMarshal.Cast<TFrom, TTo>(new ReadOnlySpan<TFrom>(in value))[index];
}
