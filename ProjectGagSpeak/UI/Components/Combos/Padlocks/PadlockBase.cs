using Dalamud.Interface;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.UI.Components.Combos;

// The true core of the abstract combos for padlocks. Handles all shared logic operations.
public abstract class PadlockBase<T> where T : IPadlockable
{
    protected readonly ILogger _logger;
    protected readonly UiSharedService _uiShared;

    // Basic identifiers used for the combos.
    protected string _label = string.Empty;
    protected string _password = string.Empty;
    protected string _timer = string.Empty;
    public Padlocks SelectedLock { get; protected set; } = Padlocks.None;

    protected PadlockBase(ILogger log, UiSharedService uiShared, string label)
    {
        _logger = log;
        _uiShared = uiShared;
        _label = label;
    }

    /// <summary>
    /// Contains the list of padlocks. These locks are obtained via the extract padlocks function.
    /// </summary>
    protected IEnumerable<Padlocks> ComboPadlocks => ExtractPadlocks();

    // Resets selections and all inputs.
    public void ResetSelection()
    {
        SelectedLock = Padlocks.None;
        ResetInputs();
    }

    public void ResetInputs()
    {
        _password = string.Empty;
        _timer = string.Empty;
    }

    public float PadlockLockWithActiveWindowHeight() => SelectedLock.IsTwoRowLock()
    ? ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2
    : ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

    public float PadlockLockWindowHeight() => SelectedLock.IsTwoRowLock()
        ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y
        : ImGui.GetFrameHeight();

    public float PadlockUnlockWindowHeight() => GetLatestPadlock().IsPasswordLock()
        ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y
        : ImGui.GetFrameHeight();


    public virtual void DrawLockComboWithActive(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // For pairs, display the active item prior to the combo.
        DisplayActiveItem(width);
        // then draw out the combo.
        DrawLockCombo(width, tt, btt, flags);
    }

    protected abstract bool DisableCondition();

    public virtual void DrawLockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // we need to calculate the size of the button for locking, so do so.
        var buttonWidth = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Lock, "Lock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // draw the combo box.
        ImGui.SetNextItemWidth(comboWidth);
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var disabled = ImRaii.Disabled(DisableCondition());
        using (var combo = ImRaii.Combo("##" + _label + "-LockCombo", SelectedLock.ToName()))
        {
            // display the tooltip for the combo with visible.
            using (ImRaii.Enabled())
            {
                UiSharedService.AttachToolTip(tt);
                // Handle right click clearing.
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    ResetSelection();
            }

            // handle combo.
            if (combo)
            {
                foreach (var item in ComboPadlocks)
                    if (ImGui.Selectable(item.ToName(), item == SelectedLock))
                        SelectedLock = item;
            }
        }

        // draw button thing for locking / unlocking.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Lock, "Lock", disabled: SelectedLock is Padlocks.None, id: "##" + SelectedLock + "-LockButton"))
            OnLockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowLockFields(width);
    }

    public virtual void DrawUnlockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // we need to calculate the size of the button for locking, so do so.
        var buttonWidth = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Unlock, "Unlock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        var lastPadlock = GetLatestPadlock();

        // display the active padlock for the set in a disabled view.
        using (ImRaii.Disabled(true))
        {
            ImGui.SetNextItemWidth(comboWidth);
            if (ImGui.BeginCombo("##" + _label + "-UnlockCombo", lastPadlock.ToName())) { ImGui.EndCombo(); }
        }

        // draw button thing.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, "Unlock", disabled: lastPadlock is Padlocks.None, id: "##" + _label + "-UnlockButton"))
            OnUnlockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowUnlockFields(lastPadlock, width);
    }

    private void DisplayActiveItem(float width)
    {
        T activeItem = GetLatestActiveItem();
        // disable the actively selected padlock.
        ImGui.SetNextItemWidth(width);
        using (ImRaii.Disabled(true))
        {
            if (ImGui.BeginCombo("##" + _label + "ActiveDisplay", ToActiveItemString(activeItem))) ImGui.EndCombo();
        }
    }

    /// <summary>
    /// Abstract function method used to determine how the ComboPadlocks fetches its padlock list.
    /// </summary>
    protected abstract IEnumerable<Padlocks> ExtractPadlocks();
    protected abstract Padlocks GetLatestPadlock();
    protected abstract T GetLatestActiveItem();
    protected virtual string ToActiveItemString(T item) => item?.ToString() ?? string.Empty;
    protected abstract void OnLockButtonPress();
    protected abstract void OnUnlockButtonPress();

    protected void ShowLockFields(float width)
    {
        if (SelectedLock is Padlocks.None)
            return;

        var leftWidth = width * (2 / 3f);
        var rightWidth = width - leftWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        switch (SelectedLock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input" + _label, "Enter 4 digit combination...", ref _password, 4);
                break;
            case Padlocks.PasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input" + _label, "Enter password...", ref _password, 20);
                break;
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(leftWidth);
                ImGui.InputTextWithHint("##Password_Input" + _label, "Enter password...", ref _password, 20);
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(rightWidth);
                ImGui.InputTextWithHint("##Timer_Input" + _label, "Ex: 0h2m7s", ref _timer, 12);
                break;
            case Padlocks.TimerPadlock:
            case Padlocks.OwnerTimerPadlock:
            case Padlocks.DevotionalTimerPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Timer_Input" + _label, "Ex: 0h2m7s", ref _timer, 12);
                break;
        }
    }

    protected void ShowUnlockFields(Padlocks padlock, float width)
    {
        if (!GsPadlockEx.IsPasswordLock(padlock))
            return;

        switch (padlock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input_Unlock" + _label, "Enter 4 digit combination...", ref _password, 4);
                break;
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input_Unlock" + _label, "Enter password...", ref _password, 20);
                break;
        }
    }
}
