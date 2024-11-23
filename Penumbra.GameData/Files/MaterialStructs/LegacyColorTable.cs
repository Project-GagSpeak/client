using Penumbra.GameData.Files.StainMapStructs;
using Penumbra.GameData.Files.Utility;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files.MaterialStructs;

public sealed class LegacyColorTable : IEnumerable<LegacyColorTableRow>, IColorTable
{
    [InlineArray(NumRows)]
    private struct Table
    {
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "InlineArray")]
        private LegacyColorTableRow _element0;
    }

    public const int NumRows = 16;
    public const int Size    = NumRows * LegacyColorTableRow.Size;

    int IColorTable.Width   => LegacyColorTableRow.NumVec4;
    int IColorTable.RowSize => LegacyColorTableRow.Size;
    int IColorTable.Height  => NumRows;
    int IColorTable.Size    => Size;
    byte IColorTable.DimensionLogs => 0;

    private Table _rowData;

    public ref LegacyColorTableRow this[int i]
        => ref _rowData[i];

    public IEnumerator<LegacyColorTableRow> GetEnumerator()
    {
        for (var i = 0; i < NumRows; ++i)
            yield return _rowData[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Span<byte> AsBytes()
        => MemoryMarshal.AsBytes(_rowData[..]);

    public Span<Half> AsHalves()
        => MemoryMarshal.Cast<LegacyColorTableRow, Half>(_rowData);

    public Span<byte> RowAsBytes(int i)
        => MemoryMarshal.AsBytes(_rowData[i][..]);

    public Span<Half> RowAsHalves(int i)
        => _rowData[i];

    public Span<LegacyColorTableRow> AsRows()
        => _rowData;

    public bool SetDefault()
    {
        var ret = false;
        for (var i = 0; i < NumRows; ++i)
            ret |= SetDefaultRow(i);

        return ret;
    }

    public bool SetDefaultRow(int i)
    {
        if (_rowData[i] == LegacyColorTableRow.Default)
            return false;

        _rowData[i] = LegacyColorTableRow.Default;
        return true;
    }

    public bool ApplyDye(StmFile<LegacyDyePack> stm, ReadOnlySpan<StainId> stainIds, LegacyColorDyeTable dyeTable)
    {
        if (stainIds.Length == 0)
            return false;

        var ret = false;
        for (var rowIdx = 0; rowIdx < LegacyColorDyeTable.NumRows; ++rowIdx)
        {
            var dyeRow = dyeTable[rowIdx];
            if (stainIds[0] != 0 && stm.TryGetValue(dyeRow.Template, stainIds[0], out var dyes))
                ret |= _rowData[rowIdx].ApplyDye(dyeRow, dyes);
        }

        return ret;
    }

    public bool ApplyDyeToRow(StmFile<LegacyDyePack> stm, ReadOnlySpan<StainId> stainIds, int rowIdx, LegacyColorDyeTableRow dyeRow)
    {
        if (rowIdx < 0 || rowIdx >= NumRows || stainIds.Length == 0)
            return false;

        var ret = false;
        if (stainIds[0] != 0 && stm.TryGetValue(dyeRow.Template, stainIds[0], out var dyes))
            ret |= _rowData[rowIdx].ApplyDye(dyeRow, dyes);

        return ret;
    }

    public LegacyColorTable()
    {
        SetDefault();
    }

    private LegacyColorTable(ref SpanBinaryReader reader)
    {
        _rowData = reader.Read<Table>();
    }

    public LegacyColorTable(ColorTable newTable)
    {
        for (var i = 0; i < NumRows; ++i)
            this[i] = new LegacyColorTableRow(newTable[i]);
    }

    public LegacyColorTable(IColorTable other)
    {
        if (other is ColorTable newTable)
        {
            for (var i = 0; i < NumRows; ++i)
                _rowData[i] = new LegacyColorTableRow(newTable[i]);
        }
        else if (other is LegacyColorTable table)
        {
            for (var i = 0; i < NumRows; ++i)
                _rowData[i] = table[i];
        }
        else
            SetDefault();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < NumRows; ++i)
        {
            var row = this[i];
            for (var j = 0; j < LegacyColorTableRow.NumVec4 * LegacyColorTableRow.Halves; ++j)
                sb.Append($"{row[j]:F1} ");
            sb[^1] = '\n';
        }

        return sb.ToString();
    }

    /// <summary>
    /// Attempts to read a legacy color table from the given reader.
    /// If the reader doesn't hold enough data, nothing will be read, and this will return a default table.
    /// </summary>
    public static LegacyColorTable TryReadFrom(ref SpanBinaryReader reader)
        => reader.Remaining >= Size ? new(ref reader) : new();
}
