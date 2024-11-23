using Penumbra.GameData.Files.StainMapStructs;
using Penumbra.GameData.Files.Utility;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files.MaterialStructs;

public sealed class ColorTable : IEnumerable<ColorTableRow>, IColorTable
{
    [InlineArray(NumRows)]
    public struct Table
    {
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "InlineArray")]
        private ColorTableRow _element0;
    }

    public const int NumRows = 32;
    public const int Size    = NumRows * ColorTableRow.Size;

    public const byte DimensionLogs = 0x53;

    int IColorTable.Width
        => ColorTableRow.NumVec4;

    int IColorTable.RowSize
        => ColorTableRow.Size;

    int IColorTable.Height
        => NumRows;

    int IColorTable.Size
        => Size;

    byte IColorTable.DimensionLogs
        => DimensionLogs;

    private Table _rowData;

    public ref ColorTableRow this[int i]
        => ref _rowData[i];

    public IEnumerator<ColorTableRow> GetEnumerator()
    {
        for (var i = 0; i < NumRows; ++i)
            yield return _rowData[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Span<byte> AsBytes()
        => MemoryMarshal.AsBytes(_rowData[..]);

    public Span<Half> AsHalves()
        => MemoryMarshal.Cast<ColorTableRow, Half>(_rowData);

    public Span<byte> RowAsBytes(int i)
        => MemoryMarshal.AsBytes(_rowData[i][..]);

    public Span<Half> RowAsHalves(int i)
        => _rowData[i];

    public Span<ColorTableRow> AsRows()
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
        if (_rowData[i] == ColorTableRow.Default)
            return false;

        _rowData[i] = ColorTableRow.Default;
        return true;
    }

    public bool ApplyDye(StmFile<DyePack> stm, ReadOnlySpan<StainId> stainIds, ColorDyeTable dyeTable)
    {
        if (stainIds.Length == 0)
            return false;

        var ret = false;
        for (var rowIdx = 0; rowIdx < ColorDyeTable.NumRows; ++rowIdx)
        {
            var dyeRow  = dyeTable[rowIdx];
            var stainId = dyeRow.Channel < stainIds.Length ? stainIds[dyeRow.Channel] : 0;
            if (stainId != 0 && stm.TryGetValue(dyeRow.Template, stainId, out var dyes))
                ret |= _rowData[rowIdx].ApplyDye(dyeRow, dyes);
        }

        return ret;
    }

    public bool ApplyDye(StmFile<LegacyDyePack> stm, ReadOnlySpan<StainId> stainIds, ColorDyeTable dyeTable)
    {
        if (stainIds.Length == 0)
            return false;

        var ret = false;
        for (var rowIdx = 0; rowIdx < ColorDyeTable.NumRows; ++rowIdx)
        {
            var dyeRow  = dyeTable[rowIdx];
            var stainId = dyeRow.Channel < stainIds.Length ? stainIds[dyeRow.Channel] : 0;
            if (stainId != 0 && stm.TryGetValue(dyeRow.Template, stainId, out var dyes))
                ret |= _rowData[rowIdx].ApplyDye(dyeRow, dyes);
        }

        return ret;
    }

    public bool ApplyDyeToRow(StmFile<DyePack> stm, ReadOnlySpan<StainId> stainIds, int rowIdx, ColorDyeTableRow dyeRow)
    {
        if (rowIdx < 0 || rowIdx >= NumRows || stainIds.Length == 0)
            return false;

        var ret     = false;
        var stainId = dyeRow.Channel < stainIds.Length ? stainIds[dyeRow.Channel] : 0;
        if (stainId != 0 && stm.TryGetValue(dyeRow.Template, stainId, out var dyes))
            ret |= _rowData[rowIdx].ApplyDye(dyeRow, dyes);

        return ret;
    }

    public bool ApplyDyeToRow(StmFile<LegacyDyePack> stm, ReadOnlySpan<StainId> stainIds, int rowIdx, ColorDyeTableRow dyeRow)
    {
        if (rowIdx < 0 || rowIdx >= NumRows || stainIds.Length == 0)
            return false;

        var ret     = false;
        var stainId = dyeRow.Channel < stainIds.Length ? stainIds[dyeRow.Channel] : 0;
        if (stainId != 0 && stm.TryGetValue(dyeRow.Template, stainId, out var dyes))
            ret |= _rowData[rowIdx].ApplyDye(dyeRow, dyes);

        return ret;
    }

    public ColorTable()
        => SetDefault();

    private ColorTable(ref SpanBinaryReader reader)
        => _rowData = reader.Read<Table>();

    public ColorTable(IColorTable other)
    {
        switch (other)
        {
            case LegacyColorTable oldTable:
            {
                for (var i = 0; i < LegacyColorTable.NumRows; ++i)
                    _rowData[i] = new ColorTableRow(oldTable[i]);
                for (var i = LegacyColorTable.NumRows; i < NumRows; ++i)
                    _rowData[i] = ColorTableRow.LegacyDefault;
                break;
            }
            case ColorTable table:
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

    public static ColorTable CastOrConvert(IColorTable other)
        => other as ColorTable ?? new ColorTable(other);

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < NumRows; ++i)
        {
            var row = this[i];
            for (var j = 0; j < ColorTableRow.NumVec4 * ColorTableRow.Halves; ++j)
                sb.Append($"{row[j]:F1} ");
            sb[^1] = '\n';
        }

        return sb.ToString();
    }

    /// <summary>
    /// Attempts to read a legacy color table from the given reader.
    /// If the reader doesn't hold enough data, nothing will be read, and this will return a default table.
    /// </summary>
    public static ColorTable TryReadFrom(ref SpanBinaryReader reader)
        => reader.Remaining >= Size ? new ColorTable(ref reader) : new ColorTable();
}
