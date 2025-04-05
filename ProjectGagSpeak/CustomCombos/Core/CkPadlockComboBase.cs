using Dalamud.Interface;
using Dalamud.Utility;
using GagSpeak.UI;
using GagspeakAPI.Data.Interfaces;
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
    private readonly HashSet<uint> _popupState = [];
    private readonly Func<int, T> _generator;
    protected T MonitoredItem { get; private set; }
    private int _currentLayer = -1;

    /// <summary> Contains the list of padlocks. </summary>
    public readonly ICachingList<Padlocks> ComboPadlocks;
    protected float? InnerWidth;
    protected int? NewSelection;
/*    private int _lastSelection = 0;
    private bool _setScroll;
    private bool _closePopup;*/
    protected string Password = string.Empty;
    protected string Timer = string.Empty;

    public Padlocks SelectedLock { get; protected set; } = Padlocks.None;

    protected CkPadlockComboBase(Func<int, T> generator, ILogger log)
    {
        Log = log;
        _currentLayer = -1;
        _generator = generator;
        MonitoredItem = generator(-1);
        ComboPadlocks = new TemporaryList<Padlocks>(ExtractPadlocks());
    }


    public void ResetSelection()
    {
        ComboPadlocks.ClearList();
        ResetInputs();
    }

    public void ResetInputs() => (Password, Timer) = (string.Empty, string.Empty);

    public float PadlockLockWindowHeight() => SelectedLock.IsTwoRowLock()
    ? ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2
    : ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

    public float PadlockUnlockWindowHeight() => MonitoredItem.Padlock.IsPasswordLock()
        ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y
        : ImGui.GetFrameHeight();

    protected void UpdateLayer(int newLayer)
    {
        _currentLayer = newLayer;
        MonitoredItem = _generator(newLayer);
        ResetSelection();
    }

    protected abstract bool DisableCondition();

    // we run this here so that we can get the specific padlocks we have access to.
    // This is ok because it is only a small list being made.
    protected abstract IEnumerable<Padlocks> ExtractPadlocks();
    protected virtual string ItemName(T item) => item.ToString() ?? string.Empty;
    protected abstract void OnLockButtonPress(int layerIdx);
    protected abstract void OnUnlockButtonPress(int layerIdx);

    public virtual void DrawLockComboWithActive(string label, float width, int layerIdx, string buttonTxt, string tooltip, bool isTwoRow)
    {
        DisplayActiveItem(width);
        DrawLockCombo(label, width, layerIdx, buttonTxt, tooltip, isTwoRow);
    }

    public virtual void DrawLockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip, bool isTwoRow)
    {
        // we need to calculate the size of the button for locking, so do so.
        using var group = ImRaii.Group();
        var buttonWidth = buttonTxt.IsNullOrEmpty()
            ? CkGui.IconButtonSize(FAI.Lock).X
            : CkGui.IconTextButtonSize(FAI.Lock, "Lock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // If for whatever reason the current layer is different from the previous, reset & regenerate inputs & combo list.
        if (_currentLayer != layerIdx)
            UpdateLayer(layerIdx);

        // draw the combo box.
        ImGui.SetNextItemWidth(comboWidth);
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var disabled = ImRaii.Disabled(DisableCondition());
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
                        SelectedLock = item;
            }
        }

        // draw button thing for locking / unlocking.
        ImUtf8.SameLineInner();
        if(buttonTxt.IsNullOrEmpty())
        {
            if (CkGui.IconButton(FAI.Lock, disabled: SelectedLock is Padlocks.None, id: "##" + SelectedLock + "-LockButton"))
                OnLockButtonPress(layerIdx);
        }
        else
        {
            if (CkGui.IconTextButton(FAI.Lock, "Lock", disabled: SelectedLock is Padlocks.None, id: "##" + SelectedLock + "-LockButton"))
                OnLockButtonPress(layerIdx);
        }
        CkGui.AttachToolTip(tooltip);

        // on next line show lock fields.
        if (isTwoRow)
            TwoRowLockFields(label, width);
        else
            ThreeRowLockFields(label, width);
    }

    public void DrawUnlockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip)
        => DrawUnlockCombo(label, width, layerIdx, buttonTxt, tooltip, ImGuiComboFlags.None);

    public virtual void DrawUnlockCombo(string label, float width, int layerIdx, string buttonTxt, string tooltip, 
        ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // we need to calculate the size of the button for locking, so do so.
        using var group = ImRaii.Group();
        var buttonWidth = buttonTxt.IsNullOrEmpty()
            ? CkGui.IconButtonSize(FAI.Unlock).X
            : CkGui.IconTextButtonSize(FAI.Unlock, "Unlock");
        var unlockFieldWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // If for whatever reason the current layer is different from the previous, reset & regenerate inputs & combo list.
        if (_currentLayer != layerIdx)
            UpdateLayer(layerIdx);

        var lastPadlock = MonitoredItem.Padlock;
        // display the active padlock for the set in a disabled view.
        using (ImRaii.Disabled(!MonitoredItem.Padlock.IsPasswordLock()))
        {
            var hint = MonitoredItem.Padlock switch
            {
                Padlocks.CombinationPadlock => "Guess Combination...",
                Padlocks.PasswordPadlock => "Guess Password...",
                Padlocks.TimerPasswordPadlock => "Guess Password...",
                _ => string.Empty,
            };
            uint maxLength = MonitoredItem.Padlock switch
            {
                Padlocks.CombinationPadlock => 4,
                _ => 20,
            };
            CkGui.InputTextRightIcon("##UnlockerField_" + label, unlockFieldWidth, hint, ref Password, maxLength, FAI.Key);
        }

        // draw button thing.
        ImUtf8.SameLineInner();
        if (buttonTxt.IsNullOrEmpty())
        {
            if (CkGui.IconButton(FAI.Unlock, disabled: MonitoredItem.Padlock is Padlocks.None, id: "##" + label + "-UnlockButton"))
                OnUnlockButtonPress(layerIdx);
        }
        else
        {
            if (CkGui.IconTextButton(FAI.Unlock, "Unlock", disabled: MonitoredItem.Padlock is Padlocks.None, id: "##" + label + "-UnlockButton"))
                OnUnlockButtonPress(layerIdx);
        }
        CkGui.AttachToolTip(tooltip);
    }

    private void DisplayActiveItem(float width)
    {
        ImGui.SetNextItemWidth(width);
        using var disabled = ImRaii.Disabled(true);
        using var combo = ImRaii.Combo("ActiveDisplay", ItemName(MonitoredItem));
    }

    /// <summary> Draws out the padlock fields below. </summary>
    /// <remarks> IsTwoRow defines if it is made for restrictions/restraints or gags. </remarks>
    protected void TwoRowLockFields(string id, float width)
    {
        var leftWidth = width * (2 / 3f);
        var rightWidth = width - leftWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        string passFieldLabel = "##Input_" + id;
        string passFieldHint = SelectedLock switch
        {
            Padlocks.CombinationPadlock => "Enter 4 digit combination...",
            _ => "Enter password...",
        };
        string timerFieldHint = "Ex: 0h2m7s";
        uint maxLength = SelectedLock switch
        {
            Padlocks.CombinationPadlock => 4,
            _ => 20,
        };

        CkGui.InputTextRightIcon(passFieldLabel, leftWidth, passFieldHint, ref Password, maxLength, FAI.Key);
        ImUtf8.SameLineInner();
        CkGui.InputTextRightIcon("##Timer_" + id, rightWidth, timerFieldHint, ref Timer, 12, FAI.Clock);
    }

    protected void ThreeRowLockFields(string id, float width)
    {
        var iconButtonSize = CkGui.IconButtonSize(FAI.Clock);
        var inputTextWidth = width - iconButtonSize.X - ImGui.GetStyle().ItemInnerSpacing.X;

        string passFieldLabel = "##Input_" + id;
        string passFieldHint = SelectedLock switch
        {
            Padlocks.CombinationPadlock => "Enter 4 digit combination...",
            _ => "Enter password...",
        };
        string timerFieldHint = "Ex: 0h2m7s";
        uint maxLength = SelectedLock switch
        {
            Padlocks.CombinationPadlock => 4,
            _ => 20,
        };

        // Password Row
        using (ImRaii.Group())
        {
            using (ImRaii.Disabled(!SelectedLock.IsPasswordLock()))
            {
                ImGui.SetNextItemWidth(inputTextWidth);
                ImGui.InputTextWithHint(passFieldLabel, passFieldHint, ref Password, maxLength);
            }
            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.Key);
        }
        // Timer Row
        using (ImRaii.Group())
        {
            using (ImRaii.Disabled(!SelectedLock.IsTimerLock()))
            {
                ImGui.SetNextItemWidth(inputTextWidth);
                ImGui.InputTextWithHint("##Timer_" + id, timerFieldHint, ref Timer, 12);
            }
            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.Clock);
        }
    }
}
