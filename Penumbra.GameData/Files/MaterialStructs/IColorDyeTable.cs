namespace Penumbra.GameData.Files.MaterialStructs;

public interface IColorDyeTable
{
    /// <summary> The size of one row of this table, in bytes. </summary>
    int RowSize { get; }

    /// <summary> The height of this table, in rows. </summary>
    int Height { get; }

    /// <summary> The size of this table, in bytes. </summary>
    int Size { get; }

    /// <summary> Gets the contents of this table, as bytes. </summary>
    Span<byte> AsBytes();

    /// <summary> Gets the contents of a row, as <see cref="Half"/>. </summary>
    Span<byte> RowAsBytes(int i);

    /// <summary> Resets this table to default values. </summary>
    /// <returns> Whether something actually changed. </returns>
    bool SetDefault();

    /// <summary> Resets a row to default values. </summary>
    /// <returns> Whether something actually changed. </returns>
    bool SetDefaultRow(int i);
}
