using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files.MaterialStructs;

public sealed class ColorDyeTable : IEnumerable<ColorDyeTableRow>, IColorDyeTable
{
    [InlineArray(NumRows)]
    private struct Table
    {
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "InlineArray")]
        private ColorDyeTableRow _element0;
    }

    public const int NumRows = 32;
    public const int Size    = NumRows * ColorDyeTableRow.Size;

    int IColorDyeTable.RowSize
        => ColorDyeTableRow.Size;

    int IColorDyeTable.Height
        => NumRows;

    int IColorDyeTable.Size
        => Size;

    private Table _rowData;

    public ref ColorDyeTableRow this[int i]
        => ref _rowData[i];

    public IEnumerator<ColorDyeTableRow> GetEnumerator()
    {
        for (var i = 0; i < NumRows; ++i)
            yield return _rowData[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Span<byte> AsBytes()
        => MemoryMarshal.AsBytes(_rowData[..]);

    public Span<byte> RowAsBytes(int i)
        => MemoryMarshal.AsBytes(new Span<ColorDyeTableRow>(ref _rowData[i]));

    public bool SetDefault()
    {
        var ret = false;
        for (var i = 0; i < NumRows; ++i)
            ret |= SetDefaultRow(i);

        return ret;
    }

    public bool SetDefaultRow(int i)
    {
        if (_rowData[i] == default)
            return false;

        _rowData[i] = default;
        return true;
    }

    public ColorDyeTable()
        => SetDefault();

    private ColorDyeTable(ref SpanBinaryReader reader)
    {
        reader.Read<ColorDyeTableRow>(NumRows).CopyTo(_rowData);
    }

    public ColorDyeTable(IColorDyeTable other)
    {
        switch (other)
        {
            case LegacyColorDyeTable oldTable:
            {
                for (var i = 0; i < LegacyColorDyeTable.NumRows; ++i)
                    _rowData[i] = new ColorDyeTableRow(oldTable[i]);
                for (var i = LegacyColorDyeTable.NumRows; i < NumRows; ++i)
                    SetDefaultRow(i);
                break;
            }
            case ColorDyeTable table:
            {
                for (var i = 0; i < NumRows; ++i)
                    _rowData[i] = table[i];
                break;
            }
            default:
                SetDefault();
                break;
        }
    }

    public static ColorDyeTable CastOrConvert(IColorDyeTable other)
        => other as ColorDyeTable ?? new ColorDyeTable(other);

    /// <summary>
    /// Attempts to read a color dye table from the given reader.
    /// If the reader doesn't hold enough data, nothing will be read, and this will return a default table.
    /// </summary>
    public static ColorDyeTable TryReadFrom(ref SpanBinaryReader reader)
        => reader.Remaining >= Size ? new ColorDyeTable(ref reader) : new ColorDyeTable();
}
