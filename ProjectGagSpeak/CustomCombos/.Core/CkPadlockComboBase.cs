using Dalamud.Interface;
using GagSpeak.UI;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

// The true core of the abstract combos for padlocks. Handles all shared logic operations.
public abstract class CkPadlockComboBase<T> where T : IPadlockableRestriction
{
    protected readonly UiSharedService _uiShared;

    private readonly HashSet<uint> _popupState = [];

    /// <summary> Contains the list of padlocks. </summary>
    public readonly TemporaryList<Padlocks> ComboPadlocks;
    protected readonly T MonitoredItem;

    protected readonly ILogger Log;
    protected float? InnerWidth;
    protected int? NewSelection;
    private int _lastSelection = -1;
    private bool _setScroll;
    private bool _closePopup;
    private readonly string _label = string.Empty;
    protected string Password = string.Empty;
    protected string Timer = string.Empty;

    public Padlocks SelectedLock { get; protected set; } = Padlocks.None;

    protected CkPadlockComboBase(Func<T> monitorGenerator, ILogger log, UiSharedService uiShared, string label)
    {
        Log = log;
        _uiShared = uiShared;
        _label = label;

        MonitoredItem = monitorGenerator();
        ComboPadlocks = new TemporaryList<Padlocks>(ExtractPadlocks());
    }

    public void ResetSelection() 
    {
        SelectedLock = Padlocks.None;
        ResetInputs();
    }

    public void ResetInputs() => (Password, Timer) = (string.Empty, string.Empty);

    public float PadlockLockWindowHeight() => SelectedLock.IsTwoRowLock()
    ? ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2
    : ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

    public float PadlockUnlockWindowHeight() => MonitoredItem.Padlock.IsPasswordLock()
        ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y
        : ImGui.GetFrameHeight();

    protected abstract bool DisableCondition();

    // we run this here so that we can get the specific padlocks we have access to.
    // This is ok because it is only a small list being made.
    protected abstract IEnumerable<Padlocks> ExtractPadlocks();
    protected virtual string ItemName(T item) => item.ToString() ?? string.Empty;
    protected abstract void OnLockButtonPress();
    protected abstract void OnUnlockButtonPress();

    public virtual void DrawLockComboWithActive(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        DisplayActiveItem(width);
        DrawLockCombo(width, tt, btt, flags);
    }

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
        var lastPadlock = MonitoredItem.Padlock;

        // display the active padlock for the set in a disabled view.
        using (ImRaii.Disabled(true))
        {
            ImGui.SetNextItemWidth(comboWidth);
            if (ImGui.BeginCombo("##" + _label + "-UnlockCombo", MonitoredItem.Padlock.ToName())) { ImGui.EndCombo(); }
        }

        // draw button thing.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, "Unlock", disabled: MonitoredItem.Padlock is Padlocks.None, id: "##" + _label + "-UnlockButton"))
            OnUnlockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowUnlockFields(lastPadlock, width);
    }

    private void DisplayActiveItem(float width)
    {
        ImGui.SetNextItemWidth(width);
        using var disabled = ImRaii.Disabled(true);
        using var combo = ImRaii.Combo(_label + "ActiveDisplay", ItemName(MonitoredItem));
    }

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
                ImGui.InputTextWithHint("##Combination_Input" + _label, "Enter 4 digit combination...", ref Password, 4);
                break;
            case Padlocks.PasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input" + _label, "Enter password...", ref Password, 20);
                break;
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(leftWidth);
                ImGui.InputTextWithHint("##Password_Input" + _label, "Enter password...", ref Password, 20);
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(rightWidth);
                ImGui.InputTextWithHint("##Timer_Input" + _label, "Ex: 0h2m7s", ref Timer, 12);
                break;
            case Padlocks.TimerPadlock:
            case Padlocks.OwnerTimerPadlock:
            case Padlocks.DevotionalTimerPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Timer_Input" + _label, "Ex: 0h2m7s", ref Timer, 12);
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
                ImGui.InputTextWithHint("##Combination_Input_Unlock" + _label, "Enter 4 digit combination...", ref Password, 4);
                break;
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input_Unlock" + _label, "Enter password...", ref Password, 20);
                break;
        }
    }
}
