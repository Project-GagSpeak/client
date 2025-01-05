using Dalamud.Interface;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.UI.Components.Combos;
public class PairPadlockRestraint : PairPadlockComboBase
{
    public PairPadlockRestraint(ILogger log, MainHub mainHub, UiSharedService uiShared, Pair pairData, string comboLabelBase)
        : base(log, mainHub, uiShared, pairData, comboLabelBase) { }

    public float PadlockLockWinHeight() => SelectedLock.IsTwoRowLock() 
        ? ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2
        : ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

    public float PadlockUnlockWinHeight() => (_pairRef.LastWardrobeData?.Padlock.ToPadlock() ?? SelectedLock).IsPasswordLock()
        ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y
        : ImGui.GetFrameHeight();

    protected override void DisplayDisabledActiveItem(float width)
    {
        // disable the actively selected padlock.
        ImGui.SetNextItemWidth(width);
        using (ImRaii.Disabled(true))
        {
            if (ImGui.BeginCombo("##" + comboLabelBase + "ActiveSetDisplay", _pairRef.ActiveSetName())) { ImGui.EndCombo(); }
        }
    }


    public override void DrawUnlockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // if the latest wardrobe data is null, return.
        if (_pairRef.LastWardrobeData is null) return;

        // we need to calculate the size of the button for locking, so do so.
        var buttonWidth = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Unlock, "Unlock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // display the active padlock for the set in a disabled view.
        using (ImRaii.Disabled(true))
        {
            ImGui.SetNextItemWidth(comboWidth);
            if (ImGui.BeginCombo("##DummyComboDisplayRestraintLock", _pairRef.LastWardrobeData.Padlock)) { ImGui.EndCombo(); }
        }

        // draw button thing.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, "Unlock", disabled: _pairRef.LastWardrobeData.Padlock.ToPadlock() is Padlocks.None, id: "##" + comboLabelBase + "-UnlockButton"))
            OnUnlockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowUnlockFields(_pairRef.LastWardrobeData.Padlock.ToPadlock());
    }


    protected override void OnLockButtonPress()
    {
        if (_pairRef.LastWardrobeData is null) return;

        // create a deep clone our of wardrobe data to analyze and modify.
        var wardrobeData = _pairRef.LastWardrobeData.DeepCloneData();
        if (wardrobeData is null) return;

        _logger.LogDebug("Verifying lock for padlock: " + SelectedLock.ToName() + " with password " + Password + " and timer " + Timer, LoggerType.PadlockHandling);
        (bool valid, string errorStr) = LockHelperExtensions.VerifyLock(ref wardrobeData, SelectedLock, Password, Timer, MainHub.UID, _pairRef.PairPerms);
        if (valid is false)
        {
            _logger.LogError("Failed to lock padlock: " + SelectedLock.ToName() + " due to: " + errorStr, LoggerType.PadlockHandling);
            return;
        }

        // update the wardrobe data with the new slot.
        _ = _mainHub.UserPushPairDataWardrobeUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, wardrobeData, WardrobeUpdateType.RestraintLocked, Padlocks.None, UpdateDir.Other));
        _logger.LogDebug("Locking Restraint Set with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
        PairCombos.Opened = InteractionType.None;
        ResetSelection();
        ResetInputs();
    }

    protected override void OnUnlockButtonPress()
    {
        if (_pairRef.LastWardrobeData is null) return;

        // create a deep clone our of wardrobe data to analyze and modify.
        var wardrobeData = _pairRef.LastWardrobeData.DeepCloneData();
        if (wardrobeData is null) return;

        _logger.LogDebug("Verifying unlock for padlock: " + SelectedLock.ToName(), LoggerType.PadlockHandling);
        // safely store the previous password in the case of success.
        var prevLock = wardrobeData.Padlock.ToPadlock();

        // verify if we can unlock.
        (bool valid, string errorStr) = LockHelperExtensions.VerifyUnlock(ref wardrobeData, _pairRef.UserData, Password, MainHub.UID, _pairRef.PairPerms);
        if (valid is false)
        {
            _logger.LogError("Failed to lock padlock: " + SelectedLock.ToName() + " due to: " + errorStr, LoggerType.PadlockHandling);
            return;
        }

        // update the wardrobe data with the new slot.
        _ = _mainHub.UserPushPairDataWardrobeUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, wardrobeData, WardrobeUpdateType.RestraintUnlocked, prevLock, UpdateDir.Other));
        _logger.LogDebug("Unlocking Restraint Set with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
        PairCombos.Opened = InteractionType.None;

        ResetSelection();
        ResetInputs();
    }
}
