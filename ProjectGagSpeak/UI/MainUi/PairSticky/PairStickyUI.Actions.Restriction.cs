using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Permissions;

public partial class PairStickyUI
{
    private int _restrictionLayer = 0;
    private void DrawRestrictionActions()
    {

        // Drawing out restriction layers.
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.Combo("##RestrictionLayer", ref _restrictionLayer, ["Layer 1", "Layer 2", "Layer 3", "Layer 4", "Layer 5"], 5);
        CkGui.AttachToolTip("Select the layer to apply a Restriction to.");

        var curSlot = SPair.LastRestrictionsData.Restrictions[_restrictionLayer];

        // register display texts for the buttons.
        var applyText = "Apply Restriction";
        var applyTT = "Applies a Restriction to " + DisplayName + ". Click to select set.";
        var lockText = curSlot.Padlock is Padlocks.None ? "Lock "+DisplayName+"'s Restriction" : "Locked with a " + curSlot.Padlock;
        var lockTT = curSlot.Padlock is Padlocks.None ? "Locks the Restriction on " + DisplayName+ ". Click to view options." : "This Restriction is locked with a " + curSlot.Padlock;
        var unlockText = "Unlock " + DisplayName + "'s Restriction";
        var unlockTT = "Unlock " + DisplayName + "'s Restriction. Click to view options.";
        var removeText = "Remove " + DisplayName + "'s Restriction";
        var removeTT = "Remove " + DisplayName + "'s Restriction. Click to view options.";


        // Expander for ApplyRestriction
        if (CkGui.IconTextButton(FAI.CommentDots, applyText, WindowMenuWidth, true, !curSlot.CanApply() || !SPair.PairPerms.ApplyRestrictions))
            OpenOrClose(InteractionType.ApplyRestriction);
        CkGui.AttachToolTip(applyTT);

        // Interaction Window for ApplyRestriction
        if (OpenedInteraction is InteractionType.ApplyRestriction)
        {
            using (ImRaii.Child("###RestrictionApply", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairRestrictionItems.DrawComboButton("##PairApplyRestriction", WindowMenuWidth, _restrictionLayer, "Apply", "Select a Restriction to apply");
            ImGui.Separator();
        }

        // Expander for LockRestriction
        using (ImRaii.PushColor(ImGuiCol.Text, (curSlot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockText, WindowMenuWidth, true, !curSlot.CanLock() || !SPair.PairPerms.LockRestrictions))
                OpenOrClose(InteractionType.LockRestriction);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(curSlot.Padlock) ? "--SEP----COL--" + curSlot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestriction
        if (OpenedInteraction is InteractionType.LockRestriction)
        {
            using (ImRaii.Child("###RestrictionLock", new Vector2(WindowMenuWidth, _pairRestrictionPadlocks.PadlockLockWindowHeight())))
                _pairRestrictionPadlocks.DrawLockComboWithActive("PairLockRestriction", WindowMenuWidth, _restrictionLayer, lockText, lockTT, false);
            ImGui.Separator();
        }

        // Expander for unlocking.
        if (CkGui.IconTextButton(FAI.Unlock, unlockText, WindowMenuWidth, true, !curSlot.CanUnlock() || !SPair.PairPerms.UnlockRestrictions))
            OpenOrClose(InteractionType.UnlockRestriction);
        CkGui.AttachToolTip(unlockTT);

        // Interaction Window for UnlockRestriction
        if (OpenedInteraction is InteractionType.UnlockRestriction)
        {
            using (ImRaii.Child("###RestrictionUnlockNew", new Vector2(WindowMenuWidth, _pairRestrictionPadlocks.PadlockUnlockWindowHeight(_restrictionLayer))))
                _pairRestrictionPadlocks.DrawUnlockCombo("PairUnlockRestriction", WindowMenuWidth, _restrictionLayer, unlockTT, unlockText);
            ImGui.Separator();
        }

        // Expander for removing.
        if (CkGui.IconTextButton(FAI.TimesCircle, removeText, WindowMenuWidth, true, !curSlot.CanRemove() || !SPair.PairPerms.RemoveRestrictions))
            OpenOrClose(InteractionType.RemoveRestriction);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestriction
        if (OpenedInteraction is InteractionType.RemoveRestriction)
        {
            using (ImRaii.Child("###RestrictionRemove", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
            {
                if (ImGui.Button("Remove Restriction", ImGui.GetContentRegionAvail()))
                {
                    // construct the dto to send.
                    var dto = new PushKinksterRestrictionUpdate(SPair.UserData, DataUpdateType.Removed)
                    {
                        Layer = _restrictionLayer,
                        RestrictionId = Guid.Empty,
                        Enabler = string.Empty,
                    };

                    // push to server.
                    _hub.UserChangeKinksterRestrictionState(dto).ConfigureAwait(false);
                    _logger.LogDebug("Removing Restriction From layer " + _restrictionLayer, LoggerType.StickyUI);
                    CloseInteraction();
                }
            }
        }
        ImGui.Separator();
    }
}
