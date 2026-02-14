using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

public partial class SidePanelPair
{
    #region Gags
    private void DrawGagActions(KinksterInfoCache cache, Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Gag Actions");

        if (CkGuiUtils.LayerIdxCombo("##gagLayer", width, cache.GagLayer, out var newVal, 3))
            cache.GagLayer = newVal;
        CkGui.AttachToolTip("Select the layer to apply a Gag to.");

        if (k.ActiveGags.GagSlots[cache.GagLayer] is not { } slot)
            return;

        var hasGag = slot.GagItem is not GagType.None;
        var hasPadlock = slot.Padlock is not Padlocks.None;
        var applyTxt = hasGag ? $"A {slot.GagItem} is applied." : $"Apply a Gag to {dispName}";
        var applyTT = hasGag ? $"This user is currently Gagged with a {slot.GagItem.GagName()}." : $"Apply a Gag to {dispName}.";
        var lockTxt = hasPadlock ? $"Locked with a {slot.Padlock.ToName()}" : hasGag
            ? $"Lock {dispName}'s Gag" : "No Gag To Lock!";
        var lockTT = hasPadlock ? $"This Gag is locked with a {slot.Padlock.ToName()}" : hasGag
            ? $"Locks the Gag on {dispName}." : "Cannot lock a Gag that is not applied.";

        var unlockTxt = hasPadlock ? $"Unlock {dispName}'s Gag" : "No Padlock to unlock!";
        var unlockTT = hasPadlock ? $"Attempt to unlock {dispName}'s Gag." : "Cannot unlock a Gag that is not locked!";
        var removeTxt = hasGag ? $"Remove {dispName}'s Gag" : "Nothing to remove!";
        var removeTT = $"{removeTxt}.";


        // Applying.
        if (CkGui.IconTextButton(FAI.CommentDots, applyTxt, width, true, !k.PairPerms.ApplyGags || !slot.CanApply()))
            cache.ToggleInteraction(InteractionType.ApplyGag);
        CkGui.AttachToolTip(applyTT);

        if (cache.OpenItem is InteractionType.ApplyGag)
        {
            using (ImRaii.Child("###ApplyGag", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Gags.DrawComboButton("##ApplyGag", width, cache.GagLayer, "Apply", "Select a Gag to Apply");
            ImGui.Separator();
        }


        // Locking
        using (ImRaii.PushColor(ImGuiCol.Text, slot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, !k.PairPerms.LockGags || !slot.CanLock()))
                cache.ToggleInteraction(InteractionType.LockGag);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(slot.Padlock) ? "--SEP----COL--" + slot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        if (cache.OpenItem is InteractionType.LockGag)
        {
            using (ImRaii.Child("###LockGag", new Vector2(width, CkStyle.TwoRowHeight())))
                cache.GagLocks.DrawLockCombo("##LockGag", width, cache.GagLayer, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Unlocking.
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, !slot.CanUnlock() || !k.PairPerms.UnlockGags))
            cache.ToggleInteraction(InteractionType.UnlockGag);
        CkGui.AttachToolTip(unlockTT);

        if (cache.OpenItem is InteractionType.UnlockGag)
        {
            using (ImRaii.Child("###UnlockGag", new Vector2(width, ImGui.GetFrameHeight())))
                cache.GagLocks.DrawUnlockCombo("##UnlockGag", width, cache.GagLayer, unlockTT, unlockTxt);
            ImGui.Separator();
        }


        // Removing.
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, !slot.CanRemove() || !k.PairPerms.RemoveGags))
            cache.ToggleInteraction(InteractionType.RemoveGag);
        CkGui.AttachToolTip(removeTT);

        if (cache.OpenItem is InteractionType.RemoveGag)
        {
            if (ImGui.Button("Remove Gag", new Vector2(width, ImGui.GetFrameHeight())))
            {
                var dto = new PushKinksterActiveGagSlot(k.UserData, DataUpdateType.Removed)
                {
                    Layer = cache.GagLayer,
                    Gag = GagType.None,
                    Enabler = MainHub.UID,
                };

                UiService.SetUITask(async () =>
                {
                    var result = await _hub.UserChangeKinksterActiveGag(dto).ConfigureAwait(false);
                    if (result.ErrorCode is not GagSpeakApiEc.Success)
                    {
                        _logger.LogDebug($"Failed to Remove ({slot.GagItem.GagName()}) on {dispName}, Reason:{result}", LoggerType.StickyUI);
                        return;
                    }
                    else
                    {
                        _logger.LogDebug($"Removed ({slot.GagItem.GagName()}) from {dispName}", LoggerType.StickyUI);
                        cache.ClearInteraction();
                    }
                });
            }
        }
    }
    #endregion Gags

    #region Restrictions
    private void DrawRestrictionActions(KinksterInfoCache cache, Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Restriction Actions");

        // Drawing out restriction layers.
        if (CkGuiUtils.LayerIdxCombo("##restrictionLayer", width, cache.RestrictionLayer, out var newVal, 5))
            cache.RestrictionLayer = newVal;
        CkGui.AttachToolTip("Select the layer to apply a Restriction to.");

        if (k.ActiveRestrictions.Restrictions[cache.RestrictionLayer] is not { } slot)
            return;

        // register display texts for the buttons.
        var hasItem = slot.Identifier != Guid.Empty;
        var itemName = k.LightCache.Restrictions.TryGetValue(slot.Identifier, out var item) ? item.Label : string.Empty;
        var hasPadlock = slot.Padlock is not Padlocks.None;
        var applyTxt = hasItem ? $"Applied Item: {itemName}" : $"Apply a Restriction to {dispName}";
        var applyTT = $"Applies a Restriction to {dispName}.";
        var lockTxt = hasPadlock ? $"Locked with a {slot.Padlock.ToName()}" : hasItem
            ? $"Lock {dispName}'s {itemName}" : "No Restriction To Lock!";
        var lockTT = hasPadlock ? $"This Restriction is locked with a {slot.Padlock.ToName()}" : hasItem
            ? $"Locks {dispName}'s {itemName} Restriction item." : "No Restriction to lock on this layer!";
        var unlockTxt = hasPadlock ? $"Unlock {dispName}'s {itemName}" : "No Padlock to unlock!";
        var unlockTT = hasPadlock ? $"Attempt to unlock {dispName}'s {itemName} Restriction item." : "No padlock is set!";
        var removeTxt = hasItem ? $"Remove {dispName}'s {itemName}" : "Nothing to remove!";
        var removeTT = $"{removeTxt}.";

        // Expander for ApplyRestriction
        if (CkGui.IconTextButton(FAI.CommentDots, applyTxt, width, true, !slot.CanApply() || !k.PairPerms.ApplyRestrictions))
            cache.ToggleInteraction(InteractionType.ApplyRestriction);
        CkGui.AttachToolTip(applyTT);

        if (cache.OpenItem is InteractionType.ApplyRestriction)
        {
            using (ImRaii.Child("###ApplyRestriction", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Restrictions.DrawComboButton("##ApplyRestriction", width, cache.RestrictionLayer, "Apply", "Select a Restriction to apply");
            ImGui.Separator();
        }

        // Expander for LockRestriction
        using (ImRaii.PushColor(ImGuiCol.Text, (slot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, !slot.CanLock() || !k.PairPerms.LockRestrictions))
                cache.ToggleInteraction(InteractionType.LockRestriction);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(slot.Padlock) ? "--SEP----COL--" + slot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        if (cache.OpenItem is InteractionType.LockRestriction)
        {
            using (ImRaii.Child("###LockRestriction", new Vector2(width, CkStyle.TwoRowHeight())))
                cache.RestrictionLocks.DrawLockCombo("##LockRestriction", width, cache.RestrictionLayer, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Expander for unlocking.
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, !slot.CanUnlock() || !k.PairPerms.UnlockRestrictions))
            cache.ToggleInteraction(InteractionType.UnlockRestriction);
        CkGui.AttachToolTip(unlockTT);

        if (cache.OpenItem is InteractionType.UnlockRestriction)
        {
            using (ImRaii.Child("###UnlockRestriction", new Vector2(width, ImGui.GetFrameHeight())))
                cache.RestrictionLocks.DrawUnlockCombo("##UnlockRestriction", width, cache.RestrictionLayer, unlockTT, unlockTxt);
            ImGui.Separator();
        }

        // Expander for removing.
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, !slot.CanRemove() || !k.PairPerms.RemoveRestrictions))
            cache.ToggleInteraction(InteractionType.RemoveRestriction);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestriction
        if (cache.OpenItem is InteractionType.RemoveRestriction)
        {
            if (ImGui.Button("Remove Restriction", new Vector2(width, ImGui.GetFrameHeight())))
            {
                var dto = new PushKinksterActiveRestriction(k.UserData, DataUpdateType.Removed) { Layer = cache.RestrictionLayer };
                UiService.SetUITask(async () =>
                {
                    var result = await _hub.UserChangeKinksterActiveRestriction(dto).ConfigureAwait(false);
                    if (result.ErrorCode is not GagSpeakApiEc.Success)
                    {
                        _logger.LogDebug($"Failed to Remove Restriction Item on {dispName}, Reason:{result}", LoggerType.StickyUI);
                        return;
                    }
                    else
                    {
                        _logger.LogDebug($"Removed Restriction Item from {dispName} on layer {cache.RestrictionLayer}", LoggerType.StickyUI);
                        cache.ClearInteraction();
                    }
                });
            }
        }
    }
    #endregion Restrictions

    #region Restraints
    private void DrawRestraintActions(KinksterInfoCache cache, Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Restraint Actions");
        var itemName = string.Empty;
        var itemLayers = 0;
        var allLayersSet = false;

        if (k.LightCache.Restraints.TryGetValue(k.ActiveRestraint.Identifier, out var item))
        {
            itemName = item.Label;
            itemLayers = item.Layers.Count;
            allLayersSet = ((int)k.ActiveRestraint.ActiveLayers & ((1 << itemLayers) - 1)) == ((1 << itemLayers) - 1);
        }

        var hasItem = k.ActiveRestraint.Identifier != Guid.Empty;
        var hasPadlock = k.ActiveRestraint.Padlock is not Padlocks.None;

        var applyTxt = hasItem ? $"Applied Set: {itemName}" : $"Apply a Restraint to {dispName}";
        var applyTT = $"Applies a Restraint to {dispName}.";
        var applyLayerText = hasItem ? $"Add Layer(s) to {dispName}'s Set" : "Layers Currently Inaccessible.";
        var applyLayerTT = hasItem ? $"Apply a Restraint Layer to {dispName}'s Restraint Set." : "Must apply a Restraint Set first!";
        var lockTxt = hasPadlock ? $"Locked with a {k.ActiveRestraint.Padlock.ToName()}" : hasItem
            ? $"Lock {dispName}'s Restraint Set" : "No Restraint Set to lock!";
        var lockTT = hasPadlock ? $"This Restraint Set is locked with a {k.ActiveRestraint.Padlock.ToName()}" : hasItem
            ? $"Locks the Restraint Set on {dispName}." : "No Restraint Set to lock!";

        var unlockTxt = hasPadlock ? $"Unlock {dispName}'s Restraint Set" : "No Padlock to unlock!";
        var unlockTT = hasPadlock ? $"Attempt to unlock {dispName}'s Restraint Set." : "No padlock is set!";
        var removeLayerText = hasItem ? $"Remove Layer(s) from {dispName}'s Set." : "Layers Currently Inaccessible.";
        var removeLayerTT = hasItem ? $"Remove a Restraint Layer from {dispName}'s Restraint Set." : "Must apply a Restraint Set first!";
        var removeTxt = hasItem ? $"Remove {dispName}'s Restraint Set" : "Nothing to remove!";
        var removeTT = $"{removeTxt}.";

        // Expander for ApplyRestraint
        if (CkGui.IconTextButton(FAI.Handcuffs, applyTxt, width, true, !k.PairPerms.ApplyRestraintSets || !k.ActiveRestraint.CanApply()))
            cache.ToggleInteraction(InteractionType.ApplyRestraint);
        CkGui.AttachToolTip(applyTT);

        // Interaction Window for ApplyRestraint
        if (cache.OpenItem is InteractionType.ApplyRestraint)
        {
            using (ImRaii.Child("SetApplyChild", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Restraints.DrawComboButton("##PairApplyRestraint", width, applyTT);
            ImGui.Separator();
        }

        // Expander for ApplyRestraintLayer
        var disableApplyLayer = !hasItem || itemLayers <= 0 || allLayersSet || (hasPadlock ? !k.PairPerms.ApplyLayersWhileLocked : !k.PairPerms.ApplyLayers);
        if (CkGui.IconTextButton(FAI.LayerGroup, applyLayerText, width, true, disableApplyLayer))
            cache.ToggleInteraction(InteractionType.ApplyRestraintLayers);
        CkGui.AttachToolTip(applyLayerTT);

        // Interaction Window for ApplyRestraintLayer
        if (cache.OpenItem is InteractionType.ApplyRestraintLayers)
        {
            using (ImRaii.Child("SetApplyLayerChild", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Restraints.DrawApplyLayersComboButton(width);
            ImGui.Separator();
        }

        // Expander for LockRestraint
        var disableLockExpand = k.ActiveRestraint.Identifier == Guid.Empty || k.ActiveRestraint.Padlock is not Padlocks.None || !k.PairPerms.LockRestraintSets;
        using (ImRaii.PushColor(ImGuiCol.Text, hasPadlock ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, disableLockExpand))
                cache.ToggleInteraction(InteractionType.LockRestraint);
        }
        CkGui.AttachToolTip(lockTT +
            (PadlockEx.IsTimerLock(k.ActiveRestraint.Padlock) ? "--SEP----COL--" + k.ActiveRestraint.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestraint
        if (cache.OpenItem is InteractionType.LockRestraint)
        {
            using (ImRaii.Child("SetLockChild", new Vector2(width, CkStyle.TwoRowHeight())))
                cache.RestraintLocks.DrawLockCombo("##PairLockRestraint", width, 0, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = k.ActiveRestraint.Padlock is Padlocks.None || !k.PairPerms.UnlockRestraintSets;
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, disableUnlockExpand))
            cache.ToggleInteraction(InteractionType.UnlockRestraint);
        CkGui.AttachToolTip(unlockTT);

        // Interaction Window for UnlockRestraint
        if (cache.OpenItem is InteractionType.UnlockRestraint)
        {
            using (ImRaii.Child("SetUnlockChild", new Vector2(width, ImGui.GetFrameHeight())))
                cache.RestraintLocks.DrawUnlockCombo("##PairUnlockRestraint", width, 0, unlockTxt, unlockTT);
            ImGui.Separator();
        }

        // Expander for RemoveRestraintLayer
        var blockLayerRemove = !hasItem || itemLayers <= 0 || k.ActiveRestraint.ActiveLayers is 0 || (hasPadlock ? !k.PairPerms.RemoveLayersWhileLocked : !k.PairPerms.RemoveLayers);
        if (CkGui.IconTextButton(FAI.LayerGroup, removeLayerText, width, true, blockLayerRemove))
            cache.ToggleInteraction(InteractionType.RemoveRestraintLayers);
        CkGui.AttachToolTip(removeLayerTT);

        // Interaction Window for ApplyRestraintLayer
        if (cache.OpenItem is InteractionType.RemoveRestraintLayers)
        {
            using (ImRaii.Child("SetRemoveLayerChild", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Restraints.DrawRemoveLayersComboButton(width);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = k.ActiveRestraint.Identifier == Guid.Empty || k.ActiveRestraint.Padlock is not Padlocks.None || !k.PairPerms.RemoveRestraintSets;
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, disableRemoveExpand))
            cache.ToggleInteraction(InteractionType.RemoveRestraint);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestraint
        if (cache.OpenItem is InteractionType.RemoveRestraint)
        {
            if (ImGui.Button("Remove Restraint", new Vector2(width, ImGui.GetFrameHeight())))
            {
                UiService.SetUITask(async () =>
                {
                    var result = await _hub.UserChangeKinksterActiveRestraint(new(k.UserData, DataUpdateType.Removed)).ConfigureAwait(false);
                    if (result.ErrorCode is not GagSpeakApiEc.Success)
                        _logger.LogDebug($"Failed to Remove {dispName}'s Restraint Set. ({result})", LoggerType.StickyUI);
                    else
                        cache.ClearInteraction();
                });
            }
        }
    }
    #endregion Restraints

    #region Moodles
    private void DrawMoodlesActions(KinksterInfoCache cache, Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Moodles Actions");
        ImGui.TextUnformatted("NOT YET IMPLEMENTED");
        // if (!k.IsRendered)
        //     CkGui.ColorTextInline("( Not Visible! )", ImGuiColors.DalamudRed);

        // var clientIpcValid = MoodleCache.IpcData.Statuses.Count > 0 && k.IsRendered;
        // var kinksterIpcValid = k.MoodleData.Statuses.Count > 0 && k.IsRendered;

        // ////////// APPLY MOODLES FROM PAIR's LIST //////////
        // var canApplyOther = k.PairPerms.MoodleAccess.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) && kinksterIpcValid;
        // var applyOtherStatusTxt = canApplyOther ? $"Apply a Status from {dispName}'s list" : $"Cannot apply {dispName}'s Statuses";
        // var applyOtherStatusTT = canApplyOther
        //     ? $"Applies a Moodle Status from {dispName}'s Statuses to them."
        //     : $"You don't have permission to apply Statuses to {dispName} or they have none!";
        // if (CkGui.IconTextButton(FAI.PersonCirclePlus, applyOtherStatusTxt, width, true, !canApplyOther))
        //     cache.ToggleInteraction(InteractionType.ApplyPairMoodle);
        // CkGui.AttachToolTip(applyOtherStatusTT);

        // if (cache.OpenItem is InteractionType.ApplyPairMoodle)
        // {
        //     using (ImRaii.Child("ApplyPairMoodles", new Vector2(width, ImGui.GetFrameHeight())))
        //         cache.Statuses.DrawApplyStatuses($"##OtherPresets-{k.UserData.UID}", width, $"Applies Selected Status to {dispName}");
        //     ImGui.Separator();
        // }

        // ////////// APPLY PRESETS FROM PAIR's LIST //////////
        // var applyOtherPresetTxt = canApplyOther ? $"Apply a Preset from {dispName}'s list" : $"Cannot apply {dispName}'s Presets";
        // var applyOtherPresetTT = canApplyOther
        //     ? $"Applies a Preset from {dispName}'s Presets List to them."
        //     : $"You don't have permission to apply Presets to {dispName} or they have none!";
        // if (CkGui.IconTextButton(FAI.FileCirclePlus, applyOtherPresetTxt, width, true, !canApplyOther))
        //     cache.ToggleInteraction(InteractionType.ApplyPairMoodlePreset);
        // CkGui.AttachToolTip(applyOtherPresetTT);

        // if (cache.OpenItem is InteractionType.ApplyPairMoodlePreset)
        // {
        //     using (ImRaii.Child("ApplyPairPresets", new Vector2(width, ImGui.GetFrameHeight())))
        //         cache.Presets.DrawApplyPresets($"##OtherPresets-{k.UserData.UID}", width, $"Applies Selected Preset to {dispName}");
        //     ImGui.Separator();
        // }

        // ////////// APPLY MOODLES FROM OWN LIST //////////
        // var canApplyOwn = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) && clientIpcValid;
        // var applyOwnStatusTxt = canApplyOwn ? $"Apply a Status from your list" : "Cannot apply your Statuses";
        // var applyOwnStatusTT = canApplyOwn
        //     ? $"Applies one of your Moodle Statuses to {dispName}."
        //     : "You don't have permission to apply your own Statuses, or you have none!";
        // if (CkGui.IconTextButton(FAI.UserPlus, applyOwnStatusTxt, width, true, !canApplyOwn))
        //     cache.ToggleInteraction(InteractionType.ApplyOwnMoodle);
        // CkGui.AttachToolTip(applyOwnStatusTT);

        // if (cache.OpenItem is InteractionType.ApplyOwnMoodle)
        // {
        //     using (ImRaii.Child("ApplyOwnMoodles", new Vector2(width, ImGui.GetFrameHeight())))
        //         cache.OwnStatuses.DrawApplyStatuses($"##OwnStatus-{k.UserData.UID}", width, $"Applies Selected Status to {dispName}");
        //     ImGui.Separator();
        // }

        // ////////// APPLY PRESETS FROM OWN LIST //////////
        // var applyOwnPresetTxt = canApplyOwn ? $"Apply a Preset from your list" : "Cannot apply your Presets";
        // var applyOwnPresetTT = canApplyOwn
        //     ? $"Applies one of your Moodle Presets to {dispName}."
        //     : "You don't have permission to apply your Presets, or you have none created!";
        // if (CkGui.IconTextButton(FAI.FileCirclePlus, applyOwnPresetTxt, width, true, !canApplyOwn))
        //     cache.ToggleInteraction(InteractionType.ApplyOwnMoodlePreset);
        // CkGui.AttachToolTip(applyOwnPresetTT);

        // if (cache.OpenItem is InteractionType.ApplyOwnMoodlePreset)
        // {
        //     using (ImRaii.Child("ApplyOwnPresets", new Vector2(width, ImGui.GetFrameHeight())))
        //         cache.OwnPresets.DrawApplyPresets($"##OwnPresets-{k.UserData.UID}", width, $"Applies Selected Preset to {dispName}");
        //     ImGui.Separator();
        // }


        // ////////// REMOVE MOODLES //////////
        // var canRemove = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles) && clientIpcValid;
        // var removeStatusTxt = canRemove ? $"Remove a Status from {dispName}" : "Cannot remove Statuses";
        // var removeStatusTT = canRemove
        //     ? $"Removes a Moodle Status from {dispName}'s Status Manager (Active Display)"
        //     : $"Permission to remove Moodles was not granted by {dispName}, or they have none active!";
        // if (CkGui.IconTextButton(FAI.UserMinus, removeStatusTxt, width, true, !canRemove))
        //     cache.ToggleInteraction(InteractionType.RemoveMoodle);
        // CkGui.AttachToolTip(removeStatusTT);

        // if (cache.OpenItem is InteractionType.RemoveMoodle)
        // {
        //     using (ImRaii.Child("RemoveMoodles", new Vector2(width, ImGui.GetFrameHeight())))
        //         cache.ActiveStatuses.DrawRemoveStatuses("##ActivePairStatuses" + dispName, width, $"Removes Selected Status to {dispName}");
        // }
    }
    #endregion Moodles

    #region Toybox
    private void DrawToyboxActions(KinksterInfoCache cache, Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Toybox Actions");

        //////// Pattern Execution ////////
        var canPlayPattern = k.PairPerms.ExecutePatterns && !k.PairGlobals.InVibeRoom && k.LightCache.Patterns.Any();
        var playPatternTxt = canPlayPattern ? $"Play a Pattern to {dispName}'s Toy(s)" : "Cannot Play Patterns";
        var playPatternTT = canPlayPattern
            ? $"Play one of {dispName}'s patterns to their active toys."
            : "You don't have permission to play Patterns, or there are no Patterns available!";
        if (CkGui.IconTextButton(FAI.PlayCircle, playPatternTxt, width, true, !canPlayPattern))
            cache.ToggleInteraction(InteractionType.StartPattern);
        CkGui.AttachToolTip(playPatternTT);

        if (cache.OpenItem is InteractionType.StartPattern)
        {
            using (ImRaii.Child("PatternExecute", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Patterns.Draw("##ExecutePattern" + k.UserData.UID, width, "Execute a Pattern");
            ImGui.Separator();
        }

        ///////// Stop Active Pattern ////////
        var canStopPattern = k.PairPerms.StopPatterns && !k.PairGlobals.InVibeRoom && k.ActivePattern != Guid.Empty;
        var stopPatternTxt = canStopPattern ? $"Stop {dispName}'s Active Pattern" : "Cannot Stop Active Pattern";
        var stopPatternTT = canStopPattern
            ? $"Stops the currently running Pattern on {dispName}'s Toy(s)."
            : "You don't have permission to stop Patterns, or there is no active Pattern running!";
        if (CkGui.IconTextButton(FAI.StopCircle, stopPatternTxt, width, true, !canStopPattern))
        {
            // Avoid blocking the UI by executing this off the UI thread.
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserChangeKinksterActivePattern(new(k.UserData, Guid.Empty, DataUpdateType.PatternStopped));
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                    _logger.LogError($"Failed to stop {dispName}'s active pattern. ({res.ErrorCode})", LoggerType.StickyUI);
                else
                    cache.ClearInteraction();
            });
        }
        CkGui.AttachToolTip(stopPatternTT);

        ///////// Toggle Alarms ////////
        var canToggleAlarms = k.PairPerms.ToggleAlarms && !k.PairGlobals.InVibeRoom && k.LightCache.Alarms.Any();
        var toggleAlarmTxt = canToggleAlarms ? $"Toggle one of {dispName}'s Alarms" : $"Cannot Toggle {dispName}'s Alarms";
        var toggleAlarmTT = canToggleAlarms
            ? $"Toggles the state of {dispName}'s Alarms."
            : $"Either {dispName} has not created any Alarms, or you don't have permission to toggle them.";
        if (CkGui.IconTextButton(FAI.Clock, toggleAlarmTxt, width, true, !canToggleAlarms))
            cache.ToggleInteraction(InteractionType.ToggleAlarm);
        CkGui.AttachToolTip(toggleAlarmTT);

        if (cache.OpenItem is InteractionType.ToggleAlarm)
        {
            using (ImRaii.Child("AlarmToggle", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Alarms.Draw($"##AlarmToggle-{k.UserData.UID}", width, "this Alarm");
            ImGui.Separator();
        }

        //////// Toggle Triggers ////////
        var canToggleTriggers = k.PairPerms.ToggleTriggers && !k.PairGlobals.InVibeRoom && k.LightCache.Triggers.Any();
        var toggleTriggerTxt = canToggleTriggers ? $"Toggle one of {dispName}'s Triggers" : $"Cannot Toggle {dispName}'s Triggers";
        var toggleTriggerTT = canToggleTriggers
            ? $"Toggles the state of {dispName}'s Triggers."
            : $"Either {dispName} has not created any Triggers, or you don't have permission to toggle them.";
        if (CkGui.IconTextButton(FAI.LandMineOn, toggleTriggerTxt, width, true, !canToggleTriggers))
            cache.ToggleInteraction(InteractionType.ToggleTrigger);
        CkGui.AttachToolTip(toggleTriggerTT);

        if (cache.OpenItem is InteractionType.ToggleTrigger)
        {
            using (ImRaii.Child("TriggerToggle", new Vector2(width, ImGui.GetFrameHeight())))
                cache.Triggers.Draw($"##ToggleTrigger-{k.UserData.UID}", width, "this Trigger");
        }
    }
    #endregion Toybox

    #region Misc
    private void DrawMiscActions(KinksterInfoCache cache, Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Misc Actions");

        // Effect Sending
        var hasEffect = k.PairHardcore.IsEnabled(HcAttribute.HypnoticEffect);
        var hypnoTxt = hasEffect ? $"{dispName} is being Hypnotized" : $"Hypnotize {dispName}";
        var hypnoTT = hasEffect ? $"{dispName} is currently under hypnosis state.--SEP--Cannot apply an effect until {dispName} is not hypnotized."
            : $"Configure and apply a hypnosis effect on {dispName}.";

        if (CkGui.IconTextButton(FAI.Dizzy, hypnoTxt, width, true, hasEffect || !k.PairPerms.HypnoEffectSending))
            cache.ToggleHypnosisView();
        CkGui.AttachToolTip(hypnoTT);

        if (cache.OpenItem is InteractionType.HypnosisEffect)
        {
            var buttonW = CkGui.IconTextButtonSize(FAI.Upload, "Send Effect");
            var txtWidth = width - buttonW - ImGui.GetStyle().ItemInnerSpacing.X;
            CkGui.IconInputText($"##HypnoTime-{k.UserData.UID}", txtWidth, FAI.Clock, "Ex: 20m5s", ref cache.HypnoTimer, 12);

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Upload, "Send Effect", buttonW, disabled: cache.HypnoTimer.IsNullOrEmpty()))
                cache.TrySendHypnosisAction();

            cache.DrawHypnosisEditor(width);
        }
    }
    #endregion Misc
}
