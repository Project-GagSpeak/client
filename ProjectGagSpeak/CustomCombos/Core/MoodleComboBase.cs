using CkCommons.Gui;
using CkCommons.Textures;
using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

public abstract class MoodleComboBase<T> : CkFilterComboCache<T>
{
    protected readonly MainHub _mainHub;
    protected readonly Kinkster _kinksterRef;
    protected float IconScale;

    protected MoodleComboBase(ILogger log, MainHub hub, Kinkster pair, float scale, Func<IReadOnlyList<T>> generator)
        : base(generator, log)
    {
        _mainHub = hub;
        IconScale = scale;
        _kinksterRef = pair;  
        Current = default;
    }

    protected virtual Vector2 IconSize => MoodleIcon.Size * IconScale;

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();
    protected abstract bool CanDoAction(T item);
    protected abstract void OnApplyButton(T item);
    protected virtual void OnRemoveButton(T item)
    { }

    /// <summary> The function for all filter combo buttons. </summary>
    /// <returns> True if anything was selected, false otherwise. </returns>
    /// <remarks> The action passed in will be invoked if the button interaction was successful. </remarks>
    protected bool DrawComboButton(string label, string preview, float width, bool isApply, string tt)
    {
        // we need to first extract the width of the button.
        var buttonText = isApply ? "Apply" : "Remove";
        var comboWidth = width - ImUtf8.ItemInnerSpacing.X - CkGui.IconTextButtonSize(FAI.PersonRays, buttonText);

        // if we have a new item selected we need to update some conditionals.
        var ret = Draw(label, preview, string.Empty, comboWidth, IconSize.Y, CFlags.HeightLargest);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.PersonRays, buttonText, disabled: DisableCondition()) && Current is { } item)
        {
            if (isApply) OnApplyButton(item);
            else OnRemoveButton(item);
        }
        CkGui.AttachToolTip(tt);

        return ret;
    }
}
