using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary mapping ModelCharaIds to lists of NPCs using that ModelChara. </summary>
/// <remarks> This requires <paramref name="nameDicts"/> and <paramref name="bNpcNames"/> to have finished. </remarks>
public sealed class DictModelChara(
    IDalamudPluginInterface pluginInterface,
    Logger log,
    IDataManager gameData,
    DictBNpcNames bNpcNames,
    NameDicts nameDicts)
    : DataSharer<IReadOnlyList<IReadOnlyList<(string Name, ObjectKind Kind, uint Id)>>>(pluginInterface, log, "ModelObjectDict",
            ClientLanguage.English, 3,
            () => CreateModelObjects(gameData, bNpcNames, nameDicts),
            System.Threading.Tasks.Task.WhenAll(nameDicts.Awaiter, bNpcNames.Awaiter)),
        IReadOnlyDictionary<ModelCharaId, IReadOnlyList<(string Name, ObjectKind Kind, uint Id)>>
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyList<IReadOnlyList<(string Name, ObjectKind Kind, uint Id)>> CreateModelObjects(IDataManager gameData,
        DictBNpcNames bNpcNames, NameDicts nameDicts)
    {
        var modelSheet = gameData.GetExcelSheet<ModelChara>(gameData.Language)!;
        var bag        = new ConcurrentBag<(int ModelID, string Name, ObjectKind Kind, uint Id)>();
        var ret = Enumerable
            .Repeat((IReadOnlyList<(string Name, ObjectKind Kind, uint Id)>)Array.Empty<(string Name, ObjectKind Kind, uint Id)>(),
                (int)modelSheet.Count).ToArray();

        // Add Ornaments.
        var oTask = System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var ornament in gameData.GetExcelSheet<Ornament>(gameData.Language)!)
                AddChara(ornament.Model, ObjectKind.Ornament, ornament.RowId, ornament.RowId);
        });

        // Add Mounts.
        var mTask = System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var mount in gameData.GetExcelSheet<Mount>(gameData.Language)!)
                AddChara((int)mount.ModelChara.RowId, ObjectKind.MountType, mount.RowId, mount.RowId);
        });

        // Add Companions.
        var cTask = System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var companion in gameData.GetExcelSheet<Companion>(gameData.Language)!)
                AddChara((int)companion.Model.RowId, ObjectKind.Companion, companion.RowId, companion.RowId);
        });

        // Add EventNPCs.
        var eTask = System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var eNpc in gameData.GetExcelSheet<ENpcBase>(gameData.Language)!)
                AddChara((int)eNpc.ModelChara.RowId, ObjectKind.EventNpc, eNpc.RowId, eNpc.RowId);
        });

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2),
        };

        // Add all Battle NPCs by the reverse name resolving.
        Parallel.ForEach(gameData.GetExcelSheet<BNpcBase>(gameData.Language)!.Where(b => b.RowId < bNpcNames.Count), options, bNpc =>
        {
            foreach (var name in bNpcNames[bNpc.RowId])
                AddChara((int)bNpc.ModelChara.RowId, ObjectKind.BattleNpc, name.Id, bNpc.RowId);
        });

        System.Threading.Tasks.Task.WaitAll(oTask, mTask, cTask, eTask);

        // Group the gathered data up.
        foreach (var group in bag.GroupBy(t => t.ModelID))
            ret[group.Key] = group.Select(t => (t.Name, t.Kind, t.Id)).ToArray();

        return ret;

        // Add a modelChara if a name can be fetched.
        void AddChara(int modelChara, ObjectKind kind, uint dataId, uint displayId)
        {
            if (modelChara >= modelSheet.Count)
                return;

            if (nameDicts.TryGetName(kind, dataId, out var name))
                bag.Add((modelChara, name, kind, displayId));
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<ModelCharaId, IReadOnlyList<(string Name, ObjectKind Kind, uint Id)>>> GetEnumerator()
        => Value.Select((list, idx) => new KeyValuePair<ModelCharaId, IReadOnlyList<(string Name, ObjectKind Kind, uint Id)>>((uint)idx, list))
            .GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(ModelCharaId key)
        => key.Id < Count;

    /// <inheritdoc/>
    public bool TryGetValue(ModelCharaId key, out IReadOnlyList<(string Name, ObjectKind Kind, uint Id)> value)
    {
        if (key.Id >= Count)
        {
            value = Array.Empty<(string Name, ObjectKind Kind, uint Id)>();
            return false;
        }

        value = Value[(int)key.Id];
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<(string Name, ObjectKind Kind, uint Id)> this[ModelCharaId key]
        => TryGetValue(key, out var value) ? value : Array.Empty<(string Name, ObjectKind Kind, uint Id)>();

    /// <inheritdoc/>
    public IEnumerable<ModelCharaId> Keys
        => Enumerable.Range(0, Value.Count).Select(v => new ModelCharaId((uint)v));

    /// <inheritdoc/>
    public IEnumerable<IReadOnlyList<(string Name, ObjectKind Kind, uint Id)>> Values
        => Value;

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => 16 * (Count + 1) + TotalCount * 4;

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Value.Sum(l => l.Count);
}
