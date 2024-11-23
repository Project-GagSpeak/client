using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary that maps StainIds to Stains. </summary>
public sealed class DictStain(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : DataSharer<IReadOnlyDictionary<byte, (string Name, uint Dye, bool Gloss)>>(pluginInterface, log, "Stains", gameData.Language, 4,
        () => CreateStainData(gameData)), IReadOnlyDictionary<StainId, Stain>
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<byte, (string Name, uint Dye, bool Gloss)> CreateStainData(IDataManager dataManager)
    {
        var stainSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Stain>(dataManager.Language);
        return stainSheet.Where(s => s.Color != 0 && s.Name.ByteLength > 0)
            .ToFrozenDictionary(s => (byte)s.RowId, s =>
            {
                var stain = new Stain(s);
                return (stain.Name, stain.RgbaColor, stain.Gloss);
            });
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<StainId, Stain>> GetEnumerator()
        => Value.Select(kvp
                => new KeyValuePair<StainId, Stain>(new StainId(kvp.Key), new Stain(kvp.Value.Name, kvp.Value.Dye, kvp.Key, kvp.Value.Gloss)))
            .GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(StainId key)
        => Value.ContainsKey(key.Id);

    /// <inheritdoc/>
    public bool TryGetValue(StainId key, out Stain value)
    {
        if (!Value.TryGetValue(key.Id, out var data))
        {
            value = default;
            return false;
        }

        value = new Stain(data.Name, data.Dye, key.Id, data.Gloss);
        return true;
    }

    /// <inheritdoc/>
    public Stain this[StainId key]
        => TryGetValue(key, out var data) ? data : throw new ArgumentOutOfRangeException(nameof(key));

    /// <inheritdoc/>
    public IEnumerable<StainId> Keys
        => Value.Keys.Select(k => new StainId(k));

    /// <inheritdoc/>
    public IEnumerable<Stain> Values
        => Value.Select(kvp => new Stain(kvp.Value.Name, kvp.Value.Dye, kvp.Key, kvp.Value.Gloss));

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(24, Count);

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Count;
}
