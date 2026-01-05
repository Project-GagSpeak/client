using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Structs;

namespace GagSpeak.CustomCombos.Glamourer;

/// <summary> In Theory, only one of these should need to be made, as when drawing you define the label. </summary>
public sealed class GameStainCombo(ILogger log) : CkFilterComboColors(CreateFunc(), log)
{
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var buttonWidth = ImGui.GetContentRegionAvail().X;
        var totalWidth = ImGui.GetContentRegionMax().X;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(buttonWidth / 2 / totalWidth, 0.5f));

        return base.DrawSelectable(globalIdx, selected);
    }

    private static Func<IReadOnlyList<KeyValuePair<byte, (string Name, uint Color, bool Gloss)>>> CreateFunc()
        => () => ItemSvc.Stains.Select(kvp => kvp)
            .Prepend(new KeyValuePair<StainId, Stain>(Stain.None.RowIndex, Stain.None)).Select(kvp
                => new KeyValuePair<byte, (string, uint, bool)>(kvp.Key.Id, (kvp.Value.Name, kvp.Value.RgbaColor, kvp.Value.Gloss))).ToList();
}

