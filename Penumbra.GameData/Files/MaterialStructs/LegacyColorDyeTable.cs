using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files.MaterialStructs;

public sealed class LegacyColorDyeTable : IEnumerable<LegacyColorDyeTableRow>, IColorDyeTable
{
    [InlineArray(NumRows)]
    private struct Table
    {
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "InlineArray")]
        private LegacyColorDyeTableRow _element0;
    }

    public const int NumRows = 16;
    public const int Size    = NumRows * LegacyColorDyeTableRow.Size;

    int IColorDyeTable.RowSize => LegacyColorDyeTableRow.Size;
    int IColorDyeTable.Height  => NumRows;
    int IColorDyeTable.Size    => Size;

    private Table _rowData;

    public ref LegacyColorDyeTableRow this[int i]
        => ref _rowData[i];

    public IEnumerator<LegacyColorDyeTableRow> GetEnumerator()
    {
        for (var i = 0; i < NumRows; ++i)
            yield return _rowData[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Span<byte> AsBytes()
        => MemoryMarshal.AsBytes(_rowData[..]);

    public Span<byte> RowAsBytes(int i)
        => MemoryMarshal.AsBytes(new Span<LegacyColorDyeTableRow>(ref _rowData[i]));

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

    public LegacyColorDyeTable()
        => SetDefault();

    private LegacyColorDyeTable(ref SpanBinaryReader reader)
        => reader.Read<LegacyColorDyeTableRow>(NumRows).CopyTo(_rowData);

    public LegacyColorDyeTable(IColorDyeTable other)
    {
        switch (other)
        {
            case ColorDyeTable newTable:
            {
                for (var i = 0; i < NumRows; ++i)
                    _rowData[i] = new LegacyColorDyeTableRow(newTable[i]);
                break;
            }
            case LegacyColorDyeTable table:
            {
                for (var i = 0; i < NumRows; ++i)
                    _rowData[i] = table[i];
                break;
            }
            default: SetDefault();
                break;
        }
    }

    /// <summary>
    /// Attempts to read a legacy color dye table from the given reader.
    /// If the reader doesn't hold enough data, nothing will be read, and this will return a default table.
    /// </summary>
    public static LegacyColorDyeTable TryReadFrom(ref SpanBinaryReader reader)
        => reader.Remaining >= Size ? new LegacyColorDyeTable(ref reader) : new LegacyColorDyeTable();
}
