using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// </summary>
public partial class PairStickyUI
{
    private void DrawGagActions()
    {
        // Drawing out gag layers.
        _pairCombos.DrawGagLayerSelection(ImGui.GetContentRegionAvail().X);

        var gagSlot = SPair.LastGagData.GagSlots[_pairCombos.CurGagLayer];

        // register display texts for the buttons.
        var applyGagText = gagSlot.GagItem is GagType.None ? "Apply a Gag to " + PermissionData.DispName : "A " + gagSlot.GagItem + " is applied.";
        var applyGagTT = gagSlot.GagItem is GagType.None ? "Apply a Gag to " + PermissionData.DispName + ". Click to view options." : "This user is currently Gagged with a " + gagSlot.GagItem;
        var lockGagText = gagSlot.Padlock is Padlocks.None ? "Lock "+PermissionData.DispName+"'s Gag" : "Locked with a " + gagSlot.Padlock;
        var lockGagTT = gagSlot.Padlock is Padlocks.None ? "Locks the Gag on " + PermissionData.DispName+ ". Click to view options." : "This Gag is locked with a " + gagSlot.Padlock;

        var unlockGagText = "Unlock " + PermissionData.DispName + "'s Gag";
        var unlockGagTT = "Unlock " + PermissionData.DispName + "'s Gag. Click to view options.";
        var removeGagText = "Remove " + PermissionData.DispName + "'s Gag";
        var removeGagTT = "Remove " + PermissionData.DispName + "'s Gag. Click to view options.";


        // Expander for ApplyGag
        var disableApplyExpand = !SPair.PairPerms.ApplyGags || gagSlot.Padlock is not Padlocks.None;
        if (CkGui.IconTextButton(FAI.CommentDots, applyGagText, WindowMenuWidth, true, disableApplyExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.ApplyGag) ? InteractionType.None : InteractionType.ApplyGag;
        CkGui.AttachToolTip(applyGagTT);

        // Interaction Window for ApplyGag
        if (PairCombos.Opened is InteractionType.ApplyGag)
        {
            using (ImRaii.Child("###GagApply", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.GagItemCombo.DrawComboButton("##PairApplyGag", WindowMenuWidth, _pairCombos.CurGagLayer, "Apply", "Select a Gag to Apply");
            ImGui.Separator();
        }

        // Expander for LockGag
        var disableLockExpand = gagSlot.GagItem is GagType.None || gagSlot.Padlock is not Padlocks.None || !SPair.PairPerms.LockGags;
        using (ImRaii.PushColor(ImGuiCol.Text, (gagSlot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockGagText, WindowMenuWidth, true, disableLockExpand))
                PairCombos.Opened = (PairCombos.Opened == InteractionType.LockGag) ? InteractionType.None : InteractionType.LockGag;
        }
        CkGui.AttachToolTip(lockGagTT + 
            ((GsPadlockEx.IsTimerLock(gagSlot.Padlock)) ? "--SEP----COL--" + gagSlot.Timer.ToGsRemainingTimeFancy() : "")
            , color: ImGuiColors.ParsedPink);

        // Interaction Window for LockGag
        if (PairCombos.Opened is InteractionType.LockGag)
        {
            using (ImRaii.Child("###GagLock", new Vector2(WindowMenuWidth, _pairCombos.GagPadlockCombo.PadlockLockWindowHeight())))
                _pairCombos.GagPadlockCombo.DrawLockComboWithActive("PairGagLock", WindowMenuWidth, _pairCombos.CurGagLayer, lockGagText, lockGagTT, false);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = gagSlot.Padlock is Padlocks.None or Padlocks.MimicPadlock || !SPair.PairPerms.UnlockGags;
        if (CkGui.IconTextButton(FAI.Unlock, unlockGagText, WindowMenuWidth, true, disableUnlockExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.UnlockGag) ? InteractionType.None : InteractionType.UnlockGag;
        CkGui.AttachToolTip(unlockGagTT);

        // Interaction Window for UnlockGag
        if (PairCombos.Opened is InteractionType.UnlockGag)
        {
            using (ImRaii.Child("###GagUnlockNew", new Vector2(WindowMenuWidth, _pairCombos.GagPadlockCombo.PadlockUnlockWindowHeight())))
                _pairCombos.GagPadlockCombo.DrawUnlockCombo("PairGagUnlock", WindowMenuWidth, _pairCombos.CurGagLayer, unlockGagTT, unlockGagText);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = gagSlot.GagItem is GagType.None || gagSlot.Padlock is not Padlocks.None || !SPair.PairPerms.RemoveGags;
        if (CkGui.IconTextButton(FAI.TimesCircle, removeGagText, WindowMenuWidth, true, disableRemoveExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.RemoveGag) ? InteractionType.None : InteractionType.RemoveGag;
        CkGui.AttachToolTip(removeGagTT);

        // Interaction Window for RemoveGag
        if (PairCombos.Opened is InteractionType.RemoveGag)
        {
            using (ImRaii.Child("###GagRemove", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
            {
                if (ImGui.Button("Remove Gag", ImGui.GetContentRegionAvail()))
                {
                    // construct the dto to send.
                    var dto = new PushPairGagDataUpdateDto(_permData.PairUserData, DataUpdateType.Removed)
                    {
                        Layer = (GagLayer)_pairCombos.CurGagLayer,
                        Gag = GagType.None,
                        Enabler = MainHub.UID,
                    };

                    // push to server.
                    _hub.UserPushPairDataGags(dto).ConfigureAwait(false);
                    PairCombos.Opened = InteractionType.None;
                    _logger.LogDebug("Removing Gag From layer " + (GagLayer)_pairCombos.CurGagLayer, LoggerType.Permissions);
                }
            }
        }
        ImGui.Separator();
    }
}
