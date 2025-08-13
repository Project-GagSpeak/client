using CkCommons.Gui;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class CkFilterComboIconButton<T> : CkFilterComboCache<T>
{
    protected FontAwesomeIcon ButtonIcon = FAI.None;

    protected CkFilterComboIconButton(ILogger log, FAI icon, IEnumerable<T> items)
        : base(items, log)
    {
        ButtonIcon = icon;
    }

    protected CkFilterComboIconButton(ILogger log, FAI icon, Func<IReadOnlyList<T>> generator)
        : base(generator, log)
    {
        ButtonIcon = icon;
    }

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();

    /// <summary> What will occur when the button is pressed. </summary>
    protected abstract Task<bool> OnButtonPress();

    public bool Draw(string label, float width, string tt)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - CkGui.IconButtonSize(ButtonIcon).X - ImGui.GetStyle().ItemInnerSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.
        var previewLabel = Current is not null ? ToString(Current) : "Select an Item...";
        var ret = Draw(label, previewLabel, string.Empty, comboWidth, ImGui.GetTextLineHeightWithSpacing(), CFlags.None);
        
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();
        if (CkGui.IconButton(ButtonIcon, disabled: DisableCondition(), id: label + "-Button"))
            _ = OnButtonPress();
        CkGui.AttachToolTip(tt);

        return ret;
    }
}
