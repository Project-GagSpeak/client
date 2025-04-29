using Dalamud.Interface;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class CkFilterComboIconButton<T> : CkFilterComboCache<T>
{
    protected FontAwesomeIcon ButtonIcon = FAI.None;
    protected string ButtonText = string.Empty;
    protected string ButtonToolTip = string.Empty;

    protected CkFilterComboIconButton(IEnumerable<T> items, ILogger log, FontAwesomeIcon icon, string buttonText, string buttonTT)
        : base(items, log)
    {
        ButtonText = buttonText;
        ButtonToolTip = buttonTT;
    }

    protected CkFilterComboIconButton(Func<IReadOnlyList<T>> generator, ILogger log, FontAwesomeIcon icon, string buttonText, string buttonTT)
        : base(generator, log)
    {
        ButtonText = buttonText;
        ButtonToolTip = buttonTT;
    }

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();

    /// <summary> What will occur when the button is pressed. </summary>
    protected abstract void OnButtonPress();

    public bool DrawComboButton(string label, string tt, float width, float itemH, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - CkGui.IconTextButtonSize(ButtonIcon, ButtonText) - 
            ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().ItemSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.
        var ret = Draw(label, tt, string.Empty, comboWidth, itemH, flags);
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(ButtonIcon, ButtonText, disabled: DisableCondition(), id: label + "-Button"))
            OnButtonPress();
        CkGui.AttachToolTip(ButtonToolTip);

        return ret;
    }
}
