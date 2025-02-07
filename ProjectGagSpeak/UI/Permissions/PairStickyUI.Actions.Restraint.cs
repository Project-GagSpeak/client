using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Extensions;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// 
/// Yes its messy, yet it's long, but i functionalized it best i could for the insane 
/// amount of logic being performed without adding too much overhead.
/// </summary>
public partial class PairStickyUI
{
    private void DrawWardrobeActions()
    {
        if (StickyPair.LastWardrobeData is null || StickyPair.LastLightStorage is null) return;

        var applyRestraintText = "Apply Restraint Set";
        var applyRestraintTT = "Applies a Restraint Set to " + PairNickOrAliasOrUID + ". Click to select set.";
        var lockRestraintText = StickyPair.LastWardrobeData.Padlock.ToPadlock() is Padlocks.None
            ? "Lock Restraint Set" : "Locked with a " + StickyPair.LastWardrobeData.Padlock;
        var lockRestraintTT = StickyPair.LastWardrobeData.Padlock.ToPadlock() is Padlocks.None
            ? "Locks the Restraint Set applied to " + PairNickOrAliasOrUID + ". Click to view options."
            : "Set is currently locked with a " + StickyPair.LastWardrobeData.Padlock;
        var unlockRestraintText = "Unlock Restraint Set";
        var unlockRestraintTT = "Unlocks the Restraint Set applied to " + PairNickOrAliasOrUID + ". Click to view options.";
        var removeRestraintText = "Remove Restraint Set";
        var removeRestraintTT = "Removes the Restraint Set applied to " + PairNickOrAliasOrUID + ". Click to view options.";

        // Expander for ApplyRestraint
        var disableApplyExpand = !PairPerms.ApplyRestraintSets || StickyPair.LastWardrobeData.Padlock.ToPadlock() is not Padlocks.None;
        if (_uiShared.IconTextButton(FontAwesomeIcon.Handcuffs, applyRestraintText, WindowMenuWidth, true, disableApplyExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.ApplyRestraint) ? InteractionType.None : InteractionType.ApplyRestraint;
        UiSharedService.AttachToolTip(applyRestraintTT);

        // Interaction Window for ApplyRestraint
        if (PairCombos.Opened is InteractionType.ApplyRestraint)
        {
            using (ImRaii.Child("SetApplyChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.RestraintApplyCombo.DrawComboButton("##ApplyRestraint-" + PairUID, applyRestraintText, WindowMenuWidth, 1.35f, ImGui.GetTextLineHeightWithSpacing());
            ImGui.Separator();
        }

        // Expander for LockRestraint
        var disableLockExpand = StickyPair.LastWardrobeData.ActiveSetId.IsEmptyGuid() || StickyPair.LastWardrobeData.Padlock.ToPadlock() is not Padlocks.None || !PairPerms.LockRestraintSets;
        using (ImRaii.PushColor(ImGuiCol.Text, (StickyPair.LastWardrobeData.Padlock.ToPadlock() is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.Lock, lockRestraintText, WindowMenuWidth, true, disableLockExpand))
                PairCombos.Opened = (PairCombos.Opened == InteractionType.LockRestraint) ? InteractionType.None : InteractionType.LockRestraint;
        }
        UiSharedService.AttachToolTip(lockRestraintTT +
            ((GsPadlockEx.IsTimerLock(StickyPair.LastWardrobeData.Padlock.ToPadlock())) ? "--SEP----COL--" + StickyPair.LastWardrobeData.Timer.ToGsRemainingTimeFancy() : "")
            , color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestraint
        if (PairCombos.Opened is InteractionType.LockRestraint)
        {
            using (ImRaii.Child("SetLockChild", new Vector2(WindowMenuWidth, _pairCombos.RestraintPadlockCombos.PadlockLockWithActiveWindowHeight())))
                _pairCombos.RestraintPadlockCombos.DrawLockComboWithActive(WindowMenuWidth, lockRestraintText, lockRestraintTT);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = StickyPair.LastWardrobeData.Padlock.ToPadlock() is Padlocks.None || !PairPerms.UnlockRestraintSets;
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, unlockRestraintText, WindowMenuWidth, true, disableUnlockExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.UnlockRestraint) ? InteractionType.None : InteractionType.UnlockRestraint;
        UiSharedService.AttachToolTip(unlockRestraintTT);

        // Interaction Window for UnlockRestraint
        if (PairCombos.Opened is InteractionType.UnlockRestraint)
        {
            using (ImRaii.Child("SetUnlockChild", new Vector2(WindowMenuWidth, _pairCombos.RestraintPadlockCombos.PadlockUnlockWindowHeight())))
                _pairCombos.RestraintPadlockCombos.DrawUnlockCombo(WindowMenuWidth, unlockRestraintText, unlockRestraintTT);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = StickyPair.LastWardrobeData.ActiveSetId.IsEmptyGuid() || StickyPair.LastWardrobeData.Padlock.ToPadlock() is not Padlocks.None || !PairPerms.RemoveRestraintSets;
        if (_uiShared.IconTextButton(FontAwesomeIcon.TimesCircle, removeRestraintText, WindowMenuWidth, true, disableRemoveExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.RemoveRestraint) ? InteractionType.None : InteractionType.RemoveRestraint;
        UiSharedService.AttachToolTip(removeRestraintTT);

        // Interaction Window for RemoveRestraint
        if (PairCombos.Opened is InteractionType.RemoveRestraint)
        {
            using (ImRaii.Child("SetRemove", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
            {
                if (ImGui.Button("Remove Restraint", ImGui.GetContentRegionAvail()))
                {
                    var newWardrobeData = StickyPair.LastWardrobeData.DeepClone();
                    if (newWardrobeData is null) return;

                    // update the data to remove the restraint set.
                    var prevSetId = newWardrobeData.ActiveSetId;
                    newWardrobeData.ActiveSetId = Guid.Empty;
                    newWardrobeData.ActiveSetEnabledBy = string.Empty;
                    // send it off then log success.
                    _ = _apiHubMain.UserPushPairDataWardrobeUpdate(new(StickyPair.UserData, MainHub.PlayerUserData, newWardrobeData, WardrobeUpdateType.RestraintDisabled, prevSetId.ToString(), UpdateDir.Other));
                    PairCombos.Opened = InteractionType.None;
                    _logger.LogDebug("Removing Restraint Set from " + PairNickOrAliasOrUID, LoggerType.Permissions);
                }
            }
        }
        ImGui.Separator();
    }
}
