using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Raii;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Common;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.CustomCombos.Moodles;
using GagSpeak.CustomCombos.Padlock;
using GagSpeak.CustomCombos.Pairs;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using ImGuiNET;
using OtterGui.Text;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace GagSpeak.Gui.MainWindow;

public class StickyWindowSelections
{
    public int      GagLayer = 0;
    public int      RestrictionLayer= 0;
    public int      RestraintLayer = 0;
    public Guid     OwnStatus = Guid.Empty;
    public Guid     OwnPreset = Guid.Empty;
    public Guid     PairStatus = Guid.Empty;
    public Guid     PairPreset = Guid.Empty;
    public Guid     Removal = Guid.Empty;
    public string   HypnoTimer = string.Empty;
    public uint     EmoteId = 0;
    public int      CyclePose = 0;
    public int      Intensity = 0;
    public int      VibrateIntensity = 0;
    public float    Duration = 0;
    public float    VibeDuration = 0;

    public InteractionType OpenInteraction { get; private set; } = InteractionType.None;

    public void CloseInteraction() => OpenInteraction = InteractionType.None;
    public void OpenOrClose(InteractionType type)
    {
        if (OpenInteraction == type)
            CloseInteraction();
        else
            OpenInteraction = type;
    }
}

public class KinksterInteractionsUI : WindowMediatorSubscriberBase
{
    private readonly MainMenuTabs _mainTabMenu;
    private readonly MainHub _hub;
    private readonly KinksterPermsForClient _kinksterPerms;
    private readonly ClientPermsForKinkster _permsForKinkster;

    // Private variables for the sticky UI and its respective combos.
    private PairGagCombo _pairGags;
    private PairGagPadlockCombo _pairGagPadlocks;
    private PairRestrictionCombo _pairRestrictionItems;
    private PairRestrictionPadlockCombo _pairRestrictionPadlocks;
    private PairRestraintCombo _pairRestraintSets;
    private PairRestraintPadlockCombo _pairRestraintSetPadlocks;
    private PairMoodleStatusCombo _pairMoodleStatuses;
    private PairMoodlePresetCombo _pairMoodlePresets;
    private PairPatternCombo _pairPatterns;
    private PairAlarmCombo _pairAlarmToggles;
    private PairTriggerCombo _pairTriggerToggles;
    private OwnMoodleStatusToPairCombo _moodleStatuses;
    private OwnMoodlePresetToPairCombo _moodlePresets;
    private EmoteCombo _emoteCombo;
    private PairMoodleStatusCombo _activePairStatusCombo;
    private HypnoEffectEditor _hypnoEditor;

    // (i was not liking all the private variables ok?)
    private StickyWindowSelections _selections = new();
    private Kinkster? _kinkster = null;
    private InteractionsTab _openTab = InteractionsTab.None;
    private InteractionType _openInteraction => _selections.OpenInteraction;
    public KinksterInteractionsUI(
        ILogger<KinksterInteractionsUI> logger,
        GagspeakMediator mediator,
        MainMenuTabs mainTabMenu,
        MainHub hub,
        KinksterPermsForClient permsForSelf,
        ClientPermsForKinkster permsForKinkster,
        HypnoEffectManager effectPresetManager)
        : base(logger, mediator, $"StickyPermissionUI")
    {
        _mainTabMenu = mainTabMenu;
        _hub = hub;
        _kinksterPerms = permsForSelf;
        _permsForKinkster = permsForKinkster;

        _hypnoEditor = new HypnoEffectEditor("KinksterEffectEditor", effectPresetManager);

        Flags = WFlags.NoCollapse | WFlags.NoTitleBar | WFlags.NoResize | WFlags.NoScrollbar;

        Mediator.Subscribe<KinksterInteractionUiChangeMessage>(this, (msg) => UpdateWindow(msg.Kinkster, msg.Type));
        Mediator.Subscribe<PairWasRemovedMessage>(this, (msg) => IsOpen = false);
        Mediator.Subscribe<ClosedMainUiMessage>(this, (msg) => IsOpen = false);
        Mediator.Subscribe<MainWindowTabChangeMessage>(this, _ => { if (_.NewTab != MainMenuTabs.SelectedTab.Whitelist) SilentClose(); });
        IsOpen = false;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _hypnoEditor.Dispose();
    }

    private void SilentClose()
    {
        _openTab = InteractionsTab.None;
        IsOpen = false;
        _hypnoEditor.OnEditorClose();
    }

    private void UpdateWindow(Kinkster kinkster, InteractionsTab type)
    {
        if (_openTab == type && kinkster == _kinkster)
        {
            SilentClose();
            return;
        }
        else if (_openTab == type && kinkster != _kinkster)
        {
            _openTab = InteractionsTab.None;
            _hypnoEditor.OnEditorClose();
        }


        _logger.LogInformation($"Updating Sticky UI for {kinkster.GetNickAliasOrUid()} with type {type}.");
        // if this is not 0 it means they do not have the same UID and are different.
        if (kinkster.CompareTo(_kinkster) != 0 || _openTab is InteractionsTab.None)
            SetWindowForKinkster(kinkster);

        // After setting the window for the Kinkster, we need to make sure the main window is opened.
        Mediator.Publish(new UiToggleMessage(typeof(MainUI), ToggleType.Show));
        _mainTabMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist;
        _openTab = type;
        IsOpen = true;
    }

    private string DispName = "Anon. Kinkster";
    private float GetKinksterPermWidth() => (ImGui.CalcTextSize($"{DispName} prevents Removing Layers While Locked. ").X + ImGui.GetFrameHeightWithSpacing() * 2).AddWinPadX();

    public void SetWindowForKinkster(Kinkster kinkster)
    {
        _kinkster = kinkster;
        _selections = new StickyWindowSelections();

        _pairGags = new PairGagCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairGagPadlocks = new PairGagPadlockCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairRestrictionItems = new PairRestrictionCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairRestrictionPadlocks = new PairRestrictionPadlockCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairRestraintSets = new PairRestraintCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairRestraintSetPadlocks = new PairRestraintPadlockCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairMoodleStatuses = new PairMoodleStatusCombo(_logger, _hub, _kinkster, 1.3f, () => _selections.CloseInteraction());
        _pairMoodlePresets = new PairMoodlePresetCombo(_logger, _hub, _kinkster, 1.3f, () => _selections.CloseInteraction());
        _pairPatterns = new PairPatternCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairAlarmToggles = new PairAlarmCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _pairTriggerToggles = new PairTriggerCombo(_logger, _hub, _kinkster, () => _selections.CloseInteraction());
        _moodleStatuses = new OwnMoodleStatusToPairCombo(_logger, _hub, _kinkster, 1.3f, () => _selections.CloseInteraction());
        _moodlePresets = new OwnMoodlePresetToPairCombo(_logger, _hub, _kinkster, 1.3f, () => _selections.CloseInteraction());
        _activePairStatusCombo = new PairMoodleStatusCombo(_logger, _hub, _kinkster, 1.3f,
            () => [ .. _kinkster.LastIpcData.DataInfo.Values.OrderBy(x => x.Title)
        ], () => _selections.CloseInteraction());

        _emoteCombo = new EmoteCombo(_logger, 1.3f, () => [
            .._kinkster.PairPerms.AllowLockedEmoting ? EmoteExtensions.LoopedEmotes() : EmoteExtensions.SittingEmotes()
        ]);

        // set the max width to the longest possible text string.
        DispName = _kinkster.GetNickAliasOrUid();
    }

    protected override void PreDrawInternal()
    {
        // Magic that makes the sticky pair window move with the main UI.
        var position = MainUI.LastPos;
        position.X += MainUI.LastSize.X;
        position.Y += ImGui.GetFrameHeightWithSpacing();
        ImGui.SetNextWindowPos(position);

        Flags |= WFlags.NoMove;

        DispName = _kinkster?.GetNickAliasOrUid() ?? "Anon. Kinkster";
        var width = ImGuiHelpers.GlobalScale * _openTab switch
        {
            InteractionsTab.KinkstersPerms => GetKinksterPermWidth(),
            InteractionsTab.PermsForKinkster => 300f,
            _ => 280f
        };

        var size = new Vector2(width, MainUI.LastSize.Y - ImGui.GetFrameHeightWithSpacing() * 2);
        ImGui.SetNextWindowSize(size);
    }

    protected override void PostDrawInternal()
    { }

    protected override void DrawInternal()
    {
        using var _ = CkRaii.Child("##KinksterInteractions", ImGui.GetContentRegionAvail(), WFlags.NoScrollbar);
        var width = ImGui.GetContentRegionAvail().X;

        // if the pair is not valid dont draw anything after this.
        if (_kinkster is null)
        {
            CkGui.ColorTextCentered("Kinkster is null!", ImGuiColors.DalamudRed);
            return;
        }

        if (_openTab is InteractionsTab.KinkstersPerms)
            _kinksterPerms.DrawPermissions(_kinkster, DispName, width);
        else if (_openTab is InteractionsTab.PermsForKinkster)
            _permsForKinkster.DrawPermissions(_kinkster, DispName, width);
        else if (_openTab is InteractionsTab.Interactions)
            DrawInteractions(_kinkster, DispName, width);
    }

    private void DrawInteractions(Kinkster k, string dispName, float width)
    {
        /* ----------- GLOBAL SETTINGS ----------- */
        DrawCommon(k, width, dispName);

        if (k.IsOnline)
        {
            DrawGagActions(k, width, dispName); // done
            DrawRestrictionActions(k, width, dispName); // done
            DrawRestraintActions(k, width, dispName); // done
            DrawMoodlesActions(k, width, dispName);
            DrawToyboxActions(k, width, dispName);
            DrawMiscActions(k, width, dispName); // done
            DrawHardcoreActions(k, width, dispName);
            DrawShockActions(k, width, dispName);
        }

        ImGui.TextUnformatted("Individual Pair Functions");
        if (CkGui.IconTextButton(FAI.Trash, "Unpair Permanently", width, true, !KeyMonitor.CtrlPressed() || !KeyMonitor.ShiftPressed()))
            _hub.UserRemoveKinkster(new(k.UserData)).ConfigureAwait(false);
        CkGui.AttachToolTip($"--COL--CTRL + SHIFT + L-Click--COL-- to remove {dispName}", color: ImGuiColors.DalamudRed);
    }

    private void DrawCommon(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Common Pair Functions");
        var isPaused = _kinkster!.IsPaused;
        if (!isPaused)
        {
            if (CkGui.IconTextButton(FAI.User, "Open Profile", width, true))
                Mediator.Publish(new KinkPlateOpenStandaloneMessage(_kinkster));
            CkGui.AttachToolTip($"Opens {dispName}'s profile!");

            if (CkGui.IconTextButton(FAI.ExclamationTriangle, $"Report {dispName}'s KinkPlate", width, true))
                Mediator.Publish(new ReportKinkPlateMessage(_kinkster.UserData));
            CkGui.AttachToolTip($"Snapshot {dispName}'s KinkPlate and make a report with its state.");
        }

        if (_kinkster.IsOnline)
        {
            if (CkGui.IconTextButton(isPaused ? FAI.Play : FAI.Pause, isPaused ? "Unpause " : "Pause " + dispName, width, true))
                UiService.SetUITask(PermissionHelper.ChangeOwnUnique(_hub, _kinkster.UserData, _kinkster.PairPerms, nameof(PairPerms.IsPaused), !isPaused));
            CkGui.AttachToolTip(!isPaused ? "Pause" : "Resume" + $"pairing with {dispName}.");
        }

        if (_kinkster.IsVisible)
        {
            if (CkGui.IconTextButton(FAI.Sync, "Reload IPC data", width, true))
                _kinkster.ApplyLatestMoodles(forced: true);
            CkGui.AttachToolTip("This reapplies the latest data from Customize+ and Moodles");
        }

        ImGui.Separator();
    }

    #region Gags
    private void DrawGagActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Gag Actions");

        if (CkGuiUtils.LayerIdxCombo("##gagLayer", width, _selections.GagLayer, out var newVal, 3))
            _selections.GagLayer = newVal;
        CkGui.AttachToolTip("Select the layer to apply a Gag to.");

        if (k.ActiveGags.GagSlots[_selections.GagLayer] is not { } slot)
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
        {
            Svc.Logger.Information($"Opening Apply Gag for {dispName} with Layer {_selections.GagLayer}.");
            _selections.OpenOrClose(InteractionType.ApplyGag);
        }
        CkGui.AttachToolTip(applyTT);
        
        if (_openInteraction is InteractionType.ApplyGag)
        {
            using (ImRaii.Child("###ApplyGag", new Vector2(width, ImGui.GetFrameHeight())))
                _pairGags.DrawComboButton("##ApplyGag", width, _selections.GagLayer, "Apply", "Select a Gag to Apply");
            ImGui.Separator();
        }


        // Locking
        using (ImRaii.PushColor(ImGuiCol.Text, slot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, !k.PairPerms.LockGags || !slot.CanLock()))
                _selections.OpenOrClose(InteractionType.LockGag);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(slot.Padlock) ? "--SEP----COL--" + slot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);
        
        if (_openInteraction is InteractionType.LockGag)
        {
            using (ImRaii.Child("###LockGag", new Vector2(width, CkStyle.TwoRowHeight())))
                _pairGagPadlocks.DrawLockCombo("##LockGag", width, _selections.GagLayer, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Unlocking.
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, !slot.CanUnlock() || !k.PairPerms.UnlockGags))
            _selections.OpenOrClose(InteractionType.UnlockGag);
        CkGui.AttachToolTip(unlockTT);

        if (_openInteraction is InteractionType.UnlockGag)
        {
            using (ImRaii.Child("###UnlockGag", new Vector2(width, ImGui.GetFrameHeight())))
                _pairGagPadlocks.DrawUnlockCombo("##UnlockGag", width, _selections.GagLayer, unlockTT, unlockTxt);
            ImGui.Separator();
        }


        // Removing.
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, !slot.CanRemove() || !k.PairPerms.RemoveGags))
            _selections.OpenOrClose(InteractionType.RemoveGag);
        CkGui.AttachToolTip(removeTT);

        if (_selections.OpenInteraction is InteractionType.RemoveGag)
        {
            if (ImGui.Button("Remove Gag", new Vector2(width, ImGui.GetFrameHeight())))
            {
                var dto = new PushKinksterActiveGagSlot(k.UserData, DataUpdateType.Removed)
                {
                    Layer = _selections.GagLayer,
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
                        _selections.CloseInteraction();
                    }
                });
            }
        }

        ImGui.Separator();
    }
    #endregion Gags

    #region Restrictions
    private void DrawRestrictionActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Restriction Actions");

        // Drawing out restriction layers.
        if (CkGuiUtils.LayerIdxCombo("##restrictionLayer", width, _selections.RestrictionLayer, out var newVal, 5))
            _selections.RestrictionLayer = newVal;
        CkGui.AttachToolTip("Select the layer to apply a Restriction to.");

        if (k.ActiveRestrictions.Restrictions[_selections.RestrictionLayer] is not { } slot)
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
            _selections.OpenOrClose(InteractionType.ApplyRestriction);
        CkGui.AttachToolTip(applyTT);

        if (_selections.OpenInteraction is InteractionType.ApplyRestriction)
        {
            using (ImRaii.Child("###ApplyRestriction", new Vector2(width, ImGui.GetFrameHeight())))
                _pairRestrictionItems.DrawComboButton("##ApplyRestriction", width, _selections.RestrictionLayer, "Apply", "Select a Restriction to apply");
            ImGui.Separator();
        }

        // Expander for LockRestriction
        using (ImRaii.PushColor(ImGuiCol.Text, (slot.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, !slot.CanLock() || !k.PairPerms.LockRestrictions))
                _selections.OpenOrClose(InteractionType.LockRestriction);
        }
        CkGui.AttachToolTip(lockTT + (PadlockEx.IsTimerLock(slot.Padlock) ? "--SEP----COL--" + slot.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        if (_selections.OpenInteraction is InteractionType.LockRestriction)
        {
            using (ImRaii.Child("###LockRestriction", new Vector2(width, CkStyle.TwoRowHeight())))
                _pairRestrictionPadlocks.DrawLockCombo("##LockRestriction", width, _selections.RestrictionLayer, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Expander for unlocking.
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, !slot.CanUnlock() || !k.PairPerms.UnlockRestrictions))
            _selections.OpenOrClose(InteractionType.UnlockRestriction);
        CkGui.AttachToolTip(unlockTT);

        if (_selections.OpenInteraction is InteractionType.UnlockRestriction)
        {
            using (ImRaii.Child("###UnlockRestriction", new Vector2(width, ImGui.GetFrameHeight())))
                _pairRestrictionPadlocks.DrawUnlockCombo("##UnlockRestriction", width, _selections.RestrictionLayer, unlockTT, unlockTxt);
            ImGui.Separator();
        }

        // Expander for removing.
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, !slot.CanRemove() || !k.PairPerms.RemoveRestrictions))
            _selections.OpenOrClose(InteractionType.RemoveRestriction);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestriction
        if (_selections.OpenInteraction is InteractionType.RemoveRestriction)
        {
            if (ImGui.Button("Remove Restriction", new Vector2(width, ImGui.GetFrameHeight())))
            {
                var dto = new PushKinksterActiveRestriction(k.UserData, DataUpdateType.Removed) { Layer = _selections.RestrictionLayer };
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
                        _logger.LogDebug($"Removed Restriction Item from {dispName} on layer {_selections.RestrictionLayer}", LoggerType.StickyUI);
                        _selections.CloseInteraction();
                    }
                });
            }
        }
        ImGui.Separator();
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
            _selections.OpenOrClose(InteractionType.ApplyRestraint);
        CkGui.AttachToolTip(applyTT);

        // Interaction Window for ApplyRestraint
        if (_selections.OpenInteraction is InteractionType.ApplyRestraint)
        {
            using (ImRaii.Child("SetApplyChild", new Vector2(width, ImGui.GetFrameHeight())))
                _pairRestraintSets.DrawComboButton("##PairApplyRestraint", width, applyTT);
            ImGui.Separator();
        }

        // Expander for ApplyRestraintLayer
        var disableApplyLayer = !hasItem || itemLayers <= 0 || allLayersSet || (hasPadlock ? !k.PairPerms.ApplyLayersWhileLocked : !k.PairPerms.ApplyLayers); 
        if (CkGui.IconTextButton(FAI.LayerGroup, applyLayerText, width, true, disableApplyLayer))
            _selections.OpenOrClose(InteractionType.ApplyRestraintLayers);
        CkGui.AttachToolTip(applyLayerTT);

        // Interaction Window for ApplyRestraintLayer
        if (_selections.OpenInteraction is InteractionType.ApplyRestraintLayers)
        {
            using (ImRaii.Child("SetApplyLayerChild", new Vector2(width, ImGui.GetFrameHeight())))
                _pairRestraintSets.DrawApplyLayersComboButton(width);
            ImGui.Separator();
        }

        // Expander for LockRestraint
        var disableLockExpand = k.ActiveRestraint.Identifier == Guid.Empty || k.ActiveRestraint.Padlock is not Padlocks.None || !k.PairPerms.LockRestraintSets;
        using (ImRaii.PushColor(ImGuiCol.Text, (k.ActiveRestraint.Padlock is Padlocks.None ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudYellow)))
        {
            if (CkGui.IconTextButton(FAI.Lock, lockTxt, width, true, disableLockExpand))
                _selections.OpenOrClose(InteractionType.LockRestraint);
        }
        CkGui.AttachToolTip(lockTT +
            (PadlockEx.IsTimerLock(k.ActiveRestraint.Padlock) ? "--SEP----COL--" + k.ActiveRestraint.Timer.ToGsRemainingTimeFancy() : ""), color: ImGuiColors.ParsedPink);

        // Interaction Window for LockRestraint
        if (_selections.OpenInteraction is InteractionType.LockRestraint)
        {
            using (ImRaii.Child("SetLockChild", new Vector2(width, CkStyle.TwoRowHeight())))
                _pairRestraintSetPadlocks.DrawLockCombo("##PairLockRestraint", width, 0, lockTxt, lockTT, true);
            ImGui.Separator();
        }

        // Expander for unlocking.
        var disableUnlockExpand = k.ActiveRestraint.Padlock is Padlocks.None || !k.PairPerms.UnlockRestraintSets;
        if (CkGui.IconTextButton(FAI.Unlock, unlockTxt, width, true, disableUnlockExpand))
            _selections.OpenOrClose(InteractionType.UnlockRestraint);
        CkGui.AttachToolTip(unlockTT);

        // Interaction Window for UnlockRestraint
        if (_selections.OpenInteraction is InteractionType.UnlockRestraint)
        {
            using (ImRaii.Child("SetUnlockChild", new Vector2(width, ImGui.GetFrameHeight())))
                _pairRestraintSetPadlocks.DrawUnlockCombo("##PairUnlockRestraint", width, 0, unlockTxt, unlockTT);
            ImGui.Separator();
        }

        // Expander for RemoveRestraintLayer
        var blockLayerRemove = !hasItem || itemLayers <= 0 || k.ActiveRestraint.ActiveLayers is 0 || (hasPadlock ? !k.PairPerms.RemoveLayersWhileLocked : !k.PairPerms.RemoveLayers);
        if (CkGui.IconTextButton(FAI.LayerGroup, removeLayerText, width, true, blockLayerRemove))
            _selections.OpenOrClose(InteractionType.RemoveRestraintLayers);
        CkGui.AttachToolTip(removeLayerTT);

        // Interaction Window for ApplyRestraintLayer
        if (_selections.OpenInteraction is InteractionType.RemoveRestraintLayers)
        {
            using (ImRaii.Child("SetRemoveLayerChild", new Vector2(width, ImGui.GetFrameHeight())))
                _pairRestraintSets.DrawRemoveLayersComboButton(width);
            ImGui.Separator();
        }

        // Expander for removing.
        var disableRemoveExpand = k.ActiveRestraint.Identifier == Guid.Empty || k.ActiveRestraint.Padlock is not Padlocks.None || !k.PairPerms.RemoveRestraintSets;
        if (CkGui.IconTextButton(FAI.TimesCircle, removeTxt, width, true, disableRemoveExpand))
            _selections.OpenOrClose(InteractionType.RemoveRestraint);
        CkGui.AttachToolTip(removeTT);

        // Interaction Window for RemoveRestraint
        if (_selections.OpenInteraction is InteractionType.RemoveRestraint)
        {
            if (ImGui.Button("Remove Restraint", new Vector2(width, ImGui.GetFrameHeight())))
            {
                UiService.SetUITask(async () =>
                {
                    var result = await _hub.UserChangeKinksterActiveRestraint(new(k.UserData, DataUpdateType.Removed)).ConfigureAwait(false);
                    if (result.ErrorCode is not GagSpeakApiEc.Success)
                    {
                        _logger.LogDebug($"Failed to Remove {dispName}'s Restraint Set. ({result})", LoggerType.StickyUI);
                        return;
                    }
                    else
                    {
                        _logger.LogDebug($"Removed {dispName}'s Restraint Set on layer {_selections.RestrictionLayer}", LoggerType.StickyUI);
                        _selections.CloseInteraction();
                    }
                });
            }
        }
        ImGui.Separator();
    }
    #endregion Restraints

    #region Moodles
    private void DrawMoodlesActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Moodles Actions");
        if (!k.IsVisible) 
            CkGui.ColorTextInline("( Not Visible! )", ImGuiColors.DalamudRed);

        var clientIpcValid = MoodleCache.IpcData.Statuses.Count > 0 && k.IsVisible;
        var kinksterIpcValid = k.LastIpcData.Statuses.Count > 0 && k.IsVisible;

        ////////// APPLY MOODLES FROM PAIR's LIST //////////
        var canApplyOther = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) && kinksterIpcValid;
        var applyOtherStatusTxt = canApplyOther ? $"Apply a Status from {dispName}'s list" : $"Cannot apply {dispName}'s Statuses";
        var applyOtherStatusTT = canApplyOther
            ? $"Applies a Moodle Status from {dispName}'s Statuses to them."
            : $"You don't have permission to apply Statuses to {dispName} or they have none!";
        if (CkGui.IconTextButton(FAI.PersonCirclePlus, applyOtherStatusTxt, width, true, !canApplyOther))
            _selections.OpenOrClose(InteractionType.ApplyPairMoodle);
        CkGui.AttachToolTip(applyOtherStatusTT);

        if (_selections.OpenInteraction is InteractionType.ApplyPairMoodle)
        {
            using (ImRaii.Child("ApplyPairMoodles", new Vector2(width, ImGui.GetFrameHeight())))
                _pairMoodleStatuses.DrawApplyStatuses($"##OtherPresets-{k.UserData.UID}", width, $"Applies Selected Status to {dispName}");
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM PAIR's LIST //////////
        var applyOtherPresetTxt = canApplyOther ? $"Apply a Preset from {dispName}'s list" : $"Cannot apply {dispName}'s Presets";
        var applyOtherPresetTT = canApplyOther
            ? $"Applies a Preset from {dispName}'s Presets List to them."
            : $"You don't have permission to apply Presets to {dispName} or they have none!";
        if (CkGui.IconTextButton(FAI.FileCirclePlus, applyOtherPresetTxt, width, true, !canApplyOther))
            _selections.OpenOrClose(InteractionType.ApplyPairMoodlePreset);
        CkGui.AttachToolTip(applyOtherPresetTT);

        if (_selections.OpenInteraction is InteractionType.ApplyPairMoodlePreset)
        {
            using (ImRaii.Child("ApplyPairPresets", new Vector2(width, ImGui.GetFrameHeight())))
                _pairMoodlePresets.DrawApplyPresets($"##OtherPresets-{k.UserData.UID}", width, $"Applies Selected Preset to {dispName}");
            ImGui.Separator();
        }

        ////////// APPLY MOODLES FROM OWN LIST //////////
        var canApplyOwn = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) && clientIpcValid;
        var applyOwnStatusTxt = canApplyOwn ? $"Apply a Status from your list" : "Cannot apply your Statuses";
        var applyOwnStatusTT = canApplyOwn
            ? $"Applies one of your Moodle Statuses to {dispName}."
            : "You don't have permission to apply your own Statuses, or you have none!";
        if (CkGui.IconTextButton(FAI.UserPlus, applyOwnStatusTxt, width, true, !canApplyOwn))
            _selections.OpenOrClose(InteractionType.ApplyOwnMoodle);
        CkGui.AttachToolTip(applyOwnStatusTT);

        if (_selections.OpenInteraction is InteractionType.ApplyOwnMoodle)
        {
            using (ImRaii.Child("ApplyOwnMoodles", new Vector2(width, ImGui.GetFrameHeight())))
                _moodleStatuses.DrawApplyStatuses($"##OwnStatus-{k.UserData.UID}", width, $"Applies Selected Status to {dispName}");
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM OWN LIST //////////
        var applyOwnPresetTxt = canApplyOwn ? $"Apply a Preset from your list" : "Cannot apply your Presets";
        var applyOwnPresetTT = canApplyOwn
            ? $"Applies one of your Moodle Presets to {dispName}."
            : "You don't have permission to apply your Presets, or you have none created!";
        if (CkGui.IconTextButton(FAI.FileCirclePlus, applyOwnPresetTxt, width, true, !canApplyOwn))
            _selections.OpenOrClose(InteractionType.ApplyOwnMoodlePreset);
        CkGui.AttachToolTip(applyOwnPresetTT);

        if (_selections.OpenInteraction is InteractionType.ApplyOwnMoodlePreset)
        {
            using (ImRaii.Child("ApplyOwnPresets", new Vector2(width, ImGui.GetFrameHeight())))
                _moodlePresets.DrawApplyPresets($"##OwnPresets-{k.UserData.UID}", width, $"Applies Selected Preset to {dispName}");
            ImGui.Separator();
        }


        ////////// REMOVE MOODLES //////////
        var canRemove = k.PairPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles) && clientIpcValid;
        var removeStatusTxt = canRemove ? $"Remove a Status from {dispName}" : "Cannot remove Statuses";
        var removeStatusTT = canRemove
            ? $"Removes a Moodle Status from {dispName}'s Status Manager (Active Display)"
            : $"Permission to remove Moodles was not granted by {dispName}, or they have none active!";
        if (CkGui.IconTextButton(FAI.UserMinus, removeStatusTxt, width, true, !canRemove))
            _selections.OpenOrClose(InteractionType.RemoveMoodle);
        CkGui.AttachToolTip(removeStatusTT);

        if (_selections.OpenInteraction is InteractionType.RemoveMoodle)
        {
            using (ImRaii.Child("RemoveMoodles", new Vector2(width, ImGui.GetFrameHeight())))
                _activePairStatusCombo.DrawRemoveStatuses("##ActivePairStatuses" + dispName, width, $"Removes Selected Status to {dispName}");
        }
        ImGui.Separator();
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
            _selections.OpenOrClose(InteractionType.StartPattern);
        CkGui.AttachToolTip(playPatternTT);

        if (_selections.OpenInteraction is InteractionType.StartPattern)
        {
            using (ImRaii.Child("PatternExecute", new Vector2(width, ImGui.GetFrameHeight())))
                _pairPatterns.DrawComboIconButton("##ExecutePattern" + k.UserData.UID, width, "Execute a Pattern");
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
                {
                    _logger.LogError($"Failed to stop {dispName}'s active pattern. ({res.ErrorCode})", LoggerType.StickyUI);
                    return;
                }
                _logger.LogDebug($"Stopped active Pattern running on {dispName}'s toy(s)", LoggerType.StickyUI);
                _selections.CloseInteraction();
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
            _selections.OpenOrClose(InteractionType.ToggleAlarm);
        CkGui.AttachToolTip(toggleAlarmTT);

        if (_selections.OpenInteraction is InteractionType.ToggleAlarm)
        {
            using (ImRaii.Child("AlarmToggle", new Vector2(width, ImGui.GetFrameHeight())))
                _pairAlarmToggles.DrawComboIconButton($"##AlarmToggle-{k.UserData.UID}", width, "Toggle an Alarm");
            ImGui.Separator();
        }

        //////// Toggle Triggers ////////
        var canToggleTriggers = k.PairPerms.ToggleTriggers && !k.PairGlobals.InVibeRoom && k.LightCache.Triggers.Any();
        var toggleTriggerTxt = canToggleTriggers ? $"Toggle one of {dispName}'s Triggers" : $"Cannot Toggle {dispName}'s Triggers";
        var toggleTriggerTT = canToggleTriggers
            ? $"Toggles the state of {dispName}'s Triggers."
            : $"Either {dispName} has not created any Triggers, or you don't have permission to toggle them.";
        if (CkGui.IconTextButton(FAI.LandMineOn, toggleTriggerTxt, width, true, !canToggleTriggers))
            _selections.OpenOrClose(InteractionType.ToggleTrigger);
        CkGui.AttachToolTip(toggleTriggerTT);

        if (_selections.OpenInteraction is InteractionType.ToggleTrigger)
        {
            using (ImRaii.Child("TriggerToggle", new Vector2(width, ImGui.GetFrameHeight())))
                _pairTriggerToggles.DrawComboIconButton("##ToggleTrigger" + k.UserData.UID, width, "Toggle a Trigger");
        }

        ImGui.Separator();
    }
    #endregion Toybox

    #region Misc
    private void DrawMiscActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Misc Actions");
        var hasEffect = k.PairGlobals.HypnoState();
        var hypnoTxt = hasEffect ? $"{dispName} is being Hypnotized" : $"Hypnotize {dispName}";
        var hypnoTT = hasEffect ? $"{dispName} is currently under hypnosis state.--SEP--Cannot apply an effect until {dispName} is not hypnotized."
            : $"Configure and apply a hypnosis effect on {dispName}.";

        if (CkGui.IconTextButton(FAI.Dizzy, hypnoTxt, width, true, hasEffect || !k.PairPerms.HypnoEffectSending || k.PairGlobals.HypnoState()))
        {
            if (_selections.OpenInteraction is not InteractionType.HypnosisEffect)
            {
                // open it.
                _selections.OpenOrClose(InteractionType.HypnosisEffect);
                if (_hypnoEditor.IsEffectNull)
                    _hypnoEditor.SetBlankEffect();
            }
            else
            {
                _selections.CloseInteraction();
            }
        }
        CkGui.AttachToolTip(hypnoTT);

        if (_selections.OpenInteraction is InteractionType.HypnosisEffect)
        {
            // draw out the editor for setting a time up.
            var buttonW = CkGui.IconTextButtonSize(FAI.Upload, "Send Effect");
            var txtWidth = width - buttonW - ImGui.GetStyle().ItemInnerSpacing.X;
            CkGui.IconInputText($"##HypnoTime-{k.UserData.UID}", txtWidth, FAI.Clock, "Ex: 20m5s", ref _selections.HypnoTimer, 12);
            
            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Upload, "Send Effect", buttonW, disabled: _selections.HypnoTimer.IsNullOrEmpty()))
                UiService.SetUITask(TrySendHypnosisAction(k, dispName));

            // Draw the editor below this and the preview display.
            _hypnoEditor.DrawCompactEditorTabs(width);
            var size = _hypnoEditor.DisplayPreviewWidthConstrained(width, Constants.DefaultHypnoPath);
            ImGui.Dummy(size);
        }

        ImGui.Separator();
    }
    #endregion Misc

    #region Hardcore
    private void DrawHardcoreActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Hardcore Actions");
        var kg = k.PairGlobals;

        //if (!k.PairPerms.InHardcore)
        //{
        //    ImGui.Separator();
        //    return;
        //}
        // Required Close-Ranged Hardcore commands must be in range
        var inRange = PlayerData.Available && k.VisiblePairGameObject is { } vo && PlayerData.DistanceTo(vo) < 3;
        var pairlockTag = k.PairPerms.PairLockedStates ? Constants.DevotedString : string.Empty;

        (FAI Icon, string Text) hcLabel = kg.HcFollowState() ? (FAI.StopCircle, $"Have {dispName} stop following you.") : (FAI.PersonWalkingArrowRight, $"Make {dispName} follow you.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !inRange || !k.PairPerms.AllowLockedFollowing || !k.IsVisible || !kg.CanChangeHcFollow(MainHub.UID), "##HcLockedFollowing"))
        {
            var newStr = kg.HcFollowState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
            UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.LockedFollowing), newStr));
        }

        // ForceEmote is a special child...
        DrawLockedEmoteSection(k, width, dispName, pairlockTag);


        hcLabel = kg.HcConfinedState() ? (FAI.StopCircle, $"Release {dispName}.") : (FAI.HouseLock, $"Lock {dispName} away.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowIndoorConfinement || !kg.CanChangeHcConfined(MainHub.UID), "##HcForcedStay"))
        {
            var newStr = kg.HcConfinedState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
            UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.IndoorConfinement), newStr));
        }

        // Hiding chat message history window, but still allowing typing.
        hcLabel = kg.HcChatVisState() ? (FAI.StopCircle, $"Make {dispName}'s Chat Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Window.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowHidingChatBoxes || !kg.CanChangeHcChatVis(MainHub.UID), "##HcForcedChatVis"))
        {
            var newStr = kg.HcChatVisState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
            UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.ChatBoxesHidden), newStr));
        }

        // Hiding Chat input, but still allowing typing.
        hcLabel = kg.HcChatInputVisState() ? (FAI.StopCircle, $"Make {dispName}'s Chat Input Visible.") : (FAI.CommentSlash, $"Hide {dispName}'s Chat Input.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowHidingChatInput || !kg.CanChangeHcChatInputVis(MainHub.UID), "##HcForcedChatInputVis"))
        {
            var newStr = kg.HcChatInputVisState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
            UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.ChatInputHidden), newStr));
        }

        // Preventing Chat Input at all.
        hcLabel = kg.HcBlockChatInputState() ? (FAI.StopCircle, $"Reallow {dispName}'s Chat Input.") : (FAI.CommentDots, $"Block {dispName}'s Chat Input.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowChatInputBlocking || !kg.CanChangeHcBlockChatInput(MainHub.UID), "##HcForcedChatBlocking"))
        {
            var newStr = kg.HcBlockChatInputState() ? string.Empty : $"{MainHub.UID}{pairlockTag}";
            UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, kg, nameof(GlobalPerms.ChatInputBlocked), newStr));
        }

        ImGui.Separator();
    }

    private void DrawLockedEmoteSection(Kinkster k, float width, string dispName, string pairlockTag)
    {
        // What to display if we cant do LockedEmote
        if (!k.PairGlobals.LockedEmoteState.NullOrEmpty())
        {
            if (CkGui.IconTextButton(FAI.StopCircle, $"Let {dispName} move again.", width, true, id: "##HcForcedStay"))
            {
                UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, k.PairGlobals, nameof(GlobalPerms.LockedEmoteState), string.Empty));
                _selections.CloseInteraction();
            }
            CkGui.AttachToolTip($"Release {dispName} from forced emote state.");
            return;
        }

        // If we can do LockedEmote, display options.
        (FAI Icon, string Text) hcLabel = k.PairPerms.AllowLockedEmoting ? (FAI.PersonArrowDownToLine, $"Force {dispName} into an Emote State.") : (FAI.Chair, $"Force {dispName} to Sit.");
        if (CkGui.IconTextButton(hcLabel.Icon, hcLabel.Text, width, true, !k.PairPerms.AllowLockedSitting && k.PairGlobals.CanChangeHcEmote(MainHub.UID), "##HcLockedEmote"))
            _selections.OpenOrClose(InteractionType.LockedEmoteState);
        CkGui.AttachToolTip($"Force {dispName} to perform any {(k.PairPerms.AllowLockedEmoting ? "looped emote state" : "sitting or cycle pose state")}.");

        if (_selections.OpenInteraction is not InteractionType.LockedEmoteState)
            return;

        using (ImRaii.Child("LockedEmoteState", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y)))
        {
            var comboWidth = width - CkGui.IconTextButtonSize(FAI.PersonRays, "Force State") - ImGui.GetStyle().ItemInnerSpacing.X;
            // Handle Emote Stuff.
            var emoteList = k.PairPerms.AllowLockedEmoting ? EmoteExtensions.LoopedEmotes() : EmoteExtensions.SittingEmotes();
            _emoteCombo.Draw("##LockedEmoteCombo", _selections.EmoteId, comboWidth, 1.3f);
            // Handle Cycle Poses
            var canSetCyclePose = EmoteService.IsAnyPoseWithCyclePose((ushort)_emoteCombo.Current.RowId);
            var maxCycles = canSetCyclePose ? EmoteService.CyclePoseCount((ushort)_emoteCombo.Current.RowId) : 0;
            if (!canSetCyclePose) _selections.CyclePose = 0;
            using (ImRaii.Disabled(!canSetCyclePose))
            {
                ImGui.SetNextItemWidth(comboWidth);
                ImGui.SliderInt("##EnforceCyclePose", ref _selections.CyclePose, 0, maxCycles);
                CkGui.AttachToolTip("Select the cycle pose for the forced emote.");
            }
            // the application button thing.
            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.PersonRays, "Force State"))
            {
                var newStr = $"{MainHub.UID}|{_emoteCombo.Current.RowId}|{_selections.CyclePose}{pairlockTag}";
                _logger.LogDebug($"Sending EmoteState update for emote: {_emoteCombo.Current.Name}");
                UiService.SetUITask(PermissionHelper.ChangeOtherGlobal(_hub, k.UserData, k.PairGlobals, nameof(GlobalPerms.LockedEmoteState), newStr));
                _selections.CloseInteraction();
            }
            CkGui.AttachToolTip("Apply the selected forced emote state.");
        }
        ImGui.Separator();
    }

    #endregion Hardcore

    #region Shock Collar
    private void DrawShockActions(Kinkster k, float width, string dispName)
    {
        ImGui.TextUnformatted("Shock Collar Actions");

        // the permissions to reference.
        var preferPairCode = k.PairPerms.HasValidShareCode();
        var maxIntensity = preferPairCode ? k.PairPerms.MaxIntensity : k.PairGlobals.MaxIntensity;
        var maxDuration = preferPairCode ? k.PairPerms.GetTimespanFromDuration() : k.PairGlobals.GetTimespanFromDuration();
        var piShockShareCodePref = preferPairCode ? k.PairPerms.PiShockShareCode : k.PairGlobals.GlobalShockShareCode;

        // Shock Expander
        var AllowShocks = preferPairCode ? k.PairPerms.AllowShocks : k.PairGlobals.AllowShocks;
        if (CkGui.IconTextButton(FAI.BoltLightning, $"Shock {dispName}'s Shock Collar", width, true, !AllowShocks))
            _selections.OpenOrClose(InteractionType.ShockAction);
        CkGui.AttachToolTip($"Perform a Shock action to {dispName}'s Shock Collar.");

        if (_selections.OpenInteraction is InteractionType.ShockAction)
        {
            using (ImRaii.Child("ShockCollarActionChild", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                var sliderWidth = width - CkGui.IconTextButtonSize(FAI.BoltLightning, "Shock") - ImGui.GetStyle().ItemInnerSpacing.X;

                ImGui.SetNextItemWidth(sliderWidth);
                ImGui.SliderInt($"##SC-Intensity-{k.UserData.UID}", ref _selections.Intensity, 0, maxIntensity, " % d%%", ImGuiSliderFlags.None);

                ImGui.SetNextItemWidth(sliderWidth);
                ImGui.SliderFloat($"##SC-Duration-{k.UserData.UID}", ref _selections.Duration, 0.0f, (float)maxDuration.TotalMilliseconds / 1000f, "%.1fs", ImGuiSliderFlags.None);

                ImUtf8.SameLineInner();
                if (CkGui.IconTextButton(FAI.BoltLightning, "Send Shock"))
                {
                    int newMaxDuration;
                    if (_selections.Duration % 1 == 0 && _selections.Duration >= 1 && _selections.Duration <= 15) { newMaxDuration = (int)_selections.Duration; }
                    else { newMaxDuration = (int)(_selections.Duration * 1000); }
                    _logger.LogDebug("Sending Shock to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                    UiService.SetUITask(async () =>
                    {
                        var res = await _hub.UserShockKinkster(new(k.UserData, 0, _selections.Intensity, newMaxDuration));
                        if (res.ErrorCode is not GagSpeakApiEc.Success)
                        {
                            _logger.LogDebug($"Failed to send Shock to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                            return;
                        }
                        _logger.LogDebug($"Sent Shock to {dispName}'s Shock Collar with duration: {newMaxDuration} (milliseconds)", LoggerType.StickyUI);
                        GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockSent);
                        _selections.CloseInteraction();
                    });
                }
            }
            ImGui.Separator();
        }


        // Vibrate Expander
        var AllowVibrations = preferPairCode ? k.PairPerms.AllowVibrations : k.PairGlobals.AllowVibrations;
        if (CkGui.IconTextButton(FAI.WaveSquare, $"Vibrate {dispName}'s Shock Collar", width, true, false))
            _selections.OpenOrClose(InteractionType.VibrateAction);
        CkGui.AttachToolTip($"Perform a Vibrate action to {dispName}'s Shock Collar.");

        if (_selections.OpenInteraction is InteractionType.VibrateAction)
        {
            using (ImRaii.Child("VibrateCollarActionChild", new Vector2(width, ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y), false))
            {
                var sliderWidth = width - CkGui.IconTextButtonSize(FAI.HeartCircleBolt, "Vibrate") - ImGui.GetStyle().ItemInnerSpacing.X;

                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                ImGui.SetNextItemWidth(sliderWidth);
                ImGui.SliderInt("##IntensitySliderRef" + k.UserData.UID, ref _selections.VibrateIntensity, 0, 100, "%d%%", ImGuiSliderFlags.None);

                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + k.UserData.UID, ref _selections.VibeDuration, 0.0f, ((float)maxDuration.TotalMilliseconds / 1000f), "%.1fs", ImGuiSliderFlags.None);

                ImUtf8.SameLineInner();
                if (CkGui.IconTextButton(FAI.HeartCircleBolt, "Send Vibration"))
                {
                    var newMaxDuration = (_selections.VibeDuration % 1 == 0 && _selections.VibeDuration >= 1 && _selections.VibeDuration <= 15)
                        ? (int)_selections.VibeDuration : (int)(_selections.VibeDuration * 1000);

                    _logger.LogDebug("Sending Vibration to Shock Collar with duration: " + newMaxDuration + "(milliseconds)");
                    UiService.SetUITask(async () =>
                    {
                        var res = await _hub.UserShockKinkster(new(k.UserData, 1, _selections.VibrateIntensity, newMaxDuration));
                        if (res.ErrorCode is not GagSpeakApiEc.Success)
                        {
                            _logger.LogDebug($"Failed to send Vibration to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                            return;
                        }
                        _logger.LogDebug($"Sent Vibration to {dispName}'s Shock Collar with duration: {newMaxDuration} (milliseconds)", LoggerType.StickyUI);
                        _selections.CloseInteraction();
                    });
                }
            }
            ImGui.Separator();
        }


        // Beep Expander
        var AllowBeeps = preferPairCode ? k.PairPerms.AllowBeeps : k.PairGlobals.AllowBeeps;
        if (CkGui.IconTextButton(FAI.LandMineOn, $"Beep {dispName}'s Shock Collar", width, true, !AllowBeeps))
            _selections.OpenOrClose(InteractionType.BeepAction);
        CkGui.AttachToolTip($"Beep {dispName}'s Shock Collar");

        if (_selections.OpenInteraction is InteractionType.BeepAction)
        {
            using (ImRaii.Child("BeepCollar", new Vector2(width, ImGui.GetFrameHeight())))
            {
                var sliderWidth = width - CkGui.IconTextButtonSize(FAI.LandMineOn, "Beep") - ImGui.GetStyle().ItemInnerSpacing.X;
                // draw a slider float that references the duration, going from 0.1f to 15f by a scaler of 0.1f that displays X.Xs
                var max = ((float)maxDuration.TotalMilliseconds / 1000f);
                ImGui.SetNextItemWidth(width);
                ImGui.SliderFloat("##DurationSliderRef" + k.UserData.UID, ref _selections.VibeDuration, 0.1f, max, "%.1fs", ImGuiSliderFlags.None);

                ImUtf8.SameLineInner();
                if (CkGui.IconTextButton(FAI.LandMineOn, "Send Beep"))
                {
                    _logger.LogDebug($"Sending Beep foir {_selections.VibeDuration}ms! (note that values between 1 and 15 are full seconds)");
                    UiService.SetUITask(async () =>
                    {
                        var newMaxDuration = (_selections.VibeDuration % 1 == 0 && _selections.VibeDuration >= 1 && _selections.VibeDuration <= 15)
                            ? (int)_selections.VibeDuration : (int)(_selections.VibeDuration * 1000);
                        var res = await _hub.UserShockKinkster(new ShockCollarAction(k.UserData, 2, _selections.Intensity, newMaxDuration));
                        if (res.ErrorCode is not GagSpeakApiEc.Success)
                        {
                            _logger.LogDebug($"Failed to send Beep to {dispName}'s Shock Collar. ({res})", LoggerType.StickyUI);
                            return;
                        }
                        _logger.LogDebug($"Sent Beep to {dispName}'s Shock Collar with duration: {newMaxDuration} (milliseconds)", LoggerType.StickyUI);
                        _selections.CloseInteraction();
                    });
                }
            }
            ImGui.Separator();
        }
    }
    #endregion Shock Collar

    #region Misc Action Helpers
    private async Task TrySendHypnosisAction(Kinkster k, string dispName)
    {
        if (!_hypnoEditor.TryGetEffect(out var effect))
        {
            _logger.LogTrace("Effect was null or time parsing failed!");
            return;
        }
        if (!PadlockEx.TryParseTimeSpan(_selections.HypnoTimer, out var newTime))
        {
            _selections.HypnoTimer = string.Empty;
            _logger.LogTrace("Effect was null or time parsing failed!");
            return;
        }
        // compose the DTO to send.
        var dto = new HypnoticAction(k.UserData, (int)newTime.TotalSeconds, effect);
        if (await _hub.UserHypnotizeKinkster(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            switch (res.ErrorCode)
            {
                case GagSpeakApiEc.BadUpdateKind: Svc.Toasts.ShowError("Invalid Update Kind. Please try again."); break;
                case GagSpeakApiEc.InvalidTime: Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 1h2m7s)."); break;
                case GagSpeakApiEc.LackingPermissions: Svc.Toasts.ShowError("You do not have permission to perform this action."); break;
                default: Svc.Logger.Debug($"Failed to send Hypnosis Effect to {dispName}: {res.ErrorCode}."); break;
            }
        }
        else
        {
            _logger.LogDebug($"Sent Hypnosis Effect to {dispName} with duration: {newTime} (seconds)", LoggerType.StickyUI);
        }
    }
    #endregion Misc Action Helpers


}
