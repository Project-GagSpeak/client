using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary that maps WorldIds to their names. </summary>
public sealed class DictWorld(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : DataSharer<IReadOnlyDictionary<ushort, string>>(pluginInterface, log, "Worlds", gameData.Language, 8, () => CreateWorldData(gameData)),
        IReadOnlyDictionary<WorldId, string>
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<ushort, string> CreateWorldData(IDataManager gameData)
    {
        var sheet = gameData.GetExcelSheet<World>()!;
        var dict  = new Dictionary<ushort, string>((int)sheet.Count);
        foreach (var w in sheet.Where(IsValid))
            dict.TryAdd((ushort)w.RowId, string.Intern(w.Name.ExtractText()));
        return dict.ToFrozenDictionary();
    }

    private static bool IsValid(World world)
    {
        if (world.Name.IsEmpty)
            return false;

        if (world.DataCenter.RowId is 0)
            return false;

        if (world.IsPublic)
            return true;

        return char.IsUpper((char)world.Name.Data.Span[0]);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<WorldId, string>> GetEnumerator()
        => Value.Select(kvp => new KeyValuePair<WorldId, string>(new WorldId(kvp.Key), kvp.Value)).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(WorldId key)
        => Value.ContainsKey(key.Id);

    /// <inheritdoc/>
    public bool TryGetValue(WorldId key, [NotNullWhen(true)] out string? value)
        => Value.TryGetValue(key.Id, out value);

    /// <inheritdoc/>
    public string this[WorldId key]
        => Value[key.Id];

    /// <inheritdoc/>
    public IEnumerable<WorldId> Keys
        => Value.Keys.Select(k => new WorldId(k));

    /// <inheritdoc/>
    public IEnumerable<string> Values
        => Value.Values;

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(16, Count) + Values.Sum(v => v.Length * 2);

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Count;
}
