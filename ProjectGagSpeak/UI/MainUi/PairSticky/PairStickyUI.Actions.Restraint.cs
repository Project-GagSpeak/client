using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Permissions;

public partial class PairStickyUI
{
    private void DrawWardrobeActions()
    {
        var applyRestraintText = "Apply Restraint Set";
        var applyRestraintTT = "Applies a Restraint Set to " + PermissionData.DispName + ". Click to select set.";
        var lockRestraintText = SPair.LastRestraintData.Padlock is Padlocks.None
            ? "Lock Restraint Set" : "Locked with a " + SPair.LastRestraintData.Padlock;
        var lockRestraintTT = SPair.LastRestraintData.Padlock is Padlocks.None
            ? "Locks the Restraint Set applied to " + PermissionData.DispName + ". Click to view options."
            : "Set is currently locked with a " + SPair.LastRestraintData.Padlock;
        var unlockRestraintText = "Unlock Restraint Set";
        var unlockRestraintTT = "Unlocks the Restraint Set applied to " + PermissionData.DispName + ". Click to view options.";
        var removeRestraintText = "Remove Restraint Set";
        var removeRestraintTT = "Removes the Restraint Set applied to " + PermissionData.DispName + ". Click to view options.";

        // Expander for ApplyRestraint
        var disableApplyExpand = !SPair.PairPerms.ApplyRestraintSets || SPair.LastRestraintData.Padlock is not Padlocks.None;
        if (CkGui.IconTextButton(FAI.Handcuffs, applyRestraintText, WindowMenuWidth, true, disableApplyExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.ApplyRestraint) ? InteractionType.None : InteractionType.ApplyRestraint;
        CkGui.AttachToolTip(applyRestraintTT);

        // Interaction Window for ApplyRestraint
        if (PairCombos.Opened is InteractionType.ApplyRestraint)
        {
            using (ImRaii.Child("SetApplyChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.RestraintItemCombo.DrawComboButton("##PairApplyRestraint", WindowMenuWidth, -1, "Apply", applyRestraintText);
            ImGui.Separator();
        }

        // Expander for LockRestraint
        var disableLockExpand = SPair.LastRestraintData.Identifier.IsEmptyGuid() || SPair.LastRestraintData.Padlock is not Padlocks.None || !SPair.PairPerms.LockRestraintSets;
        using (ImRaii.PushColor(ImGuiCol.Text, (SPair.LastRestraintData.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockRestraintText, WindowMenuWidth, true, disableLockExpand))
                PairCombos.Opened = (PairCombos.Opened == InteractionType.LockRestraint) ? InteractionType.None : InteractionType.LockRestraint;
        }
        CkGui.AttachToolTip(lockRestraintTT +
            ((PadlockEx.IsTimerLock(SPair.LastRestraintData.Padlock)) ? "--SEP----COL--" + SPair.LastRestraintData.Timer.ToGsRemainingTimeFancy() : "")
            , color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestraint
        if (PairCombos.Opened is InteractionType.LockRestraint)
        {
            using (ImRaii.Child("SetLockChild", new Vector2(WindowMenuWidth, _pairCombos.RestraintPadlockCombo.PadlockLockWindowHeight())))
                _pairCombos.RestraintPadlockCombo.DrawLockComboWithActive("PairLockRestraint", WindowMenuWidth, 0, lockRestraintText, lockRestraintTT, false);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = SPair.LastRestraintData.Padlock is Padlocks.None || !SPair.PairPerms.UnlockRestraintSets;
        if (CkGui.IconTextButton(FAI.Unlock, unlockRestraintText, WindowMenuWidth, true, disableUnlockExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.UnlockRestraint) ? InteractionType.None : InteractionType.UnlockRestraint;
        CkGui.AttachToolTip(unlockRestraintTT);

        // Interaction Window for UnlockRestraint
        if (PairCombos.Opened is InteractionType.UnlockRestraint)
        {
            using (ImRaii.Child("SetUnlockChild", new Vector2(WindowMenuWidth, _pairCombos.RestraintPadlockCombo.PadlockUnlockWindowHeight())))
                _pairCombos.RestraintPadlockCombo.DrawUnlockCombo("PairUnlockRestraint", WindowMenuWidth, 0, unlockRestraintText, unlockRestraintTT);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = SPair.LastRestraintData.Identifier.IsEmptyGuid() || SPair.LastRestraintData.Padlock is not Padlocks.None || !SPair.PairPerms.RemoveRestraintSets;
        if (CkGui.IconTextButton(FAI.TimesCircle, removeRestraintText, WindowMenuWidth, true, disableRemoveExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.RemoveRestraint) ? InteractionType.None : InteractionType.RemoveRestraint;
        CkGui.AttachToolTip(removeRestraintTT);

        // Interaction Window for RemoveRestraint
        if (PairCombos.Opened is InteractionType.RemoveRestraint)
        {
            using (ImRaii.Child("SetRemove", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
            {
                if (ImGui.Button("Remove Restraint", ImGui.GetContentRegionAvail()))
                {
                    // construct the dto to send.
                    var dto = new PushPairRestraintDataUpdateDto(_permData.PairUserData, DataUpdateType.Removed)
                    {
                        ActiveSetId = Guid.Empty,
                        Enabler = string.Empty,
                    };
                    _hub.UserPushPairDataRestraint(dto).ConfigureAwait(false);
                    PairCombos.Opened = InteractionType.None;
                    _logger.LogDebug("Removing Restraint from " + PermissionData.DispName, LoggerType.Permissions);
                }
            }
        }
        ImGui.Separator();
    }
}
