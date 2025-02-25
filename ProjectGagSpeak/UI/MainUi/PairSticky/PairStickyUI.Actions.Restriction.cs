using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components;
using GagSpeak.UI.Components.Combos;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Permissions;

public partial class PairStickyUI
{
    private void DrawRestrictionActions()
    {

        // Drawing out restriction layers.
        _pairCombos.DrawRestrictionLayerSelection(ImGui.GetContentRegionAvail().X);

        var restrictionSlot = SPair.LastRestrictionsData.Restrictions[_pairCombos.CurRestrictionLayer];

        // register display texts for the buttons.
        var applyRestrictionText = "Apply Restriction";
        var applyRestrictionTT = "Applies a Restriction to " + PermActData.DispName + ". Click to select set.";
        var lockRestrictionText = restrictionSlot.Padlock is Padlocks.None ? "Lock "+PermActData.DispName+"'s Restriction" : "Locked with a " + restrictionSlot.Padlock;
        var lockRestrictionTT = restrictionSlot.Padlock is Padlocks.None ? "Locks the Restriction on " + PermActData.DispName+ ". Click to view options." : "This Restriction is locked with a " + restrictionSlot.Padlock;

        var unlockRestrictionText = "Unlock " + PermActData.DispName + "'s Restriction";
        var unlockRestrictionTT = "Unlock " + PermActData.DispName + "'s Restriction. Click to view options.";
        var removeRestrictionText = "Remove " + PermActData.DispName + "'s Restriction";
        var removeRestrictionTT = "Remove " + PermActData.DispName + "'s Restriction. Click to view options.";


        // Expander for ApplyRestriction
        if (_ui.IconTextButton(FontAwesomeIcon.CommentDots, applyRestrictionText, WindowMenuWidth, true, !restrictionSlot.CanApply() || !SPair.PairPerms.ApplyRestrictions))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.ApplyRestriction) ? InteractionType.None : InteractionType.ApplyRestriction;
        UiSharedService.AttachToolTip(applyRestrictionTT);

        // Interaction Window for ApplyRestriction
        if (PairCombos.Opened is InteractionType.ApplyRestriction)
        {
            using (ImRaii.Child("###RestrictionApply", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.RestrictionApplyCombo.DrawComboButton("##ApplyRestriction-" + _pairCombos.CurRestrictionLayer, "Select a Restriction to apply", WindowMenuWidth, ImGui.GetTextLineHeightWithSpacing());
            ImGui.Separator();
        }

        // Expander for LockRestriction
        using (ImRaii.PushColor(ImGuiCol.Text, (restrictionSlot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (_ui.IconTextButton(FontAwesomeIcon.Lock, lockRestrictionText, WindowMenuWidth, true, !restrictionSlot.CanLock() || !SPair.PairPerms.LockRestrictions))
                PairCombos.Opened = (PairCombos.Opened == InteractionType.LockRestriction) ? InteractionType.None : InteractionType.LockRestriction;
        }
        UiSharedService.AttachToolTip(lockRestrictionTT + 
            ((GsPadlockEx.IsTimerLock(restrictionSlot.Padlock)) ? "--SEP----COL--" + restrictionSlot.Timer.ToGsRemainingTimeFancy() : "")
            , color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestriction
        if (PairCombos.Opened is InteractionType.LockRestriction)
        {
            using (ImRaii.Child("###RestrictionLock", new Vector2(WindowMenuWidth, _pairCombos.RestrictionPadlockCombo.PadlockLockWindowHeight())))
                _pairCombos.RestrictionPadlockCombo.DrawLockComboWithActive(WindowMenuWidth, lockRestrictionText, lockRestrictionTT);
            ImGui.Separator();
        }

        // Expander for unlocking.
        if (_ui.IconTextButton(FontAwesomeIcon.Unlock, unlockRestrictionText, WindowMenuWidth, true, !restrictionSlot.CanUnlock() || !SPair.PairPerms.UnlockRestrictions))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.UnlockRestriction) ? InteractionType.None : InteractionType.UnlockRestriction;
        UiSharedService.AttachToolTip(unlockRestrictionTT);

        // Interaction Window for UnlockRestriction
        if (PairCombos.Opened is InteractionType.UnlockRestriction)
        {
            using (ImRaii.Child("###RestrictionUnlockNew", new Vector2(WindowMenuWidth, _pairCombos.RestrictionPadlockCombo.PadlockUnlockWindowHeight())))
                _pairCombos.RestrictionPadlockCombo.DrawUnlockCombo(WindowMenuWidth, unlockRestrictionTT, unlockRestrictionText);
            ImGui.Separator();
        }

        // Expander for removing.
        if (_ui.IconTextButton(FontAwesomeIcon.TimesCircle, removeRestrictionText, WindowMenuWidth, true, !restrictionSlot.CanRemove() || !SPair.PairPerms.RemoveRestrictions))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.RemoveRestriction) ? InteractionType.None : InteractionType.RemoveRestriction;
        UiSharedService.AttachToolTip(removeRestrictionTT);

        // Interaction Window for RemoveRestriction
        if (PairCombos.Opened is InteractionType.RemoveRestriction)
        {
            using (ImRaii.Child("###RestrictionRemove", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
            {
                if (ImGui.Button("Remove Restriction", ImGui.GetContentRegionAvail()))
                {
                    // construct the dto to send.
                    var dto = new PushPairRestrictionDataUpdateDto(_permData.PairUserData, DataUpdateType.Removed)
                    {
                        AffectedIndex = _pairCombos.CurRestrictionLayer,
                        RestrictionId = Guid.Empty,
                        Enabler = string.Empty,
                    };

                    // push to server.
                    _hub.UserPushPairDataRestrictions(dto).ConfigureAwait(false);
                    PairCombos.Opened = InteractionType.None;
                    _logger.LogDebug("Removing Restriction From layer " + _pairCombos.CurRestrictionLayer, LoggerType.Permissions);
                }
            }
        }
        ImGui.Separator();
    }
}
