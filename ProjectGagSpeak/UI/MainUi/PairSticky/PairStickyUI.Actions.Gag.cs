using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// </summary>
public partial class PairStickyUI
{
    private int _gagLayer = 0;
    private void DrawGagActions()
    {
        // Drawing out gag layers.
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.Combo("##int", ref _gagLayer, ["Layer 1", "Layer 2", "Layer 3"], 3);
        CkGui.AttachToolTip("Select the layer to apply a Gag to.");

        var gagSlot = SPair.LastGagData.GagSlots[_gagLayer];

        // register display texts for the buttons.
        var applyText = gagSlot.GagItem is GagType.None ? "Apply a Gag to " + DisplayName : "A " + gagSlot.GagItem + " is applied.";
        var applyTT = gagSlot.GagItem is GagType.None ? "Apply a Gag to " + DisplayName + ". Click to view options." : "This user is currently Gagged with a " + gagSlot.GagItem;
        var lockText = gagSlot.Padlock is Padlocks.None ? "Lock "+DisplayName+"'s Gag" : "Locked with a " + gagSlot.Padlock;
        var lockTT = gagSlot.Padlock is Padlocks.None ? "Locks the Gag on " + DisplayName+ ". Click to view options." : "This Gag is locked with a " + gagSlot.Padlock;
        var unlockText = "Unlock " + DisplayName + "'s Gag";
        var unlockTT = "Unlock " + DisplayName + "'s Gag. Click to view options.";
        var removeText = "Remove " + DisplayName + "'s Gag";
        var removeTT = "Remove " + DisplayName + "'s Gag. Click to view options.";


        // Expander for ApplyGag
        var disableApplyExpand = !SPair.PairPerms.ApplyGags || gagSlot.Padlock is not Padlocks.None;
        if (CkGui.IconTextButton(FAI.CommentDots, applyText, WindowMenuWidth, true, disableApplyExpand))
            OpenOrClose(InteractionType.ApplyGag);
        CkGui.AttachToolTip(applyTT);

        // Interaction Window for ApplyGag
        if (OpenedInteraction is InteractionType.ApplyGag)
        {
            using (ImRaii.Child("##GagApply", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairGags.DrawComboButton("##PairApplyGag", WindowMenuWidth, _gagLayer, "Apply", "Select a Gag to Apply");
            ImGui.Separator();
        }

        // Expander for LockGag
        var disableLockExpand = gagSlot.GagItem is GagType.None || gagSlot.Padlock is not Padlocks.None || !SPair.PairPerms.LockGags;
        using (ImRaii.PushColor(ImGuiCol.Text, gagSlot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockText, WindowMenuWidth, true, disableLockExpand))
                OpenOrClose(InteractionType.LockGag);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(gagSlot.Padlock) ? "--SEP----COL--" + gagSlot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        // Interaction Window for LockGag
        if (OpenedInteraction is InteractionType.LockGag)
        {
            using (ImRaii.Child("##GagLock", new Vector2(WindowMenuWidth, _pairGagPadlocks.PadlockLockWindowHeight())))
                _pairGagPadlocks.DrawLockComboWithActive("PairGagLock", WindowMenuWidth, _gagLayer, lockText, lockTT, false);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = gagSlot.Padlock is Padlocks.None or Padlocks.MimicPadlock || !SPair.PairPerms.UnlockGags;
        if (CkGui.IconTextButton(FAI.Unlock, unlockText, WindowMenuWidth, true, disableUnlockExpand))
            OpenOrClose(InteractionType.UnlockGag);
        CkGui.AttachToolTip(unlockTT);

        // Interaction Window for UnlockGag
        if (OpenedInteraction is InteractionType.UnlockGag)
        {
            using (ImRaii.Child("##GagUnlockNew", new Vector2(WindowMenuWidth, _pairGagPadlocks.PadlockUnlockWindowHeight(_gagLayer))))
                _pairGagPadlocks.DrawUnlockCombo("PairGagUnlock", WindowMenuWidth, _gagLayer, unlockTT, unlockText);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = gagSlot.GagItem is GagType.None || gagSlot.Padlock is not Padlocks.None || !SPair.PairPerms.RemoveGags;
        if (CkGui.IconTextButton(FAI.TimesCircle, removeText, WindowMenuWidth, true, disableRemoveExpand))
            OpenOrClose(InteractionType.RemoveGag);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveGag
        if (OpenedInteraction is InteractionType.RemoveGag)
        {
            using (ImRaii.Child("###GagRemove", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
            {
                if (ImGui.Button("Remove Gag", ImGui.GetContentRegionAvail()))
                {
                    // construct the dto to send.
                    var dto = new PushKinksterGagSlotUpdate(SPair.UserData, DataUpdateType.Removed)
                    {
                        Layer = _gagLayer,
                        Gag = GagType.None,
                        Enabler = MainHub.UID,
                    };

                    // push to server.
                    _hub.UserChangeKinksterGagState(dto).ConfigureAwait(false);
                    OpenedInteraction = InteractionType.None;
                    _logger.LogDebug("Removing Gag From layer " + _gagLayer, LoggerType.Permissions);
                }
            }
        }
        ImGui.Separator();
    }
}
