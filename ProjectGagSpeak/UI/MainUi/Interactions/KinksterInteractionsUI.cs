using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;
public class KinksterInteractionsUI : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly KinksterPermsForClient _kinksterPerms;
    private readonly ClientPermsForKinkster _permsForKinkster;
    private readonly KinksterHardcore _hcInteractions;
    private readonly KinksterShockCollar _shockInteractions;
    private readonly InteractionsService _service;

    public KinksterInteractionsUI(ILogger<KinksterInteractionsUI> logger, GagspeakMediator mediator,
        MainHub hub, KinksterPermsForClient permsForSelf, ClientPermsForKinkster permsForKinkster,
        KinksterHardcore hcInteractions, KinksterShockCollar shockInteractions, 
        InteractionsService service)
        : base(logger, mediator, $"StickyPermissionUI")
    {
        _hub = hub;
        _kinksterPerms = permsForSelf;
        _permsForKinkster = permsForKinkster;
        _hcInteractions = hcInteractions;
        _shockInteractions = shockInteractions;
        _service = service;

        Flags = WFlags.NoCollapse | WFlags.NoTitleBar | WFlags.NoResize | WFlags.NoScrollbar;
        IsOpen = false;
    }

    private Kinkster? Kinkster => _service.Kinkster;
    private string DispName => _service.DispName;
    private float GetWindowWidth()
        => _service.CurrentTab switch
        {
            InteractionsTab.KinkstersPerms => (ImGui.CalcTextSize($"{DispName} prevents removing locked layers ").X + ImGui.GetFrameHeightWithSpacing() * 2).AddWinPadX(),
            InteractionsTab.PermsForKinkster => 300f,
            _ => 280f
        };

    protected override void PreDrawInternal()
    {
        // Magic that makes the sticky pair window move with the main UI.
        var position = MainUI.LastPos;
        position.X += MainUI.LastSize.X;
        position.Y += ImGui.GetFrameHeightWithSpacing();
        ImGui.SetNextWindowPos(position);

        Flags |= WFlags.NoMove;

        _service.UpdateDispName();
        var width = GetWindowWidth();
        var size = new Vector2(width, MainUI.LastSize.Y - ImGui.GetFrameHeightWithSpacing() * 2);
        ImGui.SetNextWindowSize(size);
    }

    protected override void PostDrawInternal()
    { }

    protected override void DrawInternal()
    {
        using var _ = CkRaii.Child("InteractionsUI", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        var width = ImGui.GetContentRegionAvail().X;

        // if the pair is not valid dont draw anything after this.
        if (Kinkster is null)
        {
            CkGui.ColorTextCentered("Kinkster is null!", ImGuiColors.DalamudRed);
            return;
        }

        if (_service.CurrentTab is InteractionsTab.KinkstersPerms)
            _kinksterPerms.DrawPermissions(Kinkster, DispName, width);

        else if (_service.CurrentTab is InteractionsTab.PermsForKinkster)
            _permsForKinkster.DrawPermissions(Kinkster, DispName, width);

        else if (_service.CurrentTab is InteractionsTab.Interactions)
            DrawInteractions(Kinkster, DispName, width);
    }

    private void DrawInteractions(Kinkster k, string dispName, float width)
    {
        /* ----------- GLOBAL SETTINGS ----------- */
        DrawCommon(k, width, dispName);
        ImGui.Separator();

        if (k.IsOnline)
        {
            DrawGagActions(k, width, dispName);
            ImGui.Separator();

            DrawRestrictionActions(k, width, dispName);
            ImGui.Separator();

            DrawRestraintActions(k, width, dispName);
            ImGui.Separator();

            DrawMoodlesActions(k, width, dispName);
            ImGui.Separator();

            DrawToyboxActions(k, width, dispName);
            ImGui.Separator();

            DrawMiscActions(k, width, dispName);
            ImGui.Separator();
        }
        if (k.PairPerms.InHardcore)
        {
            _hcInteractions.DrawHardcoreActions(width, k, dispName);
            ImGui.Separator();
        }
        if (k.PairPerms.HasValidShareCode() || k.PairGlobals.HasValidShareCode())
        {
            _shockInteractions.DrawShockActions(width, k, dispName);
            ImGui.Separator();
        }

        ImGui.TextUnformatted("Individual Pair Functions");
        if (CkGui.IconTextButton(FAI.Trash, "Unpair Permanently", width, true, !KeyMonitor.CtrlPressed() || !KeyMonitor.ShiftPressed()))
            _hub.UserRemoveKinkster(new(k.UserData)).ConfigureAwait(false);
        CkGui.AttachToolTip($"--COL--CTRL + SHIFT + L-Click--COL-- to remove {dispName}", color: ImGuiColors.DalamudRed);
    }

    private void DrawCommon(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Common Pair Functions");
        var isPaused = Kinkster!.IsPaused;
        if (!isPaused)
        {
            if (CkGui.IconTextButton(FAI.User, "Open Profile", width, true))
                Mediator.Publish(new KinkPlateOpenStandaloneMessage(Kinkster));
            CkGui.AttachToolTip($"Opens {dispName}'s profile!");

            if (CkGui.IconTextButton(FAI.ExclamationTriangle, $"Report {dispName}'s KinkPlate", width, true))
                Mediator.Publish(new ReportKinkPlateMessage(Kinkster.UserData));
            CkGui.AttachToolTip($"Snapshot {dispName}'s KinkPlate and make a report with its state.");
        }

        if (Kinkster.IsOnline)
        {
            if (CkGui.IconTextButton(isPaused ? FAI.Play : FAI.Pause, isPaused ? "Unpause " : "Pause " + dispName, width, true))
                UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, Kinkster.UserData, Kinkster.PairPerms, nameof(PairPerms.IsPaused), !isPaused));
            CkGui.AttachToolTip(!isPaused ? "Pause" : "Resume" + $"pairing with {dispName}.");
        }

        if (Kinkster.IsVisible)
        {
            if (CkGui.IconTextButton(FAI.Sync, "Reload Appearance data", width, true))
                Kinkster.ReapplyLatestData();
            CkGui.AttachToolTip("This reapplies the latest data from Customize+ and Moodles");
        }
    }

    #region Gags
    private void DrawGagActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Gag Actions");

        if (CkGuiUtils.LayerIdxCombo("##gagLayer", width, _service.GagLayer, out var newVal, 3))
            _service.GagLayer = newVal;
        CkGui.AttachToolTip("Select the layer to apply a Gag to.");

        if (k.ActiveGags.GagSlots[_service.GagLayer] is not { } slot)
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
            _service.ToggleInteraction(InteractionType.ApplyGag);
        CkGui.AttachToolTip(applyTT);
        
        if (_service.OpenItem is InteractionType.ApplyGag)
        {
            using (ImRaii.Child("###ApplyGag", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Gags.DrawComboButton("##ApplyGag", width, _service.GagLayer, "Apply", "Select a Gag to Apply");
            ImGui.Separator();
        }


        // Locking
        using (ImRaii.PushColor(ImGuiCol.Text, slot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, !k.PairPerms.LockGags || !slot.CanLock()))
                _service.ToggleInteraction(InteractionType.LockGag);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(slot.Padlock) ? "--SEP----COL--" + slot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);
        
        if (_service.OpenItem is InteractionType.LockGag)
        {
            using (ImRaii.Child("###LockGag", new Vector2(width, CkStyle.TwoRowHeight())))
                _service.GagLocks.DrawLockCombo("##LockGag", width, _service.GagLayer, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Unlocking.
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, !slot.CanUnlock() || !k.PairPerms.UnlockGags))
            _service.ToggleInteraction(InteractionType.UnlockGag);
        CkGui.AttachToolTip(unlockTT);

        if (_service.OpenItem is InteractionType.UnlockGag)
        {
            using (ImRaii.Child("###UnlockGag", new Vector2(width, ImGui.GetFrameHeight())))
                _service.GagLocks.DrawUnlockCombo("##UnlockGag", width, _service.GagLayer, unlockTT, unlockTxt);
            ImGui.Separator();
        }


        // Removing.
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, !slot.CanRemove() || !k.PairPerms.RemoveGags))
            _service.ToggleInteraction(InteractionType.RemoveGag);
        CkGui.AttachToolTip(removeTT);

        if (_service.OpenItem is InteractionType.RemoveGag)
        {
            if (ImGui.Button("Remove Gag", new Vector2(width, ImGui.GetFrameHeight())))
            {
                var dto = new PushKinksterActiveGagSlot(k.UserData, DataUpdateType.Removed)
                {
                    Layer = _service.GagLayer,
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
                        _service.CloseInteraction();
                    }
                });
            }
        }
    }
    #endregion Gags

    #region Restrictions
    private void DrawRestrictionActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Restriction Actions");

        // Drawing out restriction layers.
        if (CkGuiUtils.LayerIdxCombo("##restrictionLayer", width, _service.RestrictionLayer, out var newVal, 5))
            _service.RestrictionLayer = newVal;
        CkGui.AttachToolTip("Select the layer to apply a Restriction to.");

        if (k.ActiveRestrictions.Restrictions[_service.RestrictionLayer] is not { } slot)
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
            _service.ToggleInteraction(InteractionType.ApplyRestriction);
        CkGui.AttachToolTip(applyTT);

        if (_service.OpenItem is InteractionType.ApplyRestriction)
        {
            using (ImRaii.Child("###ApplyRestriction", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Restrictions.DrawComboButton("##ApplyRestriction", width, _service.RestrictionLayer, "Apply", "Select a Restriction to apply");
            ImGui.Separator();
        }

        // Expander for LockRestriction
        using (ImRaii.PushColor(ImGuiCol.Text, (slot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, !slot.CanLock() || !k.PairPerms.LockRestrictions))
                _service.ToggleInteraction(InteractionType.LockRestriction);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(slot.Padlock) ? "--SEP----COL--" + slot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        if (_service.OpenItem is InteractionType.LockRestriction)
        {
            using (ImRaii.Child("###LockRestriction", new Vector2(width, CkStyle.TwoRowHeight())))
                _service.RestrictionLocks.DrawLockCombo("##LockRestriction", width, _service.RestrictionLayer, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Expander for unlocking.
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, !slot.CanUnlock() || !k.PairPerms.UnlockRestrictions))
            _service.ToggleInteraction(InteractionType.UnlockRestriction);
        CkGui.AttachToolTip(unlockTT);

        if (_service.OpenItem is InteractionType.UnlockRestriction)
        {
            using (ImRaii.Child("###UnlockRestriction", new Vector2(width, ImGui.GetFrameHeight())))
                _service.RestrictionLocks.DrawUnlockCombo("##UnlockRestriction", width, _service.RestrictionLayer, unlockTT, unlockTxt);
            ImGui.Separator();
        }

        // Expander for removing.
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, !slot.CanRemove() || !k.PairPerms.RemoveRestrictions))
            _service.ToggleInteraction(InteractionType.RemoveRestriction);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestriction
        if (_service.OpenItem is InteractionType.RemoveRestriction)
        {
            if (ImGui.Button("Remove Restriction", new Vector2(width, ImGui.GetFrameHeight())))
            {
                var dto = new PushKinksterActiveRestriction(k.UserData, DataUpdateType.Removed) { Layer = _service.RestrictionLayer };
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
                        _logger.LogDebug($"Removed Restriction Item from {dispName} on layer {_service.RestrictionLayer}", LoggerType.StickyUI);
                        _service.CloseInteraction();
                    }
                });
            }
        }
    }
    #endregion Restrictions

    #region Restraints
    private void DrawRestraintActions(Kinkster k, float width, string dispName)
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
            _service.ToggleInteraction(InteractionType.ApplyRestraint);
        CkGui.AttachToolTip(applyTT);

        // Interaction Window for ApplyRestraint
        if (_service.OpenItem is InteractionType.ApplyRestraint)
        {
            using (ImRaii.Child("SetApplyChild", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Restraints.DrawComboButton("##PairApplyRestraint", width, applyTT);
            ImGui.Separator();
        }

        // Expander for ApplyRestraintLayer
        var disableApplyLayer = !hasItem || itemLayers <= 0 || allLayersSet || (hasPadlock ? !k.PairPerms.ApplyLayersWhileLocked : !k.PairPerms.ApplyLayers); 
        if (CkGui.IconTextButton(FAI.LayerGroup, applyLayerText, width, true, disableApplyLayer))
            _service.ToggleInteraction(InteractionType.ApplyRestraintLayers);
        CkGui.AttachToolTip(applyLayerTT);

        // Interaction Window for ApplyRestraintLayer
        if (_service.OpenItem is InteractionType.ApplyRestraintLayers)
        {
            using (ImRaii.Child("SetApplyLayerChild", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Restraints.DrawApplyLayersComboButton(width);
            ImGui.Separator();
        }

        // Expander for LockRestraint
        var disableLockExpand = k.ActiveRestraint.Identifier == Guid.Empty || k.ActiveRestraint.Padlock is not Padlocks.None || !k.PairPerms.LockRestraintSets;
        using (ImRaii.PushColor(ImGuiCol.Text, hasPadlock ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudWhite))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, disableLockExpand))
                _service.ToggleInteraction(InteractionType.LockRestraint);
        }
        CkGui.AttachToolTip(lockTT +
            (PadlockEx.IsTimerLock(k.ActiveRestraint.Padlock) ? "--SEP----COL--" + k.ActiveRestraint.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestraint
        if (_service.OpenItem is InteractionType.LockRestraint)
        {
            using (ImRaii.Child("SetLockChild", new Vector2(width, CkStyle.TwoRowHeight())))
                _service.RestraintLocks.DrawLockCombo("##PairLockRestraint", width, 0, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = k.ActiveRestraint.Padlock is Padlocks.None || !k.PairPerms.UnlockRestraintSets;
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, disableUnlockExpand))
            _service.ToggleInteraction(InteractionType.UnlockRestraint);
        CkGui.AttachToolTip(unlockTT);

        // Interaction Window for UnlockRestraint
        if (_service.OpenItem is InteractionType.UnlockRestraint)
        {
            using (ImRaii.Child("SetUnlockChild", new Vector2(width, ImGui.GetFrameHeight())))
                _service.RestraintLocks.DrawUnlockCombo("##PairUnlockRestraint", width, 0, unlockTxt, unlockTT);
            ImGui.Separator();
        }

        // Expander for RemoveRestraintLayer
        var blockLayerRemove = !hasItem || itemLayers <= 0 || k.ActiveRestraint.ActiveLayers is 0 || (hasPadlock ? !k.PairPerms.RemoveLayersWhileLocked : !k.PairPerms.RemoveLayers);
        if (CkGui.IconTextButton(FAI.LayerGroup, removeLayerText, width, true, blockLayerRemove))
            _service.ToggleInteraction(InteractionType.RemoveRestraintLayers);
        CkGui.AttachToolTip(removeLayerTT);

        // Interaction Window for ApplyRestraintLayer
        if (_service.OpenItem is InteractionType.RemoveRestraintLayers)
        {
            using (ImRaii.Child("SetRemoveLayerChild", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Restraints.DrawRemoveLayersComboButton(width);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = k.ActiveRestraint.Identifier == Guid.Empty || k.ActiveRestraint.Padlock is not Padlocks.None || !k.PairPerms.RemoveRestraintSets;
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, disableRemoveExpand))
            _service.ToggleInteraction(InteractionType.RemoveRestraint);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestraint
        if (_service.OpenItem is InteractionType.RemoveRestraint)
        {
            if (ImGui.Button("Remove Restraint", new Vector2(width, ImGui.GetFrameHeight())))
            {
                UiService.SetUITask(async () =>
                {
                    var result = await _hub.UserChangeKinksterActiveRestraint(new(k.UserData, DataUpdateType.Removed)).ConfigureAwait(false);
                    if (result.ErrorCode is not GagSpeakApiEc.Success)
                        _logger.LogDebug($"Failed to Remove {dispName}'s Restraint Set. ({result})", LoggerType.StickyUI);
                    else
                        _service.CloseInteraction();
                });
            }
        }
    }
    #endregion Restraints

    #region Moodles
    private void DrawMoodlesActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Moodles Actions");
        if (!k.IsVisible) 
            CkGui.ColorTextInline("( Not Visible! )", ImGuiColors.DalamudRed);

        var clientIpcValid = MoodleCache.IpcData.Statuses.Count > 0 && k.IsVisible;
        var kinksterIpcValid = k.LastMoodlesData.Statuses.Count > 0 && k.IsVisible;

        ////////// APPLY MOODLES FROM PAIR's LIST //////////
        var canApplyOther = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) && kinksterIpcValid;
        var applyOtherStatusTxt = canApplyOther ? $"Apply a Status from {dispName}'s list" : $"Cannot apply {dispName}'s Statuses";
        var applyOtherStatusTT = canApplyOther
            ? $"Applies a Moodle Status from {dispName}'s Statuses to them."
            : $"You don't have permission to apply Statuses to {dispName} or they have none!";
        if (CkGui.IconTextButton(FAI.PersonCirclePlus, applyOtherStatusTxt, width, true, !canApplyOther))
            _service.ToggleInteraction(InteractionType.ApplyPairMoodle);
        CkGui.AttachToolTip(applyOtherStatusTT);

        if (_service.OpenItem is InteractionType.ApplyPairMoodle)
        {
            using (ImRaii.Child("ApplyPairMoodles", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Statuses.DrawApplyStatuses($"##OtherPresets-{k.UserData.UID}", width, $"Applies Selected Status to {dispName}");
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM PAIR's LIST //////////
        var applyOtherPresetTxt = canApplyOther ? $"Apply a Preset from {dispName}'s list" : $"Cannot apply {dispName}'s Presets";
        var applyOtherPresetTT = canApplyOther
            ? $"Applies a Preset from {dispName}'s Presets List to them."
            : $"You don't have permission to apply Presets to {dispName} or they have none!";
        if (CkGui.IconTextButton(FAI.FileCirclePlus, applyOtherPresetTxt, width, true, !canApplyOther))
            _service.ToggleInteraction(InteractionType.ApplyPairMoodlePreset);
        CkGui.AttachToolTip(applyOtherPresetTT);

        if (_service.OpenItem is InteractionType.ApplyPairMoodlePreset)
        {
            using (ImRaii.Child("ApplyPairPresets", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Presets.DrawApplyPresets($"##OtherPresets-{k.UserData.UID}", width, $"Applies Selected Preset to {dispName}");
            ImGui.Separator();
        }

        ////////// APPLY MOODLES FROM OWN LIST //////////
        var canApplyOwn = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) && clientIpcValid;
        var applyOwnStatusTxt = canApplyOwn ? $"Apply a Status from your list" : "Cannot apply your Statuses";
        var applyOwnStatusTT = canApplyOwn
            ? $"Applies one of your Moodle Statuses to {dispName}."
            : "You don't have permission to apply your own Statuses, or you have none!";
        if (CkGui.IconTextButton(FAI.UserPlus, applyOwnStatusTxt, width, true, !canApplyOwn))
            _service.ToggleInteraction(InteractionType.ApplyOwnMoodle);
        CkGui.AttachToolTip(applyOwnStatusTT);

        if (_service.OpenItem is InteractionType.ApplyOwnMoodle)
        {
            using (ImRaii.Child("ApplyOwnMoodles", new Vector2(width, ImGui.GetFrameHeight())))
                _service.OwnStatuses.DrawApplyStatuses($"##OwnStatus-{k.UserData.UID}", width, $"Applies Selected Status to {dispName}");
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM OWN LIST //////////
        var applyOwnPresetTxt = canApplyOwn ? $"Apply a Preset from your list" : "Cannot apply your Presets";
        var applyOwnPresetTT = canApplyOwn
            ? $"Applies one of your Moodle Presets to {dispName}."
            : "You don't have permission to apply your Presets, or you have none created!";
        if (CkGui.IconTextButton(FAI.FileCirclePlus, applyOwnPresetTxt, width, true, !canApplyOwn))
            _service.ToggleInteraction(InteractionType.ApplyOwnMoodlePreset);
        CkGui.AttachToolTip(applyOwnPresetTT);

        if (_service.OpenItem is InteractionType.ApplyOwnMoodlePreset)
        {
            using (ImRaii.Child("ApplyOwnPresets", new Vector2(width, ImGui.GetFrameHeight())))
                _service.OwnPresets.DrawApplyPresets($"##OwnPresets-{k.UserData.UID}", width, $"Applies Selected Preset to {dispName}");
            ImGui.Separator();
        }


        ////////// REMOVE MOODLES //////////
        var canRemove = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles) && clientIpcValid;
        var removeStatusTxt = canRemove ? $"Remove a Status from {dispName}" : "Cannot remove Statuses";
        var removeStatusTT = canRemove
            ? $"Removes a Moodle Status from {dispName}'s Status Manager (Active Display)"
            : $"Permission to remove Moodles was not granted by {dispName}, or they have none active!";
        if (CkGui.IconTextButton(FAI.UserMinus, removeStatusTxt, width, true, !canRemove))
            _service.ToggleInteraction(InteractionType.RemoveMoodle);
        CkGui.AttachToolTip(removeStatusTT);

        if (_service.OpenItem is InteractionType.RemoveMoodle)
        {
            using (ImRaii.Child("RemoveMoodles", new Vector2(width, ImGui.GetFrameHeight())))
                _service.ActiveStatuses.DrawRemoveStatuses("##ActivePairStatuses" + dispName, width, $"Removes Selected Status to {dispName}");
        }
    }
    #endregion Moodles

    #region Toybox
    private void DrawToyboxActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Toybox Actions");

        //////// Pattern Execution ////////
        var canPlayPattern = k.PairPerms.ExecutePatterns && !k.PairGlobals.InVibeRoom && k.LightCache.Patterns.Any();
        var playPatternTxt = canPlayPattern ? $"Play a Pattern to {dispName}'s Toy(s)" : "Cannot Play Patterns";
        var playPatternTT = canPlayPattern
            ? $"Play one of {dispName}'s patterns to their active toys."
            : "You don't have permission to play Patterns, or there are no Patterns available!";
        if (CkGui.IconTextButton(FAI.PlayCircle, playPatternTxt, width, true, !canPlayPattern))
            _service.ToggleInteraction(InteractionType.StartPattern);
        CkGui.AttachToolTip(playPatternTT);

        if (_service.OpenItem is InteractionType.StartPattern)
        {
            using (ImRaii.Child("PatternExecute", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Patterns.Draw("##ExecutePattern" + k.UserData.UID, width, "Execute a Pattern");
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
                    _service.CloseInteraction();
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
            _service.ToggleInteraction(InteractionType.ToggleAlarm);
        CkGui.AttachToolTip(toggleAlarmTT);

        if (_service.OpenItem is InteractionType.ToggleAlarm)
        {
            using (ImRaii.Child("AlarmToggle", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Alarms.Draw($"##AlarmToggle-{k.UserData.UID}", width, "this Alarm");
            ImGui.Separator();
        }

        //////// Toggle Triggers ////////
        var canToggleTriggers = k.PairPerms.ToggleTriggers && !k.PairGlobals.InVibeRoom && k.LightCache.Triggers.Any();
        var toggleTriggerTxt = canToggleTriggers ? $"Toggle one of {dispName}'s Triggers" : $"Cannot Toggle {dispName}'s Triggers";
        var toggleTriggerTT = canToggleTriggers
            ? $"Toggles the state of {dispName}'s Triggers."
            : $"Either {dispName} has not created any Triggers, or you don't have permission to toggle them.";
        if (CkGui.IconTextButton(FAI.LandMineOn, toggleTriggerTxt, width, true, !canToggleTriggers))
            _service.ToggleInteraction(InteractionType.ToggleTrigger);
        CkGui.AttachToolTip(toggleTriggerTT);

        if (_service.OpenItem is InteractionType.ToggleTrigger)
        {
            using (ImRaii.Child("TriggerToggle", new Vector2(width, ImGui.GetFrameHeight())))
                _service.Triggers.Draw($"##ToggleTrigger-{k.UserData.UID}", width, "this Trigger");
        }
    }
    #endregion Toybox

    #region Misc
    private void DrawMiscActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Misc Actions");

        // Effect Sending
        var hasEffect = k.PairHardcore.IsEnabled(HcAttribute.HypnoticEffect);
        var hypnoTxt = hasEffect ? $"{dispName} is being Hypnotized" : $"Hypnotize {dispName}";
        var hypnoTT = hasEffect ? $"{dispName} is currently under hypnosis state.--SEP--Cannot apply an effect until {dispName} is not hypnotized."
            : $"Configure and apply a hypnosis effect on {dispName}.";

        if (CkGui.IconTextButton(FAI.Dizzy, hypnoTxt, width, true, hasEffect || !k.PairPerms.HypnoEffectSending))
            _service.ToggleHypnosisView();
        CkGui.AttachToolTip(hypnoTT);

        if (_service.OpenItem is InteractionType.HypnosisEffect)
        {
            var buttonW = CkGui.IconTextButtonSize(FAI.Upload, "Send Effect");
            var txtWidth = width - buttonW - ImGui.GetStyle().ItemInnerSpacing.X;
            CkGui.IconInputText($"##HypnoTime-{k.UserData.UID}", txtWidth, FAI.Clock, "Ex: 20m5s", ref _service.HypnoTimer, 12);

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Upload, "Send Effect", buttonW, disabled: _service.HypnoTimer.IsNullOrEmpty()))
                _service.TrySendHypnosisAction();

            _service.DrawHypnosisEditor(width);
        }
    }
    #endregion Misc
}
