using Penumbra.GameData.Enums;
using Penumbra.String.Functions;

namespace Penumbra.GameData.Structs;

/// <summary> A struct containing all the customize data for a character model. </summary>
[StructLayout(LayoutKind.Sequential, Size = Size)]
public unsafe struct CustomizeArray : IEquatable<CustomizeArray>, IReadOnlyCollection<CustomizeValue>
{
    /// <summary> The size of the customize array. </summary>
    public const int Size = 26;

    /// <summary> The data.  </summary>
    public fixed byte Data[Size];

    /// <summary> The size of the customize array. </summary>
    public readonly int Count
        => Size;

    /// <summary> Get or set the race as its enum. </summary>
    public Race Race
    {
        get => (Race)Get(CustomizeIndex.Race).Value;
        set => Set(CustomizeIndex.Race, (CustomizeValue)(byte)value);
    }

    /// <summary> Get or set the gender as its enum. This is offset by 1 because of the Unknown option. </summary>
    public Gender Gender
    {
        get => (Gender)Get(CustomizeIndex.Gender).Value + 1;
        set => Set(CustomizeIndex.Gender, (CustomizeValue)(byte)value - 1);
    }

    /// <summary> Get or set the body type. </summary>
    public CustomizeValue BodyType
    {
        get => Get(CustomizeIndex.BodyType);
        set => Set(CustomizeIndex.BodyType, value);
    }

    /// <summary> Get or set the clan as its enum. </summary>
    public SubRace Clan
    {
        get => (SubRace)Get(CustomizeIndex.Clan).Value;
        set => Set(CustomizeIndex.Clan, (CustomizeValue)(byte)value);
    }

    /// <summary> Get or set the face. </summary>
    public CustomizeValue Face
    {
        get => Get(CustomizeIndex.Face);
        set => Set(CustomizeIndex.Face, value);
    }

    public bool Highlights
    {
        get => Get(CustomizeIndex.Highlights) != CustomizeValue.Zero;
        set => Set(CustomizeIndex.Highlights, value ? CustomizeValue.Max : CustomizeValue.Zero);
    }

    /// <summary> The default customize array for unspecified characters. </summary>
    public static readonly CustomizeArray Default = GenerateDefault();

    /// <summary> An empty customize array. </summary>
    public static readonly CustomizeArray Empty = new();

    /// <summary> Get a specific value from the customize array. </summary>
    /// <param name="index"> The desired customization option. </param>
    /// <returns> The value at the options index masked with the options mask. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public CustomizeValue Get(CustomizeIndex index)
    {
        var (offset, mask) = index.ToByteAndMask();
        return (CustomizeValue)(Data[offset] & mask);
    }

    /// <summary> Set a specific value in the customize array. </summary>
    /// <param name="index"> The desired customization option. </param>
    /// <param name="value"> The desired value. </param>
    /// <returns> Whether anything changed in the array. </returns>
    /// <remarks> The value will be masked with the options mask before setting. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Set(CustomizeIndex index, CustomizeValue value)
    {
        var (offset, mask) = index.ToByteAndMask();
        return mask != 0xFF
            ? SetIfDifferentMasked(ref Data[offset], value, mask)
            : SetIfDifferent(ref Data[offset], value);
    }

    /// <summary> Get or set a specific customize value. </summary>
    public CustomizeValue this[CustomizeIndex index]
    {
        get => Get(index);
        set => Set(index, value);
    }

    /// <summary> Compare two customize arrays and return a set of flags of all differing values. </summary>
    public static CustomizeFlag Compare(CustomizeArray lhs, CustomizeArray rhs)
    {
        CustomizeFlag ret = 0;
        foreach (var idx in Enum.GetValues<CustomizeIndex>())
        {
            var l = lhs[idx];
            var r = rhs[idx];
            if (l.Value != r.Value)
                ret |= idx.ToFlag();
        }

        return ret;
    }

    /// <summary> Iterate over all bytes in the data. </summary>
    public readonly IEnumerator<CustomizeValue> GetEnumerator()
    {
        for (var i = 0; i < Size; ++i)
            yield return AtIndex(i);
    }

    /// <summary> Iterate over all bytes in the data. </summary>
    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary> Get the byte at the given index without range-checking. </summary>
    public readonly CustomizeValue AtIndex(int index)
        => (CustomizeValue)Data[index];

    /// <summary> Set the byte at the given index without range-checking. </summary>
    public void SetByIndex(int index, CustomizeValue value)
        => Data[index] = value.Value;


    /// <summary> Read a pointer to another Customize array and copy its data. </summary>
    public void Read(void* source)
    {
        fixed (byte* ptr = Data)
        {
            MemoryUtility.MemCpyUnchecked(ptr, source, Size);
        }
    }

    /// <summary> Write this data to another Customize array given by a pointer. </summary>
    public readonly void Write(void* target)
    {
        fixed (byte* ptr = Data)
        {
            MemoryUtility.MemCpyUnchecked(target, ptr, Size);
        }
    }

    /// <summary> Compare two Customize arrays for equality. </summary>
    public readonly bool Equals(CustomizeArray other)
    {
        fixed (byte* ptr = Data)
        {
            return MemoryUtility.MemCmpUnchecked(ptr, other.Data, Size) == 0;
        }
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
        => obj is CustomizeArray other && Equals(other);

    /// <summary> Compare two Customize arrays in unmanaged memory for equality. </summary>
    public static bool Equals(CustomizeArray* lhs, CustomizeArray* rhs)
        => MemoryUtility.MemCmpUnchecked(lhs, rhs, Size) == 0;

    /// <remarks>
    /// Compare Gender and then only from Height onwards, because all screen actors are set to Height 50,
    /// the Race is implicitly included in the subrace (after height),
    /// and the body type is irrelevant for players.
    /// </remarks>>
    public static bool ScreenActorEquals(CustomizeArray* lhs, CustomizeArray* rhs)
        => lhs->Data[1] == rhs->Data[1] && MemoryUtility.MemCmpUnchecked(lhs->Data + 4, rhs->Data + 4, Size - 4) == 0;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        fixed (byte* ptr = Data)
        {
            var p = (int*)ptr;
            var u = *(ushort*)(p + 6);
            return HashCode.Combine(*p, p[1], p[2], p[3], p[4], p[5], u);
        }
    }

    /// <summary> Write a Customize array as Base64 string. </summary>
    public readonly string WriteBase64()
    {
        fixed (byte* ptr = Data)
        {
            var data = new ReadOnlySpan<byte>(ptr, Size);
            return Convert.ToBase64String(data);
        }
    }

    /// <summary> Write this Customize array byte-wise to string. </summary>
    public override string ToString()
    {
        var sb = new StringBuilder(Size * 3);
        for (var i = 0; i < Size - 1; ++i)
            sb.Append($"{Data[i]:X2} ");
        sb.Append($"{Data[Size - 1]:X2}");
        return sb.ToString();
    }


    /// <summary> Try to load a Base64 string into this Customize array. Returns true on success. </summary>
    [SkipLocalsInit]
    public bool LoadBase64(string base64)
    {
        var buffer = stackalloc byte[Size];
        var span   = new Span<byte>(buffer, Size);
        if (!Convert.TryFromBase64String(base64, span, out var written) || written != Size)
            return false;

        Read(buffer);
        return true;
    }

    /// <summary> Set one value to another according to a mask and return if anything changed. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool SetIfDifferentMasked(ref byte oldValue, CustomizeValue newValue, byte mask)
    {
        var tmp = (byte)(newValue.Value & mask);
        tmp = (byte)(tmp | (oldValue & ~mask));
        if (oldValue == tmp)
            return false;

        oldValue = tmp;
        return true;
    }

    /// <summary> Set one value to another and return if anything changed. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool SetIfDifferent(ref byte oldValue, CustomizeValue newValue)
    {
        if (oldValue == newValue.Value)
            return false;

        oldValue = newValue.Value;
        return true;
    }

    /// <summary> Create a default customize array that can be used when converting non-humans to humans etc. </summary>
    private static CustomizeArray GenerateDefault()
    {
        var ret = new CustomizeArray
        {
            Race   = Race.Hyur,
            Clan   = SubRace.Midlander,
            Gender = Gender.Male,
        };
        ret.Set(CustomizeIndex.BodyType,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.Height,          (CustomizeValue)50);
        ret.Set(CustomizeIndex.Face,            (CustomizeValue)1);
        ret.Set(CustomizeIndex.Hairstyle,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.SkinColor,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.EyeColorRight,   (CustomizeValue)1);
        ret.Set(CustomizeIndex.HighlightsColor, (CustomizeValue)1);
        ret.Set(CustomizeIndex.TattooColor,     (CustomizeValue)1);
        ret.Set(CustomizeIndex.Eyebrows,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.EyeColorLeft,    (CustomizeValue)1);
        ret.Set(CustomizeIndex.EyeShape,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.Nose,            (CustomizeValue)1);
        ret.Set(CustomizeIndex.Jaw,             (CustomizeValue)1);
        ret.Set(CustomizeIndex.Mouth,           (CustomizeValue)1);
        ret.Set(CustomizeIndex.LipColor,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.MuscleMass,      (CustomizeValue)50);
        ret.Set(CustomizeIndex.TailShape,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.BustSize,        (CustomizeValue)50);
        ret.Set(CustomizeIndex.FacePaint,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.FacePaintColor,  (CustomizeValue)1);
        return ret;
    }
}
