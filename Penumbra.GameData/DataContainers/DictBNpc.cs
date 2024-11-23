using System.Collections.Frozen;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A dictionary that matches BNpcNameId to names. </summary>
public sealed class DictBNpc(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : NameDictionary(pluginInterface, log, gameData, "BNpcs", 7, () => CreateBNpcData(gameData))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<uint, string> CreateBNpcData(IDataManager gameData)
    {
        var sheet = gameData.GetExcelSheet<BNpcName>(gameData.Language)!;
        var dict  = new Dictionary<uint, string>((int) sheet.Count);
        foreach (var n in sheet.Where(n => n.Singular.ByteLength > 0))
            dict.TryAdd(n.RowId, DataUtility.ToTitleCaseExtended(n.Singular, n.Article));
        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="NameDictionary.ContainsKey"/>
    public bool ContainsKey(BNpcNameId key)
        => Value.ContainsKey(key.Id);

    /// <inheritdoc cref="NameDictionary.TryGetValue"/>
    public bool TryGetValue(BNpcNameId key, [NotNullWhen(true)] out string? value)
        => Value.TryGetValue(key.Id, out value);

    /// <inheritdoc cref="NameDictionary.this"/>
    public string this[BNpcNameId key]
        => Value[key.Id];
}
