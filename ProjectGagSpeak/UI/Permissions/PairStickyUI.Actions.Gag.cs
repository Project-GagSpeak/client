using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components.Combos;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// </summary>
public partial class PairStickyUI
{
    private void DrawGagActions()
    {
        if(StickyPair.LastAppearanceData is null) return;

        // Drawing out gag layers.
        _pairCombos.DrawGagLayerSelection(ImGui.GetContentRegionAvail().X);

        var gagSlot = StickyPair.LastAppearanceData.GagSlots[PairCombos.GagLayer];

        // register display texts for the buttons.
        var applyGagText = gagSlot.GagType.ToGagType() is GagType.None ? "Apply a Gag to " + PairNickOrAliasOrUID : "A " + gagSlot.GagType + " is applied.";
        var applyGagTT = gagSlot.GagType.ToGagType() is GagType.None ? "Apply a Gag to " + PairNickOrAliasOrUID + ". Click to view options." : "This user is currently Gagged with a " + gagSlot.GagType;
        var lockGagText = gagSlot.Padlock.ToPadlock() is Padlocks.None ? "Lock "+PairNickOrAliasOrUID+"'s Gag" : "Locked with a " + gagSlot.Padlock;
        var lockGagTT = gagSlot.Padlock.ToPadlock() is Padlocks.None ? "Locks the Gag on " + PairNickOrAliasOrUID+ ". Click to view options." : "This Gag is locked with a " + gagSlot.Padlock;

        var unlockGagText = "Unlock " + PairNickOrAliasOrUID + "'s Gag";
        var unlockGagTT = "Unlock " + PairUID + "'s Gag. Click to view options.";
        var removeGagText = "Remove " + PairNickOrAliasOrUID + "'s Gag";
        var removeGagTT = "Remove " + PairNickOrAliasOrUID + "'s Gag. Click to view options.";


        // Expander for ApplyGag
        var disableApplyExpand = !PairPerms.ApplyGags || gagSlot.Padlock.ToPadlock() is not Padlocks.None;
        if (_uiShared.IconTextButton(FontAwesomeIcon.CommentDots, applyGagText, WindowMenuWidth, true, disableApplyExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.ApplyGag) ? InteractionType.None : InteractionType.ApplyGag;
        UiSharedService.AttachToolTip(applyGagTT);

        // Interaction Window for ApplyGag
        if (PairCombos.Opened is InteractionType.ApplyGag)
        {
            using (ImRaii.Child("###GagApply", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.GagApplyCombos[PairCombos.GagLayer].DrawComboButton("##ApplyGag-" + PairCombos.GagLayer, "Select a Gag to apply", WindowMenuWidth, 1.15f, ImGui.GetTextLineHeightWithSpacing());
            ImGui.Separator();
        }

        // Expander for LockGag
        var disableLockExpand = gagSlot.GagType.ToGagType() is GagType.None || gagSlot.Padlock.ToPadlock() is not Padlocks.None || !PairPerms.LockGags;
        using (ImRaii.PushColor(ImGuiCol.Text, (gagSlot.Padlock.ToPadlock() is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.Lock, lockGagText, WindowMenuWidth, true, disableLockExpand))
                PairCombos.Opened = (PairCombos.Opened == InteractionType.LockGag) ? InteractionType.None : InteractionType.LockGag;
        }
        UiSharedService.AttachToolTip(lockGagTT + 
            ((GsPadlockEx.IsTimerLock(gagSlot.Padlock.ToPadlock())) ? "--SEP----COL--" + gagSlot.Timer.ToGsRemainingTimeFancy() : "")
            , color: ImGuiColors.ParsedPink);

        // Interaction Window for LockGag
        if (PairCombos.Opened is InteractionType.LockGag)
        {
            using (ImRaii.Child("###GagLock", new Vector2(WindowMenuWidth, _pairCombos.GagPadlockCombos[PairCombos.GagLayer].PadlockLockWithActiveWindowHeight())))
                _pairCombos.GagPadlockCombos[PairCombos.GagLayer].DrawLockComboWithActive(WindowMenuWidth, lockGagText, lockGagTT);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = gagSlot.Padlock.ToPadlock() is Padlocks.None or Padlocks.MimicPadlock || !PairPerms.UnlockGags;
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, unlockGagText, WindowMenuWidth, true, disableUnlockExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.UnlockGag) ? InteractionType.None : InteractionType.UnlockGag;
        UiSharedService.AttachToolTip(unlockGagTT);

        // Interaction Window for UnlockGag
        if (PairCombos.Opened is InteractionType.UnlockGag)
        {
            using (ImRaii.Child("###GagUnlockNew", new Vector2(WindowMenuWidth, _pairCombos.GagPadlockCombos[PairCombos.GagLayer].PadlockUnlockWindowHeight())))
                _pairCombos.GagPadlockCombos[PairCombos.GagLayer].DrawUnlockCombo(WindowMenuWidth, unlockGagTT, unlockGagText);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = gagSlot.GagType.ToGagType() is GagType.None || gagSlot.Padlock.ToPadlock() is not Padlocks.None || !PairPerms.RemoveGags;
        if (_uiShared.IconTextButton(FontAwesomeIcon.TimesCircle, removeGagText, WindowMenuWidth, true, disableRemoveExpand))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.RemoveGag) ? InteractionType.None : InteractionType.RemoveGag;
        UiSharedService.AttachToolTip(removeGagTT);

        // Interaction Window for RemoveGag
        if (PairCombos.Opened is InteractionType.RemoveGag)
        {
            using (ImRaii.Child("###GagRemove", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
            {
                if (ImGui.Button("Remove Gag", ImGui.GetContentRegionAvail()))
                {
                    var newAppearance = StickyPair.LastAppearanceData.DeepClone();
                    if (newAppearance is null) return;

                    newAppearance.GagSlots[PairCombos.GagLayer].GagType = GagType.None.GagName();
                    _ = _apiHubMain.UserPushPairDataAppearanceUpdate(new(StickyPair.UserData, MainHub.PlayerUserData, newAppearance, (GagLayer)PairCombos.GagLayer, GagUpdateType.GagRemoved, Padlocks.None, UpdateDir.Other));
                    _logger.LogDebug("Removing Gag from "+PairNickOrAliasOrUID, LoggerType.Permissions);
                    PairCombos.Opened = InteractionType.None;
                }
            }
        }
        ImGui.Separator();
    }
}
