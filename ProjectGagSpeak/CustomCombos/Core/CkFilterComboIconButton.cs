using Dalamud.Interface;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class CkFilterComboIconButton<T> : CkFilterComboCache<T>
{
    protected FontAwesomeIcon ButtonIcon = FAI.None;
    protected string ButtonText = string.Empty;

    protected CkFilterComboIconButton(IEnumerable<T> items, ILogger log, FAI icon, string buttonText)
        : base(items, log)
    {
        ButtonIcon = icon;
        ButtonText = buttonText;
    }

    protected CkFilterComboIconButton(Func<IReadOnlyList<T>> generator, ILogger log, FAI icon, string buttonText)
        : base(generator, log)
    {
        ButtonIcon = icon;
        ButtonText = buttonText;
    }

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();

    /// <summary> What will occur when the button is pressed. </summary>
    protected abstract Task<bool> OnButtonPress();

    public bool DrawComboIconButton(string label, float width, string tt, Action? onButtonSuccess = null)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - CkGui.IconTextButtonSize(ButtonIcon, ButtonText) - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().ItemSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.
        var previewLabel = Current?.ToString() ?? "Select an Item...";
        var ret = Draw(label, previewLabel, string.Empty, comboWidth, ImGui.GetTextLineHeightWithSpacing(), CFlags.None);
        
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(ButtonIcon, ButtonText, disabled: DisableCondition(), id: label + "-Button"))
        {
            _ = Task.Run(async () =>
            {
                if (await OnButtonPress() is true)
                    onButtonSuccess?.Invoke();
            });
        }
        CkGui.AttachToolTip(tt);

        return ret;
    }
}
