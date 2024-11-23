using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A set of data about which ModelCharaIds represent human models. </summary>
public sealed class HumanModelList(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : DataSharer<Tuple<BitArray, int>>(pluginInterface, log, "HumanModels", gameData.Language, 4, () => GetValidHumanModels(gameData))
{
    /// <summary> Whether the given ID represents a human model. </summary>
    public bool IsHuman(ModelCharaId modelId)
        => modelId.Id < Count && Value.Item1[(int)modelId.Id];

    /// <summary> The number of ModelCharaIds in total. </summary>
    public int Count
        => Value.Item1.Count;

    /// <summary> The number of human ModelCharaIds. </summary>
    public int HumanCount
        => Value.Item2;

    /// <summary> Go through all ModelChara rows and return a bitfield of those that resolve to human models. </summary>
    private static Tuple<BitArray, int> GetValidHumanModels(IDataManager gameData)
    {
        var sheet = gameData.GetExcelSheet<ModelChara>()!;
        var ret   = new BitArray((int)sheet.Count, false);
        var count = 0;
        foreach (var (_, idx) in sheet.Select((m, i) => (m, i)).Where(p => p.m.Type == (byte)CharacterBase.ModelType.Human))
        {
            ret[idx] = true;
            ++count;
        }

        return new Tuple<BitArray, int>(ret, count);
    }

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => Value.Item1.Length / 8 + 16;

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Count;
}
