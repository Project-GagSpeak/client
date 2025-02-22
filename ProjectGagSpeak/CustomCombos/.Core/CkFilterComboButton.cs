using Dalamud.Interface.Utility;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class CkFilterComboButton<T> : CkFilterComboCache<T>
{
    protected readonly UiSharedService _uiShared;
    protected string ButtonText = string.Empty;
    protected string ButtonToolTip = string.Empty;

    protected CkFilterComboButton(IEnumerable<T> items, ILogger log, UiSharedService ui, string buttonText, string buttonTT)
        : base(items, log)
    {
        _uiShared = ui;
        ButtonText = buttonText;
        ButtonToolTip = buttonTT;
    }

    protected CkFilterComboButton(Func<IReadOnlyList<T>> generator, ILogger log, UiSharedService ui, string buttonText, string buttonTT)
        : base(generator, log)
    {
        _uiShared = ui;
        ButtonText = buttonText;
        ButtonToolTip = buttonTT;
    }

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();

    /// <summary> What will occur when the button is pressed. </summary>
    protected abstract void OnButtonPress();

    /// <summary> The virtual function for all filter combo buttons. </summary>
    /// <returns> True if anything was selected, false otherwise. </returns>
    protected bool Draw(string label, string tt, float width, float itemH, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - ImGuiHelpers.GetButtonSize(ButtonText).X - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().ItemSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.
        var ret = Draw(label, tt, string.Empty, comboWidth, itemH, flags);
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();

        // disable the button if we should.
        using var disabled = ImRaii.Disabled(DisableCondition());
        if (ImGuiUtil.DrawDisabledButton(ButtonText, new Vector2(), string.Empty, DisableCondition()))
            OnButtonPress();
        UiSharedService.AttachToolTip(ButtonToolTip);

        return ret;
    }
}
