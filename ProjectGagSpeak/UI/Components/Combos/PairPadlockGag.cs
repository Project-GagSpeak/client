using Dalamud.Interface;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.UI.Components.Combos;
public class PairPadlockGag : PairPadlockComboBase
{
    public PairPadlockGag(ILogger log, MainHub mainHub, UiSharedService uiShared, Pair pairData, string comboLabelBase)
        : base(log, mainHub, uiShared, pairData, comboLabelBase) { }

    public float PadlockLockWinHeight() => SelectedLock.IsTwoRowLock()
        ? ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2
        : ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

    public float PadlockUnlockWinHeight()
    {
        if (_pairRef.LastAppearanceData is null) return ImGui.GetFrameHeight();

        // otherwise, return based on the current Padlock type.
        return _pairRef.LastAppearanceData.GagSlots[PairCombos.GagLayer].Padlock.ToPadlock().IsPasswordLock()
            ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y
            : ImGui.GetFrameHeight();
    }

    protected override void DisplayDisabledActiveItem(float width)
    {
        if(_pairRef.LastAppearanceData is null) return;

        // get the active gag for the current gag layer.
        var activeGag = _pairRef.LastAppearanceData.GagSlots[PairCombos.GagLayer].GagType.ToGagType();
        // disable the actively selected padlock.
        ImGui.SetNextItemWidth(width);
        using (ImRaii.Disabled(true))
        {
            if (ImGui.BeginCombo("##" + comboLabelBase + "ActiveGagDisplay", activeGag.GagName())) { ImGui.EndCombo(); }
        }
    }


    public override void DrawUnlockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // if the latest appearance data is null, return.
        if (_pairRef.LastAppearanceData is null)
            return;

        // we need to calculate the size of the button for locking, so do so.
        var buttonWidth = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Unlock, "Unlock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // disable the actively selected padlock.
        var padlock = _pairRef.LastAppearanceData.GagSlots[PairCombos.GagLayer].Padlock;
        ImGui.SetNextItemWidth(comboWidth);
        using (ImRaii.Disabled(true))
        {
            if (ImGui.BeginCombo("##" + comboLabelBase + "DisplayPadlockLock", padlock)) { ImGui.EndCombo(); }
            UiSharedService.AttachToolTip(tt);
        }
        // draw button thing.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, "Unlock", disabled: padlock.ToPadlock() is Padlocks.None, id: "##" + comboLabelBase + "-UnlockButton"))
            OnUnlockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowUnlockFields(padlock.ToPadlock());
    }


    protected override void OnLockButtonPress()
    {
        if (_pairRef.LastAppearanceData is null) return;

        // create a deep clone our of appearance data to analyze and modify.
        var appearanceData = _pairRef.LastAppearanceData.DeepCloneData();
        if (appearanceData is null) return;

        _logger.LogDebug("Verifying lock for padlock: " + SelectedLock.ToName(), LoggerType.PadlockHandling);
        var slotToUpdate = appearanceData.GagSlots[PairCombos.GagLayer];
        (bool valid, string errorStr) = LockHelperExtensions.VerifyLock(ref slotToUpdate, SelectedLock, Password, Timer, MainHub.UID, _pairRef.PairPerms);
        if (valid is false)
        {
            _logger.LogError("Failed to lock padlock: " + SelectedLock.ToName() + " due to: " + errorStr, LoggerType.PadlockHandling);
            return;
        }

        // update the appearance data with the new slot.
        appearanceData.GagSlots[PairCombos.GagLayer] = slotToUpdate;
        _ = _mainHub.UserPushPairDataAppearanceUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, appearanceData, (GagLayer)PairCombos.GagLayer, GagUpdateType.GagLocked, Padlocks.None, UpdateDir.Other));
        _logger.LogDebug("Locking Gag with GagPadlock " + SelectedLock.ToName() + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
        PairCombos.Opened = InteractionType.None;
    }

    protected override void OnUnlockButtonPress()
    {
        if (_pairRef.LastAppearanceData is null) return;

        // create a deep clone our of appearance data to analyze and modify.
        var appearanceData = _pairRef.LastAppearanceData.DeepCloneData();
        if (appearanceData is null) return;

        var slotToUpdate = appearanceData.GagSlots[PairCombos.GagLayer];
        _logger.LogDebug("Verifying unlock for padlock: " + slotToUpdate.Padlock, LoggerType.PadlockHandling);

        // safely store the preivous password in the case of success.
        var prevLock = slotToUpdate.Padlock.ToPadlock();

        // verify if we can unlock.
        (bool valid, string errorStr) = LockHelperExtensions.VerifyUnlock(ref slotToUpdate, _pairRef.UserData, Password, MainHub.UID, _pairRef.PairPerms);
        if (valid is false)
        {
            _logger.LogError("Failed to lock padlock: " + slotToUpdate.Padlock + " due to: " + errorStr, LoggerType.PadlockHandling);
            return;
        }

        // update the appearance data with the new slot.
        appearanceData.GagSlots[PairCombos.GagLayer] = slotToUpdate;
        _ = _mainHub.UserPushPairDataAppearanceUpdate(new(_pairRef.UserData, MainHub.PlayerUserData, appearanceData, (GagLayer)PairCombos.GagLayer, GagUpdateType.GagUnlocked, prevLock, UpdateDir.Other));
        _logger.LogDebug("Unlocking Gag with GagPadlock " + slotToUpdate.Padlock + " to " + _pairRef.GetNickAliasOrUid(), LoggerType.Permissions);
        PairCombos.Opened = InteractionType.None;
    }
}
