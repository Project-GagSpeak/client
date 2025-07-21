using CkCommons;
using CkCommons.Gui;
using Dalamud.Interface.Colors;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos;

// The true core of the abstract combos for padlocks. Handles all shared logic operations.
// 
// Maintainers Note:
// - The PadlockBase Draw() call handles the passing in of the layer it works with. This layer can be defaulted to -1 if unused.
// - The layer is used so that when Lock() and Unlock() operations are used, their respective parent object knows what layer called it.
// This prevents us from needing to store multiple caches of the same padlock, and redundant code blocks.
// - (?) Is is the parent classes responsibility to ensure whenever padlock combo changes layers, its selections are reset and cleared.
public abstract class CkPadlockComboBase<T> where T : IPadlockableRestriction
{
    protected readonly ILogger Log;

    public readonly ICachingList<T> Items;

    /// <summary> Contains the list of padlocks. </summary>
    public readonly ICachingList<Padlocks> ComboPadlocks;
    protected float? InnerWidth;
    protected int? NewSelection;
    protected string Password = string.Empty;
    protected string Timer = string.Empty;
    private bool _closePopup;

    public Padlocks SelectedLock { get; protected set; } = Padlocks.None;
    protected CkPadlockComboBase(IEnumerable<T> items, IEnumerable<Padlocks> padlocks, ILogger log)
    {
        Log = log;
        Items = (ICachingList<T>)(new TemporaryList<T>(items));
        ComboPadlocks = new TemporaryList<Padlocks>(padlocks);
    }

    protected CkPadlockComboBase(Func<IReadOnlyList<T>> itemGen, IEnumerable<Padlocks> padlocks, ILogger log)
    {
        Log = log;
        Items = (ICachingList<T>)(new LazyList<T>(itemGen));
        ComboPadlocks = new TemporaryList<Padlocks>(padlocks);
    }

    protected CkPadlockComboBase(Func<IReadOnlyList<T>> itemGen, Func<IReadOnlyList<Padlocks>> padlockGen, ILogger log)
    {
        Log = log;
        Items = new LazyList<T>(itemGen);
        ComboPadlocks = new LazyList<Padlocks>(padlockGen);
    }

    public void ResetSelection()
    {
        ComboPadlocks.ClearList();
        ResetInputs();
    }

    protected void RefreshStorage(string label)
    {
        Log.LogTrace($"Clearing storage for padlock combo: {label}", LoggerType.Combos);
        Items.ClearList();
    }

    public void ResetInputs() => (Password, Timer) = (string.Empty, string.Empty);

    protected abstract bool DisableCondition(int layerIdx);

    protected virtual string ItemName(T item) => item.ToString() ?? string.Empty;

    /// <summary> The logic that occurs when the lock button is pressed. </summary>
    /// <returns> If the operation was successful or not. </returns>
    protected abstract Task<bool> OnLockButtonPress(string label, int layerIdx);

    /// <summary> The logic that occurs when the unlock button is pressed. </summary>
    /// <returns> If the operation was successful or not. </returns>
    protected abstract Task<bool> OnUnlockButtonPress(string label, int layerIdx);

    public virtual void DrawLockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip, bool isTwoRow)
    {
        // we need to calculate the size of the button for locking, so do so.
        using var group = ImRaii.Group();
        var buttonWidth = buttonTxt.IsNullOrEmpty()
            ? CkGui.IconButtonSize(FAI.Lock).X
            : CkGui.IconTextButtonSize(FAI.Lock, "Lock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // draw the combo box.
        ImGui.SetNextItemWidth(comboWidth);
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var disabled = ImRaii.Disabled(DisableCondition(layerIdx));
        using (var combo = ImRaii.Combo(label, SelectedLock.ToName()))
        {
            // display the tooltip for the combo with visible.
            using (ImRaii.Enabled())
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    ResetSelection();
            }

            // handle combo.
            if (combo)
            {
                foreach (var item in ComboPadlocks)
                    if (ImGui.Selectable(item.ToName(), item == SelectedLock))
                    {
                        SelectedLock = item;
                        _closePopup = true;
                    }

                if(_closePopup)
                {
                    RefreshStorage(label);
                    _closePopup = false;
                }
            }
        }

        // draw button thing for locking / unlocking.
        ImUtf8.SameLineInner();
        if(buttonTxt.IsNullOrEmpty())
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
        // we need to calculate the size of the button for locking, so do so.
        using var group = ImRaii.Group();
        var buttonWidth = buttonTxt.IsNullOrEmpty()
            ? CkGui.IconButtonSize(FAI.Unlock).X
            : CkGui.IconTextButtonSize(FAI.Unlock, "Unlock");
        var unlockWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        var lastPadlock = Items[layerIdx].Padlock;
        // display the active padlock for the set in a disabled view.
        using (ImRaii.Group())
        {
            var hint = lastPadlock switch
            {
                Padlocks.CombinationPadlock => "Guess Combo...",
                Padlocks.PasswordPadlock => "Guess Password...",
                Padlocks.TimerPasswordPadlock => "Guess Password...",
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
            (ITFlags flags, int len) = SelectedLock == Padlocks.CombinationPadlock ? (ITFlags.CharsDecimal, 4) : (ITFlags.None, 20);
            ImGui.SetNextItemWidth(width);
            using (ImRaii.Disabled(!Items[layerIdx].Padlock.IsPasswordLock()))
                ImGui.InputTextWithHint($"##Unlocker_{label}", hint, ref Password, (uint)len, flags);

            using var s = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(0, ImGui.GetStyle().FramePadding.Y));
            var widthOffset = ImGui.GetFrameHeight() * (isTimer ? 2 : 1);
            ImGui.SetCursorScreenPos(ImGui.GetItemRectMax() - new Vector2(widthOffset, ImGui.GetFrameHeight()));
            if (isTimer)
            {
                CkGui.AnimatedHourglass(3000);
                CkGui.AttachToolTip($"--COL--{Items[layerIdx].Timer.ToGsRemainingTimeFancy()}--COL--", color: ImGuiColors.ParsedPink);
                ImGui.SameLine(0, -ImGui.GetStyle().FramePadding.X);
            }
            CkGui.FramedIconText(FAI.Key, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }
    }

    /// <summary> Draws out the padlock fields below. </summary>
    /// <remarks> IsTwoRow defines if it is made for restrictions/restraints or gags. </remarks>
    protected void TwoRowLockFields(string id, float width)
    {
        var leftWidth = width * .6f;
        var rightWidth = width - leftWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        var passLabel = "##Input_" + id;
        var passHint = SelectedLock == Padlocks.CombinationPadlock ? "Set 4 digit combo.." : "Guess password..";
        var timerHint = "Ex: 0h2m7s";
        var maxLength = SelectedLock == Padlocks.CombinationPadlock ? 4 : 20;
        var flags = SelectedLock == Padlocks.CombinationPadlock ? ITFlags.CharsDecimal : ITFlags.None;

        using (ImRaii.Disabled(!SelectedLock.IsPasswordLock()))
            CkGui.IconInputText($"##Input_{id}", leftWidth, FAI.Key, passHint, ref Password, maxLength, flags);
        CkGui.AttachToolTip("If interactable, a valid password must be entered here to lock this padlock.");

        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(!PadlockEx.TimerLocks.Contains(SelectedLock)))
            CkGui.IconInputText($"##Timer_{id}", rightWidth, FAI.Clock, timerHint, ref Timer, 12);
        CkGui.AttachToolTip("If interactable, a valid time must be entered here to lock this padlock." +
            "--SEP--Ex: 0h2m7s (0 hours, 2 minutes, 7 seconds).");
    }

    protected void ThreeRowLockFields(string id, float width)
    {
        var passHint = SelectedLock == Padlocks.CombinationPadlock ? "Set 4 digit combo.." : "Guess password..";
        var timerHint = "Ex: 0h2m7s";
        var maxLength = SelectedLock == Padlocks.CombinationPadlock ? 4 : 20;
        var flags = SelectedLock == Padlocks.CombinationPadlock ? ITFlags.CharsDecimal : ITFlags.None;

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
