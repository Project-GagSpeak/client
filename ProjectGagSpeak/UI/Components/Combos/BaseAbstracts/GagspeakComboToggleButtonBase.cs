using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

// base abstract class for combos that have a button counterpart beside it.
public abstract class GagspeakComboToggleButtonBase<T> : GagspeakComboButtonBase<T>
{
    // some extra disable texts to show.
    private string _offText;
    private string _offTooltip;

    protected GagspeakComboToggleButtonBase(ILogger log, UiSharedService ui, string onText, string onTT, 
        string offText, string offTT) : base(log, ui, onText, onTT)
    {
        _offText = offText;
        _offTooltip = offTT;
    }

    /// <summary>
    /// Override the button with a toggle button.
    /// </summary>
    protected override void DrawButton(string label)
    {
        var isToggledOn = IsToggledOn(CurrentSelection);
        // draw the kind of button based on the type.
        if (IsIconButton())
        {
            if (_uiShared.IconTextButton(_buttonIcon, isToggledOn ? _buttonText : _offText, null, false, DisableCondition(), label + "-Button"))
                OnButtonPress();
        }
        else
        {
            if (ImGuiUtil.DrawDisabledButton(isToggledOn ? _buttonText : _offText, new Vector2(), string.Empty, DisableCondition()))
                OnButtonPress();
        }
        UiSharedService.AttachToolTip(isToggledOn ? _buttonTT : _offTooltip);
    }

    private float GetButtonWidth() => ImGuiHelpers.GetButtonSize(IsToggledOn(CurrentSelection) ? _buttonText : _offText).X - ImGui.GetStyle().ItemInnerSpacing.X;

    protected abstract bool IsToggledOn(T? selection);
}
