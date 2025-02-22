using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;

namespace GagSpeak.CustomCombos.Glamourer;
public sealed class GameStainCombo(float _comboWidth, DictStain _stains, ILogger stainLog)
    : CkFilterComboColors(_comboWidth, CreateFunc(_stains), stainLog)
{
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var buttonWidth = ImGui.GetContentRegionAvail().X;
        var totalWidth = ImGui.GetContentRegionMax().X;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(buttonWidth / 2 / totalWidth, 0.5f));

        return base.DrawSelectable(globalIdx, selected);
    }

    private static Func<IReadOnlyList<KeyValuePair<byte, (string Name, uint Color, bool Gloss)>>> CreateFunc(DictStain stains)
        => () => stains.Select(kvp => kvp)
            .Prepend(new KeyValuePair<StainId, Stain>(Stain.None.RowIndex, Stain.None)).Select(kvp
                => new KeyValuePair<byte, (string, uint, bool)>(kvp.Key.Id, (kvp.Value.Name, kvp.Value.RgbaColor, kvp.Value.Gloss))).ToList();
}

