using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files.MaterialStructs;

/// <summary>
/// A color table of yet unknown structure.
/// It will be read and written as-is, for round-trip I/O, but can only be further manipulated manually.
/// </summary>
internal sealed class OpaqueColorTable : IColorTable
{
    public readonly byte   DimensionLogs;
    public readonly Half[] Data;

    public int Width
        => 1 << (DimensionLogs & 0xF);

    public int RowSize
        => 8 * Width;

    public int Height
        => 1 << (DimensionLogs >> 4);

    public int Size
        => 2 * Data.Length;

    byte IColorTable.DimensionLogs => DimensionLogs;

    public OpaqueColorTable(byte dimensionLogs)
    {
        DimensionLogs = dimensionLogs;
        Data          = new Half[4 * Width * Height];
    }

    public OpaqueColorTable(IColorTable other)
    {
        DimensionLogs = other.DimensionLogs;
        Data          = new Half[4 * Width * Height];
        other.AsHalves().CopyTo(Data);
    }

    public Span<byte> AsBytes()
        => MemoryMarshal.AsBytes(Data.AsSpan());

    public Span<Half> AsHalves()
        => Data;

    public Span<byte> RowAsBytes(int i)
        => MemoryMarshal.AsBytes(RowAsHalves(i));

    public Span<Half> RowAsHalves(int i)
        => Data.AsSpan(i).Slice(Width * i, Width);

    public bool SetDefault()
        => throw new NotSupportedException();

    public bool SetDefaultRow(int i)
        => throw new NotSupportedException();

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Height; ++i)
        {
            var row = RowAsHalves(i);
            for (var j = 0; j < Width; ++j)
                sb.Append($"{row[j]:F1} ");
            sb[^1] = '\n';
        }

        return sb.ToString();
    }

    /// <summary>
    /// Attempts to read a color table of the given dimensions from the given reader.
    /// If the reader doesn't hold enough data, nothing will be read, and this will return an all-zero table.
    /// </summary>
    public static OpaqueColorTable TryReadFrom(byte dimensionLogs, ref SpanBinaryReader reader)
    {
        var table = new OpaqueColorTable(dimensionLogs);
        if (reader.Remaining >= table.Size)
            reader.Read<Half>(table.Data.Length).CopyTo(table.Data);

        return table;
    }
}
