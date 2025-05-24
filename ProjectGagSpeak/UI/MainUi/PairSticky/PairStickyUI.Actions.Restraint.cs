using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Permissions;

public partial class PairStickyUI
{
    private int _rsLayerIdx = 0;
    private void DrawWardrobeActions()
    {
        var applyText = "Apply Restraint Set";
        var applyTT = "Applies a Restraint Set to " + PermissionData.DispName + ". Click to select set.";
        var lockText = SPair.LastRestraintData.Padlock is Padlocks.None ? "Lock Restraint Set" : "Locked with a " + SPair.LastRestraintData.Padlock;
        var lockTT = SPair.LastRestraintData.Padlock is Padlocks.None ? "Locks the Restraint Set applied to " + PermissionData.DispName + ". Click to view options." 
            : "Set is currently locked with a " + SPair.LastRestraintData.Padlock;
        var unlockText = "Unlock Restraint Set";
        var unlockTT = "Unlocks the Restraint Set applied to " + PermissionData.DispName + ". Click to view options.";
        var removeText = "Remove Restraint Set";
        var removeTT = "Removes the Restraint Set applied to " + PermissionData.DispName + ". Click to view options.";

        // Expander for ApplyRestraint
        var disableApplyExpand = !SPair.PairPerms.ApplyRestraintSets || SPair.LastRestraintData.Padlock is not Padlocks.None;
        if (CkGui.IconTextButton(FAI.Handcuffs, applyText, WindowMenuWidth, true, disableApplyExpand))
            OpenOrClose(InteractionType.ApplyRestraint);
        CkGui.AttachToolTip(applyTT);

        // Interaction Window for ApplyRestraint
        if (OpenedInteraction is InteractionType.ApplyRestraint)
        {
            using (ImRaii.Child("SetApplyChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairRestrictionItems.DrawComboButton("##PairApplyRestraint", WindowMenuWidth, -1, "Apply", applyText);
            ImGui.Separator();
        }

        // Expander for LockRestraint
        var disableLockExpand = SPair.LastRestraintData.Identifier.IsEmptyGuid() || SPair.LastRestraintData.Padlock is not Padlocks.None || !SPair.PairPerms.LockRestraintSets;
        using (ImRaii.PushColor(ImGuiCol.Text, (SPair.LastRestraintData.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockText, WindowMenuWidth, true, disableLockExpand))
                OpenOrClose(InteractionType.LockRestraint);
        }
        CkGui.AttachToolTip(lockTT +
            (PadlockEx.IsTimerLock(SPair.LastRestraintData.Padlock) ? "--SEP----COL--" + SPair.LastRestraintData.Timer.ToGsRemainingTimeFancy() : "")
            , color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestraint
        if (OpenedInteraction is InteractionType.LockRestraint)
        {
            using (ImRaii.Child("SetLockChild", new Vector2(WindowMenuWidth, _pairRestraintSetPadlocks.PadlockLockWindowHeight())))
                _pairRestraintSetPadlocks.DrawLockComboWithActive("PairLockRestraint", WindowMenuWidth, 0, lockText, lockTT, false);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = SPair.LastRestraintData.Padlock is Padlocks.None || !SPair.PairPerms.UnlockRestraintSets;
        if (CkGui.IconTextButton(FAI.Unlock, unlockText, WindowMenuWidth, true, disableUnlockExpand))
            OpenOrClose(InteractionType.UnlockRestraint);
        CkGui.AttachToolTip(unlockTT);

        // Interaction Window for UnlockRestraint
        if (OpenedInteraction is InteractionType.UnlockRestraint)
        {
            using (ImRaii.Child("SetUnlockChild", new Vector2(WindowMenuWidth, _pairRestraintSetPadlocks.PadlockUnlockWindowHeight(0))))
                _pairRestraintSetPadlocks.DrawUnlockCombo("PairUnlockRestraint", WindowMenuWidth, 0, unlockText, unlockTT);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = SPair.LastRestraintData.Identifier.IsEmptyGuid() || SPair.LastRestraintData.Padlock is not Padlocks.None || !SPair.PairPerms.RemoveRestraintSets;
        if (CkGui.IconTextButton(FAI.TimesCircle, removeText, WindowMenuWidth, true, disableRemoveExpand))
            OpenOrClose(InteractionType.RemoveRestraint);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestraint
        if (OpenedInteraction is InteractionType.RemoveRestraint)
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
                    OpenedInteraction = InteractionType.None;
                    _logger.LogDebug("Removing Restraint from " + PermissionData.DispName, LoggerType.Permissions);
                }
            }
        }
        ImGui.Separator();
    }
}
