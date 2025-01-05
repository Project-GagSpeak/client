using Dalamud.Interface;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.UI.Components.Combos;
public abstract class PairPadlockComboBase
{
    protected readonly ILogger _logger;
    protected readonly MainHub _mainHub;
    protected readonly UiSharedService _uiShared;

    protected string comboLabelBase = string.Empty;
    protected readonly Pair _pairRef;
    protected IEnumerable<Padlocks> ComboPadlocks => ExtractPadlocks();
    protected string _password = string.Empty;
    protected string _timer = string.Empty;
    public string Password { get => _password; private set => _password = value; }
    public string Timer { get => _timer; private set => _timer = value; }
    public Padlocks SelectedLock { get; private set; } = Padlocks.None;

    protected PairPadlockComboBase(ILogger log, MainHub mainHub, UiSharedService uiShared, Pair pairData, string comboLabelBase)
    {
        _logger = log;
        _mainHub = mainHub;
        _uiShared = uiShared;
        _pairRef = pairData;
    }

    private IEnumerable<Padlocks> ExtractPadlocks() => LockHelperExtensions.GetLocksForPair(_pairRef.PairPerms);

    public void ResetSelection() => SelectedLock = Padlocks.None;
    public void ResetInputs()
    {
        Password = string.Empty;
        Timer = string.Empty;
    }
    
    protected abstract void DisplayDisabledActiveItem(float width);

    public void DrawLockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        DisplayDisabledActiveItem(width);

        // we need to calculate the size of the button for locking, so do so.
        var buttonWidth = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Lock, "Lock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // draw the combo box.
        ImGui.SetNextItemWidth(comboWidth);
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using (var combo = ImRaii.Combo("##" + comboLabelBase + "-LockCombo", SelectedLock.ToName()))
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

        // draw button thing.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Lock, "Lock", disabled: SelectedLock is Padlocks.None, id: "##" + comboLabelBase + "-LockButton"))
            OnLockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowLockFields();
    }

    public abstract void DrawUnlockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None);

    protected abstract void OnLockButtonPress();

    protected abstract void OnUnlockButtonPress();

    protected void ShowLockFields()
    {
        if (SelectedLock is Padlocks.None)
            return;

        float width = ImGui.GetContentRegionAvail().X;
        switch (SelectedLock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input", "Enter 4 digit combination...", ref _password, 4);
                break;
            case Padlocks.PasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                break;
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(width * (2 / 3f));
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref _timer, 12);
                break;
            case Padlocks.TimerPadlock:
            case Padlocks.OwnerTimerPadlock:
            case Padlocks.DevotionalTimerPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref _timer, 12);
                break;
        }
    }

    protected void ShowUnlockFields(Padlocks padlock)
    {
        if (!LockHelperExtensions.IsPasswordLock(padlock)) 
            return;

        float width = ImGui.GetContentRegionAvail().X;
        switch (padlock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input", "Enter 4 digit combination...", ref _password, 4);
                break;
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                break;
        }
    }
}
