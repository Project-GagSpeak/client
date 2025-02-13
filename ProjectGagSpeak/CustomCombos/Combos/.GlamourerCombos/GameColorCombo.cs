using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui.Widgets;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;
public sealed class StainColorCombo(float _comboWidth, DictStain _stains, ILogger stainLog)
    : CustomFilterComboColors(_comboWidth, MouseWheelType.Unmodified, CreateFunc(_stains), stainLog)
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

