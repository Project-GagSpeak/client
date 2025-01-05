using Dalamud.Interface.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

// base abstract class for combos that have a button counterpart beside it.
public abstract class PairCustomComboToggle<T> : PairCustomComboBase<T>
{
    private readonly UiSharedService _uiShared;

    protected float _popupWidth = 0;
    private string _onText;
    private string _offText;
    private string _onTooltip;
    private string _offTooltip;

    protected PairCustomComboToggle(ILogger log, UiSharedService uiShared, MainHub mainHub,
        Pair pair, string onText, string onTooltip, string offText, string offTooltip)
        : base(log, pair, mainHub)
    {
        _uiShared = uiShared;
        _onText = onText;
        _onTooltip = onTooltip;
        _offText = offText;
        _offTooltip = offTooltip;
    }

    public override bool DrawCombo(string label, string tt, float width, float popupWidthScale, float itemH, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // first we need to ensure that the current selection is not null, or else we declare the button width as 0.
        var buttonWidth = GetButtonWidth();
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemSpacing.X;
        _popupWidth = comboWidth * popupWidthScale;

        // Draw the combo box.
        var result = base.DrawCombo(label, tt, comboWidth, popupWidthScale, itemH, flags);

        // Draw the toggle button beside the combo.
        ImUtf8.SameLineInner();
        var isToggledOn = IsToggledOn(CurrentSelection);
        if (ImGuiUtil.DrawDisabledButton(isToggledOn ? _onText : _offText, new Vector2(), string.Empty, CurrentSelection is null))
        {
            if (CurrentSelection is not null) OnTogglePressed(CurrentSelection);
        }
        return result;
    }

    protected abstract void OnTogglePressed(T item);

    private float GetButtonWidth() => ImGuiHelpers.GetButtonSize(IsToggledOn(CurrentSelection) ? _onText : _offText).X - ImGui.GetStyle().ItemInnerSpacing.X;

    protected abstract bool IsToggledOn(T? selection);

    protected override float GetFilterWidth() => _popupWidth - 2 * ImGui.GetStyle().FramePadding.X;
}
