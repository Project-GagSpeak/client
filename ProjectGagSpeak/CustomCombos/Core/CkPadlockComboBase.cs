using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

// The true core of the abstract combos for padlocks. Handles all shared logic operations.
//
// Maintainers Note:
// - The PadlockBase Draw() call handles the passing in of the layer it works with. This layer can be defaulted to -1 if unused.
// - The layer is used so that when Lock() and Unlock() operations are used, their respective parent object knows what layer called it.
// This prevents us from needing to store multiple caches of the same padlock, and redundant code blocks.
// - (?) It is the parent classes responsibility to ensure whenever padlock combo changes layers, its selections are reset and cleared.
public abstract class CkPadlockComboBase<T> where T : IPadlockableRestriction
{
    protected readonly ILogger Log;

    protected T ActiveItem;
    protected string Password = string.Empty;
    protected string Timer = string.Empty;
    protected Padlocks SelectedLock { get; set; } = Padlocks.None;

    private readonly Func<List<Padlocks>> _padlocksListGenerator;
    private List<Padlocks> _padlocksList;
    private bool _closePopup;

    protected CkPadlockComboBase(Func<List<Padlocks>> padlocks, ILogger log)
    {
        Log = log;
        _padlocksListGenerator = padlocks;
        _padlocksList = padlocks();
    }

    /// <summary>
    /// Clears the list of stored locks, changes the selected padlock to none, and resets the text input fields.
    /// </summary>
    protected void ResetSelection()
    {
        _padlocksList.Clear();
        SelectedLock = Padlocks.None;
        ResetInputs();
    }

    /// <summary>
    /// Resets text input fields.
    /// </summary>
    protected void ResetInputs() => (Password, Timer) = (string.Empty, string.Empty);

    /// <summary>
    /// The conditions under which a lock or unlock group needs to be disabled.
    /// </summary>
    /// <param name="layerIdx">the layer to affect</param>
    /// <returns><see langword="true"/> if the row should be disabled.</returns>
    protected abstract bool DisableCondition(int layerIdx);

    protected virtual string ItemName(T item) => item.ToString() ?? string.Empty;

    /// <summary> The logic that occurs when the lock button is pressed. </summary>
    /// <returns> If the operation was successful or not. </returns>
    protected abstract Task OnLockButtonPress(string label, int layerIdx);

    /// <summary> The logic that occurs when the unlock button is pressed. </summary>
    /// <returns> If the operation was successful or not. </returns>
    protected abstract Task OnUnlockButtonPress(string label, int layerIdx);

    /// <summary>
    /// Handles drawing the Item Lock inputs. <see cref="ActiveItem"/> must be set in the overriding method.
    /// </summary>
    /// <param name="label">Internal label for this item</param>
    /// <param name="width">Total available width, px.</param>
    /// <param name="layerIdx">The layer index of the affecting item</param>
    /// <param name="buttonTxt">The text to display on the Lock button</param>
    /// <param name="tooltip">The tooltip to display on the Lock button.</param>
    /// <param name="isTwoRow">If the Item Lock inputs should be drawn on two or three rows.</param>
    public virtual void DrawLockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip, bool isTwoRow)
    {
        // we need to calculate the size of the button for locking
        using var group = ImRaii.Group();
        var buttonWidth = buttonTxt.IsNullOrEmpty()
            ? CkGui.IconButtonSize(FAI.Lock).X
            : CkGui.IconTextButtonSize(FAI.Lock, "Lock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // draw the combo box.
        var tooltipIconSize = CkGui.IconSize(FAI.InfoCircle);
        ImGui.SetNextItemWidth(comboWidth);
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var disabled = ImRaii.Disabled(DisableCondition(layerIdx));
        using (var combo = ImRaii.Combo(label, SelectedLock.ToName()))
        {
            using (ImRaii.Enabled())
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    ResetSelection();
            }

            // handle combo.
            if (combo)
            {
                if (_padlocksList.Count == 0)
                {
                    _padlocksList = _padlocksListGenerator();
                }

                foreach (var item in _padlocksList)
                {
                    if (ImGui.Selectable(item.ToName(), item == SelectedLock))
                    {
                        SelectedLock = item;
                        _closePopup = true;
                    }

                    if (item != 0)
                    {
                        ImGui.SameLine(comboWidth - tooltipIconSize.X);
                        CkGui.HoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint(), ImGui.GetColorU32(ImGuiCol.TextDisabled));
                        CkGui.AttachToolTip(item.ToTooltip(), color: ImGuiColors.ParsedGold);
                    }
                }

                if (_closePopup)
                {
                    _padlocksList.Clear();
                    _closePopup = false;
                }
            }
        }

        // draw button thing for locking / unlocking.
        ImUtf8.SameLineInner();
        if (buttonTxt.IsNullOrEmpty())
        {
            if (CkGui.IconButton(FAI.Lock, disabled: SelectedLock is Padlocks.None, id: "##" + SelectedLock + "-LockButton"))
                OnLockButtonPress(label, layerIdx);
        }
        else
        {
            if (CkGui.IconTextButton(FAI.Lock, "Lock", disabled: SelectedLock is Padlocks.None, id: "##" + SelectedLock + "-LockButton"))
                OnLockButtonPress(label, layerIdx);
        }

        CkGui.AttachToolTip(tooltip);

        // on next line show lock fields.
        if (isTwoRow)
            TwoRowLockFields(label, width);
        else
            ThreeRowLockFields(label, width);
    }

    public virtual void DrawUnlockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip)
    {
        // we need to calculate the size of the button for locking
        using var group = ImRaii.Group();
        var buttonWidth = buttonTxt.IsNullOrEmpty()
            ? CkGui.IconButtonSize(FAI.Unlock).X
            : CkGui.IconTextButtonSize(FAI.Unlock, "Unlock");
        var unlockWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        var lastPadlock = ActiveItem.Padlock;
        // display the active padlock for the set in a disabled view.
        using (ImRaii.Group())
        {
            var hint = lastPadlock switch
            {
                Padlocks.Combination => "Guess Combo...",
                Padlocks.Password => "Guess Password...",
                Padlocks.TimerPassword => "Guess Password...",
                _ => string.Empty,
            };
            DrawUnlockFieldSpecial(unlockWidth, hint, lastPadlock.IsTimerLock());
        }

        // draw button thing.
        ImUtf8.SameLineInner();
        if (buttonTxt.IsNullOrEmpty())
        {
            if (CkGui.IconButton(FAI.Unlock, disabled: lastPadlock is Padlocks.None, id: "##" + label + "-UnlockButton"))
                OnUnlockButtonPress(label, layerIdx);
        }
        else
        {
            if (CkGui.IconTextButton(FAI.Unlock, "Unlock", disabled: lastPadlock is Padlocks.None, id: "##" + label + "-UnlockButton"))
                OnUnlockButtonPress(label, layerIdx);
        }

        CkGui.AttachToolTip(tooltip);

        // The special unlock field.
        void DrawUnlockFieldSpecial(float width, string hint, bool isTimer)
        {
            using var _ = ImRaii.Group();
            (ITFlags flags, int len) = SelectedLock == Padlocks.Combination ? (ITFlags.CharsDecimal, 4) : (ITFlags.None, 20);
            ImGui.SetNextItemWidth(width);
            using (ImRaii.Disabled(!lastPadlock.IsPasswordLock()))
                ImGui.InputTextWithHint($"##Unlocker_{label}", hint, ref Password, len, flags);

            using var s = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(0, ImGui.GetStyle().FramePadding.Y));
            var widthOffset = ImGui.GetFrameHeight() * (isTimer ? 2 : 1);
            ImGui.SetCursorScreenPos(ImGui.GetItemRectMax() - new Vector2(widthOffset, ImGui.GetFrameHeight()));
            if (isTimer)
            {
                CkGui.AnimatedHourglass(3000);
                CkGui.AttachToolTip($"--COL--{ActiveItem.Timer.ToGsRemainingTimeFancy()}--COL--", color: ImGuiColors.ParsedPink);
                ImGui.SameLine(0, -ImGui.GetStyle().FramePadding.X);
            }

            CkGui.FramedIconText(FAI.Key, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }
    }

    /// <summary> Draws out the padlock fields below. </summary>
    /// <remarks> IsTwoRow defines if it is made for restrictions/restraints or gags. </remarks>
    private void TwoRowLockFields(string id, float width)
    {
        var leftWidth = width * .6f;
        var rightWidth = width - leftWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        var passHint = SelectedLock == Padlocks.Combination ? "Set 4 digit combo.." : "Set password..";
        const string timerHint = "Ex: 0h2m7s";
        var maxLength = SelectedLock == Padlocks.Combination ? 4 : 20;
        var flags = SelectedLock == Padlocks.Combination ? ITFlags.CharsDecimal : ITFlags.None;

        using (ImRaii.Disabled(!SelectedLock.IsPasswordLock()))
            CkGui.IconInputText($"##Input_{id}", leftWidth, FAI.Key, passHint, ref Password, maxLength, flags);
        CkGui.AttachToolTip("If interactable, a valid password must be entered here to lock this padlock.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(!PadlockEx.TimerLocks.Contains(SelectedLock)))
            CkGui.IconInputText($"##Timer_{id}", rightWidth, FAI.Clock, timerHint, ref Timer, 12);
        CkGui.AttachToolTip("If interactable, a valid time must be entered here to lock this padlock." +
            "--SEP--Ex: 0h2m7s (0 hours, 2 minutes, 7 seconds).");
    }

    private void ThreeRowLockFields(string id, float width)
    {
        var passHint = SelectedLock == Padlocks.Combination ? "Set 4 digit combo.." : "Set password..";
        const string timerHint = "Ex: 0h2m7s";
        var maxLength = SelectedLock == Padlocks.Combination ? 4 : 20;
        var flags = SelectedLock == Padlocks.Combination ? ITFlags.CharsDecimal : ITFlags.None;

        // Password Row
        using (ImRaii.Disabled(!SelectedLock.IsPasswordLock()))
            CkGui.IconInputTextOuter("##Input_" + id, width, FAI.Key, passHint, ref Password, maxLength, flags);
        CkGui.AttachToolTip("If interactable, a valid password must be entered here to lock this padlock.");

        // Timer Row.
        using (ImRaii.Disabled(!PadlockEx.TimerLocks.Contains(SelectedLock)))
            CkGui.IconInputTextOuter("##Timer_" + id, width, FAI.Clock, timerHint, ref Timer, 12);
        CkGui.AttachToolTip("If interactable, a valid time must be entered here to lock this padlock." +
            "--SEP--Ex: 0h2m7s (0 hours, 2 minutes, 7 seconds).");
    }
}
