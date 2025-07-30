using CkCommons.Gui;
using Dalamud.Interface;
using GagSpeak.Gui;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class CkFilterComboIconButton<T> : CkFilterComboCache<T>
{
    protected FontAwesomeIcon ButtonIcon = FAI.None;
    protected string ButtonText = string.Empty;

    protected CkFilterComboIconButton(ILogger log, FAI icon, string buttonText, IEnumerable<T> items)
        : base(items, log)
    {
        ButtonIcon = icon;
        ButtonText = buttonText;
    }

    protected CkFilterComboIconButton(ILogger log, FAI icon, string buttonText, Func<IReadOnlyList<T>> generator)
        : base(generator, log)
    {
        ButtonIcon = icon;
        ButtonText = buttonText;
    }

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();

    /// <summary> What will occur when the button is pressed. </summary>
    protected abstract Task<bool> OnButtonPress();

    public bool DrawComboIconButton(string label, float width, string tt)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - CkGui.IconTextButtonSize(ButtonIcon, ButtonText) - ImGui.GetStyle().ItemInnerSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.
        var previewLabel = Current is not null ? ToString(Current) : "Select an Item...";
        var ret = Draw(label, previewLabel, string.Empty, comboWidth, ImGui.GetTextLineHeightWithSpacing(), CFlags.None);
        
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(ButtonIcon, ButtonText, disabled: DisableCondition(), id: label + "-Button"))
            _ = OnButtonPress();
        CkGui.AttachToolTip(tt);

        return ret;
    }
}
