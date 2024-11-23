using ImGuiNET;
using OtterGui.Text;
using Penumbra.GameData.Enums;

namespace Penumbra.GameData.Gui;

/// <summary> A simple combo wrapper to select a bonus item slot. </summary>
public static class BonusSlotCombo
{
    /// <summary> Draw a combo selecting among actual bonus item slots. </summary>
    /// <param name="label"> The label for the combo. </param>
    /// <param name="tooltip"> A hover tooltip for the combo. </param>
    /// <param name="slot"> The current and returned slot. </param>
    /// /// <param name="width"> The width of the selection preview, if 0, it is sized to fit the longest value. </param>
    /// <returns> True on change, false otherwise. </returns>
    public static bool Draw(string label, string tooltip, ref BonusItemFlag slot, float width = 0)
    {
        if (width == 0)
            width = ImUtf8.CalcTextSize(BonusItemFlag.Glasses.ToName()).X + ImGui.GetFrameHeightWithSpacing();
        ImGui.SetNextItemWidth(width);
        using var combo = ImUtf8.Combo(label, slot.ToName());
        var       ret   = false;
        if (combo)
            foreach (var tmpSlot in BonusExtensions.AllFlags)
            {
                if (ImUtf8.Selectable(tmpSlot.ToName(), tmpSlot == slot) && slot != tmpSlot)
                {
                    slot = tmpSlot;
                    ret  = true;
                }
            }

        ImUtf8.HoverTooltip(tooltip);
        return ret;
    }
}
