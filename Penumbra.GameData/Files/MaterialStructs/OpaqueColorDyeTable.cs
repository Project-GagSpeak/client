using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files.MaterialStructs;

/// <summary>
/// A color dye table of yet unknown structure.
/// It will be read and written as-is, for round-trip I/O, but can only be further manipulated manually.
/// </summary>
internal sealed class OpaqueColorDyeTable : IColorDyeTable
{
    public readonly byte   HeightLog;
    public readonly byte[] Data;

    public unsafe int RowSize
        => Data.Length / Height;

    public int Height
        => 1 << HeightLog;

    public unsafe int Size
        => Data.Length;

    public OpaqueColorDyeTable(byte heightLog, int size)
    {
        HeightLog = heightLog;
        Data      = new byte[size];
    }

    public unsafe OpaqueColorDyeTable(IColorDyeTable other)
    {
        HeightLog = (byte)BitOperations.Log2((uint)other.Height);
        if (other.Height != Height)
            throw new ArgumentException($"The source color dye table must have a power of 2 of rows.");
        Data = new byte[other.Size];
        other.AsBytes().CopyTo(AsBytes());
    }

    public Span<byte> AsBytes()
        => MemoryMarshal.AsBytes(Data.AsSpan());

    public Span<byte> RowAsBytes(int i)
        => AsBytes()[(i * RowSize)..((i + 1) * RowSize)];

    public bool SetDefault()
        => throw new NotSupportedException();

    public bool SetDefaultRow(int i)
        => throw new NotSupportedException();

    /// <summary>
    /// Attempts to read a color dye table from the given reader.
    /// </summary>
    public static OpaqueColorDyeTable TryReadFrom(byte heightLog, ref SpanBinaryReader reader)
    {
        var table = new OpaqueColorDyeTable(heightLog, reader.Remaining);
        reader.Read<byte>(table.Data.Length).CopyTo(table.Data);

        return table;
    }
}
