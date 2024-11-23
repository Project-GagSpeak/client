using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A list to efficiently identify character models. </summary>
public sealed class IdentificationListModels(IDalamudPluginInterface pi, IDataManager gameData, Logger log)
    : KeyList<ModelChara>(pi, log, "ModelIdentification", gameData.Language, 8, () => CreateModelList(gameData), ToKey, ValidKey, ValueKeySelector)
{

    /// <summary> Find all models affected by the given set of input data. </summary>
    /// <param name="type"> The base type of the model. </param>
    /// <param name="modelId"> The primary ID of the model. </param>
    /// <param name="modelBase"> The base parameter of the model. If 0, check all bases. </param>
    /// <param name="variant"> The variant. If 0, check all variants. </param>
    /// <returns> A list of all affected ModelChara. </returns>
    public IEnumerable<ModelChara> Between(CharacterBase.ModelType type, PrimaryId modelId, byte modelBase = 0, Variant variant = default)
    {
        if (modelBase == 0)
            return Between(ToKey(type, modelId, 0, 0), ToKey(type, modelId, 0xFF, 0xFF));
        if (variant == 0)
            return Between(ToKey(type, modelId, modelBase, 0), ToKey(type, modelId, modelBase, 0xFF));

        return Between(ToKey(type, modelId, modelBase, variant), ToKey(type, modelId, modelBase, variant));
    }

    /// <summary> Convert the input data to a key. </summary>
    public static ulong ToKey(CharacterBase.ModelType type, PrimaryId model, byte modelBase, Variant variant)
        => ((ulong)type << 32) | ((ulong)model.Id << 16) | ((ulong)modelBase << 8) | variant.Id;

    /// <summary> Convert a ModelChara to a key. </summary>
    private static ulong ToKey(ModelChara row)
        => ToKey((CharacterBase.ModelType)row.Type, row.Model, row.Base, row.Variant);

    /// <summary> All non-zero keys are valid. </summary>
    private static bool ValidKey(ulong key)
        => key != 0;

    /// <summary> Sort by ModelChara RowId after keys. </summary>
    private static int ValueKeySelector(ModelChara data)
        => (int)data.RowId;

    /// <summary> Just take all ModelChara. </summary>
    private static IEnumerable<ModelChara> CreateModelList(IDataManager gameData)
        => gameData.GetExcelSheet<ModelChara>(gameData.Language)!;

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => 24 + Value.Count * 40;
}
