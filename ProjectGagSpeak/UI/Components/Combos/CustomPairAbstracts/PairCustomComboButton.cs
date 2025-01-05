using Dalamud.Interface;
using Dalamud.Interface.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

// base abstract class for combos that have a button counterpart beside it.
public abstract class PairCustomComboButton<T> : PairCustomComboBase<T>
{
    protected readonly UiSharedService _uiShared;

    private bool _isIconButton = false;
    protected float _popupWidth = 0;

    private float ButtonWidth = 0;
    private FontAwesomeIcon ButtonIcon = FontAwesomeIcon.None;
    private string ButtonText = string.Empty;
    private string ButtonTooltip = string.Empty;

    protected PairCustomComboButton(ILogger log, UiSharedService uiShared, MainHub mainHub, Pair pair, 
        FontAwesomeIcon icon, string bText, string bTT) : base(log, pair, mainHub)
    {
        _uiShared = uiShared;
        _isIconButton = true;
        ButtonIcon = icon;
        ButtonText = bText;
        ButtonTooltip = bTT;
    }

    protected PairCustomComboButton(ILogger log, UiSharedService uiShared, MainHub mainHub, Pair pair, 
        string bText, string bTT) : base(log, pair, mainHub)
    {
        _uiShared = uiShared;
        _isIconButton = false;
        ButtonText = bText;
        ButtonTooltip = bTT;
    }

    // override for the base draw combo to allow room for a button and action execution.
    public override bool DrawCombo(string label, string tt, float width, float popupWidthScale, float itemH, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        try
        {
            // if the button width is not yet stored, store it.
            if (ButtonWidth is 0) ButtonWidth = GetButtonWidth();

            // we need to first extract the width of the button.
            var comboWidth = width - ButtonWidth - ImGui.GetStyle().ItemSpacing.X;
            _popupWidth = comboWidth * popupWidthScale;

            // if we have a new item selected we need to update some conditionals.
            var result = base.DrawCombo(label, tt, comboWidth, popupWidthScale, itemH, flags);
            // move just beside it to draw the button.
            ImUtf8.SameLineInner();

            // draw the kind of button based on the type.
            if (_isIconButton)
            {
                if (_uiShared.IconTextButton(ButtonIcon, ButtonText, null, false, DisableCondition(), label + "-Button"))
                    OnButtonPress();
            }
            else
            {
                if (ImGuiUtil.DrawDisabledButton(ButtonText, new Vector2(), string.Empty, DisableCondition()))
                    OnButtonPress();
            }

            // return false if by this point we have not selected anything.
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error drawing combo button.");
            return false;
        }
    }

    protected abstract bool DisableCondition();

    private float GetButtonWidth()
    {
        if (_isIconButton) return _uiShared.GetIconTextButtonSize(ButtonIcon, ButtonText);
        else return ImGuiHelpers.GetButtonSize(ButtonText).X - ImGui.GetStyle().ItemInnerSpacing.X;
    }

    protected override float GetFilterWidth() => _popupWidth - 2 * ImGui.GetStyle().FramePadding.X;

    protected abstract void OnButtonPress();
}
