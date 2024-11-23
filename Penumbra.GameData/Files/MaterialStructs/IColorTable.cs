namespace Penumbra.GameData.Files.MaterialStructs;

public interface IColorTable
{
    /// <summary> The width of this table, in vectors of 4 <see cref="Half"/> per row. </summary>
    int Width { get; }

    /// <summary> The size of one row of this table, in bytes. </summary>
    int RowSize { get; }

    /// <summary> The height of this table, in rows. </summary>
    int Height { get; }

    /// <summary> The size of this table, in bytes. </summary>
    int Size { get; }

    /// <summary>
    /// Binary logarithms of the dimensions of this table, for <see cref="TableFlags.TableDimensionLogs"/>.
    /// Low nibble is the log of <see cref="Width"/>, high nibble is the log of <see cref="Height"/>.
    /// Must be zero (instead of 0x42) for legacy tables.
    /// </summary>
    byte DimensionLogs { get; }

    /// <summary> Gets the contents of this table, as bytes. </summary>
    Span<byte> AsBytes();

    /// <summary> Gets the contents of this table, as <see cref="Half"/>. </summary>
    Span<Half> AsHalves();

    /// <summary> Gets the contents of a row, as bytes. </summary>
    Span<byte> RowAsBytes(int i);

    /// <summary> Gets the contents of a row, as <see cref="Half"/>. </summary>
    Span<Half> RowAsHalves(int i);

    /// <summary> Resets this table to default values. </summary>
    /// <returns> Whether something actually changed. </returns>
    bool SetDefault();

    /// <summary> Resets a row to default values. </summary>
    /// <returns> Whether something actually changed. </returns>
    bool SetDefaultRow(int i);
}
