using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary that matches action keys to their identities. </summary>
public sealed class DictAction(IDalamudPluginInterface pluginInterface, Logger log, IDataManager data)
    : DictLuminaName<Lumina.Excel.Sheets.Action>(pluginInterface, log, "Actions", data.Language, 8, () => CreateActionList(data))
{
    /// <remarks>This is too much effort to do accurately.</remarks>>
    protected override int TypeSize
        => 128;

    /// <summary> Create the list. </summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<Lumina.Excel.Sheets.Action>> CreateActionList(IDataManager gameData)
    {
        var sheet   = gameData.GetExcelSheet<Lumina.Excel.Sheets.Action>(gameData.Language)!;
        var storage = new ConcurrentDictionary<string, ConcurrentBag<Lumina.Excel.Sheets.Action>>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
        };

        // Iterate through all actions and add start, end and hit keys.
        Parallel.ForEach(sheet.Where(a => !a.Name.IsEmpty), options, action =>
        {
            var startKey = action.AnimationStart.ValueNullable?.Name.ValueNullable?.Key.ToString();
            var endKey   = action.AnimationEnd.ValueNullable?.Key.ToString();
            var hitKey   = action.ActionTimelineHit.ValueNullable?.Key.ToString();
            AddAction(startKey, action);
            AddAction(endKey,   action);
            AddAction(hitKey,   action);
        });

        return storage.ToFrozenDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<Lumina.Excel.Sheets.Action>)kvp.Value.DistinctBy(a => a.RowId).ToArray());

        void AddAction(string? key, Lumina.Excel.Sheets.Action action)
        {
            if (key.IsNullOrEmpty())
                return;

            key = key.ToLowerInvariant();
            if (storage.TryGetValue(key, out var actions))
                actions.Add(action);
            else
                storage[key] = [action];
        }
    }
}
