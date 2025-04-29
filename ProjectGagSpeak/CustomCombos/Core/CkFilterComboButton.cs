using Dalamud.Interface.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class CkFilterComboButton<T> : CkFilterComboCache<T>
{
    protected CkFilterComboButton(IEnumerable<T> items, ILogger log)
        : base(items, log)
    { }

    protected CkFilterComboButton(Func<IReadOnlyList<T>> generator, ILogger log)
        : base(generator, log)
    { }

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();

    /// <summary> What will occur when the button is pressed. </summary>
    protected abstract void OnButtonPress(int layerIdx);

    /// <summary> The virtual function for all filter combo buttons. </summary>
    /// <returns> True if anything was selected, false otherwise. </returns>
    public bool DrawComboButton(string label, float width, int layerIdx, string bText, string tooltip)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - ImGuiHelpers.GetButtonSize(bText).X - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().ItemSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.

        var previewLabel = Current?.ToString() ?? "Select an Item...";
        var ret = Draw(label, previewLabel, string.Empty, comboWidth, ImGui.GetTextLineHeightWithSpacing(), ImGuiComboFlags.None);
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();

        // disable the button if we should.
        using var disabled = ImRaii.Disabled(DisableCondition());
        if (ImGuiUtil.DrawDisabledButton(bText, new Vector2(), string.Empty, DisableCondition()))
            OnButtonPress(layerIdx);
        CkGui.AttachToolTip(tooltip);

        return ret;
    }
}
